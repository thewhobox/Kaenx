﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Device.Net;
using Hid.Net.UWP;
using Kaenx.Classes.Bus.Actions;
using Kaenx.Classes.Helper;
using Kaenx.DataContext.Local;
using Kaenx.Konnect;
using Kaenx.Konnect.Builders;
using Kaenx.Konnect.Connections;
using Kaenx.Konnect.Interfaces;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;

namespace Kaenx.Classes.Bus
{
    public class BusConnection :INotifyPropertyChanged
    {
        private ResourceLoader loader = ResourceLoader.GetForCurrentView("Bus");
        private bool _isConnected = false;
        private bool _cancelIsUser = false;

        public bool IsConnected {  
            get { return _isConnected; } 
            set { _isConnected = value; Changed("IsConnected"); }
        }

        public delegate void ConnectionChangedHandler(bool isConnected);
        public event ConnectionChangedHandler ConnectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<IKnxInterface> InterfaceList { get; } = new ObservableCollection<IKnxInterface>();
        public Queue<IBusAction> busActions { get; set; } = new Queue<IBusAction>();
        public ObservableCollection<IBusAction> History { get; set; } = new ObservableCollection<IBusAction>();

        public List<IBusAction> actions { get { return busActions.ToList(); } }



        private IKnxInterface _selectedInterface;
        public IKnxInterface SelectedInterface
        {
            get { return _selectedInterface; }
            set { 
                _selectedInterface = value; 
                Changed("SelectedInterface");
                if (_selectedInterface == null) return;
                Windows.Storage.ApplicationDataContainer container = Windows.Storage.ApplicationData.Current.LocalSettings;
                container.Values["lastUsedInterface"] = _selectedInterface.Hash;
            }
        }
        private IKnxConnection searchConn = new KnxIpTunneling(new IPEndPoint(IPAddress.Parse("224.0.23.12"), 3671));
        private DispatcherTimer searchTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(30) };



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
            Run();


            UWPHidDeviceFactory.Register(new DebugLogger(), new DebugTracer());

            searchConn.OnSearchResponse += SearchConn_OnSearchResponse;
            searchTimer.Tick += (a, b) => SearchForDevices();
            searchTimer.Start();
            Task.Run(async () =>
            {
                await Task.Delay(3000);
                SearchForDevices();
            });

            InterfaceList.CollectionChanged += InterfaceList_CollectionChanged;

