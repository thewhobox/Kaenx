using Kaenx.Konnect.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Kaenx.Konnect.Classes
{
    public class ReceiverParserDispatcher
    {
        private readonly List<IReceiveParser> _responseParsers;

        public ReceiverParserDispatcher()
        {
            _responseParsers = new List<IReceiveParser>();
            List<Type> parsers = new List<Type>();
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();

            foreach (Type type in types)
            {
                if (type.IsClass && !type.IsNested && type.Namespace == "Kaenx.Konnect.Parser")
                    parsers.Add(type);
            }


            foreach (Type t in parsers)
            {
                IReceiveParser parser = (IReceiveParser)Activator.CreateInstance(t);
                _responseParsers.Add(parser);
            }
        }

        public IResponse Build(byte[] responseBytes)
        {
            var headerLength = ParseHeaderLength(responseBytes[0]);
            var protocolVersion = ParseProtocolVersion(responseBytes[1]);
            var serviceTypeIdentifier = ParseServiceTypeIdentifier(responseBytes[2], responseBytes[3]);
            var totalLength = ParseTotalLength(responseBytes[4], responseBytes[5]);

            Console.WriteLine($"ServiceType: {serviceTypeIdentifier} {responseBytes[2]:X}-{responseBytes[3]:X}");

            return _responseParsers.AsQueryable().SingleOrDefault(x => x.ServiceTypeIdentifier == serviceTypeIdentifier)
              ?.Build(headerLength, protocolVersion, totalLength, responseBytes.Skip(6).ToArray());
        }

        private static ushort ParseTotalLength(byte data, byte data1)
        {
            return BitConverter.ToUInt16(new[] { data1, data }, 0);
        }

        private static ushort ParseServiceTypeIdentifier(byte data, byte data1)
        {
            return BitConverter.ToUInt16(new[] { data1, data }, 0);
        }

        private static byte ParseProtocolVersion(byte data)
        {
            return data;
        }

        private static byte ParseHeaderLength(byte data)
        {
            return data;
        }
    }
}
