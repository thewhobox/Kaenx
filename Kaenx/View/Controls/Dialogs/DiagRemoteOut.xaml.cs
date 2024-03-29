﻿using Kaenx.Classes.Bus;
using Kaenx.Konnect.Remote;
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

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View.Controls.Dialogs
{
    public sealed partial class DiagRemoteOut : ContentDialog
    {
        public DiagRemoteOut()
        {
            this.InitializeComponent();
            this.DataContext = BusRemoteConnection.Instance.Remote;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private async void ClickConnect(object sender, RoutedEventArgs e)
        {
            ConnectRequest req = new ConnectRequest();
            req.Group = InGroup.Text;
            req.Code = InCode.Text;
            IRemoteMessage resp = await BusRemoteConnection.Instance.Remote.Send(req);

            switch(resp)
            {
                case StateResponse statemsg:
                    OutState.Text = statemsg.Code.ToString();
                    break;

                case ConnectResponse connmsg:
                    BusRemoteConnection.Instance.Remote.IsConnected = true;
                    BusRemoteConnection.Instance.Remote.ChannelId = connmsg.ChannelId;
                    BusRemoteConnection.Instance.Remote.GroupRemote = req.Group;
                    OutState.Text = "ChannelId: " + connmsg.ChannelId;
                    break;

                default:
                    OutState.Text = "Unbekannte Antwort: " + resp;
                    //throw new Exception("Unerwartete Antwort: DiagRemoteOut Connect - " + resp);
                    return;
            }
        }
    }
}