            LocalContext _context = new LocalContext();
            foreach(LocalInterface inter in _context.Interfaces)
            {
                InterfaceList.Add(BusInterfaceHelper.GetInterface(inter));
            }
        }

        private void InterfaceList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Windows.Storage.ApplicationDataContainer container = Windows.Storage.ApplicationData.Current.LocalSettings;
            string hash = container.Values["lastUsedInterface"]?.ToString();


            if (hash == null || _selectedInterface != null || !InterfaceList.Any(i => i.Hash == hash)) return;


            IKnxInterface inter = InterfaceList.Single(i => i.Hash == hash);
            SelectedInterface = inter;
        }

        private void SearchConn_OnSearchResponse(Konnect.Responses.SearchResponse response)
        {
            if(InterfaceList.Any(i => i.Hash == response.FriendlyName + "#IP#" + response.endpoint.ToString())) {
                IKnxInterface inter = InterfaceList.Single(i => i.Hash == response.FriendlyName + "#IP#" + response.endpoint.ToString());
                inter.LastFound = DateTime.Now;
            }
            else
            {
                KnxInterfaceIp inter = new KnxInterfaceIp();
                inter.Endpoint = response.endpoint;
                inter.Name = response.FriendlyName;
                inter.LastFound = DateTime.Now;
                _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    InterfaceList.Add(inter);
                });
            }
        }

        private void SearchForDevices()
        {
            Windows.Storage.ApplicationDataContainer container = Windows.Storage.ApplicationData.Current.LocalSettings;
            string hash = container.Values["lastUsedInterface"]?.ToString();
            Debug.WriteLine("Last Interface: " + hash);

            List<IKnxInterface> toDelete = new List<IKnxInterface>();
            foreach(IKnxInterface inter in InterfaceList)
            {
                if (inter.LastFound.Year == 1) continue;
                if ((DateTime.Now - TimeSpan.FromMinutes(2)) > inter.LastFound)
                    toDelete.Add(inter);
            }
            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                foreach (IKnxInterface inter in toDelete)
                    InterfaceList.Remove(inter);
            });

            SearchRequest req = new SearchRequest();
            IPAddress a = GetIpAddress();
            req.Build(new IPEndPoint(GetIpAddress(), (searchConn as KnxIpTunneling).Port));
            searchConn.Send(req, true);

            SearchForHid();
        }

        private async void SearchForHid()
        {
            IEnumerable<ConnectedDeviceDefinition> devices = await DeviceManager.Current.GetConnectedDeviceDefinitionsAsync(new FilterDeviceDefinition() { DeviceType = DeviceType.Hid, UsagePage = 0xFFA0 });

            foreach(ConnectedDeviceDefinition def in devices)
            {
                KnxInterfaceUsb inter = KnxInterfaceUsb.CheckHid(def.VendorId, def.ProductId, def.DeviceId);
                if (inter == null) continue;

                if (InterfaceList.Any(i => i.Hash == inter.Hash))
                    continue;

                _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    InterfaceList.Add(inter);
                });
            }
        }


        public IPAddress GetIpAddress()
        {
            string hostName = Dns.GetHostName();
            foreach(IPAddress addr in Dns.GetHostAddresses(hostName))
            {
                if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return addr;
            }
            return null;
        }


        public void CancelCurrent()
        {
            _cancelIsUser = true;
            _cancelTokenSource?.Cancel();
        }

        private void Connection_ConnectionChanged(bool isConnected)
        {
            IsConnected = isConnected;
            ConnectionChanged?.Invoke(isConnected);
        }

        private async void Run()
        {
            bool alreadyShowedWarning = false;

            while (true)
            {
                await Task.Delay(2000);

                if (CurrentAction != null) continue;


                if(busActions.Count == 0)
                {
                    //if (IsConnected) connection.Disconnect();
                    continue;
                }


                if (SelectedInterface == null)
                {
                    if (!alreadyShowedWarning)
                    {
                        ViewHelper.Instance.ShowNotification("all", loader.GetString("NoInterfaceSelected"), 3000, ViewHelper.MessageType.Error);
                        alreadyShowedWarning = true;
                    }
                    continue;
                }

                alreadyShowedWarning = false;


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


            CurrentAction.Connection = KnxInterfaceHelper.GetConnection(SelectedInterface);
            CurrentAction.Connection.ConnectionChanged += Connection_ConnectionChanged;
            
            CurrentAction.ProgressIsIndeterminate = true;
            CurrentAction.TodoText = loader.GetString("Action_Connecting");

            _cancelTokenSource = new CancellationTokenSource();

            int c = 0;
            while (!CurrentAction.Connection.IsConnected && !_cancelTokenSource.IsCancellationRequested)
            {
                c++;
                await CurrentAction.Connection.Connect();

                if (c == 20)
                {
                    CurrentAction.TodoText = _cancelIsUser ? loader.GetString("Action_Canceled") : loader.GetString("Action_Timeout");
                    CurrentAction_Finished(null, null);
                    return;
                }
            }

            CurrentAction.ProgressIsIndeterminate = false;
            CurrentAction.Finished += CurrentAction_Finished;

            Task runner = Task.Run(() => CurrentAction.Run(_cancelTokenSource.Token), _cancelTokenSource.Token);
            try
            {
                await Task.Delay(30000, _cancelTokenSource.Token);
            } catch { }


            if (!_cancelTokenSource.IsCancellationRequested)
            {
                CurrentAction.TodoText = loader.GetString("Action_TimeoutProc");
                CurrentAction_Finished(null, null);
            } else
            {
                if (_cancelIsUser)
                {
                    CurrentAction.TodoText = loader.GetString("Action_Canceled");
                    CurrentAction_Finished(null, null);
                }
            }
        }

        private async void CurrentAction_Finished(IBusAction sender, object data)
        {
            _cancelTokenSource?.Cancel();
            await CurrentAction.Connection.Disconnect();
            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                History.Insert(0, CurrentAction);
                try
                {
                    if(CurrentAction != null)
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
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
