using Kaenx.Classes;
using Kaenx.Classes.Buildings;
using Kaenx.Classes.Controls;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Project;
using Kaenx.Konnect.Addresses;
using Kaenx.View.Controls.Dialogs;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
    public sealed partial class Groups : Page, INotifyPropertyChanged
    {
        private ProjectContext context = new ProjectContext(SaveHelper.connProject);

        private ResourceLoader loader = ResourceLoader.GetForCurrentView("Groups");
        private FunctionGroup _selectedGroup;
        private LineDevice _selectedDevice;
        private Project _project;

        private LineDevice defaultDevice = new LineDevice(true);
        private FunctionGroup defaultGroup = new FunctionGroup(new Function());

        public LineDevice SelectedDevice
        {
            get { return _selectedDevice == null ? defaultDevice : _selectedDevice; }
            set { _selectedDevice = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedDevice")); }
        }

        public FunctionGroup SelectedGroup
        {
            get { return _selectedGroup == null ? defaultGroup : _selectedGroup; }
            set { _selectedGroup = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedGroup")); }
        }


        public Groups()
        {
            Log.Information("Starte nun initialisierung");
            this.InitializeComponent();
            Log.Information("Starte nun DataContext zuweisung");
            ListComs.DataContext = this;
            //ListGroupComs.DataContext = this;
            OutGroupName.DataContext = this;
            OutGroupName2.DataContext = this;
            OutGroupInfo.DataContext = this;

            defaultDevice.Name = loader.GetString("MsgSelectDevice");
            defaultGroup.Name = loader.GetString("MsgSelectGroup");

            LoadContext();
        }

        private async void LoadContext()
        {
            await System.Threading.Tasks.Task.Delay(1000);
            _project = (Project)this.DataContext;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void TreeTopologie_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is LineDevice == false)
            {
                SelectedDevice = null;
                return;
            }

            SelectedDevice = (LineDevice)args.InvokedItem;
            ShowAssociatedComs();
        }

        private void ToggleExpert(object sender, RoutedEventArgs e)
        {
            AppBarToggleButton box = sender as AppBarToggleButton;

            if (box.IsChecked == true)
                ListComs.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.VisibleWhenSelected;
            else
                ListComs.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed;
        }

        private void MenuFlyout_Opening(object sender, object e)
        {
            MenuFlyout mf = sender as MenuFlyout;
            DeviceComObject com = (mf.Target as DataGridRow).DataContext as DeviceComObject;

            if(SelectedGroup.Address == null)
            {
                mf.Items[0].Visibility = Visibility.Collapsed;
                mf.Items[1].Visibility = Visibility.Collapsed;
                string funcName = com.Groups[0].ParentFunction.ParentRoom.Name + " - " + com.Groups[0].ParentFunction.Name + " " + com.Groups[0].Name;
                (mf.Items[1] as MenuFlyoutItem).Text = string.Format(loader.GetString("CListContextUnlink2"), com);
            } else
            {
                bool isExisting = com.Groups.Contains(SelectedGroup);

                mf.Items[0].Visibility = isExisting ? Visibility.Collapsed : Visibility.Visible;
                mf.Items[1].Visibility = isExisting ? Visibility.Visible : Visibility.Collapsed;
                string funcName = SelectedGroup.ParentFunction.ParentRoom.Name + " - " + SelectedGroup.ParentFunction.Name + " " + SelectedGroup.Name;
                (mf.Items[0] as MenuFlyoutItem).Text = string.Format(loader.GetString("CListContextLink2"), funcName);
                (mf.Items[1] as MenuFlyoutItem).Text = string.Format(loader.GetString("CListContextUnlink2"), funcName);
            }

            mf.Items[0].IsEnabled = SelectedGroup.Address != null;
            mf.Items[1].IsEnabled = SelectedGroup.Address != null;

            mf.Items[2].IsEnabled = com.Groups.Count > 0;
        }

        private void ListComs_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.DoubleTapped += Row_DoubleTapped;

            DeviceComObject com = e.Row.DataContext as DeviceComObject;
            e.Row.Background = new SolidColorBrush(com.IsSelected ? Colors.Orange : Colors.Transparent) { Opacity = 0.5 };
            e.Row.IsEnabled = com.IsEnabled;

            if(!com.IsOk || !com.IsEnabled)
                e.Row.Foreground = new SolidColorBrush(Colors.Gray);
        }

        private void Row_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            DeviceComObject com = row.DataContext as DeviceComObject;
            LinkComObject(com);
        }

        private void ClickLink(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem tvi = sender as MenuFlyoutItem;
            DeviceComObject com = tvi.DataContext as DeviceComObject;
            LinkComObject(com);
        }

        private void ClickLinkAll(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem tvi = sender as MenuFlyoutItem;
            DeviceComObject com = tvi.DataContext as DeviceComObject;

            if (ListComs.SelectedItems.Contains(com))
            {
                foreach (DeviceComObject com2 in ListComs.SelectedItems)
                {
                    foreach (FunctionGroup addr in com2.Groups)
                        addr.ComObjects.Remove(com2);
                    com2.Groups.Clear();
                }
            }
            else
            {
                foreach (FunctionGroup addr in com.Groups)
                    addr.ComObjects.Remove(com);

                com.Groups.Clear();
            }


            SaveHelper.SaveAssociations(SelectedDevice);
            SelectedDevice.LoadedGroup = false;
            //SaveHelper.UpdateDevice(SelectedDevice);
        }

        private void LinkComObject(DeviceComObject com)
        {
            if(SelectedGroup.Address == null)
            {
                ViewHelper.Instance.ShowNotification("main", "Bitte wählen Sie erst eine Gruppe aus.", 3000, ViewHelper.MessageType.Error);
                return;
            }

            if (com.Groups.Contains(SelectedGroup))
            {
                com.Groups.Remove(SelectedGroup);
                SelectedGroup.ComObjects.Remove(com);
            }
            else
            {
                if (!com.Groups.Contains(SelectedGroup))
                    com.Groups.Add(SelectedGroup);
                if (!_selectedGroup.ComObjects.Contains(com))
                    _selectedGroup.ComObjects.Add(com);
            }

            SaveHelper.SaveAssociations(SelectedDevice);
            ShowAssociatedComs();

            SelectedDevice.LoadedGroup = false;
            //SaveHelper.UpdateDevice(SelectedDevice);
        }

        private void ShowAssociatedComs()
        {
            if (SelectedDevice.DeviceId == null) return;

            foreach (DeviceComObject dev in SelectedDevice.ComObjects)
            {
                dev.IsSelected = false;
                dev.IsEnabled = true;
                dev.IsOk = true;
            }

            foreach (DeviceComObject com in SelectedDevice.ComObjects)
            {
                com.IsSelected = SelectedGroup.ComObjects.Contains(com);
                if (!BtnToggleFilter.IsOn) continue;

                if (com.DataPointSubType.Number == "..." || SelectedGroup.DataPointSubType.Number == "..." || com.DataPointSubType.TypeNumbers != SelectedGroup.DataPointSubType.TypeNumbers)
                {
                    if(com.DataPointSubType.SizeInBit == SelectedGroup.DataPointSubType.SizeInBit)
                    {
                        com.IsOk = false;
                        com.IsEnabled = true;
                    } else
                    {
                        com.IsOk = false;
                        com.IsEnabled = false;
                    }
                } else
                {
                    com.IsOk = true;
                    com.IsEnabled = true;
                }
            }

            ListComs.ItemsSource = null;
            ListComs.ItemsSource = SelectedDevice.ComObjects;
        }


        #region Gebäudestruktur

        private void ClickAB_Building(object sender, RoutedEventArgs e)
        {
            _project.Area.Buildings.Add(new Classes.Buildings.Building() { Name = "Neues Gebäude" });
            SaveHelper.SaveStructure();
        }

        private void ClickAB_AddFloor(object sender, RoutedEventArgs e)
        {
            Building b = (sender as MenuFlyoutItem).DataContext as Building;
            b.Floors.Add(new Floor() { Name = "Neue Etage" });
            b.IsExpanded = true;
            SaveHelper.SaveStructure();
        }

        private void ClickAB_AddRoom(object sender, RoutedEventArgs e)
        {
            Floor f = (sender as MenuFlyoutItem).DataContext as Floor;
            f.Rooms.Add(new Room() { Name = "Neuer Raum" });
            f.IsExpanded = true;
            SaveHelper.SaveStructure();
        }

        private async void ClickAB_AddFunction(object sender, RoutedEventArgs e)
        {
            Room r = (sender as MenuFlyoutItem).DataContext as Room;

            DiagAddFunction diag = new DiagAddFunction();
            await diag.ShowAsync();

            if (diag.Groups.Count == 0) return;


            Function f = new Function() { Name = diag.GetName(), ParentRoom = r };
            foreach (FunctionGroup g in diag.Groups)
            {
                g.Address = GetNextFreeAddress(f);
                g.ParentFunction = f;
                f.Subs.Add(g);
            }

            r.Functions.Add(f);
            r.IsExpanded = true;
            SaveHelper.SaveStructure();
        }


        private async void ClickAB_Rename(object sender, DoubleTappedRoutedEventArgs e)
        {
            IBuildingStruct struc = (sender as TreeViewItem).DataContext as IBuildingStruct;

            DiagNewName diag = new DiagNewName();
            diag.NewName = struc.Name;
            await diag.ShowAsync();
            if (string.IsNullOrEmpty(diag.NewName)) return;

            struc.Name = diag.NewName;
            SaveHelper.SaveStructure();
        }

        private void ClickAB_TapFunc(object sender, TappedRoutedEventArgs e)
        {
            FunctionGroup func = (sender as TreeViewItem).DataContext as FunctionGroup;
            SelectedGroup = func;
            ShowAssociatedComs();
        }

        private MulticastAddress GetNextFreeAddress(Function func, bool isCentral = false)
        {
            List<string> addresses = new List<string>();
            foreach(Building b in _project.Area.Buildings)
                foreach(Floor f in b.Floors)
                    foreach(Room r in f.Rooms)
                        foreach(Function fu in r.Functions)
                            foreach (FunctionGroup fug in fu.Subs)
                                addresses.Add(fug.Address.ToString());

            foreach (FunctionGroup fug in func.Subs)
                addresses.Add(fug.Address.ToString());


            int main = isCentral ? 0 : 1;
            int middle = 0;
            int ga = 0;

            while (addresses.Contains($"{main}/{middle}/{ga}"))
            {
                ga++;

                if(ga > 255)
                {
                    ga = 0;
                    middle++;

                    if(middle > 15)
                    {
                        middle = 0;
                        main++;
                        if(main > 15)
                        {
                            ViewHelper.Instance.ShowNotification("main", "Es sind keine Gruppenadressen mehr frei!", 10000, ViewHelper.MessageType.Error);
                        }
                    }
                }
            }

            return MulticastAddress.FromString($"{main}/{middle}/{ga}");
        }

        private void ClickAB_Delete(object sender, RoutedEventArgs e)
        {
            List<LineDevice> changedDevices = new List<LineDevice>();

            switch((sender as MenuFlyoutItem).DataContext)
            {
                case Building building:
                    foreach(Floor f in building.Floors)
                        foreach (Room r in f.Rooms)
                            foreach (Function fc in r.Functions)
                                RemoveGroupsFromDevice(fc, changedDevices);
                    building.ParentArea.Buildings.Remove(building);
                    break;

                case Floor floor:
                    foreach(Room r in floor.Rooms)
                        foreach (Function fc in r.Functions)
                            RemoveGroupsFromDevice(fc, changedDevices);
                    floor.ParentBuilding.Floors.Remove(floor);
                    break;

                case Room room:
                    foreach(Function fc in room.Functions)
                        RemoveGroupsFromDevice(fc, changedDevices);
                    room.ParentFloor.Rooms.Remove(room);
                    break;

                case Function func:
                    RemoveGroupsFromDevice(func, changedDevices);
                    func.ParentRoom.Functions.Remove(func);
                    break;
            }

            foreach(LineDevice dev in changedDevices)
            {
                dev.LoadedGroup = false;
                SaveHelper.SaveAssociations(dev);
                //SaveHelper.UpdateDevice(dev);
            }

            SaveHelper.SaveStructure();
        }

        private void RemoveGroupsFromDevice(Function func, List<LineDevice> changedDevices)
        {
            foreach (FunctionGroup fg in func.Subs)
                foreach (DeviceComObject com in fg.ComObjects)
                {
                    com.Groups.Remove(fg);
                    if (!changedDevices.Contains(com.ParentDevice))
                        changedDevices.Add(com.ParentDevice);
                }
        }

        private void ClickAB_ResetSelected(object sender, TappedRoutedEventArgs e)
        {
            SelectedGroup = defaultGroup;

            foreach (DeviceComObject dev in SelectedDevice.ComObjects)
                dev.IsSelected = false;

        }

        private void ClickAddRename(object sender, RoutedEventArgs e)
        {

        }

        private void ClickAddDelete(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        private void Click_ToggleColumn(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem item = sender as ToggleMenuFlyoutItem;

            DataGridColumn column = ListComs.Columns.Single(c => c.Tag.ToString() == item.Tag.ToString());
            column.Visibility = item.IsChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ToggleFilter(object sender, RoutedEventArgs e)
        {
            ShowAssociatedComs();
        }
    }
}
