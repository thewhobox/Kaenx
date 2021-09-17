using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kaenx.Classes.Buildings;
using Kaenx.Classes.Bus.Actions;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Connections;
using Kaenx.Konnect.Messages;
using Kaenx.Konnect.Messages.Request;
using Kaenx.Konnect.Messages.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Core;

namespace Kaenx.Test
{
    [TestClass]
    public class ProgramApplication
    {
        [TestMethod]
        public async Task SystemBFull()
        {
            var progApplication = new ProgApplication(ProgApplication.ProgAppType.Komplett);
            progApplication.Context = new CatalogContext(new DataContext.Local.LocalConnectionCatalog()
            {
                Type = DataContext.Local.LocalConnectionCatalog.DbConnectionType.Memory
            });
            progApplication.Context.Database.OpenConnection();
            progApplication.Context.Database.EnsureCreated();

            // Init in UI Thread because constructor call UI methods
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                LineDevice device = new LineDevice(true);
                Line main = new Line(2, "");
                LineMiddle middle = new LineMiddle(3, "", main);
                device.Parent = middle;
                device.Id = 4;
                device.ApplicationId = 1;
                DeviceComObject com1 = new DeviceComObject() { Number = 1, IsEnabled = true, Flag_Communication = true, Flag_Write = true, DataPointSubType = new Classes.DataPointSubType() { SizeInBit = 1 } };
                com1.Groups.Add(new FunctionGroup() { Address = new MulticastAddress(7, 7, 10) });
                device.ComObjects.Add(com1);
                DeviceComObject com2 = new DeviceComObject() { Number = 2, IsEnabled = true, Flag_Communication = true, Flag_Read = true, Flag_Transmit = true, DataPointSubType = new Classes.DataPointSubType() { SizeInBit = 1 } };
                com2.Groups.Add(new FunctionGroup() { Address = new MulticastAddress(7, 7, 20) });
                com2.Groups.Add(new FunctionGroup() { Address = new MulticastAddress(7, 7, 10) });
                device.ComObjects.Add(com2);
                progApplication.Device = device;
            });

            progApplication.Context.AppAdditionals.Add(new AppAdditional()
            {
                Id = progApplication.Device.ApplicationId,
                LoadProcedures = Encoding.UTF8.GetBytes(
                    @"<LoadProcedures xmlns='http://knx.org/xml/project/14'>
                        <LoadProcedure MergeId='2'>
                            <LdCtrlRelSegment LsmIdx='4' Size='4' Mode='0' Fill='0' />
                        </LoadProcedure>
                        <LoadProcedure MergeId='4'>
                            <LdCtrlWriteRelMem ObjIdx='4' Offset='0' Size='4' Verify='true' />
                        </LoadProcedure>
                    </LoadProcedures>"
                )
            });
            progApplication.Context.Manufacturers.Add(new ManufacturerViewModel()
            {
                Id = 1,
                ManuId = 0x0188,
                Name = "Not Assigned",
            });
            progApplication.Context.Applications.Add(new ApplicationViewModel()
            {
                Id = progApplication.Device.ApplicationId,
                Mask = "MV-07B0",
                Manufacturer = 1,
                Number = 1,
                Version = 1,
                LoadProcedure = LoadProcedureTypes.Merge,
                Table_Group_Max = 255,
                Table_Assosiations_Max = 255,
            });
            progApplication.Context.AppSegments.Add(new AppSegmentViewModel()
            {
                Id = 1,
                Size = 4,
                LsmId = 4,
            });
            progApplication.Context.AppParameterTypes.Add(new AppParameterTypeViewModel()
            {
                Id = 1,
                Type = ParamTypes.NumberInt,
                Size = 32,
            });
            progApplication.OverrideVisibleParams = new List<AppParameter>();
            progApplication.OverrideVisibleParams.Add(new AppParameter()
            {
                Id = 1,
                ParameterTypeId = 1,
                Value = 0x01020304.ToString(),
                SegmentId = 1,
                SegmentType = SegmentTypes.Memory,
                Offset = 0,
            });
            progApplication.Context.SaveChanges();

