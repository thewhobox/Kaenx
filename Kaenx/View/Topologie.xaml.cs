using Kaenx.Classes;
using Kaenx.Classes.Bus;
using Kaenx.Classes.Bus.Actions;
using Kaenx.Classes.Controls;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using Kaenx.View.Controls;
using Kaenx.Views.Easy.Controls;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

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
                action = new Classes.Bus.Actions.ProgPhysicalAddressSerial();
            else
                action = new Classes.Bus.Actions.ProgPhysicalAddress();

            action.Device = (LineDevice)item.DataContext;
            BusConnection.Instance.AddAction(action);
        }

        private void ClickReadConfig(object sender, RoutedEventArgs e)
        {
            Classes.Bus.Actions.DeviceConfig action = new Classes.Bus.Actions.DeviceConfig();
            action.Device = (LineDevice)((MenuFlyoutItem)e.OriginalSource).DataContext;
            action.Finished += (action, obj) => {
                ((Classes.Bus.Data.DeviceConfigData)obj).Device = action.Device;
                ((Bus)App._pages["bus"]).AddReadData((Classes.Bus.Data.DeviceConfigData)obj);
            };
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

            foreach(TopologieBase tbase in diag.AddedLines)
            {
                if(tbase is Line)
                {
                    Line line = tbase as Line;
                    if (line.Id == 0) continue;

                    SaveHelper.SaveLine(line);
                    SaveHelper._project.Lines.Add(line);
                } else if(tbase is LineMiddle)
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

            if(sender is MenuFlyoutItem)
            {
                TopologieBase line = (sender as MenuFlyoutItem).DataContext as TopologieBase;
                diag.SelectedLine = line;
            } else
            {

            }

            await diag.ShowAsync();

            if (diag.SelectedDevice == null) return;

            LineMiddle lineToAdd = null;

            if(diag.SelectedLine is LineMiddle)
            {
                lineToAdd = diag.SelectedLine as LineMiddle;
            } else if(diag.SelectedLine is Line)
            {
                Line line = diag.SelectedLine as Line;
                if(line.Subs.Any(l => l.Id == 0))
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

            for(int i = 0; i < diag.Count; i++)
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
                    break;

                case LineMiddle linem:
                    linem.Parent.Subs.Remove(linem);
                    break;

                case LineDevice dev:
                    dev.Parent.Subs.Remove(dev);
                    SaveHelper.CalculateLineCurrent(dev.Parent);
                    break;
            }

            //TODO nicht das ganze Project speichern.

            SaveHelper.SaveProject();
            CalcCounts();
        }


        List<(UIElement ui, int id)> ParamStack = new List<(UIElement ui, int id)>();

        private async void ClickOpenParas(object sender, RoutedEventArgs e)
        {
            LineDevice device = (LineDevice)((MenuFlyoutItem)e.OriginalSource).DataContext;
            EControlParas paras;
            bool isFromCache = false;

            Windows.UI.Xaml.Data.Binding bindLine = new Windows.UI.Xaml.Data.Binding();
            bindLine.Path = new PropertyPath("LineName");
            bindLine.Source = device;
            ParamHeaderLine.SetBinding(TextBlock.TextProperty, bindLine);

            Windows.UI.Xaml.Data.Binding bindName = new Windows.UI.Xaml.Data.Binding();
            bindName.Path = new PropertyPath("Name");
            bindName.Source = device;
            ParamHeaderName.SetBinding(TextBlock.TextProperty, bindName);

            if (!Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down) && ParamStack.Any(p => p.id == device.UId))
            {
                (UIElement ui, int id) element = ParamStack.Single(i => i.id == device.UId);
                paras = (EControlParas) element.ui;
                ParamStack.Remove(element);
                isFromCache = true;
            } else
            {
                paras = new EControlParas(device);

                if(ParamStack.Count >= 5) //TODO move to app settings
                {
                    ParamStack.RemoveAt(0);
                }
            }

            ParamStack.Add((paras, device.UId));
            ParamPresenter.Content = paras;

            if(ColsPara.Width.Value == 0)
                SubNavPanel.SelectedItem = SubNavPanel.MenuItems[1];

            if (!isFromCache)
            {
                await Task.Delay(500);
                paras.Start();
            }
        }



        private async Task AddDeviceToLine(DeviceViewModel model, LineMiddle line)
        {
            LineDevice device = new LineDevice(model, line, true);
            device.DeviceId = model.Id;

            if (model.IsCoupler)
            {
                if (line.Subs.Any(s => s.Id == 0))
                {
                    ViewHelper.Instance.ShowNotification("main", loader.GetString("ErrMsgCoupler"), 4000, ViewHelper.MessageType.Error);
                    return;
                }
                device.Id = 0;
            }
            else if (model.IsPowerSupply)
            {
                if (_context.Devices.Any(d => d.IsPowerSupply && line.Subs.Any(l => l.DeviceId == d.Id)))
                {
                    ViewHelper.Instance.ShowNotification("main", loader.GetString("ErrMsgPowerSupply"), 4000, ViewHelper.MessageType.Error);
                    return;
                }
                device.Id = -1;
            }
            else
            {
                if (model.HasIndividualAddress)
                {
                    if (line.Subs.Count(s => s.Id != -1) > 255)
                    {
                        ViewHelper.Instance.ShowNotification("main", loader.GetString("ErrMsgMaxDevices"), 4000, ViewHelper.MessageType.Error);
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

            ProjectContext _contextP = SaveHelper.contextProject;

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
            MenuFlyout menu = (MenuFlyout)sender;

            TopologieBase line = (TopologieBase)((TreeViewItem)menu.Target).DataContext;

            MenuFlyoutItemBase mAddL = menu.Items.Single(i => i.Name == "MFI_AddLine");
            MenuFlyoutItemBase mAddD = menu.Items.Single(i => i.Name == "MFI_AddDevice");
            MenuFlyoutItemBase mProg = menu.Items.Single(i => i.Name == "MFI_Prog");
            MenuFlyoutItemBase mPara = menu.Items.Single(i => i.Name == "MFI_Para");
            MenuFlyoutSubItem mActions = (MenuFlyoutSubItem)menu.Items.Single(i => i.Name == "MFI_Actions");
            MenuFlyoutItemBase mToggle = mActions.Items.Single(i => i.Name == "MFI_Toggle");
            MenuFlyoutItem mProgS = (MenuFlyoutItem)(mProg as MenuFlyoutSubItem).Items.Single(i => i.Name == "MFI_ProgS");

            switch (line.Type)
            {
                case TopologieType.Device:

                    LineDevice dev = (LineDevice)line;

                    (mToggle as MenuFlyoutItem).Text = dev.IsDeactivated ? loader.GetString("MenToggle_Activate") : loader.GetString("MenToggle_Deactivate");

                    mAddL.Visibility = Visibility.Collapsed;
                    mAddD.Visibility = Visibility.Collapsed;
                    mProg.Visibility = Visibility.Visible;
                    mPara.Visibility = Visibility.Visible;
                    mActions.Visibility = Visibility.Visible;
                    mProgS.Visibility = string.IsNullOrEmpty(dev.SerialText) ? Visibility.Collapsed : Visibility.Visible;
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

            foreach(Line line in Lines)
            {
                cAreas++;
                foreach(LineMiddle middle in line.Subs)
                {
                    cLines++;
                    foreach(LineDevice device in middle.Subs)
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
                } catch
                {
                    InfoAppName.Text = loader.GetString("ErrMsgNoApp");
                }
                InNumber.IsEnabled = dev.Id > 0;
                if(dev.Id == 0)
                {
                    InNumber.Minimum = 0;
                    InNumber.Maximum = 0;
                } else
                {
                    InNumber.Minimum = 1;
                    InNumber.Maximum = 255;
                }
            } else if(data is LineMiddle)
            {
                LineMiddle line = data as LineMiddle;
                InfosLineMiddle.Visibility = Visibility.Visible;
                InfoLmMaxcurrent.Text = SaveHelper.CalculateLineCurrentAvailible(line).ToString();
                InfoLmCurrent.Text = SaveHelper.CalculateLineCurrentUsed(line).ToString();
                InNumber.Minimum = 0;
                InNumber.Maximum = 15;
            } else if(data is Line)
            {
                InNumber.Minimum = 0;
                InNumber.Maximum = 15;
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
            } else { 
            
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

            if(tbase is LineDevice)
            {
                foreach(Line line in Lines)
                {
                    foreach(LineMiddle middle in line.Subs)
                    {
                        if (middle.Subs.Contains(tbase) && middle.Subs.Any(m => m.Id == Value))
                        {
                            LineDevice dev = middle.Subs.Single(m => m.Id == Value);
                            if (dev != tbase)
                                return loader.GetString("ErrMsgExistDevice");
                        }
                    }
                }
            }else if(tbase is Line)
            {
                if (Lines.Any(m => m.Id == Value))
                {
                    Line line = Lines.Single(m => m.Id == Value);
                    if (line != tbase)
                        return loader.GetString("ErrMsgExistArea");
                }
            } else if(tbase is LineMiddle)
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

            switch(selected)
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
    }
}
