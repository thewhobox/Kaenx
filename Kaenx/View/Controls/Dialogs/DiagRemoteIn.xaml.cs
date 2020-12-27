using Kaenx.Classes.Bus;
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
    public sealed partial class DiagRemoteIn : ContentDialog
    {
        public DiagRemoteIn()
        {
            this.InitializeComponent();
            ClickReload(null, null);
        }

        private async void ClickCreate(object sender, RoutedEventArgs e)
        {
            //ListGroups.SelectedItem = null;
            CodesRequest req = new CodesRequest();
            req.Action = CodeRequestActions.Create;
            req.Group = RemoteConnection.Instance.ConnectionOut.Group;

            if(InCode.Text != "")
            {
                req.Code = InCode.Text;
                InCode.Text = "";
            }

            IRemoteMessage resp = await RemoteConnection.Instance.ConnectionOut.Send(req);
            
            if(resp is CodesResponse)
            {
                CodesResponse response = (CodesResponse)resp;
                ListCodes.ItemsSource = response.Codes;
            }
            else
            {

            }
        }

        private async void ClickReload(object sender, RoutedEventArgs e)
        {
            CodesRequest req = new CodesRequest();
            req.Group = RemoteConnection.Instance.ConnectionOut.Group;
            IRemoteMessage resp = await RemoteConnection.Instance.ConnectionOut.Send(req);

            if (resp is CodesResponse)
            {
                CodesResponse response = (CodesResponse)resp;
                ListCodes.ItemsSource = response.Codes;
            }
            else
            {

            }
        }

        private async void ClickRemove(object sender, RoutedEventArgs e)
        {
            CodesRequest req = new CodesRequest();
            req.Action = CodeRequestActions.Remove;
            req.Group = RemoteConnection.Instance.ConnectionOut.Group;
            req.Code = ListCodes.SelectedItem as string;
            IRemoteMessage resp = await RemoteConnection.Instance.ConnectionOut.Send(req);

            if (resp is CodesResponse)
            {
                CodesResponse response = (CodesResponse)resp;
                ListCodes.ItemsSource = response.Codes;
            }
            else
            {

            }
        }
    }
}
