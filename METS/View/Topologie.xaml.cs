using METS.Classes;
using METS.Classes.Bus;
using METS.Classes.Controls;
using METS.Classes.Helper;
using METS.Classes.Project;
using METS.Context.Catalog;
using METS.Context.Project;
using METS.Views.Easy.Controls;
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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace METS.View
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
        }

        private async void StartCalc()
        {
            await Task.Delay(500);
            CalcCounts();
        }

        private async void ClickAddRename(object sender, RoutedEventArgs e)
        {
            TopologieBase item = (TopologieBase)((MenuFlyoutItem)e.OriginalSource).DataContext;
            DiagNewName diag = new DiagNewName();
            diag.NewName = item.Name;
            await diag.ShowAsync();
            if (diag.NewName != null)
            {
                item.Name = diag.NewName;
            }
            SaveHelper.SaveProject();
        }

        private void ClickProAddr(object sender, RoutedEventArgs e)
        {
            Classes.Bus.Actions.ProgPhysicalAddress action = new Classes.Bus.Actions.ProgPhysicalAddress();
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

        private void ClickAddMain(object sender, RoutedEventArgs e)
        {
            ObservableCollection<Line> Lines = (ObservableCollection<Line>)this.DataContext;
            Line line = new Line(getFirstFreeIdMain(), loader.GetString("NewLineMain"));
            Lines.Add(line);

            SaveHelper.SaveProject();
            CalcCounts();
        }

        private void ClickAddSub(object sender, RoutedEventArgs e)
        {
            object data = ((MenuFlyoutItem)e.OriginalSource).DataContext;

            switch (data.GetType().ToString())
            {
                case "METS.Classes.Line":
                    Line main = (Line)data;
                    LineMiddle line = new LineMiddle(getFirstFreeIdSub(main), loader.GetString("NewLineMiddle"), main);
                    main.Subs.Add(line);
                    main.IsExpanded = true;
                    break;
            }

            SaveHelper.SaveProject();
            CalcCounts();
        }

        private void ClickAddDelete(object sender, RoutedEventArgs e)
        {
            TopologieBase item = (TopologieBase)((MenuFlyoutItem)e.OriginalSource).DataContext;

            switch (item.GetType().ToString())
            {
                case "METS.Classes.Line":
                    ObservableCollection<Line> coll = (ObservableCollection<Line>)this.DataContext;
                    coll.Remove((Line)item);
                    break;

                case "METS.Classes.LineMiddle":
                    LineMiddle line = (LineMiddle)item;
                    line.Parent.Subs.Remove(line);
                    break;

                case "METS.Classes.LineDevice":
                    LineDevice device = (LineDevice)item;
                    device.Parent.Subs.Remove(device);
                    SaveHelper.CalculateLineCurrent(device.Parent);
                    break;
            }

            //TODO nicht das ganze Project speichern.
            //Lokale Dateien der Geräte löschen

            SaveHelper.SaveProject();
            CalcCounts();
        }

        private async void ClickOpenParas(object sender, RoutedEventArgs e)
        {
            LineDevice device = (LineDevice)((MenuFlyoutItem)e.OriginalSource).DataContext;
            EControlParas paras = new EControlParas(device);
            //paras.DataContext = device; 

            TabViewItem item = new TabViewItem();
            item.Icon = new SymbolIcon(Symbol.AllApps);
            item.Header = $"{device.LineName} {device.Name}";
            item.Content = paras;
            tabView.Items.Add(item);
            tabView.SelectedItem = item;

            await Task.Delay(100);
            paras.Load();
        }

        private async void ClickOpenParas2(object sender, RoutedEventArgs e)
        {
            LineDevice device = (LineDevice)((MenuFlyoutItem)e.OriginalSource).DataContext;
            EControlParas2 paras = new EControlParas2(device);
            //paras.DataContext = device; 

            TabViewItem item = new TabViewItem();
            item.Icon = new SymbolIcon(Symbol.AllApps);


            StackPanel header = new StackPanel();
            header.Orientation = Orientation.Horizontal;

            TextBlock hLine = new TextBlock() { Margin = new Thickness(0, 0, 5, 0) };
            TextBlock hName = new TextBlock();

            header.Children.Add(hLine);
            header.Children.Add(hName);

            Windows.UI.Xaml.Data.Binding bindLine = new Windows.UI.Xaml.Data.Binding();
            bindLine.Path = new PropertyPath("LineName");
            bindLine.Source = device;
            hLine.SetBinding(TextBlock.TextProperty, bindLine);

            Windows.UI.Xaml.Data.Binding bindName = new Windows.UI.Xaml.Data.Binding();
            bindName.Path = new PropertyPath("Name");
            bindName.Source = device;
            hName.SetBinding(TextBlock.TextProperty, bindName);

            item.Header = header;
            item.Content = paras;
            tabView.Items.Add(item);
            tabView.SelectedItem = item;

            await Task.Delay(100);
            paras.Start();
        }

        private void ClickOpenCatalog(object sender, RoutedEventArgs e)
        {
            Frame frame = new Frame();

            TabViewItem item = new TabViewItem();
            item.Icon = new SymbolIcon(Symbol.Shop);
            item.Header = loader.GetString("Catalog");
            item.Content = frame;
            tabView.Items.Add(item);
            tabView.SelectedItem = item;

            frame.Navigate(typeof(Catalog));
        }

        private void TreeViewItem_DragOver(object sender, DragEventArgs e)
        {
            if (e.Handled == true) return;

            TopologieBase tbase = (TopologieBase)((TreeViewItem)e.OriginalSource).DataContext;
            if (tbase.Type == TopologieType.LineMiddle && ViewHelper.Instance.DragItem?.GetType() == typeof(Context.Catalog.DeviceViewModel))
            {
                LineMiddle line = (LineMiddle)tbase;
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Link;
                e.DragUIOverride.Caption = string.Format(loader.GetString("DragConnect"), line.Parent.Id, line.Id);
                e.Handled = true;
            }
        }

        private async void TreeViewItem_Drop(object sender, DragEventArgs e)
        {
            CatalogContext context = new CatalogContext();

            DeviceViewModel model = (DeviceViewModel)ViewHelper.Instance.DragItem;
            LineMiddle line = (LineMiddle)((TreeViewItem)e.OriginalSource).DataContext;

            LineDevice device = new LineDevice(model, line);
            device.DeviceId = model.Id;

            if (model.IsCoupler)
            {
                if (line.Subs.Any(s => s.Id == 0))
                {
                    ViewHelper.Instance.ShowNotification(loader.GetString("ErrMsgCoupler"), 4000, ViewHelper.MessageType.Error);
                    return;
                }
                device.Id = 0;
            }
            else if (model.IsPowerSupply)
            {
                if (context.Devices.Any(d => d.IsPowerSupply && line.Subs.Any(l => l.DeviceId == d.Id)))
                {
                    ViewHelper.Instance.ShowNotification(loader.GetString("ErrMsgPowerSupply"), 4000, ViewHelper.MessageType.Error);
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
                        ViewHelper.Instance.ShowNotification(loader.GetString("ErrMsgMaxDevices"), 4000, ViewHelper.MessageType.Error);
                        return;
                    }
                    device.Id = getFirstFreeIdDev(line);
                }
                else
                {
                    device.Id = -1;
                }
            }


            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("Dynamic");
            StorageFolder folderP = await ApplicationData.Current.LocalFolder.GetFolderAsync("Projects");
            folderP = await folderP.GetFolderAsync(_project.Id.ToString());

            if (model.HasApplicationProgram)
            {
                int apps = context.Hardware2App.Count(h => h.HardwareId == model.HardwareId);
                if (apps == 1)
                {
                    Hardware2AppModel app = context.Hardware2App.First(h => h.HardwareId == model.HardwareId);
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



            ProjectContext _contextP = new ProjectContext();

            LineDeviceModel linedevmodel = new LineDeviceModel();
            linedevmodel.Id = device.Id;
            linedevmodel.ParentId = device.Id;
            linedevmodel.Name = device.Name;
            linedevmodel.ApplicationId = device.ApplicationId;
            linedevmodel.DeviceId = device.DeviceId;
            _contextP.LineDevices.Add(linedevmodel);
            _contextP.SaveChanges();
            device.UId = linedevmodel.UId;

            if(model.HasApplicationProgram)
            {
                StorageFile file = await folder.GetFileAsync(device.ApplicationId + "-PA-Default.json");
                await file.CopyAsync(folderP, "Device_" + device.UId + "-PA.json");
            }

            line.Subs.Add(device);
            line.Subs.Sort(l => l.Id);
            line.IsExpanded = true;
            e.Handled = true;

            StorageFile fileJSON = await folder.GetFileAsync(device.ApplicationId + "-CO-Default.json");

            string json = await FileIO.ReadTextAsync(fileJSON);
            device.ComObjects = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<DeviceComObject>>(json);


            SaveHelper.CalculateLineCurrent(line);
            UpdateManager.Instance.CountUpdates();
            //SaveHelper.SaveProject();
            CalcCounts();
        }


        private void MenuFlyout_Opening(object sender, object e)
        {
            MenuFlyout menu = (MenuFlyout)sender;

            TopologieBase line = (TopologieBase)((TreeViewItem)menu.Target).DataContext;

            switch (line.Type)
            {
                case TopologieType.Device:
                    menu.Items[0].Visibility = Visibility.Collapsed;
                    menu.Items[1].Visibility = Visibility.Visible;
                    menu.Items[2].Visibility = Visibility.Visible;
                    menu.Items[3].Visibility = Visibility.Visible;
                    break;

                case TopologieType.LineMiddle:
                    menu.Items[0].Visibility = Visibility.Collapsed;
                    menu.Items[1].Visibility = Visibility.Collapsed;
                    menu.Items[2].Visibility = Visibility.Visible;
                    menu.Items[2].Visibility = Visibility.Visible;
                    break;

                case TopologieType.Line:
                    menu.Items[0].Visibility = Visibility.Visible;
                    menu.Items[1].Visibility = Visibility.Collapsed;
                    menu.Items[2].Visibility = Visibility.Visible;
                    menu.Items[2].Visibility = Visibility.Visible;
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

        private string InNumber_PreviewChanged(int Value)
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
    }
}