            UnicastAddress address = UnicastAddress.FromString(progApplication.Device.LineName);
            var connection = new TestKnxConnection();
            progApplication.Connection = connection;

            connection.Expect(new MsgConnectReq(address));

            const byte DEVICE = 0;
            const byte ADDRESS_TABLE = 1;
            const byte ASSOCIATION_TABLE = 2;
            const byte GROUP_OBJECT_TABLE = 3;
            const byte APPLICATION_PROGRAM = 4;
            const byte APPLICATION_PROGRAM2 = 5;

            const byte PID_LOAD_STATE_CONTROL = 5;
            const byte PID_TABLE_REFERENCE = 7;
            const byte PID_SERIAL_NUMBER = 11;
            const byte PID_MANUFACTURER_ID = 12;
            const byte PID_PROGRAM_VERSION = 13;
            const byte PID_DEVICE_CONTROL = 14;
            const byte PID_ORDER_INFO = 15;
            const byte PID_TABLE = 23;
            const byte PID_VERSION = 25;
            const byte PID_MAX_APDULENGTH = 56;
            const byte PID_HARDWARE_TYPE = 78;

            connection.Expect(new MsgPropertyReadReq(DEVICE, PID_MAX_APDULENGTH, address));
            connection.Response(ApciTypes.PropertyValueResponse, DEVICE, PID_MAX_APDULENGTH, 0x10, 0x01, 0x00, 254);

            // Pre download checks
            connection.Expect(new MsgDescriptorReadReq(address));
            connection.Response(ApciTypes.DeviceDescriptorResponse, 0x07, 0xB0);
            connection.Expect(new MsgPropertyReadReq(DEVICE, PID_MAX_APDULENGTH, address));
            connection.Response(ApciTypes.PropertyValueResponse, DEVICE, PID_MAX_APDULENGTH, 0x10, 0x01, 0x00, 254);
            connection.Expect(new MsgAuthorizeReq(0xffffffff, address));
            connection.Response(ApciTypes.AuthorizeResponse, 0);

            connection.Expect(new MsgPropertyDescriptionReq(ASSOCIATION_TABLE, PID_TABLE, 0, address));
            connection.Response(ApciTypes.PropertyDescriptionResponse, ASSOCIATION_TABLE, PID_TABLE, 0x03, 0x12, 0x00, 0x01, 0x30);

            /* Not According to documentation, but ETS does this anyway
            connection.Expect(new MsgPropertyReadReq(DEVICE, PID_SERIAL_NUMBER, address));
            connection.Response(ApciTypes.PropertyValueResponse, DEVICE, PID_SERIAL_NUMBER, 0x10, 0x01, 0x01, 0x88, 0x00, 0x00, 0x00, 0x00);
            */
            connection.Expect(new MsgPropertyReadReq(DEVICE, PID_MANUFACTURER_ID, address));
            connection.Response(ApciTypes.PropertyValueResponse, DEVICE, PID_MANUFACTURER_ID, 0x10, 0x01, 0x01, 0x88);
            connection.Expect(new MsgPropertyReadReq(DEVICE, PID_VERSION, address));
            connection.Response(ApciTypes.PropertyValueResponse, DEVICE, PID_VERSION, 0x10, 0x01, 0x00, 0x00);
            connection.Expect(new MsgPropertyReadReq(DEVICE, PID_HARDWARE_TYPE, address));
            connection.Response(ApciTypes.PropertyValueResponse, DEVICE, PID_HARDWARE_TYPE, 0x10, 0x00);
            connection.Expect(new MsgPropertyReadReq(DEVICE, PID_ORDER_INFO, address));
            connection.Response(ApciTypes.PropertyValueResponse, DEVICE, PID_ORDER_INFO, 0x10, 0x01, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x00);

