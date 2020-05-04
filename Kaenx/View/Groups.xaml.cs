using Kaenx.Classes;
using Kaenx.Classes.Buildings;
using Kaenx.Classes.Controls;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Project;
using Kaenx.Konnect.Addresses;
using Kaenx.View.Controls.Dialogs;
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
        private ProjectContext context = SaveHelper.contextProject;

        private ResourceLoader loader = ResourceLoader.GetForCurrentView("Groups");
        private GroupAddress _selectedGroup;
        private LineDevice _selectedDevice;
        private Project _project;

        private LineDevice defaultDevice = new LineDevice(true);
        private GroupAddress defaultGroup = new GroupAddress() { Id = -1 };

        public LineDevice SelectedDevice
        {
            get { return _selectedDevice == null ? defaultDevice : _selectedDevice; }
            set { _selectedDevice = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedDevice")); }
        }

        public GroupAddress SelectedGroup
        {
            get { return _selectedGroup == null ? defaultGroup : _selectedGroup; }
            set { _selectedGroup = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedGroup")); }
        }


        public Groups()
        {
            this.InitializeComponent();
            ListComs.DataContext = this;
            ListGroupComs.DataContext = this;
            OutGroupName.DataContext = this;
            GroupsInfo.DataContext = this;

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

        private void ClickAddMain(object sender, RoutedEventArgs e)
        {
            Group group = new Group(getFirstFreeIdMain(), loader.GetString("NewGroupMain"));
            _project.Groups.Add(group);

            GroupMainModel model = new GroupMainModel();
            model.Name = group.Name;
            model.Id = group.Id;
            model.ProjectId = _project.Id;
            context.GroupMain.Add(model);
            context.SaveChanges();
            group.UId = model.UId;

            //SaveHelper.SaveGroups();
        }

        private void ClickAddSub(object sender, RoutedEventArgs e)
        {
            object dc = ((MenuFlyoutItem)e.OriginalSource).DataContext;

            if(dc is Group)
            {
                Group item = dc as Group;
                GroupMiddle Group = new GroupMiddle(getFirstFreeIdSub(item), loader.GetString("NewGroupMiddle"), item);
                
                GroupMiddleModel model = new GroupMiddleModel();
                model.Name = Group.Name;
                model.Id = Group.Id;
                model.ProjectId = _project.Id;
                model.ParentId = item.UId;
                context.GroupMiddle.Add(model);
                context.SaveChanges();
                Group.UId = model.UId;

                item.Subs.Add(Group);
                item.IsExpanded = true;
                //SaveHelper.SaveGroups();
            }
            if(dc is GroupMiddle)
            {
                GroupMiddle item = dc as GroupMiddle;
                GroupAddress Group = new GroupAddress(getFirstFreeIdSub(item), loader.GetString("NewGroupAddr"), item);
                
                GroupAddressModel model = new GroupAddressModel();
                model.Name = Group.Name;
                model.Id = Group.Id;
                model.ProjectId = _project.Id;
                model.ParentId = item.UId;
                context.GroupAddress.Add(model);
                context.SaveChanges();
                Group.UId = model.UId;

                item.Subs.Add(Group);
                item.IsExpanded = true;
                //SaveHelper.SaveGroups();
            }
        }

        private void ClickAddDelete(object sender, RoutedEventArgs e)
        {
            object dc = ((MenuFlyoutItem)e.OriginalSource).DataContext;

            if (dc is Group)
            {
                _project.Groups.Remove(dc as Group);
                GroupMainModel model = context.GroupMain.Single(g => g.UId == (dc as Group).UId);
                context.GroupMain.Remove(model);
                context.SaveChanges();
            }
            if (dc is GroupMiddle)
            {
                GroupMiddle group = dc as GroupMiddle;
                group.Parent.Subs.Remove(group);
                GroupMiddleModel model = context.GroupMiddle.Single(g => g.UId == group.UId);
                context.GroupMiddle.Remove(model);
                context.SaveChanges();
            }
            if(dc is GroupAddress)
            {
                GroupAddress group = dc as GroupAddress;
                group.Parent.Subs.Remove(group);
                GroupAddressModel model = context.GroupAddress.Single(g => g.UId == group.UId);
                context.GroupAddress.Remove(model);
                context.SaveChanges();
            }

            SaveHelper.SaveGroups();
        }

        private async void ClickAddRename(object sender, RoutedEventArgs e)
        {
            object dc = ((MenuFlyoutItem)e.OriginalSource).DataContext;
            Group g = null;
            GroupMiddle gm = null;
            GroupAddress ga = null;
            string oldName = "";
            if (dc is Group) {
                g = dc as Group;
                oldName = g.Name;
            }
            if (dc is GroupMiddle) {
                gm = dc as GroupMiddle;
                oldName = gm.Name;
            }
            if (dc is GroupAddress) {
                ga = dc as GroupAddress;
                oldName = ga.Name;
            }

            DiagNewName diag = new DiagNewName();
            diag.NewName = oldName;
            await diag.ShowAsync();
            if (diag.NewName != null)
            {
                if (dc is Group) g.Name = diag.NewName;
                if (dc is GroupMiddle) gm.Name = diag.NewName;
                if (dc is GroupAddress) ga.Name = diag.NewName;
            }
            SaveHelper.SaveGroups();
        }

        private int getFirstFreeIdMain()
        {
            if (_project == null) _project = (Project)this.DataContext;
            for (int i = 1; i < 256; i++)
            {
                if (!_project.Groups.Any(l => l.Id == i))
                    return i;
            }
            return -1;
        }

        private int getFirstFreeIdSub(Group group)
        {
            for (int i = 1; i < 256; i++)
            {
                if (!group.Subs.Any(l => l.Id == i))
                    return i;
            }
            return -1;
        }

        private int getFirstFreeIdSub(GroupMiddle group)
        {
            for (int i = 1; i < 256; i++)
            {
                if (!group.Subs.Any(l => l.Id == i))
                    return i;
            }
            return -1;
        }

        private void TreeGroups_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if(args.InvokedItem is GroupAddress)
            {
                SelectedGroup = args.InvokedItem as GroupAddress;
            } else
            {
                SelectedGroup = null;
            }
        }

        private void TreeTopologie_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is LineDevice == false)
            {
                SelectedDevice = null;
                return;
            }

            SelectedDevice = (LineDevice)args.InvokedItem;
            ShowAssociatedGroups(null);
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


            if(SelectedGroup.Id == -1)
            {
                mf.Items[0].Visibility = Visibility.Collapsed;
                mf.Items[1].Visibility = Visibility.Collapsed;
            } else
            {
                bool isExisting = com.Groups.Contains(SelectedGroup);

                mf.Items[0].Visibility = isExisting ? Visibility.Collapsed : Visibility.Visible;
                mf.Items[1].Visibility = isExisting ? Visibility.Visible : Visibility.Collapsed;
            }

            mf.Items[0].IsEnabled = SelectedGroup.Id != -1;
            mf.Items[1].IsEnabled = SelectedGroup.Id != -1;

            mf.Items[2].IsEnabled = com.Groups.Count > 0;
        }

        private void ListComs_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.PointerPressed += Row_PointerPressed;
            e.Row.DoubleTapped += Row_DoubleTapped;
        }

        private void Row_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            DeviceComObject com = row.DataContext as DeviceComObject;
            LinkComObject(com);
        }

        private async void Row_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            await Task.Delay(400);
            ShowAssociatedGroups(ListComs.SelectedItem as DeviceComObject);
        }

        private void Groups_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            TreeViewItem tvi = sender as TreeViewItem;
            if(tvi.DataContext is GroupAddress)
            {
                GroupAddress addr = tvi.DataContext as GroupAddress;
                LinkGroupAddress(addr);
            }
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
           
            foreach(GroupAddress addr in com.Groups)
                addr.ComObjects.Remove(com);

            com.Groups.Clear();

            SaveHelper.SaveAssociations(SelectedDevice);
        }

        private void LinkComObject(DeviceComObject com)
        {
            if(SelectedGroup.Id == -1)
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
        }

        private void LinkGroupAddress(GroupAddress addr)
        {
            if(BtnToggleView.IsChecked == true)
            {
                ViewHelper.Instance.ShowNotification("main", "Das Verbinden ist in dieser Ansicht nicht verfügbar.", 3000, ViewHelper.MessageType.Warning);
                return;
            }

            if(ListComs.SelectedItem == null)
            {
                ViewHelper.Instance.ShowNotification("main", "Bitte wähle erst ein KO aus.", 3000, ViewHelper.MessageType.Error);
                return;
            }

            DeviceComObject com = ListComs.SelectedItem as DeviceComObject;

            if (com.Groups.Contains(addr))
            {
                com.Groups.Remove(addr);
                addr.ComObjects.Remove(com);
            } else
            {
                com.Groups.Add(addr);
                addr.ComObjects.Add(com);
            }

            SaveHelper.SaveAssociations(SelectedDevice);
            ShowAssociatedGroups(com);
        }

        private void ShowAssociatedGroups(DeviceComObject com)
        {
            foreach(Group g in SaveHelper._project.Groups)
            {
                bool flagG = false;

                foreach(GroupMiddle gm in g.Subs)
                {
                    bool flagGM = false;

                    foreach(GroupAddress ga in gm.Subs)
                    {
                        if (ga.ComObjects.Contains(com))
                        {
                            ga.CurrentBrush = new SolidColorBrush(Windows.UI.Colors.Green) { Opacity = 0.5 };
                            flagGM = true;
                        }
                        else
                            ga.CurrentBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);
                    }

                    if (flagGM)
                    {
                        gm.CurrentBrush = new SolidColorBrush(Windows.UI.Colors.Green) { Opacity = 0.4 };
                        flagG = true;
                    }
                    else
                        gm.CurrentBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);
                }

                if (flagG)
                {
                    g.CurrentBrush = new SolidColorBrush(Windows.UI.Colors.Green) { Opacity = 0.3 };
                }
                else
                    g.CurrentBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);
            }
        }


        #region Gebäudestruktur
        private void ClickAB_Building(object sender, RoutedEventArgs e)
        {
            _project.Area.Buildings.Add(new Classes.Buildings.Building() { Name = "Neues Gebäude" });
        }

        private void ClickAB_AddFloor(object sender, RoutedEventArgs e)
        {
            Building b = (sender as MenuFlyoutItem).DataContext as Building;
            b.Floors.Add(new Floor() { Name = "Neue Etage" });
            b.IsExpanded = true;
        }

        private void ClickAB_AddRoom(object sender, RoutedEventArgs e)
        {
            Floor f = (sender as MenuFlyoutItem).DataContext as Floor;
            f.Rooms.Add(new Room() { Name = "Neuer Raum" });
            f.IsExpanded = true;
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
                f.Subs.Add(g);
            }

            r.Functions.Add(f);
            r.IsExpanded = true;
        }


        private async void ClickAB_Rename(object sender, DoubleTappedRoutedEventArgs e)
        {
            IBuildingStruct struc = (sender as TreeViewItem).DataContext as IBuildingStruct;

            DiagNewName diag = new DiagNewName();
            diag.NewName = struc.Name;
            await diag.ShowAsync();
            if (string.IsNullOrEmpty(diag.NewName)) return;

            struc.Name = diag.NewName;
        }

        private void ClickAB_TapFunc(object sender, TappedRoutedEventArgs e)
        {
            FunctionGroup func = (sender as TreeViewItem).DataContext as FunctionGroup;
            if (func == null) return;

            OutABI_Addr.Text = func.Address.ToString();
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

        #endregion

        private void ClickAB_Delete(object sender, RoutedEventArgs e)
        {
            switch((sender as MenuFlyoutItem).DataContext)
            {
                case Function func:
                    func.ParentRoom.Functions.Remove(func);
                    break;
            }
        }
    }
}
