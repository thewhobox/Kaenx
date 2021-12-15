using Kaenx.DataContext.Import.Dynamic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

namespace Kaenx.View.Controls.Parameter
{
    public sealed partial class TemplateTable : UserControl
    {
        public TemplateTable()
        {
            this.InitializeComponent();
            CheckDataContext();
        }

        private async void CheckDataContext()
        {
            while(this.DataContext == null)
                await Task.Delay(10);

            ParameterTable table = (ParameterTable)DataContext;

            foreach(TableColumn col in table.Columns)
            {
                GridLength length;
                if (col.Unit == UnitTypes.Absolute)
                    length = new GridLength(col.Width, GridUnitType.Pixel);
                else
                    length = new GridLength(col.Width, GridUnitType.Star);

                MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = length });
            }

            foreach(TableRow row in table.Rows)
            {
                GridLength length;
                if (row.Unit == UnitTypes.Absolute)
                    length = new GridLength(row.Height, GridUnitType.Pixel);
                else
                    length = new GridLength(row.Height, GridUnitType.Star);

                MainGrid.RowDefinitions.Add(new RowDefinition() { Height = length });
            }

            int counter = 0;
            foreach(IDynParameter para in table.Parameters)
            {
                ContentControl ctrl = new ContentControl()
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch
                };

                switch(para)
                {
                    case ParamEnum pe:
                        ctrl.ContentTemplate = (DataTemplate)this.Resources["TypeEnums"];
                        break;

                    case ParamSeperator ps:
                        ctrl.ContentTemplate = (DataTemplate)this.Resources["TypeSeperator"];
                        break;
                }


                MainGrid.Children.Add(ctrl);
                TablePosition pos = table.Positions[counter++];
                Grid.SetColumn(ctrl, pos.Column-1);
                Grid.SetRow(ctrl, pos.Row-1);
                ctrl.DataContext = para;
            }
        }
    }
}