            byte[] LSM_EVENT_UNLOAD = new byte[] { 0x04, 0, 0, 0, 0, 0, 0, 0, 0, 0, };
            connection.Expect(new MsgPropertyWriteReq(APPLICATION_PROGRAM2, PID_LOAD_STATE_CONTROL, LSM_EVENT_UNLOAD, address));
            connection.Response(ApciTypes.PropertyValueResponse, APPLICATION_PROGRAM2, PID_LOAD_STATE_CONTROL, 0x10, 0x00);
            connection.Expect(new MsgPropertyWriteReq(APPLICATION_PROGRAM, PID_LOAD_STATE_CONTROL, LSM_EVENT_UNLOAD, address));
            connection.Response(ApciTypes.PropertyValueResponse, APPLICATION_PROGRAM, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 0);
            connection.Expect(new MsgPropertyWriteReq(GROUP_OBJECT_TABLE, PID_LOAD_STATE_CONTROL, LSM_EVENT_UNLOAD, address));
            connection.Response(ApciTypes.PropertyValueResponse, GROUP_OBJECT_TABLE, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 0);
            connection.Expect(new MsgPropertyWriteReq(ASSOCIATION_TABLE, PID_LOAD_STATE_CONTROL, LSM_EVENT_UNLOAD, address));
            connection.Response(ApciTypes.PropertyValueResponse, ASSOCIATION_TABLE, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 0);
            connection.Expect(new MsgPropertyWriteReq(ADDRESS_TABLE, PID_LOAD_STATE_CONTROL, LSM_EVENT_UNLOAD, address));
            connection.Response(ApciTypes.PropertyValueResponse, ADDRESS_TABLE, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 0);

            byte[] LSM_EVENT_START_LOADING = new byte[] { 0x01, 0, 0, 0, 0, 0, 0, 0, 0, 0, };
            byte[] LSM_EVENT_ALLOCATE_APP = new byte[] { 0x03, 0x0B, 0, 0, 0, 0, 0, 0, 0, 0 };
            U32ToBe(4).CopyTo(LSM_EVENT_ALLOCATE_APP, 2);
            connection.Expect(new MsgPropertyWriteReq(APPLICATION_PROGRAM, PID_LOAD_STATE_CONTROL, LSM_EVENT_START_LOADING, address));
            connection.Response(ApciTypes.PropertyValueResponse, APPLICATION_PROGRAM, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 2);
            connection.Expect(new MsgPropertyWriteReq(APPLICATION_PROGRAM, PID_LOAD_STATE_CONTROL, LSM_EVENT_ALLOCATE_APP, address));
            connection.Response(ApciTypes.PropertyValueResponse, APPLICATION_PROGRAM, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 2);
            byte[] LSM_EVENT_ALLOCATE_GROUP = new byte[] { 0x03, 0x0B, 0, 0, 0, 0, 0, 0, 0, 0 };
            U32ToBe(2 + 2 * 2).CopyTo(LSM_EVENT_ALLOCATE_GROUP, 2);
            connection.Expect(new MsgPropertyWriteReq(GROUP_OBJECT_TABLE, PID_LOAD_STATE_CONTROL, LSM_EVENT_START_LOADING, address));
            connection.Response(ApciTypes.PropertyValueResponse, GROUP_OBJECT_TABLE, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 2);
            connection.Expect(new MsgPropertyWriteReq(GROUP_OBJECT_TABLE, PID_LOAD_STATE_CONTROL, LSM_EVENT_ALLOCATE_GROUP, address));
            connection.Response(ApciTypes.PropertyValueResponse, GROUP_OBJECT_TABLE, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 2);
            byte[] LSM_EVENT_ALLOCATE_ADDRESS = new byte[] { 0x03, 0x0B, 0, 0, 0, 0, 0, 0, 0, 0 };
            U32ToBe(2 + 2 * 2).CopyTo(LSM_EVENT_ALLOCATE_ADDRESS, 2);
            connection.Expect(new MsgPropertyWriteReq(ADDRESS_TABLE, PID_LOAD_STATE_CONTROL, LSM_EVENT_START_LOADING, address));
            connection.Response(ApciTypes.PropertyValueResponse, ADDRESS_TABLE, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 2);
            connection.Expect(new MsgPropertyWriteReq(ADDRESS_TABLE, PID_LOAD_STATE_CONTROL, LSM_EVENT_ALLOCATE_ADDRESS, address));
            connection.Response(ApciTypes.PropertyValueResponse, ADDRESS_TABLE, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 2);
            byte[] LSM_EVENT_ALLOCATE_ASSOCIATION = new byte[] { 0x03, 0x0B, 0, 0, 0, 0, 0, 0, 0, 0 };
            U32ToBe(2 + 2 * 3).CopyTo(LSM_EVENT_ALLOCATE_ASSOCIATION, 2);
            connection.Expect(new MsgPropertyWriteReq(ASSOCIATION_TABLE, PID_LOAD_STATE_CONTROL, LSM_EVENT_START_LOADING, address));
            connection.Response(ApciTypes.PropertyValueResponse, ASSOCIATION_TABLE, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 2);
            connection.Expect(new MsgPropertyWriteReq(ASSOCIATION_TABLE, PID_LOAD_STATE_CONTROL, LSM_EVENT_ALLOCATE_ASSOCIATION, address));
            connection.Response(ApciTypes.PropertyValueResponse, ASSOCIATION_TABLE, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 2);

