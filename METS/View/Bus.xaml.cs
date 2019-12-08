﻿using METS.Classes;
using METS.Classes.Bus;
using METS.Classes.Bus.Actions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace METS.View
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class Bus : Page
    {
        public Bus()
        {
            this.InitializeComponent();
        }

        private void ReadInfo(object sender, RoutedEventArgs e)
        {
            BtnGetInfo.IsEnabled = false;

            string[] address = InAddress2.Text.Split(".");

            DeviceInfo action = new DeviceInfo();
            Line dM = new Line { Id = int.Parse(address[0]) };
            LineMiddle dL = new LineMiddle { Id = int.Parse(address[1]), Parent = dM };
            action.Device = new LineDevice { Name = "Unbekannt", Id = int.Parse(address[2]), Parent = dL };
            action.Finished += Action_Finished;
            BusConnection.Instance.AddAction(action);
        }

        private void Action_Finished(object sender, EventArgs e)
        {
            _ = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                BtnGetInfo.IsEnabled = true;
                GridOutput.DataContext = sender;
            });
        }
    }
}
