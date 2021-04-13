using Kaenx.Classes;
using Kaenx.Classes.Bus;
using Kaenx.Classes.Bus.Actions;
using Kaenx.Classes.Bus.Data;
using Kaenx.Classes.Controls;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using Kaenx.View.Controls;
using Kaenx.Views.Easy.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class Topologie : Page
    {
        public Project _project;
        private CatalogContext _context = new CatalogContext();
        private ResourceLoader loader = ResourceLoader.GetForCurrentView("Topologie");
        private LineDevice SelectedDevice;
        private MenuFlyout _currentFlyout = null;

        public Topologie()
        {
            this.InitializeComponent();
            StartCalc();
            SubNavPanel.SelectedItem = SubNavPanel.MenuItems[0];
        }

        private async void StartCalc()
        {
            await Task.Delay(500);
            CalcCounts();
        }

        private async void ClickRename(object sender, RoutedEventArgs e)
        {
            TopologieBase item = (TopologieBase)((MenuFlyoutItem)e.OriginalSource).DataContext;
            DiagNewName diag = new DiagNewName();
            diag.NewName = item.Name;
            await diag.ShowAsync();
            if (diag.NewName != null)
            {
                item.Name = diag.NewName;

                //TODO replace for only single save
                SaveHelper.SaveProject();
            }
        }

        private void ClickProAddr(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = (MenuFlyoutItem)e.OriginalSource;
            IBusAction action;

            if (item.Tag.ToString() == "serial")
                action = new ProgPhysicalAddressSerial();
            else
                action = new ProgPhysicalAddress();

            action.Device = (LineDevice)item.DataContext;
            BusConnection.Instance.AddAction(action);
        }

        private void ClickReadConfig(object sender, RoutedEventArgs e)
        {
            DeviceConfig action = new DeviceConfig();
            action.Device = (LineDevice)((MenuFlyoutItem)e.OriginalSource).DataContext;
            action.Finished += (action, obj) =>
            {
                if(obj is Kaenx.Classes.Bus.Data.ErrorData)
                {
                    ViewHelper.Instance.ShowNotification("main", "Konfig auslesen Fehler: " + Environment.NewLine + (obj as ErrorData).Message, 5000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                }
                else
                {
                    ViewHelper.Instance.ShowNotification("main", "Konfig auslesen Erfolgreich", 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);
                }
            };
            BusConnection.Instance.AddAction(action);
        }

        private void ClickReadSerial(object sender, RoutedEventArgs e)
        {
            DeviceSerial action = new DeviceSerial();
            action.Device = (LineDevice)((MenuFlyoutItem)e.OriginalSource).DataContext;
            BusConnection.Instance.AddAction(action);
        }

        private void ClickProApp(object sender, RoutedEventArgs e)
        {
            string type = ((MenuFlyoutItem)sender).Tag.ToString();
            int typeI;
            int.TryParse(type, out typeI);

            Classes.Bus.Actions.ProgApplication action = new Classes.Bus.Actions.ProgApplication((Classes.Bus.Actions.ProgApplication.ProgAppType)typeI);
            action.Device = (LineDevice)((MenuFlyoutItem)e.OriginalSource).DataContext;
            BusConnection.Instance.AddAction(action);
        }

        private async void ClickAdd(object sender, RoutedEventArgs e)
        {
            DiagAddLine diag = new DiagAddLine();



            if (sender is MenuFlyoutItem)
            {
                Line line = (sender as MenuFlyoutItem).DataContext as Line;

                if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
                {
                    LineMiddle newLine = new LineMiddle(getFirstFreeIdSub(line), loader.GetString("NewLineMiddle"), line);
                    SaveHelper.SaveLine(newLine);
                    line.Subs.Add(newLine);
                    return;
                }


                diag.SelectedLine = line;
            }
            else
            {
                if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
                {
                    Line newLine = new Line(getFirstFreeIdMain(), loader.GetString("NewLineMain"));
                    SaveHelper.SaveLine(newLine);
                    SaveHelper._project.Lines.Add(newLine);
                    return;
                }
            }

            await diag.ShowAsync();

            foreach (TopologieBase tbase in diag.AddedLines)
            {
                if (tbase is Line)
                {
                    Line line = tbase as Line;
                    if (line.Id == 0) continue;

                    SaveHelper.SaveLine(line);
                    SaveHelper._project.Lines.Add(line);
                }
                else if (tbase is LineMiddle)
                {
                    LineMiddle line = tbase as LineMiddle;
                    Line parent = SaveHelper._project.Lines.Single(l => l == line.Parent);
                    parent.Subs.Add(line);
                    SaveHelper.SaveLine(line);
                }
            }

            CalcCounts();
        }

        private async void ClickAddDevice(object sender, RoutedEventArgs e)
        {
            DiagAddDevice diag = new DiagAddDevice();

            if (sender is MenuFlyoutItem)
            {
                TopologieBase line = (sender as MenuFlyoutItem).DataContext as TopologieBase;
                diag.SelectedLine = line;
            }
            else
            {

            }

            await diag.ShowAsync();

            if (diag.SelectedDevice == null) return;

            LineMiddle lineToAdd = null;

            if (diag.SelectedLine is LineMiddle)
            {
                lineToAdd = diag.SelectedLine as LineMiddle;
            }
            else if (diag.SelectedLine is Line)
            {
                Line line = diag.SelectedLine as Line;
                if (line.Subs.Any(l => l.Id == 0))
                {
                    lineToAdd = line.Subs.Single(l => l.Id == 0);
                }
                else
                {
                    lineToAdd = new LineMiddle(0, "Backbone", line);
                    SaveHelper.SaveLine(lineToAdd);
                    line.Subs.Insert(0, lineToAdd);
                }
            }

            for (int i = 0; i < diag.Count; i++)
            {
                await AddDeviceToLine(diag.SelectedDevice, lineToAdd);
            }

            SaveHelper.CalculateLineCurrent(lineToAdd);
            UpdateManager.Instance.CountUpdates();
            CalcCounts();
        }

        private void ClickDelete(object sender, RoutedEventArgs e)
        {
            TopologieBase item = (TopologieBase)((MenuFlyoutItem)e.OriginalSource).DataContext;

            switch (item)
            {
                case Line line:
                    ObservableCollection<Line> coll = (ObservableCollection<Line>)this.DataContext;
                    coll.Remove(line);

                    foreach(LineMiddle linem2 in line.Subs)
                        foreach (LineDevice ldev in linem2.Subs)
                            foreach (DeviceComObject com in ldev.ComObjects)
                                foreach (Kaenx.Classes.Buildings.FunctionGroup fg in com.Groups)
                                    fg.ComObjects.Remove(com);

                    break;

                case LineMiddle linem:
                    linem.Parent.Subs.Remove(linem);

                    foreach(LineDevice ldev in linem.Subs)
                        foreach (DeviceComObject com in ldev.ComObjects)
                            foreach (Kaenx.Classes.Buildings.FunctionGroup fg in com.Groups)
                                fg.ComObjects.Remove(com);

                    break;

                case LineDevice dev:
                    dev.Parent.Subs.Remove(dev);

                    foreach(DeviceComObject com in dev.ComObjects)
                        foreach(Kaenx.Classes.Buildings.FunctionGroup fg in com.Groups)
                            fg.ComObjects.Remove(com);

                    //TODO delete coms in database and changesParam!

                    SaveHelper.CalculateLineCurrent(dev.Parent);
                    break;
            }

            //TODO nicht das ganze Project speichern.

            SaveHelper.SaveProject();
            CalcCounts();
        }


        List<(UIElement ui, int id)> ParamStack = new List<(UIElement ui, int id)>();

        private void ClickOpenParas(object sender, RoutedEventArgs e)
        {
            LineDevice device = (LineDevice)((MenuFlyoutItem)e.OriginalSource).DataContext;
            DoOpenParas(device);
        }

        private async void DoOpenParas(LineDevice device)
        {
            if (SelectedDevice == device) return;

            SelectedDevice = device;
            EControlParas paras = new EControlParas(device);


            ParamPresenter.Content = paras;
            await Task.Delay(500);
            paras.Start();
        }



        private async Task AddDeviceToLine(DeviceViewModel model, LineMiddle line)
        {
            LineDevice device = new LineDevice(model, line, true);
            device.DeviceId = model.Id;

            if (model.IsCoupler)
            {
                if (line.Subs.Any(s => s.Id == 0))
                {
                    ViewHelper.Instance.ShowNotification("main", loader.GetString("ErrMsgCoupler"), 4000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                    return;
                }
                device.Id = 0;
            }
            else if (model.IsPowerSupply)
            {
                foreach(DeviceViewModel mod in _context.Devices.Where(d => d.IsPowerSupply))
                {
                    if(line.Subs.Any(l => l.DeviceId == mod.Id))
                    {
                        ViewHelper.Instance.ShowNotification("main", loader.GetString("ErrMsgPowerSupply"), 4000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                        return;
                    }
                }
                //if (_context.Devices.Any(d => d.IsPowerSupply && line.Subs.Any(l => l.DeviceId == d.Id)))
                //{
                //    ViewHelper.Instance.ShowNotification("main", loader.GetString("ErrMsgPowerSupply"), 4000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                //    return;
                //}
                device.Id = -1;
            }
            else
            {
                if (model.HasIndividualAddress)
                {
                    if (line.Subs.Count(s => s.Id != -1) > 255)
                    {
                        ViewHelper.Instance.ShowNotification("main", loader.GetString("ErrMsgMaxDevices"), 4000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                        return;
                    }
                    device.Id = getFirstFreeIdDev(line);
                }
                else
                {
                    device.Id = -1;
                }
            }

            if (model.HasApplicationProgram)
            {
                int apps = _context.Hardware2App.Count(h => h.HardwareId == model.HardwareId);
                if (apps == 1)
                {
                    Hardware2AppModel app = _context.Hardware2App.First(h => h.HardwareId == model.HardwareId);
                    device.ApplicationId = app.ApplicationId;
                }
                else
                {
                    DiagSelectApp diag = new DiagSelectApp(model.HardwareId);
                    await diag.ShowAsync();
                    if (diag.ApplicationId == null) return;
                    device.ApplicationId = diag.ApplicationId;
                }
            }

            ProjectContext _contextP = new ProjectContext(SaveHelper.connProject);

            LineDeviceModel linedevmodel = new LineDeviceModel();
            linedevmodel.Id = device.Id;
            linedevmodel.ParentId = line.UId;
            linedevmodel.Name = device.Name;
            linedevmodel.ApplicationId = device.ApplicationId;
            linedevmodel.DeviceId = device.DeviceId;
            linedevmodel.ProjectId = SaveHelper._project.Id;
            _contextP.LineDevices.Add(linedevmodel);
            _contextP.SaveChanges();
            device.UId = linedevmodel.UId;

            line.Subs.Add(device);
            line.Subs.Sort(l => l.Id);
            line.IsExpanded = true;


            if (_context.AppAdditionals.Any(a => a.Id == device.ApplicationId))
            {
                AppAdditional adds = _context.AppAdditionals.Single(a => a.Id == device.ApplicationId);
                device.ComObjects = SaveHelper.ByteArrayToObject<ObservableCollection<DeviceComObject>>(adds.ComsDefault);
                foreach (DeviceComObject com in device.ComObjects)
                    com.DisplayName = com.Name;
            }
            else
            {
                device.ComObjects = new ObservableCollection<DeviceComObject>();
            }

            device.IsInit = false;
        }

        private void ClickRestart(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = (MenuFlyoutItem)sender;
            LineDevice dev = (LineDevice)item.DataContext;

            Classes.Bus.Actions.DeviceRestart action = new Classes.Bus.Actions.DeviceRestart();
            action.Device = dev;
            BusConnection.Instance.AddAction(action);
        }

        private void ClickToggle(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = (MenuFlyoutItem)sender;
            LineDevice dev = (LineDevice)item.DataContext;

            Classes.Bus.Actions.DeviceDeactivate action = new Classes.Bus.Actions.DeviceDeactivate();
            action.Device = dev;
            BusConnection.Instance.AddAction(action);
        }

        private void MenuFlyout_Opening(object sender, object e)
        {
            _currentFlyout = (MenuFlyout)sender;

            TopologieBase line = (TopologieBase)((TreeViewItem)_currentFlyout.Target).DataContext;

            MenuFlyoutItemBase mAddL = _currentFlyout.Items.Single(i => i.Name == "MFI_AddLine");
            MenuFlyoutItemBase mAddD = _currentFlyout.Items.Single(i => i.Name == "MFI_AddDevice");
            MenuFlyoutItemBase mProg = _currentFlyout.Items.Single(i => i.Name == "MFI_Prog");
            MenuFlyoutItemBase mPara = _currentFlyout.Items.Single(i => i.Name == "MFI_Para");
            MenuFlyoutSubItem mActions = (MenuFlyoutSubItem)_currentFlyout.Items.Single(i => i.Name == "MFI_Actions");
            MenuFlyoutItemBase mToggle = mActions.Items.Single(i => i.Name == "MFI_Toggle");
            MenuFlyoutItemBase mAddr = mActions.Items.Single(i => i.Name == "MFI_Addr");
            MenuFlyoutItem mProgS = (MenuFlyoutItem)(mProg as MenuFlyoutSubItem).Items.Single(i => i.Name == "MFI_ProgS");

            switch (line.Type)
            {
                case TopologieType.Device:

                    LineDevice dev = (LineDevice)line;

                    (mToggle as MenuFlyoutItem).Text = dev.IsDeactivated ? loader.GetString("MenToggle_Activate") : loader.GetString("MenToggle_Deactivate");

                    mAddL.Visibility = Visibility.Collapsed;
                    mAddD.Visibility = Visibility.Collapsed;
                    mProg.Visibility = Visibility.Visible;
                    mPara.Visibility = Visibility.Collapsed; //Todo show if settings says to do
                    mActions.Visibility = Visibility.Visible;
                    mAddr.Visibility = (string.IsNullOrEmpty(dev.SerialText) || dev.SerialText == "000000000000") ? Visibility.Collapsed : Visibility.Visible;
                    mProgS.Visibility = mAddr.Visibility;
                    break;

                case TopologieType.LineMiddle:
                    mAddL.Visibility = Visibility.Collapsed;
                    mAddD.Visibility = Visibility.Visible;
                    mProg.Visibility = Visibility.Collapsed;
                    mPara.Visibility = Visibility.Collapsed;
                    mActions.Visibility = Visibility.Collapsed;
                    break;

                case TopologieType.Line:
                    mAddL.Visibility = Visibility.Visible;
                    mAddD.Visibility = Visibility.Visible;
                    mProg.Visibility = Visibility.Collapsed;
                    mPara.Visibility = Visibility.Collapsed;
                    mActions.Visibility = Visibility.Collapsed;
                    break;

            }
        }

        private void CalcCounts()
        {
            int cDevices = 0;
            int cLines = 0;
            int cAreas = 0;

            ObservableCollection<Line> Lines = (ObservableCollection<Line>)this.DataContext;

            foreach (Line line in Lines)
            {
                cAreas++;
                foreach (LineMiddle middle in line.Subs)
                {
                    cLines++;
                    foreach (LineDevice device in middle.Subs)
                    {
                        cDevices++;
                    }
                }
            }

            InfoAreas.Text = cAreas.ToString();
            InfoLines.Text = cLines.ToString();
            InfoDevices.Text = cDevices.ToString();
        }

        private int getFirstFreeIdMain()
        {
            ObservableCollection<Line> Lines = (ObservableCollection<Line>)this.DataContext;
            for (int i = 1; i < 256; i++)
                if (!Lines.Any(l => l.Id == i))
                    return i;
            return -1;
        }

        private int getFirstFreeIdSub(Line line)
        {
            for (int i = 1; i < 256; i++)
                if (!line.Subs.Any(l => l.Id == i))
                    return i;
            return -1;
        }

        private int getFirstFreeIdDev(LineMiddle line)
        {
            for (int i = 1; i < 256; i++)
                if (!line.Subs.Any(l => l.Id == i))
                    return i;
            return -1;
        }

        private void TreeV_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            TopologieBase data = args.InvokedItem as TopologieBase;

            PanelSettings.DataContext = data;


            InfosApplication.Visibility = Visibility.Collapsed;
            InfosLineMiddle.Visibility = Visibility.Collapsed;

            InNumber.IsEnabled = true;
            InName.IsEnabled = true;

            if (data is LineDevice)
            {
                InfosApplication.Visibility = Visibility.Visible;
                LineDevice dev = data as LineDevice;
                try
                {
                    ApplicationViewModel app = _context.Applications.Single(a => a.Id == dev.ApplicationId);
                    InfoAppName.Text = app.Name + " " + app.VersionString + Environment.NewLine + dev.ApplicationId;
                }
                catch
                {
                    InfoAppName.Text = loader.GetString("ErrMsgNoApp");
                }
                InNumber.IsEnabled = dev.Id > 0;
                if (dev.Id == 0)
                {
                    InNumber.Minimum = 0;
                    InNumber.Maximum = 0;
                }
                else
                {
                    InNumber.Minimum = 1;
                    InNumber.Maximum = 255;
                }

                DoOpenParas(dev);
            }
            else if (data is LineMiddle)
            {
                LineMiddle line = data as LineMiddle;
                InfosLineMiddle.Visibility = Visibility.Visible;
                InfoLmMaxcurrent.Text = SaveHelper.CalculateLineCurrentAvailible(line).ToString();
                InfoLmCurrent.Text = SaveHelper.CalculateLineCurrentUsed(line).ToString();
                InNumber.Minimum = 0;
                InNumber.Maximum = 15;

                EControlLine present = new EControlLine(line);
                ParamPresenter.Content = present;
                SelectedDevice = null;
            }
            else if (data is Line)
            {
                InNumber.Minimum = 0;
                InNumber.Maximum = 15;
                SelectedDevice = null;
            }
        }

        private async void TreeViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            TopologieBase item = null;

            if (e.OriginalSource is TextBlock)
            {
                item = (TopologieBase)((TextBlock)e.OriginalSource).DataContext;
            }
            else if (e.OriginalSource is Grid)
            {
                item = (TopologieBase)((Grid)e.OriginalSource).DataContext;
            }
            else
            {

            }


            DiagNewName diag = new DiagNewName();
            diag.NewName = item.Name;
            await diag.ShowAsync();
            if (diag.NewName != null)
            {
                item.Name = diag.NewName;
            }
            SaveHelper.SaveProject();
        }

        private string InNumber_PreviewChanged(NumberBox sender, int Value)
        {
            ObservableCollection<Line> Lines = (ObservableCollection<Line>)this.DataContext;
            TopologieBase tbase = PanelSettings.DataContext as TopologieBase;

            if (tbase is LineDevice)
            {
                foreach (Line line in Lines)
                {
                    foreach (LineMiddle middle in line.Subs)
                    {
                        if (middle.Subs.Contains(tbase) && middle.Subs.Any(m => m.Id == Value))
                        {
                            LineDevice dev = middle.Subs.Single(m => m.Id == Value);
                            if (dev != tbase)
                                return loader.GetString("ErrMsgExistDevice");
                        }
                    }
                }
            }
            else if (tbase is Line)
            {
                if (Lines.Any(m => m.Id == Value))
                {
                    Line line = Lines.Single(m => m.Id == Value);
                    if (line != tbase)
                        return loader.GetString("ErrMsgExistArea");
                }
            }
            else if (tbase is LineMiddle)
            {
                foreach (Line line in Lines)
                {

                    if (line.Subs.Contains(tbase) && line.Subs.Any(m => m.Id == Value))
                    {
                        LineMiddle lineM = line.Subs.Single(m => m.Id == Value);
                        if (lineM != tbase)
                            return loader.GetString("ErrMsgExistLine");
                    }
                }
            }

            return null;
        }

        private void SubNavPanel_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            string selected = ((NavigationViewItem)args.SelectedItem).Tag.ToString();

            switch (selected)
            {
                case "topo":
                    ColsTree.Width = new GridLength(1, GridUnitType.Star);
                    ColsPara.Width = new GridLength(0, GridUnitType.Star);
                    ColsSett.Width = new GridLength(0, GridUnitType.Star);
                    break;

                case "para":
                    ColsTree.Width = new GridLength(0, GridUnitType.Star);
                    ColsPara.Width = new GridLength(1, GridUnitType.Star);
                    ColsSett.Width = new GridLength(0, GridUnitType.Star);
                    break;

                case "sett":
                    ColsTree.Width = new GridLength(0, GridUnitType.Star);
                    ColsPara.Width = new GridLength(0, GridUnitType.Star);
                    ColsSett.Width = new GridLength(1, GridUnitType.Star);
                    break;
            }
        }

        private void OpenInNewWindow(object sender, RoutedEventArgs e)
        {
            OpenInNewWindow(SelectedDevice);
        }

        private async void OpenInNewWindow(LineDevice device)
        {
            if (ParamPresenter.Content == null)
                return;

            AppWindow appWindow = await AppWindow.TryCreateAsync();
            appWindow.Title = "Neues Fenster";
            //UIElement ele = ParamPresenter.Content as UIElement;
            ParamPresenter.Content = null;


            EControlParas paras = new EControlParas(device);

            ElementCompositionPreview.SetAppWindowContent(appWindow, paras);
            await appWindow.TryShowAsync();

            await Task.Delay(500);
            paras.Start();

            appWindow.Closed += delegate
            {
                appWindow = null;
            };
        }

        private void MenuFlyoutItem_Loading(FrameworkElement sender, object args)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            LineDevice dev = (_currentFlyout.Target as TreeViewItem).DataContext as LineDevice;

            UnloadHelper helper = new UnloadHelper();
            helper.SerialVisible = dev.SerialText != "" ? Visibility.Visible : Visibility.Collapsed;

            item.DataContext = helper;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UnloadHelper helper = (sender as Button).DataContext as UnloadHelper;
            LineDevice dev = (_currentFlyout.Target as TreeViewItem).DataContext as LineDevice;
            ProgApplication action = new ProgApplication(ProgApplication.ProgAppType.Komplett);
            action.Device = dev;
            action.ProcedureType = ProgApplication.ProcedureTypes.Unload;
            action.Helper = helper;

            BusConnection.Instance.AddAction(action);
            _currentFlyout.Hide();
        }
    }
}
