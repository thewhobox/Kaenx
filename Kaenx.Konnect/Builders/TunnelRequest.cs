using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Parser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Konnect.Builders
{
    public class TunnelRequest : IRequestBuilder
    {
        private List<byte> bytes = new List<byte>();

        private BitArray ctrlByte = new BitArray(new byte[] { 0xb0 });
        private BitArray drlByte = new BitArray(new byte[] { 0xe0 });

        public void Build(IKnxAddress sourceAddress, IKnxAddress destinationAddress, ApciTypes apciType, int sCounter = 255, byte[] data = null)
        {
            Build(sourceAddress, destinationAddress, apciType, 0, sCounter, data);
        }


            //TODO sequenz obsolet machen!!
         public void Build(IKnxAddress sourceAddress, IKnxAddress destinationAddress, ApciTypes apciType, byte sequence, int sCounter, byte[] data = null)
        {
            byte[] header = { 0x06, 0x10, 0x04, 0x20 };
            bytes.AddRange(header);

            // Connection HPAI
            bytes.Add(0x04); // Body Structure Length
            bytes.Add(0x1f); // Channel Id -> Placeholder
            bytes.Add(0x1f); // Sequenz Counter -> Placeholder
            bytes.Add(0x00); // Reserved

            bytes.Add(0x11); // cEMI Message Code
            bytes.Add(0x00); // Additional Info Length

            bytes.Add(bitToByte(ctrlByte)); // Control Byte



            drlByte.Set(7, destinationAddress is MulticastAddress);

            bytes.Add(bitToByte(drlByte)); // DRL Byte

            bytes.AddRange(sourceAddress.GetBytes()); // Source Address
            bytes.AddRange(destinationAddress.GetBytes()); // Destination Address

            byte lengthData = 0x01;

            if(data != null)
            {
                lengthData = BitConverter.GetBytes((ushort)(data.Count() + 1))[0];
                if (apciType == ApciTypes.MemoryRead || apciType == ApciTypes.MemoryWrite || apciType == ApciTypes.GroupValueWrite || apciType == ApciTypes.ADCRead)
                    lengthData--;
            }
            else
            {
                switch (apciType)
                {
                    case ApciTypes.ADCRead:
                    case ApciTypes.ADCResponse:
                    case ApciTypes.GroupValueResponse:
                    case ApciTypes.GroupValueWrite:
                    case ApciTypes.Ack:
                    case ApciTypes.Connect:
                        lengthData = 0x0;
                        break;
                }
            }

            bytes.Add(lengthData);

            List<ApciTypes> datatypes = new List<ApciTypes>() { ApciTypes.Restart, ApciTypes.IndividualAddressRead, ApciTypes.DeviceDescriptorRead, ApciTypes.GroupValueResponse, ApciTypes.GroupValueWrite, ApciTypes.ADCRead, ApciTypes.ADCResponse, ApciTypes.MemoryRead, ApciTypes.MemoryResponse, ApciTypes.MemoryWrite };
            
            int _apci = (int)apciType;
            if (apciType == ApciTypes.Ack)
                _apci--;
            _apci = _apci | ((sCounter == 255 ? 0 : sCounter) << 10);
            _apci = _apci | ((sCounter == 255 ? 0 : 1) << 14);
            _apci = _apci | (((data == null && !datatypes.Contains(apciType)) ? 1 : 0) << 15);

            switch(apciType)
            {
                case ApciTypes.MemoryWrite:
                case ApciTypes.MemoryRead:
                    int number = BitConverter.ToInt16(new byte[] { data[0], 0, 0, 0 }, 0);
                    if (number > 63)
                        number = 63;
                    _apci = _apci | number;
                    byte[] data_temp = new byte[data.Length - 1];
                    for (int i = 1; i < data.Length;i++)
                    {
                        data_temp[i - 1] = data[i];
                    }
                    data = data_temp;
                    break;
            }
            
            byte[] _apci2 = BitConverter.GetBytes(Convert.ToUInt16(_apci));

            switch(apciType)
            {
                case ApciTypes.GroupValueResponse:
                case ApciTypes.GroupValueWrite:
                case ApciTypes.ADCRead:
                case ApciTypes.ADCResponse:
                case ApciTypes.Ack:
                case ApciTypes.Connect:
                    bytes.Add(_apci2[1]);
                    break;
                default:
                    bytes.Add(_apci2[1]);
                    bytes.Add(_apci2[0]);
                    break;
            }

            if(data != null)
                bytes.AddRange(data);

            byte[] length = BitConverter.GetBytes((ushort)(bytes.Count + 2));
            Array.Reverse(length);
            bytes.InsertRange(4, length);
        }

        public byte[] GetBytes()
        {
            return bytes.ToArray();
        }


        public void SetChannelId(byte channelId)
        {
            bytes[7] = channelId;
        }

        public void SetSequence(byte sequence)
        {
            bytes[8] = sequence;
        }


        public void SetPriority(Prios prio)
        {
            switch (prio)
            {
                case Prios.System:
                    ctrlByte.Set(2, true);
                    ctrlByte.Set(3, true);
                    break;

                case Prios.Alarm:
                    ctrlByte.Set(2, true);
                    ctrlByte.Set(3, false);
                    break;

                case Prios.High:
                    ctrlByte.Set(2, false);
                    ctrlByte.Set(3, true);
                    break;

                case Prios.Low:
                    ctrlByte.Set(2, false);
                    ctrlByte.Set(3, false);
                    break;

            }
        }


        private byte bitToByte(BitArray arr)
        {
            byte byteOut = 0;
            for (byte i = 0; i < arr.Count; i++)
            {
                if (arr[i])
                    byteOut |= (byte)(1 << i);
            }
            return byteOut;
        }
    }

    public enum Prios
    {
        System,
        Alarm,
        High,
        Low
    }

}
