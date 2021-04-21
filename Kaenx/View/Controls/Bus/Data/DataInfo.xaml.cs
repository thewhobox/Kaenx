using Kaenx.Classes.Bus.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace Kaenx.View.Controls.Bus.Data
{
    public sealed partial class DataInfo : UserControl
    {
        public DataInfo(IBusData data)
        {
            this.InitializeComponent();
            this.DataContext = data;
            //ViewRess.DataContext = (data as DeviceInfoData).OtherRessources;
        }

        private void DataGrid_LoadingRowGroup(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridRowGroupHeaderEventArgs e)
        {
            ICollectionViewGroup group = e.RowGroupHeader.CollectionViewGroup;
            GroupInfoCollection<OtherResource> g = group.Group as GroupInfoCollection<OtherResource>;
            e.RowGroupHeader.PropertyValue = g.Key;
        }
    }
}
