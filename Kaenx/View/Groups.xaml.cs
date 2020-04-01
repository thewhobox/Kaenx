using Kaenx.Classes;
using Kaenx.Classes.Controls;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Project;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

        private LineDevice defaultDevice = new LineDevice(true) { Name = "Bitte Gerät auswählen" };
        private GroupAddress defaultGroup = new GroupAddress() { Name = "Bitte Gruppe auswählen", Id = -1 };

        private ResourceLoader loader = ResourceLoader.GetForCurrentView("Groups");
        private GroupAddress _selectedGroup;
        private LineDevice _selectedDevice;
        private Project _project;

        private DeviceComObject _dragItem;


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

            LoadContext();
        }

        private async void LoadContext()
        {
            await System.Threading.Tasks.Task.Delay(1000);
            _project = (Project)this.DataContext;
        }

        private int count = 1;

        public event PropertyChangedEventHandler PropertyChanged;

        private void ClickAddMain(object sender, RoutedEventArgs e)
        {
            Group group = new Group(getFirstFreeIdMain(), loader.GetString("NewGroupMain"));
            _project.Groups.Add(group);
            count++;

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
                count++;

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
                count++;

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
        }


        private void ClickUnlink2(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem tvi = sender as MenuFlyoutItem;
            DeviceComObject com = tvi.DataContext as DeviceComObject;

            foreach(GroupAddress ga in com.Groups)
            {
                ga.ComObjects.Remove(com);
            }
            com.Groups.Clear();
            SaveHelper.SaveAssociations(SelectedDevice);
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
            e.Row.DoubleTapped += Groups_DoubleTapped;
        }

        private void Groups_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
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
           
            foreach(GroupAddress addr in com.Groups)
                addr.ComObjects.Remove(com);

            com.Groups.Clear();

            SaveHelper.SaveAssociations(SelectedDevice);
        }

        private void LinkComObject(DeviceComObject com)
        {
            if(SelectedGroup.Id == -1)
            {
                ViewHelper.Instance.ShowNotification("Bitte wählen Sie erst eine Gruppe aus.", 3000, ViewHelper.MessageType.Error);
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
    }
}