            connection.Expect(new MsgPropertyReadReq(APPLICATION_PROGRAM, PID_TABLE_REFERENCE, address));
            byte[] application_address = U32ToBe(0x100);
            connection.Response(ApciTypes.PropertyValueResponse, APPLICATION_PROGRAM, PID_TABLE_REFERENCE, 0x10, 0x01, application_address[0], application_address[1], application_address[2], application_address[3]);

            //Enable Verify Mode (Set bit 2)
            connection.Expect(new MsgPropertyReadReq(DEVICE, PID_DEVICE_CONTROL, address));
            connection.Response(ApciTypes.PropertyValueResponse, DEVICE, PID_DEVICE_CONTROL, 0x10, 0x01, 0x00);
            connection.Expect(new MsgPropertyReadReq(DEVICE, PID_DEVICE_CONTROL, address));
            connection.Response(ApciTypes.PropertyValueResponse, DEVICE, PID_DEVICE_CONTROL, 0x10, 0x01, 0x00);
            connection.Expect(new MsgPropertyWriteReq(DEVICE, PID_DEVICE_CONTROL, new byte[] { 0x04 }, address));
            connection.Response(ApciTypes.PropertyValueResponse, DEVICE, PID_DEVICE_CONTROL, 0x10, 0x01, 0x04);

            connection.Expect(new MsgMemoryWriteReq(0x100, new byte[] { 1, 2, 3, 4 }, address));
            connection.Response(ApciTypes.MemoryResponse, application_address[2], application_address[3], 1, 2, 3, 4);

            connection.Expect(new MsgPropertyReadReq(GROUP_OBJECT_TABLE, PID_TABLE_REFERENCE, address));
            byte[] object_table_address = U32ToBe(0x200);
            connection.Response(ApciTypes.PropertyValueResponse, GROUP_OBJECT_TABLE, PID_TABLE_REFERENCE, 0x10, 0x01, object_table_address[0], object_table_address[1], object_table_address[2], object_table_address[3]);
            connection.Expect(new MsgMemoryWriteReq(0x200, new byte[] { 0, 2, 0x17, 0x00, 0x4F, 0x00 }, address));
            connection.Response(ApciTypes.MemoryResponse, object_table_address[2], object_table_address[3], 0, 2, 0x17, 0x00, 0x4F, 0x00);

