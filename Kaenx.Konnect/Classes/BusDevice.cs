using METS.Knx.Addresses;
using METS.Knx.Builders;
using METS.Knx.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;

namespace METS.Knx.Classes
{
    public class BusDevice
    {
        private UnicastAddress _address;
        private Connection _conn;
        private Dictionary<int, TunnelResponse> responses = new Dictionary<int, TunnelResponse>();
        private List<int> acks = new List<int>();

        private int _currentSeqNum = 0;
        private bool _connected = false;
        private int lastReceivedNumber;

        public BusDevice(string address, Connection conn)
        {
            _address = UnicastAddress.FromString(address);
            _conn = conn;
            _conn.OnTunnelResponse += OnTunnelResponse;
            _conn.OnTunnelRequest += _conn_OnTunnelRequest;
        }

        private void _conn_OnTunnelRequest(TunnelResponse response)
        {
            acks.Add(response.SequenceNumber);
            Debug.WriteLine(response.SequenceNumber + ": " + response.APCI);
        }

        private void OnTunnelResponse(TunnelResponse response)
        {
            responses.Add(response.SequenceNumber, response);

            Debug.WriteLine(response.SequenceNumber + ": " + response.APCI + " - " + response.Data.Length);
        }

        public BusDevice(UnicastAddress address, Connection conn)
        {
            _address = address;
            _conn = conn;
        }


        /// <summary>
        /// Wartet auf antwort
        /// </summary>
        /// <param name="seq">Sequenznummer</param>
        /// <returns>Daten als Byte Array</returns>
        private async Task<TunnelResponse> WaitForData(int seq)
        {
            while(!responses.ContainsKey(seq))
                await Task.Delay(10); // TODO maybe erhöhen

            var resp = responses[seq];
            responses.Remove(seq);
            return resp;
        }

        private async Task WaitForAck(int seq)
        {
            while (!acks.Contains(seq))
                await Task.Delay(10); // TODO maybe erhöhen
            acks.Remove(seq);
        }



        /// <summary>
        /// Stellt eine Verbindung mit dem Gerät her.
        /// Wird für viele weitere Methoden benötigt.
        /// </summary>
        public void Connect()
        {
            TunnelRequest builder = new TunnelRequest();
            builder.Build(UnicastAddress.FromString("0.0.0"), _address, Parser.ApciTypes.Connect, 255);
            _conn.Send(builder);
            _connected = true;
        }

        /// <summary>
        /// Startet das Gerät neu.
        /// </summary>
        public void Restart()
        {
            if (!_connected) throw new Exception("Nicht mit Gerät verbunden.");

            TunnelRequest builder = new TunnelRequest();
            builder.Build(UnicastAddress.FromString("0.0.0"), _address, Parser.ApciTypes.Restart, _currentSeqNum);
            _currentSeqNum++;
            _conn.Send(builder);
        }


        public async Task<byte[]> PropertyRead(string maskId, string resourceId)
        {
            return await PropertyRead<byte[]>(maskId, resourceId);
        }

        /// <summary>
        /// Liest Property vom Gerät aus.
        /// </summary>
        /// <param name="maskId">Id der Maske (z.B. MV-0701)</param>
        /// <param name="resourceId">Name der Ressource (z.B. ApplicationId)</param>
        /// <returns></returns>
        public async Task<T> PropertyRead<T>(string maskId, string resourceId)
        {
            XDocument master = await GetKnxMaster();
            XElement mask = master.Descendants(XName.Get("MaskVersion", master.Root.Name.NamespaceName)).Single(mv => mv.Attribute("Id").Value == maskId);
            XElement prop = null;
            try
            {
                prop = mask.Descendants(XName.Get("Resource", master.Root.Name.NamespaceName)).Single(mv => mv.Attribute("Name").Value == resourceId);
            } catch
            {
                throw new Exception("Device does not support this Property");
            }

            XElement loc = prop.Element(XName.Get("Location", master.Root.Name.NamespaceName));
            int length = int.Parse(prop.Element(XName.Get("ResourceType", master.Root.Name.NamespaceName)).Attribute("Length").Value);
            string start = loc.Attribute("StartAddress")?.Value;

            switch (loc.Attribute("AddressSpace").Value)
            {
                case "SystemProperty":
                    string obj = loc.Attribute("InterfaceObjectRef").Value;
                    string pid = loc.Attribute("PropertyID").Value;
                    return await PropertyRead<T>(Convert.ToByte(obj), Convert.ToByte(pid), length, int.Parse(start));

                case "StandardMemory":
                    return await MemoryRead<T>(int.Parse(start), length);

                case "Pointer":
                    string newProp = loc.Attribute("PtrResource").Value;
                    return await PropertyRead<T>(maskId, newProp);
            }

            //TODO property aus knx_master auslesen

            return (T)Convert.ChangeType(null, typeof(T));
        }

