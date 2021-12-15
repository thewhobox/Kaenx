using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Device.Net;
using Hid.Net.Windows;
using Kaenx.Classes.Bus.Actions;
using Kaenx.Classes.Helper;
using Kaenx.DataContext.Local;
using Kaenx.Konnect;
using Kaenx.Konnect.Builders;
using Kaenx.Konnect.Connections;
using Kaenx.Konnect.Interfaces;
using Kaenx.Konnect.Messages.Request;
using Kaenx.Konnect.Messages.Response;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Usb.Net.Windows;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.UI.Xaml;
using Hid.Net.UWP;

namespace Kaenx.Classes.Bus
{
    public class BusConnection :INotifyPropertyChanged
    {
        private ResourceLoader loader = ResourceLoader.GetForCurrentView("BusConn");
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
        public int queueCount { get { return busActions.Count; } }
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
        private KnxIpRouting searchConn;
        private DispatcherTimer searchTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(10) };
        private IDeviceFactory hidFactory = new FilterDeviceDefinition(vendorId: 0x147b).CreateUwpHidDeviceFactory();


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



            try
            {
                searchConn = new KnxIpRouting();
                searchConn.OnSearchResponse += SearchConn_OnSearchResponse;
            }
            catch(System.Net.Sockets.SocketException ex)
            {
                searchConn = null;
                string errorText = "Es besteht keine Verbindung zu einenm lokalen Netzwerk, somit können auch keine IP-Schnittstellen gefunden werden.";

                if (ex.ErrorCode == 10013)
                    errorText = "Kaenx kann keinen Socket aufbauen. Bitte starten Sie den Rechner neu.";

                ViewHelper.Instance.ShowNotification("main", errorText, 6000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
            }
            catch
            {
                searchConn = null;
                ViewHelper.Instance.ShowNotification("main", "Es besteht keine Verbindung zu einenm lokalen Netzwerk, somit können auch keine IP-Schnittstellen gefunden werden.", 6000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
            }

            searchTimer.Tick += (a, b) => SearchForDevices();
            searchTimer.Start();
            SearchForDevices();


            BusRemoteConnection.Instance.Remote.OnRequestInterface += Instance_OnRequestInterface;
            BusRemoteConnection.Instance.Remote.OnRequest += ConnectionOut_OnRequest;
            BusRemoteConnection.Instance.Remote.OnResponse += Instance_OnResponse;

            InterfaceList.CollectionChanged += InterfaceList_CollectionChanged;

            LocalContext _context = new LocalContext();
            foreach(LocalInterface inter in _context.Interfaces)
            {
                InterfaceList.Add(BusInterfaceHelper.GetInterface(inter));
            }
        }

        private IKnxInterface Instance_OnRequestInterface(string hash)
        {
            return InterfaceList.Single(i => i.Hash == hash);
        }

        private void Instance_OnResponse(Konnect.Remote.IRemoteMessage message)
        {
            if(message is Konnect.Remote.SearchResponse)
            {
                Konnect.Remote.SearchResponse resp = message as Konnect.Remote.SearchResponse;

                foreach(IKnxInterface inter in resp.Interfaces)
                {
                    KnxInterfaceRemote xinter = new KnxInterfaceRemote(inter.Description, inter.Hash);
                    xinter.LastFound = DateTime.Now;
                    xinter.Name = inter.Name;
                    if (!InterfaceList.Any(i => i.Hash == xinter.Hash))
                    {
                        _=App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            InterfaceList.Add(xinter);
                        });
                    }
                    else
                    {
                        IKnxInterface i = InterfaceList.Single(i => i.Hash == inter.Hash);
                        i.LastFound = DateTime.Now;
                    }
                }
            }
        }

        private void ConnectionOut_OnRequest(Konnect.Remote.IRemoteMessage message)
        {
            if(message is Kaenx.Konnect.Remote.SearchRequest)
            {
                Konnect.Remote.SearchResponse resp = new Konnect.Remote.SearchResponse();
                resp.SequenceNumber = message.SequenceNumber;
                resp.ChannelId = message.ChannelId;
                resp.Interfaces = InterfaceList.Where(inter => !inter.IsRemote).ToList();
                _ = BusRemoteConnection.Instance.Remote.Send(resp, false);
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

        private void SearchConn_OnSearchResponse(MsgSearchRes response)

        {
            if(InterfaceList.Any(i => i.Hash == response.FriendlyName + "#IP#" + response.Endpoint.ToString())) {
                IKnxInterface inter = InterfaceList.Single(i => i.Hash == response.FriendlyName + "#IP#" + response.Endpoint.ToString());
                inter.LastFound = DateTime.Now;
            }
            else
            {
                KnxInterfaceIp inter = new KnxInterfaceIp();
                inter.IP = response.Endpoint.Address.ToString();
                inter.Port = response.Endpoint.Port;
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
            List<IKnxInterface> toDelete = new List<IKnxInterface>();
            foreach(IKnxInterface inter in InterfaceList)
            {
                if (inter.LastFound.Year == 1) continue;
                if ((DateTime.Now - TimeSpan.FromMinutes(1)) > inter.LastFound)
                    toDelete.Add(inter);
            }
            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                foreach (IKnxInterface inter in toDelete)
                    InterfaceList.Remove(inter);
            });


            if(searchConn != null)
            {
                MsgSearchReq msg = new MsgSearchReq();
                searchConn.Send(msg, true);
            }


            SearchForHid();

            if (BusRemoteConnection.Instance.Remote.IsConnected)
                _=BusRemoteConnection.Instance.Remote.Send(new Konnect.Remote.SearchRequest() { ChannelId = BusRemoteConnection.Instance.Remote.ChannelId });
        }

        private async void SearchForHid()
        {
            var devices = await hidFactory.GetConnectedDeviceDefinitionsAsync().ConfigureAwait(false);

            foreach (ConnectedDeviceDefinition def in devices)
            {
                KnxInterfaceUsb inter = KnxInterfaceUsb.CheckHid(def);
                if (inter == null) continue;

                if (InterfaceList.Any(i => i.Hash == inter.Hash))
                    continue;

                _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    InterfaceList.Add(inter);
                });

            }
        }


        public List<IPAddress> GetIpAddresses()
        {
            string hostName = Dns.GetHostName();
            return Dns.GetHostAddresses(hostName).Where(h => h.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToList();
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
                        ViewHelper.Instance.ShowNotification("all", loader.GetString("NoInterfaceSelected"), 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                        alreadyShowedWarning = true;
                    }
                    continue;
                }

                alreadyShowedWarning = false;


                IBusAction action = busActions.Dequeue();
                if (action == null) continue;
                CurrentAction = action;
                Changed("actions");
                Changed("queueCount");
                _cancelIsUser = false;
                ExecuteAction();
            }
        }

        private CancellationTokenSource _cancelTokenSource;


        public async Task<IDevice> GetDevice(KnxInterfaceUsb inter)
        {
            return await hidFactory.GetDeviceAsync(inter.ConnDefinition);
        }


        private async void ExecuteAction()
        {
            CurrentAction.Connection = await KnxInterfaceHelper.GetConnection(SelectedInterface, BusRemoteConnection.Instance.Remote, GetDevice);
            CurrentAction.Connection.ConnectionChanged += Connection_ConnectionChanged;
            
            CurrentAction.ProgressIsIndeterminate = true;
            CurrentAction.TodoText = loader.GetString("Action_Connecting");

            _cancelTokenSource = new CancellationTokenSource();

            int c = 0;
            while (!CurrentAction.Connection.IsConnected && !_cancelTokenSource.IsCancellationRequested)
            {
                c++;
                try
                {
                    await CurrentAction.Connection.Connect();
                }
                catch { }
                if(CurrentAction.Connection is KnxRemote)
                    await Task.Delay(2000);
                else
                    await Task.Delay(500);

                if (c == 3)
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
                await Task.Delay(60000000, _cancelTokenSource.Token);
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
            Changed("queueCount");
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
