﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using METS.Classes.Bus.Actions;
using METS.Knx.Builders;

namespace METS.Classes.Bus
{
    public class BusConnection :INotifyPropertyChanged
    {
        private Knx.Connection connection;
        private bool _isConnected = false;

        public bool IsConnected { 
            get { return _isConnected; } 
            set { _isConnected = value; Changed("IsConnected"); }
        }

        public delegate void ConnectionChangedHandler(bool isConnected);
        public event ConnectionChangedHandler ConnectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public Queue<IBusAction> busActions { get; set; } = new Queue<IBusAction>();
        public ObservableCollection<IBusAction> History { get; set; } = new ObservableCollection<IBusAction>();

        public List<IBusAction> actions { get { return busActions.ToList(); } }


        public delegate void TunnelResponseHandler(TunnelResponse response);
        public event TunnelResponseHandler OnTunnelResponse;




        private IBusAction _currentAction;
        public IBusAction CurrentAction
        {
            get { return _currentAction; }
            set { _currentAction = value; Changed("CurrentAction"); }
        }

        private static BusConnection _instance;
        public static BusConnection Instance
        {
            get
            {
                if (_instance == null) _instance = new BusConnection();
                return _instance;
            }
        }


        public BusConnection()
        {
            connection = new METS.Knx.Connection(new IPEndPoint(IPAddress.Parse("192.168.0.108"), Convert.ToInt32(3671)));
            connection.ConnectionChanged += Connection_ConnectionChanged;
            connection.OnTunnelRequest += Connection_OnTunnelRequest;
            Run();
        }

        private void Connection_OnTunnelRequest(TunnelResponse response)
        {
            OnTunnelResponse?.Invoke(response);
        }


        public void IncreaseSequence()
        {
            connection.IncreaseSequence();
        }

        public void SendAsync(IRequestBuilder builder)
        {
            if (!IsConnected)
                throw new Exception("Not connected");

            _ = connection.SendAsync(builder);
        }

        public void SendAsync(byte[] bytes)
        {
            if (!IsConnected)
                throw new Exception("Not connected");

            _ = connection.SendAsync(bytes);
        }

        private void Connection_ConnectionChanged(bool isConnected)
        {
            IsConnected = isConnected;
            ConnectionChanged?.Invoke(isConnected);
        }

        public void Connect()
        {
            connection.Connect();
        }

        public void Disconnect()
        {
            connection.Disconnect();
        }

        private async void Run()
        {
            while (true)
            {
                await Task.Delay(2000);

                if (CurrentAction != null) continue;

                if(busActions.Count == 0)
                {
                    //if (IsConnected) connection.Disconnect();
                    continue;
                }


                IBusAction action = busActions.Dequeue();
                if (action == null) continue;
                CurrentAction = action;
                Changed("actions");
                ExecuteAction();
            }
        }

        private async void ExecuteAction()
        {
            connection.Connect();
            await Task.Delay(500);
            CurrentAction.Finished += CurrentAction_Finished;
            CurrentAction.Run();
        }

        private async void CurrentAction_Finished(object sender, EventArgs e)
        {
            connection.Disconnect();
            await Task.Delay(500);
            History.Insert(0, CurrentAction);
            CurrentAction.Finished -= CurrentAction_Finished;
            CurrentAction = null;
            Changed("actions");
            Changed("CurrentAction");
        }

        public void AddAction(IBusAction action)
        {
            busActions.Enqueue(action);
            Changed("actions");
        }



        private void Changed(string propName)
        {
            try
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propName));
            } catch
            {
                
            }
        }
    }
}
