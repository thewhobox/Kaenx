using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kaenx.Classes.Bus.Actions;
using Kaenx.Konnect.Builders;

namespace Kaenx.Classes.Bus
{
    public class BusConnection :INotifyPropertyChanged
    {
        private bool _isConnected = false;
        private bool _cancelIsUser = false;

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

        private string _currentProgressText = "Getrennt";
        public string CurrentProgressText
        {
            get { return _currentProgressText; }
            set { _currentProgressText = value; Changed("CurrentProgressText"); }
        }


        public BusConnection()
        {
            Run();
        }

        private void Connection_OnTunnelRequest(TunnelResponse response)
        {
            OnTunnelResponse?.Invoke(response);
        }

        public void CancelCurrent()
        {
            _cancelIsUser = true;
            _cancelTokenSource?.Cancel();
        }

        //public void SendAsync(IRequestBuilder builder)
        //{
        //    if (!IsConnected)
        //        throw new Exception("Not connected");

        //    _ = connection.SendAsync(builder);
        //}

        //public void SendAsync(byte[] bytes)
        //{
        //    if (!IsConnected)
        //        throw new Exception("Not connected");

        //    _ = connection.SendAsync(bytes);
        //}

        private void Connection_ConnectionChanged(bool isConnected)
        {
            IsConnected = isConnected;
            ConnectionChanged?.Invoke(isConnected);
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
                _cancelIsUser = false;
                ExecuteAction();
            }
        }

        private CancellationTokenSource _cancelTokenSource;

        private async void ExecuteAction()
        {
            CurrentAction.Connection = new Kaenx.Konnect.Connection(new IPEndPoint(IPAddress.Parse("192.168.0.108"), Convert.ToInt32(3671)));
            CurrentAction.Connection.ConnectionChanged += Connection_ConnectionChanged;
            
            CurrentAction.ProgressIsIndeterminate = true;
            CurrentProgressText = "Verbindung wird hergestellt...";
            CurrentAction.TodoText = "Verbindung wird hergestellt...";
            _cancelTokenSource = new CancellationTokenSource();

            int c = 0;
            while (!CurrentAction.Connection.IsConnected && !_cancelTokenSource.IsCancellationRequested)
            {
                c++;
                CurrentAction.Connection.Connect();
                await Task.Delay(500);
                if (c == 20)
                {
                    CurrentAction.TodoText = _cancelIsUser ? "Wurde abgebrochen" : "Connect Timeout (10s)";
                    CurrentAction_Finished(null, null);
                    return;
                }
            }
            CurrentProgressText = "Verbunden";

            CurrentAction.ProgressIsIndeterminate = false;
            CurrentAction.Finished += CurrentAction_Finished;

            Task runner = Task.Run(() => CurrentAction.Run(_cancelTokenSource.Token), _cancelTokenSource.Token);
            try
            {
                await Task.Delay(30000, _cancelTokenSource.Token);
            } catch { }


            if (!_cancelTokenSource.IsCancellationRequested)
            {
                CurrentAction.TodoText = "Process Timeout (30s)";
                CurrentAction_Finished(null, null);
            } else
            {
                if (_cancelIsUser)
                {
                    CurrentAction.TodoText = "Wurde abgebrochen";
                    CurrentAction_Finished(null, null);
                }
            }
        }

        private async void CurrentAction_Finished(IBusAction sender, object data)
        {
            _cancelTokenSource?.Cancel();
            CurrentAction.Connection.Disconnect();
            CurrentProgressText = "Getrennt";
            await Task.Delay(500);
            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                History.Insert(0, CurrentAction);
                try
                {
                    CurrentAction.Finished -= CurrentAction_Finished;
                } catch { }
                CurrentAction = null;
                Changed("CurrentAction");
            });
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
                _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
                });
            }
        }
    }
}
