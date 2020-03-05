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
        private List<ApciTypes> responseTypes = new List<ApciTypes>() { ApciTypes.DeviceDescriptorResponse, ApciTypes.GroupValueResponse, ApciTypes.IndividualAddressSerialNumberResponse, ApciTypes.MemoryResponse };

        private UnicastAddress _address;
        private Connection _conn;
        private Dictionary<int, TunnelResponse> responses = new Dictionary<int, TunnelResponse>();

        private int _currentSeqNum = 0;
        private bool _connected = false;

        public BusDevice(string address, Connection conn)
        {
            _address = UnicastAddress.FromString(address);
            _conn = conn;
            _conn.OnTunnelRequest += OnTunnelRequest;
        }

        private void OnTunnelRequest(TunnelResponse response)
        {
            if (responseTypes.Contains(response.APCI))
            {
                responses.Add(response.SequenceNumber, response);

                Debug.WriteLine(response.SequenceNumber + ": " + response.APCI + " - " + response.Data.Length);

                TunnelRequest builder = new TunnelRequest();
                builder.Build(UnicastAddress.FromString("0.0.0"), _address, Parser.ApciTypes.Ack, response.SequenceCounter);
                _conn.Send(builder);
            }
            //TODO move ack to connection class!

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
            XElement prop = mask.Descendants(XName.Get("Resource", master.Root.Name.NamespaceName)).Single(mv => mv.Attribute("Name").Value == resourceId);

            XElement loc = prop.Element(XName.Get("Location", master.Root.Name.NamespaceName));
            int length = int.Parse(prop.Element(XName.Get("ResourceType", master.Root.Name.NamespaceName)).Attribute("Length").Value);
            string start = loc.Attribute("StartAddress").Value;

            switch (loc.Attribute("AddressSpace").Value)
            {
                case "SystemProperty":
                    string obj = loc.Attribute("InterfaceObjectRef").Value;
                    string pid = loc.Attribute("PropertyID").Value;
                    return await PropertyRead<T>(Convert.ToByte(obj), Convert.ToByte(pid), length, int.Parse(start));

                case "StandardMemory":

                    break;

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
        public async Task<T> PropertyRead<T>(byte objIdx, byte propId, int length, int start = 0)
        {
            if (!_connected) throw new Exception("Nicht mit Gerät verbunden.");

            TunnelRequest builder = new TunnelRequest();

            int x1 = length << 12;
            start = start & 0xFFF;
            int x2 = x1 | start;

            byte[] data_temp = BitConverter.GetBytes(Convert.ToInt16(x2));
            byte[] data = { objIdx, propId, data_temp[1], data_temp[0] };

            builder.Build(UnicastAddress.FromString("0.0.0"), _address, Parser.ApciTypes.PropertyValueRead, _currentSeqNum, data);
            var seq = _currentSeqNum;
            _currentSeqNum++;
            _conn.Send(builder);
            TunnelResponse resp = await WaitForData(seq);

            return (T)Convert.ChangeType(resp.Data, typeof(T));
        }



        /// <summary>
        /// Schreibt die Daten in den Speicher des Gerätes.
        /// </summary>
        /// <param name="address">Start Adresse</param>
        /// <param name="databytes">Daten zum Schreiben</param>
        public async void MemoryWrite(int address, byte[] databytes)
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
                _currentSeqNum++;
                builder.Build(UnicastAddress.FromString("0.0.0"), _address, Knx.Parser.ApciTypes.MemoryWrite, _currentSeqNum, data.ToArray());
                _conn.Send(builder);
                await Task.Delay(100);
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
            byte[] addr = BitConverter.GetBytes(Convert.ToInt16(address));
            Array.Reverse(addr);
            data.AddRange(addr);

            TunnelRequest builder = new TunnelRequest();
            builder.Build(UnicastAddress.FromString("0.0.0"), _address, Knx.Parser.ApciTypes.MemoryRead, _currentSeqNum, data.ToArray());
            var seq = _currentSeqNum;
            _currentSeqNum++;
            _conn.Send(builder);
            TunnelResponse resp = await WaitForData(seq);

            return (T)Convert.ChangeType(resp.Data, typeof(T));
        }





        public async Task<string> DeviceDescriptorRead()
        {
            TunnelRequest builder = new TunnelRequest();
            builder.Build(UnicastAddress.FromString("0.0.0"), _address, Knx.Parser.ApciTypes.DeviceDescriptorRead,_currentSeqNum);
            var seq = _currentSeqNum;
            _currentSeqNum++;
            _conn.Send(builder);
            Debug.WriteLine("waiting for: " + seq);
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
