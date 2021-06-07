using Kaenx.Classes;
using Kaenx.Classes.Buildings;
using Kaenx.Classes.Bus;
using Kaenx.Classes.Dynamic;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Local;
using Kaenx.DataContext.Project;
using Kaenx.Konnect;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Connections;
using Kaenx.Konnect.Interfaces;
using Kaenx.Konnect.Messages.Request;
using Kaenx.View.Controls.Dialogs;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class Reconstruct : Page, INotifyPropertyChanged
    {
        private string _action;
        public string Action
        {
            get { return _action; }
            set { _action = value; Changed("Action"); }
        }

        private bool _cando;
        public bool CanDo
        {
            get { return _cando; }
            set { _cando = value; Changed("CanDo"); }
        }

        private int _progMax = 1;
        public int ProgMax
        {
            get { return _progMax; }
            set { _progMax = value; Changed("ProgMax"); }
        }

        private int _progValue = 1;
        public int ProgValue
        {
            get { return _progValue; }
            set { _progValue = value; Changed("ProgValue"); }
        }

        private bool _progIndet = false;
        public bool ProgIndet
        {
            get { return _progIndet; }
            set { _progIndet = value; Changed("ProgIndet"); }
        }


        public ObservableCollection<ReconstructDevice> Devices { get; set; } = new ObservableCollection<ReconstructDevice>();

        private bool _stop = false;
        private Kaenx.DataContext.Project.ProjectContext _contextP;
        private Project _project;
        private BusConnection conn = new BusConnection();
        private Dictionary<string, string> manus = new Dictionary<string, string>();
        private CatalogContext _context = new CatalogContext();
            

        public Reconstruct()
        {
            this.InitializeComponent();
            GridInterfaces.DataContext = conn;
            this.DataContext = this;
            Init();

        }

        private void CurrentView_BackRequested(object sender, BackRequestedEventArgs e)
        {
            App.Navigate(typeof(MainPage));
            e.Handled = true;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _project = e.Parameter as Project;
            ApplicationView.GetForCurrentView().Title = _project.Name + " - Rekonstruktion";

            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            currentView.BackRequested += CurrentView_BackRequested;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.BackRequested -= CurrentView_BackRequested;
        }

        private async void Init()
        {
            XDocument master = await GetKnxMaster();
            foreach (XElement ele in master.Descendants(XName.Get("Manufacturer", master.Root.Name.NamespaceName)))
                manus.Add(ele.Attribute("Id").Value, ele.Attribute("Name").Value);



            foreach(Line linema in SaveHelper._project.Lines)
            {
                foreach(LineMiddle linemi in linema.Subs)
                {
                    linemi.Parent = linema;
                    foreach(LineDevice ldev in linemi.Subs)
                    {
                        ldev.Parent = linemi;
                        ReconstructDevice dev = new ReconstructDevice();
                        dev.Address = UnicastAddress.FromString(ldev.LineName);
                        dev.Serial = ldev.SerialText;
                        string[] names = ldev.Name.Split("_");
                        dev.DeviceName = names[0];

                        dev.ApplicationName = names[1];
                        dev.ApplicationId = ldev.ApplicationId;

                        //Todod check ändern und auf aktuellen Stand bringen
                        if(dev.ApplicationName.Contains(" 0x"))
                        {
                            dev.StateId = 2;
                            dev.Status = "In Projekt (Unvollständig)";
                        } else
                        {
                            dev.StateId = 3;
                            dev.Status = "In Projekt";
                            dev.CanRead = true;
                        }

                        dev.LineDevice = ldev;
                        dev.Manufacturer = manus[dev.ApplicationId.Substring(0, 6)];
                        Devices.Add(dev);
                    }
                }
            }



            CanDo = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ClickScanStart(object sender, RoutedEventArgs e)
        {
            if(conn.SelectedInterface == null)
            {
                ShowNotification("Bitte wählen Sie erst eine Schnittstelle aus.", InfoBarSeverity.Warning);
                return;
            }
            DoScan();
        }

        private async void DoScan()
        {
            CanDo = false;

            DiagSelectLines diag = new DiagSelectLines();
            await diag.ShowAsync();

            if (diag.Patterns.Count == 0)
            {
                CanDo = true;
                return;
            }

            List<UnicastAddress> addresses = new List<UnicastAddress>();

            foreach (SearchPattern pattern in diag.Patterns)
                foreach (UnicastAddress addr in pattern.GetAddresses())
                    if (!addresses.Any(a => a.ToString() == addr.ToString()))
                        addresses.Add(addr);

            ProgMax = addresses.Count;
            ProgValue = 0;
            IKnxConnection _conn = await KnxInterfaceHelper.GetConnection(conn.SelectedInterface, BusRemoteConnection.Instance.Remote, BusConnection.Instance.GetDevice);

            try
            {
                await _conn.Connect();
            } catch(Exception ex)
            {
                ShowNotification(ex.Message, InfoBarSeverity.Error);
                CanDo = true;
                return;
            }
             
            _conn.OnTunnelRequest += _conn_OnTunnelRequest;



            foreach (UnicastAddress addr in addresses)
            {
                if (_stop) break;
                Action = $"Scanne Linie: {addr.Area}.{addr.Line}.x";
                BusDevice dev = new BusDevice(addr, _conn);
                await dev.Connect(true);
                ProgValue++;
            }

            if (CheckStop(_conn)) return;
            ProgIndet = true;
            Action = "Warten auf Geräte...";
            await Task.Delay(15000);


            if (CheckStop(_conn)) return;
            Action = "Lese Daten aus Geräten...";
            await Task.Delay(1000);

            ProgMax = Devices.Where(d => d.StateId == 0).Count();
            ProgValue = 0;

            while (true)
            {
                if (CheckStop(_conn)) return;
                ProgValue++;

                ReconstructDevice device;
                try
                {
                    device = Devices.First(d => d.StateId == 0);
                }
                catch { break; }

                try
                {
                    await ReadDevice(device, _conn);
                }
                catch (OperationCanceledException e)
                {
                    device.Status = "Fehler 0x01";
                    device.StateId = -1;
                    continue;
                }
                catch(Exception ex)
                {
                    device.Status = "Fehler 0x00";
                    device.StateId = -1;
                    Serilog.Log.Error(ex, "Fehler beim Auslesen von Gerät");
                    continue;
                }

                if (CheckStop(_conn)) return;
                try
                {
                    SaveDevice(device);
                }
                catch
                {
                    device.Status = "Fehler 0x04";
                    device.StateId = -1;
                }
            }

            await _conn.Disconnect();
            Action = "Speichern";
            SaveHelper.SaveProject();
            Action = "Fertig!";
            CanDo = true;
        }

        private bool CheckStop(IKnxConnection _conn = null)
        {
            if (_stop)
            {
                if (_conn != null)
                    _conn.Disconnect();
                Action = "Abgebrochen";
                CanDo = true;
            }

            return _stop;
        }

        private async Task ReadDevice(ReconstructDevice device, IKnxConnection _conn)
        {
            device.Status = "Lese Info...";
            BusDevice dev = new BusDevice(device.Address, _conn);
            await dev.Connect(true);

            string appId = await dev.RessourceRead<string>("ApplicationId");

            if (appId.Length == 8)
            {
                appId = "00" + appId;
                device.Manufacturer = manus["M-" + appId.Substring(0, 4)];
            }
            else
                device.Manufacturer = manus["M-" + appId.Substring(0, 4)];

            appId = $"M-{appId.Substring(0, 4)}_A-{appId.Substring(4, 4)}-{appId.Substring(8, 2)}";

            try
            {
                Hardware2AppModel h2a = _context.Hardware2App.First(h => h.ApplicationId == appId);
                device.ApplicationName = h2a.Name;
                device.ApplicationId = h2a.ApplicationId;
                DeviceViewModel devm = _context.Devices.First(d => d.HardwareId == h2a.HardwareId);
                device.DeviceName = devm.Name;
                device.CanRead = true;
            }
            catch
            {
                Debug.WriteLine(device.Address.ToString() + ": " + appId);
                if (string.IsNullOrEmpty(device.ApplicationName))
                {
                    device.ApplicationName = appId;
                    device.ApplicationId = appId;
                }
                device.DeviceName = "Applikation nicht im Katalog";
            }



            try
            {
                device.SerialBytes = await dev.RessourceRead("DeviceSerialNumber");
                device.Serial = BitConverter.ToString(device.SerialBytes).Replace("-", "");
            }
            catch (NotSupportedException ex)
            {
                device.Serial = "Wird nicht unterstützt";
            }
            catch(TimeoutException ex)
            {
                device.Serial = "Timeout";
            }
            catch {
                device.Serial = "Fehler 0x02";
            }

            device.StateId = 2;
            device.Status = "Infos gelesen";
        }

        private void SaveDevice(ReconstructDevice device)
        {
            Line line;
            if (SaveHelper._project.Lines.Any(l => l.Id == device.Address.Area))
            {
                line = SaveHelper._project.Lines.Single(l => l.Id == device.Address.Area);
            } else
            {
                line = new Line(device.Address.Area, "Neue Linie");
                SaveHelper._project.Lines.Add(line);
            }

            LineMiddle lineM;
            if (line.Subs.Any(l => l.Id == device.Address.Line))
                lineM = line.Subs.Single(l => l.Id == device.Address.Line);
            else
            {
                lineM = new LineMiddle(device.Address.Line, "Neue Linie", line);
                line.Subs.Add(lineM);
            }
            lineM.Parent = line;

            LineDevice lined;
            if (lineM.Subs.Any(d => d.Id == device.Address.DeviceAddress))
                lined = lineM.Subs.Single(d => d.Id == device.Address.DeviceAddress);
            else
            {
                lined = new LineDevice(true);
                lineM.Subs.Add(lined);
            }

            lined.Parent = lineM;
            lined.Serial = device.SerialBytes;
            lined.ApplicationId = device.ApplicationId;
            lined.Id = device.Address.DeviceAddress;
            lined.Name = device.DeviceName + "_" + device.ApplicationName;
            lined.LoadedPA = true;

            if(!device.ApplicationName.Contains(" 0x"))
            {
                device.StateId = 3;
                device.Status = "In Projekt";
            } else
            {
                device.Status = "In Projekt (Unvollständig)";
            }
            device.LineDevice = lined;
        }

        private void _conn_OnTunnelRequest(IMessageRequest response)
        {
            if(response.ApciType == Konnect.Messages.ApciTypes.Disconnect)
            {
                if (Devices.Any(d => d.Address.ToString() == response.SourceAddress.ToString())) return;

                ReconstructDevice dev = new ReconstructDevice();
                dev.Address = (UnicastAddress)response.SourceAddress;
                dev.Status = "Gefunden";
                _= App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Devices.Add(dev);
                });
            }
        }

        private async void ClickReadStart(object sender, RoutedEventArgs e)
        {
            Action = "Starte Konfiguration auslesen...";
            CanDo = false;

            IKnxConnection _conn = await KnxInterfaceHelper.GetConnection(conn.SelectedInterface, BusRemoteConnection.Instance.Remote, BusConnection.Instance.GetDevice);

            try
            {
                await _conn.Connect();
            }
            catch (Exception ex)
            {
                ShowNotification(ex.Message, InfoBarSeverity.Error);
                CanDo = true;
                return;
            }

            IEnumerable<ReconstructDevice> devices = Devices.Where(d => d.CanRead);
            foreach (ReconstructDevice device in devices)
            {
                Action = device.Address + " - Lese Konfiguration";
                await StartReadConfig(device, _conn);
            }

            Action = "Fertig!";
            CanDo = true;
        }

        private void ClickToProject(object sender, RoutedEventArgs e)
        {
            SaveHelper.SaveProject();
            SaveHelper.SaveStructure();
            SaveHelper._project.Local.IsReconstruct = false;
            LocalContext con = new LocalContext();
            con.Projects.Update(SaveHelper._project.Local);
            con.SaveChanges();
            App.Navigate(typeof(WorkdeskEasy), SaveHelper._project);
        }

        private void ClickCancel(object sender, RoutedEventArgs e)
        {
            _stop = true;
        }


        private async Task<XDocument> GetKnxMaster()
        {
            StorageFile file = null;
            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync("knx_master.xml");
            }
            catch
            {
                StorageFile defaultFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Data/knx_master.xml"));
                file = await ApplicationData.Current.LocalFolder.CreateFileAsync("knx_master.xml");
                await FileIO.WriteTextAsync(file, await FileIO.ReadTextAsync(defaultFile));
            }

            if (file == null) return null;

            return XDocument.Load(await file.OpenStreamForReadAsync());
        }

        private void ShowNotification(string text, InfoBarSeverity severity)
        {
            InfoBar info = new InfoBar
            {
                Severity = severity,
                Message = text,
                Title = "Information",
                IsOpen = true
            };
            info.CloseButtonClick += Info_CloseButtonClick;

            switch (severity)
            {
                case InfoBarSeverity.Error:
                    info.Title = "Fehler!";
                    break;

                case InfoBarSeverity.Success:
                    info.Title = "Erfolgreich";
                    break;

                case InfoBarSeverity.Warning:
                    info.Title = "Warnung";
                    break;
            }

            InfoPanel.Children.Add(info);
        }

        private void Info_CloseButtonClick(InfoBar sender, object args)
        {
            sender.CloseButtonClick -= Info_CloseButtonClick;
            InfoPanel.Children.Remove(sender);
        }




        #region Read DeviceConfig


        private BusDevice _currentBusDevice { get; set; }
        private ReconstructDevice _currentDevice { get; set; }
        private List<int> connectedCOs = new List<int>();
        private Dictionary<string, string> defParas = new Dictionary<string, string>();
        private Dictionary<int, List<string>> CO2GA = new Dictionary<int, List<string>>();
        private Dictionary<string, byte[]> mems = new Dictionary<string, byte[]>();


        private async Task StartReadConfig(ReconstructDevice device, IKnxConnection _conn)
        {
            connectedCOs.Clear();
            CO2GA.Clear();
            _currentDevice = device;
            _currentBusDevice = new BusDevice(device.Address, _conn);
            await _currentBusDevice.Connect();

            device.Status = "Lese Maskenversion...";

            await Task.Delay(100);

            string maskVersion = "MV-" + await _currentBusDevice.DeviceDescriptorRead();
            CatalogContext context = new CatalogContext();

            device.Status = "Lese Assoziationstabelle...";
            await Task.Delay(100);

            ApplicationViewModel appModel = null;

            if (context.Applications.Any(a => a.Id == device.ApplicationId))
            {
                appModel = context.Applications.Single(a => a.Id == device.ApplicationId);
            }

            if (appModel != null)
            {
                List<string> addresses = new List<string>();
                if (!string.IsNullOrEmpty(appModel.Table_Group))
                {
                    AppSegmentViewModel segmentModel = context.AppSegments.Single(s => s.Id == appModel.Table_Group);
                    int groupAddr = segmentModel.Address;
                    byte[] datax = await _currentBusDevice.MemoryRead(groupAddr, 1);

                    int length = Convert.ToInt16(datax[0]) - 1;
                    datax = await _currentBusDevice.MemoryRead(groupAddr + 3, length * 2);

                    for (int i = 0; i < (datax.Length / 2); i++)
                    {
                        int offset = i * 2;
                        addresses.Add(MulticastAddress.FromByteArray(new byte[] { datax[offset], datax[offset + 1] }).ToString());
                    }
                }

                if (!string.IsNullOrEmpty(appModel.Table_Assosiations))
                {
                    AppSegmentViewModel segmentModel = context.AppSegments.Single(s => s.Id == appModel.Table_Assosiations);
                    int assoAddr = segmentModel.Address;

                    byte[] datax = await _currentBusDevice.MemoryRead(assoAddr, 1);
                    if (datax.Length > 0)
                    {
                        int length = Convert.ToInt16(datax[0]);

                        datax = await _currentBusDevice.MemoryRead(assoAddr + 1, length * 2);
                        List<Classes.Bus.Data.AssociationHelper> table = new List<Classes.Bus.Data.AssociationHelper>();
                        for (int i = 0; i < length; i++)
                        {
                            int offset = i * 2;
                            int grpIndex = datax[offset];
                            int COnr = datax[offset + 1];
                            string grp = addresses[grpIndex - 1];

                            if (!CO2GA.ContainsKey(COnr))
                                CO2GA.Add(COnr, new List<string>());

                            if (!CO2GA[COnr].Contains(grp))
                                CO2GA[COnr].Add(grp);

                            if (!connectedCOs.Contains(COnr))
                                connectedCOs.Add(COnr);
                        }
                    }
                }
            }


            //ProgressValue = 40;
            device.Status = "Berechne Konfiguration...";
            await GetConfig(device.ApplicationId);
        }

        private async Task GetConfig(string appId)
        {
            defParas.Clear();
            Dictionary<string, AppParameter> paras = new Dictionary<string, AppParameter>();
            Dictionary<string, AppParameterTypeViewModel> types = new Dictionary<string, AppParameterTypeViewModel>();

            foreach (AppParameter param in _context.AppParameters.Where(p => p.ApplicationId == appId))
            {
                paras.Add(param.Id, param);
                defParas.Add(param.Id, param.Value);
            }

            AppAdditional adds = _context.AppAdditionals.Single(a => a.Id == appId);

            foreach (AppParameter para in paras.Values)
            {
                AppParameterTypeViewModel paraT;
                if (types.ContainsKey(para.ParameterTypeId))
                    paraT = types[para.ParameterTypeId];
                else
                {
                    paraT = _context.AppParameterTypes.Single(t => t.Id == para.ParameterTypeId);
                    types.Add(paraT.Id, paraT);
                }
                if (paraT.Size == 0) continue;

                switch (para.SegmentType)
                {
                    case SegmentTypes.None:
                        await HandleParamGhost(para, types[para.ParameterTypeId], adds);
                        break;

                    case SegmentTypes.Memory:
                        para.Value = await GetValueFromMem(para, paraT);
                        break;

                    case SegmentTypes.Property:
                        //TODO implement also Properties
                        break;
                }
            }

            await SaveConfig(adds, paras);
        }


        private async Task HandleParamGhost(AppParameter para, AppParameterTypeViewModel paraT, AppAdditional adds)
        {
            XDocument dynamic = XDocument.Parse(System.Text.Encoding.UTF8.GetString(adds.Dynamic));

            string ns = dynamic.Root.Name.NamespaceName;
            IEnumerable<XElement> chooses = dynamic.Descendants(XName.Get("choose", ns)).Where(c => c.Attribute("ParamRefId") != null && SaveHelper.ShortId(c.Attribute("ParamRefId").Value) == para.Id);

            foreach (XElement choose in chooses)
            {
                Dictionary<string, List<int>> value2Coms = new Dictionary<string, List<int>>();

                #region Get ComIds and remove duplicates
                foreach (XElement when in choose.Elements())
                {
                    if (when.Attribute("default")?.Value == "true") continue;
                    string val = when.Attribute("test").Value;

                    if (val.Contains(">") || val.Contains("<") || val.Contains("=") || val.Contains(" ")) continue;
                    if (!value2Coms.ContainsKey(val)) value2Coms[val] = new List<int>();

                    IEnumerable<XElement> tlist = when.Descendants(XName.Get("ComObjectRefRef", ns));
                    foreach (XElement comx in tlist)
                    {
                        string id = SaveHelper.ShortId(comx.Attribute("RefId").Value);
                        AppComObject com = _context.AppComObjects.Single(c => c.Id == id);

                        if (!value2Coms[val].Contains(com.Number))
                            value2Coms[val].Add(com.Number);
                    }
                }

                List<int> toDelete = new List<int>();
                foreach (string keyval in value2Coms.Keys)
                {
                    List<int> xids = value2Coms[keyval];


                    foreach (int xid in xids)
                    {
                        bool flag = false;

                        foreach (KeyValuePair<string, List<int>> otherids in value2Coms.Where(x => x.Key != keyval))
                            if (otherids.Value.Contains(xid))
                                flag = true;

                        if (flag) toDelete.Add(xid);
                    }
                }


                foreach (int xid in toDelete)
                {
                    foreach (KeyValuePair<string, List<int>> otherids in value2Coms)
                        otherids.Value.Remove(xid);
                }
                #endregion


                //Hier weiter
                Dictionary<string, bool> val2success = new Dictionary<string, bool>();

                foreach (KeyValuePair<string, List<int>> coms in value2Coms)
                {
                    val2success[coms.Key] = false;

                    foreach (int comNr in coms.Value)
                    {
                        if (connectedCOs.Contains(comNr))
                        {
                            val2success[coms.Key] = true;
                        }
                    }
                }


                if (val2success.Count(y => y.Value == true) == 1)
                {
                    KeyValuePair<string, bool> success = val2success.Single(y => y.Value == true);
                    para.Value = success.Key;
                }
                else
                {
                    await HandleParamGhost2(para, paraT, adds, choose);
                }

            }
        }

        private async Task HandleParamGhost2(AppParameter para, AppParameterTypeViewModel paraT, AppAdditional adds, XElement choose)
        {
            string ns = choose.Name.NamespaceName;
            Dictionary<string, List<string>> value2Paras = new Dictionary<string, List<string>>();
            Dictionary<string, string> test = new Dictionary<string, string>();

            #region Get ParaIds and remove duplicates
            foreach (XElement when in choose.Elements())
            {
                if (when.Attribute("default")?.Value == "true" || when.Attribute("default")?.Value == para.Value) continue;
                string val = when.Attribute("test").Value;

                if (val.Contains(">") || val.Contains("<") || val.Contains("=") || val.Contains(" ")) continue;
                if (!value2Paras.ContainsKey(val)) value2Paras[val] = new List<string>();

                IEnumerable<XElement> tlist = when.Descendants(XName.Get("ParameterRefRef", ns));
                foreach (XElement comx in tlist)
                {
                    string id = SaveHelper.ShortId(comx.Attribute("RefId").Value);
                    AppParameter par = _context.AppParameters.Single(c => c.Id == id);
                    if (par.Access != AccessType.Full || par.SegmentId == null) continue;



                    string oldVal = defParas[par.Id];

                    AppParameterTypeViewModel partype = _context.AppParameterTypes.Single(pt => pt.Id == par.ParameterTypeId);
                    string newVal = await GetValueFromMem(par, partype);

                    if (oldVal != newVal && !value2Paras[val].Contains(id))
                    {
                        test.Add(par.Id, newVal);
                        value2Paras[val].Add(id);
                    }
                }
            }

            List<string> toDelete = new List<string>();
            foreach (string keyval in value2Paras.Keys)
            {
                List<string> xids = value2Paras[keyval];


                foreach (string xid in xids)
                {
                    bool flag = false;

                    foreach (KeyValuePair<string, List<string>> otherids in value2Paras.Where(x => x.Key != keyval))
                        if (otherids.Value.Contains(xid))
                            flag = true;

                    if (flag) toDelete.Add(xid);
                }
            }


            foreach (string xid in toDelete)
            {
                foreach (KeyValuePair<string, List<string>> otherids in value2Paras)
                    otherids.Value.Remove(xid);
            }
            #endregion


            Dictionary<string, bool> val2success = new Dictionary<string, bool>();

            foreach (KeyValuePair<string, List<string>> coms in value2Paras)
            {
                val2success[coms.Key] = coms.Value.Count > 0;
            }


            if (val2success.Count(y => y.Value == true) == 1)
            {
                KeyValuePair<string, bool> success = val2success.Single(y => y.Value == true);
                para.Value = success.Key;
            }
        }

        private async Task<string> GetValueFromMem(AppParameter para, AppParameterTypeViewModel paraT)
        {
            if (para.SegmentId == null) return "";
            if (!mems.ContainsKey(para.SegmentId))
            {
                AppSegmentViewModel seg = _context.AppSegments.Single(s => s.Id == para.SegmentId);
                byte[] temp = await _currentBusDevice.MemoryRead(seg.Address, seg.Size);
                mems.Add(para.SegmentId, temp);
            }


            byte[] bdata = null;
            int sizeB;

            if (paraT.Size % 8 == 0)
            {
                sizeB = paraT.Size / 8;
                bdata = new byte[sizeB];
                for (int i = 0; i < sizeB; i++)
                {
                    bdata[i] = mems[para.SegmentId][para.Offset + i];
                }
            }
            else
            {
                sizeB = 1;
                bdata = new byte[sizeB];
                byte temp = mems[para.SegmentId][para.Offset];

                var y = new BitArray(new byte[] { temp });
                var z = new BitArray(8);

                for (int i = 0; i < paraT.Size; i++)
                {
                    z.Set(i, y.Get(i + para.OffsetBit));
                }
                z.CopyTo(bdata, 0);
            }


            switch (paraT.Type)
            {
                case ParamTypes.Enum:
                case ParamTypes.NumberInt:
                case ParamTypes.NumberUInt:
                    int x = 0;
                    switch (bdata.Length)
                    {
                        case 1:
                            x = BitConverter.ToInt16(new byte[2] { bdata[0], 0 }, 0);
                            break;

                        case 2:
                            Array.Reverse(bdata);
                            x = BitConverter.ToInt16(bdata, 0);
                            break;

                        case 3:
                            Array.Reverse(bdata);
                            x = BitConverter.ToInt32(new byte[4] { bdata[0], bdata[1], bdata[2], 0 }, 0);
                            break;

                        case 4:
                            Array.Reverse(bdata);
                            x = BitConverter.ToInt32(bdata, 0);
                            break;
                    }

                    return x.ToString();

                case ParamTypes.Text:
                    return System.Text.Encoding.UTF8.GetString(bdata);
            }
            return "";
        }


        private async Task SaveConfig(AppAdditional adds, Dictionary<string, AppParameter> paras)
        {
            _currentDevice.Status = "Speichere Konfiguration...";

            Dictionary<string, ViewParamModel> Id2Param = new Dictionary<string, ViewParamModel>();
            Dictionary<string, ChangeParamModel> ParaChanges = new Dictionary<string, ChangeParamModel>();
            Dictionary<string, ViewParamModel> VisibleParams = new Dictionary<string, ViewParamModel>();
            List<IDynChannel> Channels = SaveHelper.ByteArrayToObject<List<IDynChannel>>(adds.ParamsHelper, true);
            ProjectContext _c = new ProjectContext(SaveHelper.connProject);

            if (_c.ChangesParam.Any(c => c.DeviceId == _currentDevice.LineDevice.UId))
            {
                var changes = _c.ChangesParam.Where(c => c.DeviceId == _currentDevice.LineDevice.UId).OrderByDescending(c => c.StateId);
                foreach (ChangeParamModel model in changes)
                {
                    if (ParaChanges.ContainsKey(model.ParamId)) continue;
                    ParaChanges.Add(model.ParamId, model);
                }
            }

            Dictionary<string, List<List<ParamCondition>>> para2Conds = new Dictionary<string, List<List<ParamCondition>>>();
            foreach (IDynChannel ch in Channels)
            {
                foreach (ParameterBlock block in ch.Blocks)
                {
                    foreach (IDynParameter para in block.Parameters)
                    {
                        if (paras.ContainsKey(para.Id))
                            para.Value = paras[para.Id].Value;

                        if (!Id2Param.ContainsKey(para.Id))
                            Id2Param.Add(para.Id, new ViewParamModel(para.Value));

                        Id2Param[para.Id].Parameters.Add(para);

                        if (!para2Conds.ContainsKey(para.Id))
                            para2Conds.Add(para.Id, new List<List<ParamCondition>>());

                        para2Conds[para.Id].Add(para.Conditions);
                    }
                }
            }

            foreach (IDynChannel ch in Channels)
            {
                if(ch.HasAccess)
                    ch.Visible = SaveHelper.CheckConditions(ch.Conditions, Id2Param) ? Visibility.Visible : Visibility.Collapsed;

                foreach (ParameterBlock block in ch.Blocks)
                {
                    if (block.HasAccess)
                    {
                        block.Visible = SaveHelper.CheckConditions(block.Conditions, Id2Param) ? Visibility.Visible : Visibility.Collapsed;
                    }


                    foreach (IDynParameter para in block.Parameters)
                    {
                        para.Visible = SaveHelper.CheckConditions(para.Conditions, Id2Param) ? Visibility.Visible : Visibility.Collapsed;
                        if (!VisibleParams.ContainsKey(para.Id))
                            VisibleParams.Add(para.Id, Id2Param[para.Id]);
                    }
                }
            }



            foreach (AppParameter newPara in paras.Values)
            {
                if (newPara.Access != AccessType.Full || !VisibleParams.ContainsKey(newPara.Id)) continue;

                bool isOneVisible = false;
                foreach (IDynParameter dPara in VisibleParams[newPara.Id].Parameters)
                {
                    if (SaveHelper.CheckConditions(dPara.Conditions, Id2Param))
                        isOneVisible = true;
                }

                if (!isOneVisible) continue;


                string oldPara = defParas[newPara.Id];
                bool changeExists = ParaChanges.ContainsKey(newPara.Id);
                ChangeParamModel change = null;
                if (changeExists)
                    change = ParaChanges[newPara.Id];

                if ((changeExists && newPara.Value != change.Value) || (!changeExists && newPara.Value != oldPara))
                {
                    ChangeParamModel ch = new ChangeParamModel();
                    ch.DeviceId = _currentDevice.LineDevice.UId;
                    ch.ParamId = newPara.Id;
                    ch.Value = newPara.Value;
                    _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        ChangeHandler.Instance.ChangedParam(ch);
                    });
                }
            }

            _currentDevice.Status = "Generiere KOs";

            await GenerateComs(Id2Param);


            _currentDevice.LineDevice.LoadedApplication = true;
            _currentDevice.LineDevice.LoadedGroup = true;
            _currentDevice.LineDevice.LoadedPA = true;
            //Finish();
        }

        

        

        
        private async Task GenerateComs(Dictionary<string, ViewParamModel> Id2Param)
        {
            AppAdditional adds = _context.AppAdditionals.Single(a => a.Id == _currentDevice.ApplicationId);
            List<DeviceComObject> comObjects = SaveHelper.ByteArrayToObject<List<DeviceComObject>>(adds.ComsAll);
            List<ParamBinding> Bindings = SaveHelper.ByteArrayToObject<List<ParamBinding>>(adds.Bindings);
            ProjectContext _contextP = new ProjectContext(SaveHelper.connProject);

            List<DeviceComObject> newObjs = new List<DeviceComObject>();
            foreach (DeviceComObject obj in comObjects)
            {
                if (obj.Conditions.Count == 0)
                {
                    newObjs.Add(obj);
                    continue;
                }

                bool flag = SaveHelper.CheckConditions(obj.Conditions, Id2Param);
                if (flag)
                    newObjs.Add(obj);
            }


            List<DeviceComObject> toAdd = new List<DeviceComObject>();
            foreach (DeviceComObject cobj in newObjs)
            {
                if (!_currentDevice.LineDevice.ComObjects.Any(co => co.Id == cobj.Id))
                    toAdd.Add(cobj);
            }

            Dictionary<string, ComObject> coms = new Dictionary<string, ComObject>();
            foreach (ComObject com in _contextP.ComObjects)
                if (!coms.ContainsKey(com.ComId))
                    coms.Add(com.ComId, com);

            Dictionary<string, FunctionGroup> groupsMap = new Dictionary<string, FunctionGroup>();

            foreach (Building building in SaveHelper._project.Area.Buildings)
                foreach (Floor floor in building.Floors)
                    foreach (Room room in floor.Rooms)
                        foreach (Function func in room.Functions)
                            foreach (FunctionGroup funcG in func.Subs)
                                if (!groupsMap.ContainsKey(funcG.Address.ToString()))
                                    groupsMap.Add(funcG.Address.ToString(), funcG);

            foreach (DeviceComObject dcom in toAdd)
            {
                List<string> groupIds = new List<string>();

                if (CO2GA.ContainsKey(dcom.Number))
                    groupIds = CO2GA[dcom.Number];

                if (dcom.Name.Contains("{{"))
                {
                    ParamBinding bind = Bindings.Single(b => b.Hash == "CO:" + dcom.Id);
                    string value = Id2Param[dcom.BindedId].Value;
                    if (string.IsNullOrEmpty(value))
                        dcom.DisplayName = dcom.Name.Replace("{{dyn}}", bind.DefaultText);
                    else
                        dcom.DisplayName = dcom.Name.Replace("{{dyn}}", value);
                }
                else
                {
                    dcom.DisplayName = dcom.Name;
                }

                await App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    _currentDevice.LineDevice.ComObjects.Add(dcom);
                });

                ComObject com = new ComObject();
                com.ComId = dcom.Id;
                com.DeviceId = _currentDevice.LineDevice.UId;

                if (groupIds.Count > 0) com.Groups = string.Join(",", groupIds);
                foreach (string groupId in groupIds)
                {
                    if (groupsMap.ContainsKey(groupId))
                    {
                        groupsMap[groupId].ComObjects.Add(dcom);
                    }
                    else
                    {
                        FunctionGroup funcGroup = CreateFuncGroup(groupId);
                        groupsMap.Add(groupId, funcGroup);
                    }
                    groupsMap[groupId].ComObjects.Add(dcom);
                    dcom.Groups.Add(groupsMap[groupId]);
                }

                _contextP.ComObjects.Add(com);
            }


            _currentDevice.Status = "Fertig";
            _currentDevice.StateId = 4;
            await App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                _currentDevice.LineDevice.ComObjects.Sort(s => s.Number);
            });
            _contextP.SaveChanges();
            SaveHelper.SaveStructure();
        }
       

        private FunctionGroup CreateFuncGroup(string groupId)
        {
            string[] groupIds = groupId.Split("/");

            Building building;
            if (SaveHelper._project.Area.Buildings.Any(b => b.Name == "Reconstruct"))
                building = SaveHelper._project.Area.Buildings.First(b => b.Name == "Reconstruct");
            else
            {
                building = new Building()
                {
                    Name = "Reconstruct",
                    ParentArea = SaveHelper._project.Area
                };
                SaveHelper._project.Area.Buildings.Add(building);
            }

            Floor floor;
            if (building.Floors.Any(f => f.Name == "Reconstruct"))
                floor = building.Floors.First(f => f.Name == "Reconstruct");
            else
            {
                floor = new Floor()
                {
                    Name = "Reconstruct",
                    ParentBuilding = building
                };
                building.Floors.Add(floor);
            }

            Room room;
            if (floor.Rooms.Any(r => r.Name == "Main " + groupIds[0]))
                room = floor.Rooms.First(r => r.Name == "Main " + groupIds[0]);
            else
            {
                room = new Room()
                {
                    Name = "Main " + groupIds[0],
                    ParentFloor = floor
                };
                floor.Rooms.Add(room);
            }

            Function func;
            if (room.Functions.Any(f => f.Name == "Middle " + groupIds[0] + "/" + groupIds[1]))
                func = room.Functions.First(f => f.Name == "Middle " + groupIds[0] + "/" + groupIds[1]);
            else
            {
                func = new Function()
                {
                    Name = "Middle " + groupIds[0] + "/" + groupIds[1],
                    ParentRoom = room
                };
                room.Functions.Add(func);
            }

            FunctionGroup funcGroup;
            if (func.Subs.Any(fg => fg.Name == "Group " + groupId))
                funcGroup = func.Subs.First(fg => fg.Name == "Group " + groupId);
            else
            {
                funcGroup = new FunctionGroup()
                {
                    Name = "Group " + groupId,
                    ParentFunction = func
                };
                funcGroup.Address = MulticastAddress.FromString(groupId);
                func.Subs.Add(funcGroup);
            }

            return funcGroup;
        }



        #endregion
    }
}