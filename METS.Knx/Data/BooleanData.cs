using System;
using System.Collections.Generic;
using System.Text;

namespace METS.Knx.Data
{
    public class BooleanData
    {
        private readonly byte[] _bytes;

        private BooleanData(byte[] bytes)
        {
            _bytes = bytes;
        }

        public byte[] GetBytes()
        {
            return _bytes;
        }

        public static BooleanData FromBoolean(bool value)
        {
            return new BooleanData(new[] { Convert.ToByte(value) });
        }
    }
}