        /// <summary>
        /// Liest Property vom Gerät aus.
        /// </summary>
        /// <param name="objIdx">Objekt Index</param>
        /// <param name="propId">Property Id</param>
        /// <param name="length">Anzahl der zu lesenden Bytes</param>
        /// <param name="start">Startindex</param>
        /// <returns>Daten als Byte Array</returns>
        public async Task<byte[]> PropertyRead(byte objIdx, byte propId, int length, int start = 0)
        {
            return await PropertyRead<byte[]>(objIdx, propId, length, start);
        }

        /// <summary>
        /// Liest Property vom Gerät aus.
        /// </summary>
        /// <param name="objIdx">Objekt Index</param>
        /// <param name="propId">Property Id</param>
        /// <param name="length">Anzahl der zu lesenden Bytes</param>
        /// <param name="start">Startindex</param>
        /// <returns>Daten</returns>
        public async Task<T> PropertyRead<T>(byte objIdx, byte propId, int length, int start = 1)
        {
            if (!_connected) throw new Exception("Nicht mit Gerät verbunden.");

            TunnelRequest builder = new TunnelRequest();


            length = 1;
            start = 1;

            int x1 = length << 12;
            start &= 0xFFF;
            int x2 = x1 | start;

            byte[] data_temp = BitConverter.GetBytes(Convert.ToInt16(x2));
            byte[] data = { objIdx, propId, data_temp[1], data_temp[0] };

            builder.Build(UnicastAddress.FromString("0.0.0"), _address, Parser.ApciTypes.PropertyValueRead, _currentSeqNum, data);
            var seq = _currentSeqNum;
            _currentSeqNum++;
            _conn.Send(builder);
            TunnelResponse resp = await WaitForData(seq);


            switch(Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.String:
                    string datas = BitConverter.ToString(resp.Data.Skip(4).ToArray()).Replace("-", "");
                    return (T)Convert.ChangeType(datas, typeof(T));

                case TypeCode.Int32:
                    byte[] datai = resp.Data.Skip(2).ToArray();
                    byte[] xint = new byte[4];

                    for (int i = 0; i < datai.Length; i++)
                    {
                            xint[i] = datai[i];
                    }
                    return (T)Convert.ChangeType(BitConverter.ToUInt32(xint, 0), typeof(T));

                default:
                    try
                    {
                        return (T)Convert.ChangeType(resp.Data.Skip(4).ToArray(), typeof(T));
                    } catch(Exception e)
                    {
                        throw new Exception("Data kann nicht in angegebenen Type konvertiert werden. " + typeof(T).ToString(), e);
                    }
            }
        }



        /// <summary>
        /// Schreibt die Daten in den Speicher des Gerätes.
        /// </summary>
        /// <param name="address">Start Adresse</param>
        /// <param name="databytes">Daten zum Schreiben</param>
        public void MemoryWrite(int address, byte[] databytes)
        {
            List<byte> datalist = databytes.ToList();

            while (datalist.Count != 0)
            {
                List<byte> data_temp = new List<byte>();
                if (datalist.Count >= 14)
                {
                    data_temp.AddRange(datalist.Take(14));
                    datalist.RemoveRange(0, 14);
                } else
                {
                    data_temp.AddRange(datalist.Take(datalist.Count));
                    datalist.RemoveRange(0, datalist.Count);
                }

                TunnelRequest builder = new TunnelRequest();
                List<byte> data = new List<byte> { Convert.ToByte(data_temp.Count) };
                byte[] addr = BitConverter.GetBytes(Convert.ToInt16(address));
                Array.Reverse(addr);
                data.AddRange(addr);
                data.AddRange(data_temp);
                builder.Build(UnicastAddress.FromString("0.0.0"), _address, Knx.Parser.ApciTypes.MemoryWrite, _currentSeqNum, data.ToArray());
                _currentSeqNum++;
                _conn.Send(builder);
            }

        }

