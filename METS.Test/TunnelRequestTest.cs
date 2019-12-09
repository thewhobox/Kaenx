using Microsoft.VisualStudio.TestTools.UnitTesting;

using METS.Knx.Builders;
using METS.Knx.Addresses;
using System;

namespace METS.Test
{
    [TestClass]
    public class TunnelRequestTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            TunnelRequest t = new TunnelRequest();
            MulticastAddress source = MulticastAddress.FromString("1/1/6");
            UnicastAddress destination = UnicastAddress.FromString("1.1.6");
            byte[] apci = new byte[] { 0xF1, 0x00 };
            byte sequence = 0x5;

            t.Build(source, destination, Knx.Parser.ApciTypes.IndividualAddressRead, sequence, 0);
            byte[] result = t.GetBytes();

            byte[] expected = new byte[]
            {
                0x06, 0x10, 0x04, 0x20,
                0x00, 0x15, 0x04, 0x1F,
                0x05, 0x00, 0x11, 0x00,
                0xB0, 0x60, 0x09, 0x06,
                0x11, 0x06, 0x01, 0xF1,
                0x00,

            };

            CollectionAssert.AreEqual(expected, result, "Result: " + BitConverter.ToString(result).Replace("-", " 0x"));
        }
    }
}
