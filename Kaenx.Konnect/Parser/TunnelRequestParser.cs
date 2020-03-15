using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Builders;
using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Responses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Kaenx.Konnect.Parser
{
    public class TunnelRequestParser : IReceiveParser
    {
        public ushort ServiceTypeIdentifier => 0x0420;

        public TunnelRequestParser() { }

        IResponse IReceiveParser.Build(byte headerLength, byte protocolVersion, ushort totalLength,
          byte[] responseBytes)
        {
            return Build(headerLength, protocolVersion, totalLength, responseBytes);
        }


        public Builders.TunnelResponse Build(byte headerLength, byte protocolVersion, ushort totalLength, byte[] responseBytes)
        {
            var structureLength = responseBytes[0];
            var communicationChannel = responseBytes[1];
            var sequenceCounter = responseBytes[2];
            var messageCode = responseBytes[4];
            var addInformationLength = responseBytes[5];
            var controlField = responseBytes[6];
            var controlField2 = responseBytes[7];
            var npduLength = responseBytes[12];
            byte[] npdu;
            byte[] apci = new byte[2];

            if(npduLength != 0)
            {
                npdu = new byte[] { responseBytes[13], responseBytes[14] };
            } else
            {
                npdu = new byte[] { responseBytes[13] };
            }


            byte[] data = null;
            int seqNumb = 0x0;
            ApciTypes type = ApciTypes.Undefined;

            if (npduLength != 0)
            {

                BitArray bitsNpdu = new BitArray(npdu);
                if(bitsNpdu.Get(6))
                {
                    seqNumb = npdu[0] >> 2;
                    seqNumb = seqNumb & 0xF;
                }

                int apci1 = ((npdu[0] & 3) << 2) | (npdu[1] >> 6 );

                switch(apci1)
                {
                    case 0:
                        type = ApciTypes.GroupValueRead;
                        break;
                    case 1:
                        type = ApciTypes.GroupValueResponse;
                        break;
                    case 2:
                        type = ApciTypes.GroupValueWrite;
                        int datai = npdu[1] & 63;
                        data = new byte[responseBytes.Length - 15 + 1];
                        data[0] = Convert.ToByte(datai);
                        for(int i = 1; i< responseBytes.Length - 15 + 1; i++)
                        {
                            data[i] = responseBytes[i];
                        }
                        break;
                    case 3:
                        type = ApciTypes.IndividualAddressWrite;
                        break;
                    case 4:
                        type = ApciTypes.IndividualAddressRead;
                        break;
                    case 5:
                        type = ApciTypes.IndividualAddressResponse;
                        break;
                    case 6:
                        type = ApciTypes.ADCRead;
                        break;
                    case 7:
                        if(npdu[1] == 0) 
                            type = ApciTypes.ADCResponse;
                        break;
                    case 8:
                        type = ApciTypes.MemoryRead;
                        break;
                    case 9:
                        type = ApciTypes.MemoryResponse;
                        break;
                    case 10:
                        type = ApciTypes.MemoryWrite;
                        break;


                    default:
                        apci1 = ((npdu[0] & 3) << 8) | npdu[1];
                        type = (ApciTypes)apci1;
                        break;
                }

                if(data == null)
                {
                    data = new byte[responseBytes.Length - 15];

                    int c = 0;
                    for (int i = 15; i < responseBytes.Length; i++)
                    {
                        data[c] = responseBytes[i];
                        c++;
                    }
                }
            } else
            {
                data = new byte[0];
                int apci3 = npdu[0] & 3;

                switch(apci3)
                {
                    case 2:
                        type = ApciTypes.Ack;
                        break;
                    default:
                        Debug.WriteLine("Unbekantes NPDU: " + apci3);
                        break;
                }
            }


            return new Builders.TunnelResponse(headerLength, protocolVersion, totalLength, structureLength, communicationChannel,
              sequenceCounter, messageCode, addInformationLength, controlField, controlField2,
              UnicastAddress.FromByteArray(new[] { responseBytes[8], responseBytes[9] }),
              MulticastAddress.FromByteArray(new[] { responseBytes[10], responseBytes[11] }), type, seqNumb,
              data);
        }
    }


    public enum ApciTypes
    {
        Undefined = -1,
        GroupValueRead = 0,
        GroupValueResponse = 64,
        GroupValueWrite = 128,
        IndividualAddressWrite = 192,
        IndividualAddressRead = 256,
        IndividualAddressResponse = 320,
        ADCRead = 384,
        ADCResponse = 448,
        SystemNetworkParameterRead = 456,
        SystemNetworkParameterWrite = 458,
        SystemNetworkParameterResponse = 457,
        MemoryRead = 512,
        Ack = 513, //TODO remove last byte
        MemoryResponse = 576,
        MemoryWrite = 640,
        UserMemoryRead = 704,
        UserMemoryResponse = 705,
        UserMemoryWrite = 706,
        UserMemoryBitWrite = 708,
        UserManufacturerInfoRead = 709,
        UserManufacturerInfoResponse = 710,
        FunctionPropertyCommand = 711,
        FunctionPropertyStateRead = 712,
        FunctionPropertyStateResponse = 713,
        DeviceDescriptorRead = 768,
        DeviceDescriptorResponse = 832,
        Restart = 896,
        OpenRoutingTable = 960,
        ReadRoutingTable = 961,
        ReadRoutingTableResponse = 962,
        WriteRoutinTable = 963,
        ReadRouterMemory = 968,
        ReadRouterMemoryResponse = 969,
        WriteRouterMemory = 970,
        ReadRouterStatus = 973,
        ReadRouterStatusResponse = 974,
        WriteRouterStatus,
        MemoryBitWrite,
        AuthorizeRequest,
        AuthorizeResponse,
        KeyWrite,
        KeyResponse,
        PropertyValueRead,
        PropertyValueResponse,
        PropertyValueWrite,
        PropertyDescriptionRead,
        PropertyDescriptionResponse,
        NetworkParameterRead,
        NetworkParameterResponse,
        IndividualAddressSerialNumberRead,
        IndividualAddressSerialNumberResponse,
        IndividualAddressSerialNumberWrite,
        DomainAddressWrite,
        DomainAddressRead,
        DomainAddressResponse,
        DomainAddressSelectiveRead,
        NetworkParameterWrite,
        LinkRead,
        LinkResponse,
        LinkWrite,
        GroupValueRead2,
        GroupValueResponse2,
        GroupValueWrite2,
        GroupValueInfoReport,
        DomainAddressSerialNumberRead,
        DomainAddressSerialNumberResponse,
        DomainAddressSerialNumberWrite,
        FileStreamInfoReport,
        Connect = 32768,
        Disconnect = 32769
    }
}