        public async Task MemoryWriteSync(int address, byte[] databytes)
        {
            List<byte> datalist = databytes.ToList();

            while (datalist.Count != 0)
            {
                List<byte> data_temp = new List<byte>();
                if (datalist.Count >= 14)
                {
                    data_temp.AddRange(datalist.Take(14));
                    datalist.RemoveRange(0, 14);
                }
                else
                {
                    data_temp.AddRange(datalist.Take(datalist.Count));
                    datalist.RemoveRange(0, datalist.Count);
                }

                TunnelRequest builder = new TunnelRequest();
                List<byte> data = new List<byte> { Convert.ToByte(data_temp.Count) };
                byte[] addr = BitConverter.GetBytes(Convert.ToInt16(address));
                Array.Reverse(addr);
                data.AddRange(addr);
                data.AddRange(data_temp);

                var seq = _currentSeqNum;
                _currentSeqNum++;
                builder.Build(UnicastAddress.FromString("0.0.0"), _address, Knx.Parser.ApciTypes.MemoryWrite, seq, data.ToArray());
                _conn.Send(builder);
                Debug.WriteLine("Warten auf: " + seq);
                await WaitForAck(seq);
            }

        }




        /// <summary>
        /// Liest den Speicher des Gerätes aus.
        /// </summary>
        /// <param name="address">Start Adresse</param>
        /// <param name="length">Anzahl der Bytes die gelesen werden sollen</param>
        /// <returns>Daten als Byte Array</returns>
        public async Task<byte[]> MemoryRead(int address, int length)
        {
            return await MemoryRead<byte[]>(address, length);
        }

        /// <summary>
        /// Liest den Speicher des Gerätes aus.
        /// </summary>
        /// <param name="address">Start Adresse</param>
        /// <param name="length">Anzahl der Bytes die gelesen werden sollen</param>
        /// <returns>Daten</returns>
        public async Task<T> MemoryRead<T>(int address, int length)
        {
            List<byte> data = new List<byte> { Convert.ToByte(length) };
            byte[] addr = BitConverter.GetBytes(address);
            data.Add(addr[1]);
            data.Add(addr[0]);

            TunnelRequest builder = new TunnelRequest();
            var seq = _currentSeqNum;
            _currentSeqNum++;
            builder.Build(UnicastAddress.FromString("0.0.0"), _address, Knx.Parser.ApciTypes.MemoryRead, seq, data.ToArray());
            _conn.Send(builder);
            Debug.WriteLine("Warten auf: " + seq);
            TunnelResponse resp = await WaitForData(seq);

            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.String:
                    string datas = BitConverter.ToString(resp.Data.Skip(2).ToArray()).Replace("-", "");
                    return (T)Convert.ChangeType(datas, typeof(T));

                case TypeCode.Int32:
                    byte[] datai = resp.Data.Skip(2).ToArray();
                    byte[] xint = new byte[4];

                    for(int i = 0; i < datai.Length; i++)
                    {
                        xint[i] = datai[i];
                    }
                    return (T)Convert.ChangeType(BitConverter.ToUInt32(xint, 0), typeof(T));

                default:
                    try
                    {
                        return (T)Convert.ChangeType(resp.Data.Skip(2).ToArray(), typeof(T));
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Data kann nicht in angegebenen Type konvertiert werden. " + typeof(T).ToString(), e);
                    }
            }
        }





        public async Task<string> DeviceDescriptorRead()
        {
            TunnelRequest builder = new TunnelRequest();
            var seq = _currentSeqNum;
            _currentSeqNum++;
            builder.Build(UnicastAddress.FromString("0.0.0"), _address, Knx.Parser.ApciTypes.DeviceDescriptorRead, seq);
            _conn.Send(builder);
            Debug.WriteLine("Warten auf: " + seq);
            TunnelResponse resp = await WaitForData(seq); 
            return BitConverter.ToString(resp.Data).Replace("-", "");
        }


        public void Disconnect()
        {
            _connected = false;
        }



        private async Task<XDocument> GetKnxMaster()
        {
            StorageFile masterFile;

            try
            {
                masterFile = await ApplicationData.Current.LocalFolder.GetFileAsync("knx_master.xml");
            }
            catch
            {
                StorageFile defaultFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Data/knx_master.xml"));
                masterFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("knx_master.xml");
                await FileIO.WriteTextAsync(masterFile, await FileIO.ReadTextAsync(defaultFile));
            }

            XDocument masterXml = XDocument.Load(await masterFile.OpenStreamForReadAsync());
            return masterXml;
        }
    }
}