            connection.Expect(new MsgPropertyReadReq(ASSOCIATION_TABLE, PID_TABLE_REFERENCE, address));
            byte[] association_table_address = U32ToBe(0x300);
            connection.Response(ApciTypes.PropertyValueResponse, ASSOCIATION_TABLE, PID_TABLE_REFERENCE, 0x10, 0x01, association_table_address[0], association_table_address[1], association_table_address[2], association_table_address[3]);
            //connection.Expect(new MsgMemoryWriteReq(0x300, new byte[] { 0, 3, 1, 1, 2, 2, 1, 2 }, address));
            //connection.Response(ApciTypes.MemoryResponse, association_table_address[2], association_table_address[3], 0, 3, 1, 1, 2, 2, 1, 2);
            connection.Expect(new MsgMemoryWriteReq(0x300, new byte[] { 0, 3, 1, 1, 1, 2, 2, 2 }, address));
            connection.Response(ApciTypes.MemoryResponse, association_table_address[2], association_table_address[3], 0, 3, 1, 1, 1, 2, 2, 2);

            connection.Expect(new MsgPropertyReadReq(ADDRESS_TABLE, PID_TABLE_REFERENCE, address));
            byte[] address_table_address = U32ToBe(0x400);
            connection.Response(ApciTypes.PropertyValueResponse, ADDRESS_TABLE, PID_TABLE_REFERENCE, 0x10, 0x01, address_table_address[0], address_table_address[1], address_table_address[2], address_table_address[3]);
            MulticastAddress addr1 = new MulticastAddress(7, 7, 10);
            MulticastAddress addr2 = new MulticastAddress(7, 7, 20);
            connection.Expect(new MsgMemoryWriteReq(0x400, new byte[] { 0, 2, addr1.GetBytes()[0], addr1.GetBytes()[1], addr2.GetBytes()[0], addr2.GetBytes()[1] }, address));
            connection.Response(ApciTypes.MemoryResponse, address_table_address[2], address_table_address[3], 0, 2, addr1.GetBytes()[0], addr1.GetBytes()[1], addr2.GetBytes()[0], addr2.GetBytes()[1]);

