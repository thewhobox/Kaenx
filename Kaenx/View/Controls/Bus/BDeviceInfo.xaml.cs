﻿using Kaenx.Classes.Bus;
using Kaenx.Classes.Bus.Actions;
using Kaenx.Classes.Bus.Data;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.Konnect.Connections;
using Kaenx.Konnect.Interfaces;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace Kaenx.View.Controls.Bus
{
    public sealed partial class BDeviceInfo : UserControl
    {
        public ObservableCollection<IBusData> ReadList { get; } = new ObservableCollection<IBusData>();
        public static ICollectionView CurrentDetailsView { get; set; }

        public delegate void AddTabItemHandler(string text, IBusData data);
        public event AddTabItemHandler OnAddTabItem;



        public BDeviceInfo()
        {
            this.InitializeComponent();
            this.DataContext = ReadList;
        }



        private void ReadInfo(object sender, RoutedEventArgs e)
        {
            DeviceInfo action = new DeviceInfo();

            action.Device = GetDevice();
            if (action.Device == null) return;

            action.Finished += Action_Finished;
            BusConnection.Instance.AddAction(action);
        }

        private void ReadMem(object sender, RoutedEventArgs e)
        {
            DeviceMem action = new DeviceMem();

            action.Device = GetDevice();
            if (action.Device == null) return;

            action.Finished += Action_Finished;
            BusConnection.Instance.AddAction(action);
        }

        private void ReadConf(object sender, RoutedEventArgs e)
        {
            //DeviceConfig action = new DeviceConfig();

            //action.Device = GetDevice();
            //if (action.Device == null) return;

            //action.Finished += Action_Finished;
            //BusConnection.Instance.AddAction(action);
            throw new NotImplementedException("Ist rausgeflogen");
        }


        private void Action_Finished(IBusAction action, object data)
        {
            _ = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                IBusData d = (IBusData)data;
                d.Device = action.Device;
                ReadList.Insert(0, d);
            });
        }

        private void ClickOpenConfig(object sender, RoutedEventArgs e)
        {
            ViewHelper.Instance.ShowNotification("main", "Nichts passiert");
        }

        public void AddReadData(IBusData info)
        {
            _ = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ReadList.Insert(0, info);
            });
        }

        private LineDevice GetDevice()
        {
            LineDevice dev = null;


            bool valid = true; //Todo add check Microsoft.Toolkit.Uwp.UI.Extensions.TextBoxRegex.GetIsValid(InAddress2);

            if (!valid)
            {
                ViewHelper.Instance.ShowNotification("main", "Ungültige Adresse!", 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                return null;
            }

            string[] address = InAddress2.Text.Split(".");



            if(SaveHelper._project != null && SaveHelper._project.Lines.Any(l => l.Id.ToString() == address[0]))
            {
                Line l = SaveHelper._project.Lines.Single(l => l.Id.ToString() == address[0]);

                if(l.Subs.Any(l => l.Id.ToString() == address[1]))
                {
                    LineMiddle lm = l.Subs.Single(l => l.Id.ToString() == address[1]);

                    if(lm.Subs.Any(l => l.Id.ToString() == address[2]))
                    {
                        dev = lm.Subs.Single(l => l.Id.ToString() == address[2]);
                    }
                }
            }

            if (dev == null)
            {
                ViewHelper.Instance.ShowNotification("main", "Adresse konnte keinem Gerät zugewiesen werden.", 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning);
                Line dM = new Line { Id = int.Parse(address[0]) };
                LineMiddle dL = new LineMiddle { Id = int.Parse(address[1]), Parent = dM };
                dev = new LineDevice() { Name = "Unbekannt", Id = int.Parse(address[2]), Parent = dL };
            }

            return dev;
        }

        private async void SetTest(object sender, RoutedEventArgs e)
        {
            LineDevice ldev = GetDevice();
            IKnxConnection conn = await KnxInterfaceHelper.GetConnection(BusConnection.Instance.SelectedInterface, BusRemoteConnection.Instance.Remote, BusConnection.Instance.GetDevice);
            await conn.Connect();
            await System.Threading.Tasks.Task.Delay(2000);
            Konnect.Classes.BusDevice dev = new Konnect.Classes.BusDevice(ldev.LineName, conn);
            await dev.Connect();
            await dev.PropertyWrite(0, 21, System.Text.Encoding.UTF8.GetBytes("123"));
            dev.Disconnect();
            await System.Threading.Tasks.Task.Delay(200);
            await conn.Disconnect();
        }

        private async void SetTest2(object sender, RoutedEventArgs e)
        {
            LineDevice ldev = GetDevice();
            IKnxConnection conn = await KnxInterfaceHelper.GetConnection(BusConnection.Instance.SelectedInterface, BusRemoteConnection.Instance.Remote, BusConnection.Instance.GetDevice);
            await conn.Connect();
            await System.Threading.Tasks.Task.Delay(2000);
            Konnect.Classes.BusDevice dev = new Konnect.Classes.BusDevice(ldev.LineName, conn);
            await dev.Connect();
            string resp = await dev.PropertyRead<string>(0, 21);
            dev.Disconnect();
            await System.Threading.Tasks.Task.Delay(200);
            await conn.Disconnect();
        }

        private void DataGrid_LoadingRowGroup(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridRowGroupHeaderEventArgs e)
        {
            ICollectionViewGroup group = e.RowGroupHeader.CollectionViewGroup;
            GroupInfoCollection<OtherResource> g = group.Group as GroupInfoCollection<OtherResource>;
            e.RowGroupHeader.PropertyValue = g.Key;
        }

        private async void ReadMax(object sender, RoutedEventArgs e)
        {
            LineDevice ldev = GetDevice();
            IKnxConnection conn = await KnxInterfaceHelper.GetConnection(BusConnection.Instance.SelectedInterface, BusRemoteConnection.Instance.Remote, BusConnection.Instance.GetDevice);
            await conn.Connect();
            await System.Threading.Tasks.Task.Delay(2000);
            Konnect.Classes.BusDevice dev = new Konnect.Classes.BusDevice(ldev.LineName, conn);
            await dev.Connect(true);
            int resp = await dev.PropertyRead<int>(0, 56);
            ViewHelper.Instance.ShowNotification("main", "MaxAPDU: " + resp, 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational);
            await dev.Disconnect();
            await conn.Disconnect();
        }

        private void GridReads_LoadingRow(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridRowEventArgs e)
        {
            e.Row.DoubleTapped += Row_DoubleTapped;
        }

        private void Row_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            //ViewHelper.Instance.ShowNotification("main", "Doppelklick gemacht!", 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational);
            IBusData data = (sender as Microsoft.Toolkit.Uwp.UI.Controls.DataGridRow).DataContext as IBusData;
            OnAddTabItem?.Invoke("Info", data);//data.Device.LineName + " Info", data);
        }

        private async void SetTest3(object sender, RoutedEventArgs e)
        {

            LineDevice ldev = GetDevice();
            IKnxConnection conn = await KnxInterfaceHelper.GetConnection(BusConnection.Instance.SelectedInterface, BusRemoteConnection.Instance.Remote, BusConnection.Instance.GetDevice);
            await conn.Connect();
            await System.Threading.Tasks.Task.Delay(2000);
            Konnect.Classes.BusDevice dev = new Konnect.Classes.BusDevice(ldev.LineName, conn);
            await dev.Connect();
            
            await dev.ResourceWrite("ApplicationId", new byte[] { 0x00, 0x83, 0x10, 0x3C, 0x00 });
            await Task.Delay(2000);
            string appId = await dev.ResourceRead<string>("ApplicationId");

            await dev.Disconnect();
            await conn.Disconnect();

        }
    }
}
