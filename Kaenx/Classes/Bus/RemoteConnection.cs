using Kaenx.DataContext.Local;
using Kaenx.Konnect.Connections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Kaenx.Konnect.Connections.RemoteToServer;

namespace Kaenx.Classes.Bus
{
    public class RemoteConnection : INotifyPropertyChanged
    {
        public static RemoteConnection Instance { get; set; } = new RemoteConnection();

        public event PropertyChangedEventHandler PropertyChanged;

        public RemoteToServer ConnectionOut { get; set; }




        public event MessageHandler OnRequest;
        public event MessageHandler OnResponse;


        private bool _isActive = false;
        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; Changed("IsActive"); }
        }

        private bool _isConnected = false;
        public bool IsConnected
        {
            get { return _isConnected; }
            set { _isConnected = value; Changed("IsConnected"); }
        }


        private string _state = "Nicht aktiviert";
        public string State
        {
            get { return _state; }
            set { _state = value; Changed("State"); }
        }



        public async Task SetNewConnection(LocalRemote remote)
        {
            State = "Verbinde...";
            IsActive = true;
            ConnectionOut = new RemoteToServer(remote.Host, remote.Authentification);

            try
            {
                await ConnectionOut.Connect();
                State = "Verbunden (" + ConnectionOut.Group + ")";
                ConnectionOut.OnRequest += ConnectionOut_OnRequest;
                ConnectionOut.OnResponse += ConnectionOut_OnResponse;
            } catch(Exception ex)
            {
                IsActive = false;
                State = ex.Message;
            }
        }

        private void ConnectionOut_OnResponse(Konnect.Remote.IRemoteMessage message)
        {
            OnResponse?.Invoke(message);
        }

        private void ConnectionOut_OnRequest(Konnect.Remote.IRemoteMessage message)
        {
            OnRequest?.Invoke(message);
        }

        public async Task Disconnect()
        {
            ConnectionOut.OnRequest -= ConnectionOut_OnRequest;
            ConnectionOut.OnResponse -= ConnectionOut_OnResponse;
            ConnectionOut.Disconnect();
            IsActive = false;
            State = "Getrennt";
            ConnectionOut = null;
        }



        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