            connection.Expect(new MsgPropertyWriteReq(APPLICATION_PROGRAM, PID_PROGRAM_VERSION, new byte[] { 0x01, 0x88, 0x00, 0x01, 0x01 }, address));
            connection.Response(ApciTypes.PropertyValueResponse, APPLICATION_PROGRAM, PID_PROGRAM_VERSION, 0x10, 0x01, 0x01, 0x88, 0x00, 0x01, 0x01);
            byte[] LSM_EVENT_LOAD_COMPLETE = new byte[] { 0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            connection.Expect(new MsgPropertyWriteReq(APPLICATION_PROGRAM, PID_LOAD_STATE_CONTROL, LSM_EVENT_LOAD_COMPLETE, address));
            connection.Response(ApciTypes.PropertyValueResponse, APPLICATION_PROGRAM, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 1);
            connection.Expect(new MsgPropertyWriteReq(GROUP_OBJECT_TABLE, PID_LOAD_STATE_CONTROL, LSM_EVENT_LOAD_COMPLETE, address));
            connection.Response(ApciTypes.PropertyValueResponse, GROUP_OBJECT_TABLE, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 1);
            connection.Expect(new MsgPropertyWriteReq(ASSOCIATION_TABLE, PID_LOAD_STATE_CONTROL, LSM_EVENT_LOAD_COMPLETE, address));
            connection.Response(ApciTypes.PropertyValueResponse, ASSOCIATION_TABLE, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 1);
            connection.Expect(new MsgPropertyWriteReq(ADDRESS_TABLE, PID_LOAD_STATE_CONTROL, LSM_EVENT_LOAD_COMPLETE, address));
            connection.Response(ApciTypes.PropertyValueResponse, ADDRESS_TABLE, PID_LOAD_STATE_CONTROL, 0x10, 0x01, 1);

            connection.Expect(new MsgRestartReq(address));

            typeof(ProgApplication).GetField("_token", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(progApplication, new CancellationTokenSource().Token);
            Task task = (Task)typeof(ProgApplication).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(progApplication, null);
            await task.ConfigureAwait(false);

            Assert.IsTrue(connection.ExpectedMessages.Count == 0, "Not all expected messages were sent");
        }

        private static byte[] U32ToBe(UInt32 value)
        {
            return new byte[] {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)(value >> 0)
            };
        }
    }

    class TestKnxConnection : IKnxConnection
    {
        public bool IsConnected { get; set; }
        public ConnectionErrors LastError { get; set; }
        public UnicastAddress PhysicalAddress { get; set; }

        public event IKnxConnection.TunnelRequestHandler OnTunnelRequest;
        public event IKnxConnection.TunnelResponseHandler OnTunnelResponse;
        public event IKnxConnection.TunnelAckHandler OnTunnelAck;
        public event IKnxConnection.SearchResponseHandler OnSearchResponse;
        public event IKnxConnection.ConnectionChangedHandler ConnectionChanged;

        public readonly Queue<IMessage> ExpectedMessages = new Queue<IMessage>();
        private byte sequenceCounter = 0;
        private int sequenceNumber = 0;

        public void Expect(IMessageRequest message)
        {
            ExpectedMessages.Enqueue(message);
        }

        public void Response(ApciTypes resApci, params byte[] resData)
        {
            var q = from t in Assembly.GetAssembly(typeof(IMessageResponse)).GetTypes()
                    where t.IsClass && t.IsNested == false && (t.Namespace == "Kaenx.Konnect.Messages.Response" || t.Namespace == "Kaenx.Konnect.Messages.Request")
                    select t;

            IMessage message = null;

            foreach (Type t in q.ToList())
            {
                IMessage resp = (IMessage)Activator.CreateInstance(t);

                if (resp.ApciType == resApci)
                {
                    message = resp;
                    break;
                }
            }

            if (message == null)
                message = new MsgDefaultRes() { ApciType = resApci };
            message.Raw = resData;
            message.ParseDataCemi();
            ExpectedMessages.Enqueue(message);
        }

        public Task Connect()
        {
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task Disconnect()
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public Task Send(byte[] data, bool ignoreConnected = false)
        {
            throw new NotImplementedException();
        }

        public Task<byte> Send(IMessage message, bool ignoreConnected = false)
        {
            message.ChannelId = 0;
            message.SequenceCounter = sequenceCounter++;

            Assert.IsTrue(ExpectedMessages.Count > 0, "Expected no more messages");
            IMessage expected = ExpectedMessages.Dequeue();
            expected.SequenceNumber = message.SequenceNumber;
            Assert.AreEqual(expected.DestinationAddress.ToString(), message.DestinationAddress.ToString());
            Assert.AreEqual(expected.ApciType, message.ApciType);
            if (!Enumerable.SequenceEqual(expected.GetBytesCemi(), message.GetBytesCemi()))
            {
                Assert.Fail("Messages are not equal. Expected: {0}, got: {1}", BitConverter.ToString(expected.GetBytesCemi()), BitConverter.ToString(message.GetBytesCemi()));
            }
            CollectionAssert.AreEqual(expected.GetBytesCemi(), message.GetBytesCemi());
            OnTunnelAck?.Invoke(new MsgAckRes()
            {
                ChannelId = 0,
                SequenceCounter = message.SequenceCounter,
                SequenceNumber = message.SequenceNumber,
                SourceAddress = message.DestinationAddress,
                DestinationAddress = message.SourceAddress
            });

            while (ExpectedMessages.Count != 0 && ExpectedMessages.Peek() is IMessageResponse)
            {
                IMessageResponse response = (IMessageResponse)ExpectedMessages.Dequeue();
                response.SequenceNumber = sequenceNumber;
                sequenceNumber = (sequenceNumber + 1) % 16;
                OnTunnelResponse?.Invoke(response);
            }
            return Task.FromResult(message.SequenceCounter);
        }

        public Task<bool> SendStatusReq()
        {
            return Task.FromResult(IsConnected);
        }
    }
}
