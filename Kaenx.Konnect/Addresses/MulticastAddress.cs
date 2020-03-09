using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Konnect.Addresses
{
    public class MulticastAddress : IKnxAddress
    {
        public MulticastAddress(byte mainGroup, byte middleGroup, byte subGroup)
        {
            MainGroup = mainGroup;
            MiddleGroup = middleGroup;
            SubGroup = subGroup;
        }

        public byte MainGroup { get; }
        public byte MiddleGroup { get; }
        public byte SubGroup { get; }

        public byte[] GetBytes()
        {
            return new[] { (byte)((MainGroup << 3) | MiddleGroup), SubGroup };
        }

        public static MulticastAddress FromByteArray(byte[] bytes)
        {
            return new MulticastAddress((byte)(bytes[0] >> 3), (byte)(bytes[0] & 0x07), bytes[1]);
        }

        public static MulticastAddress FromString(string address)
        {
            var addressParts = address.Split('/');
            if (addressParts.Length != 3)
                throw new Exception("Invalid address string.");

            return new MulticastAddress(Convert.ToByte(addressParts[0]), Convert.ToByte(addressParts[1]), Convert.ToByte(addressParts[2]));
        }

        public override string ToString()
        {
            return MainGroup.ToString() + "/" + MiddleGroup.ToString() + "/" + SubGroup.ToString();
        }
    }
}
