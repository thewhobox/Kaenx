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

namespace METS.View.Controls
{
    public sealed partial class NumberBox : UserControl
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(int), typeof(NumberBox), new PropertyMetadata(null));
        public static readonly DependencyProperty ValueOkProperty = DependencyProperty.Register("ValueOk", typeof(int), typeof(NumberBox), new PropertyMetadata(null));
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(int), typeof(NumberBox), new PropertyMetadata(null));
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(int), typeof(NumberBox), new PropertyMetadata(null));


        public delegate bool PreviewChangedHandler(int Value);
        public event PreviewChangedHandler PreviewChanged;

        public int Value { 
            get { return (int)GetValue(ValueProperty); }
            set
            {
                bool error = false;
                bool handled = PreviewChanged?.Invoke(value) == true;
                if (handled)
                    error = true;

                BtnUp.IsEnabled = value < (int)GetValue(MaximumProperty);
                BtnDown.IsEnabled = value > (int)GetValue(MinimumProperty);

                if (value > (int)GetValue(MaximumProperty))
                    error = true;

                if (value < (int)GetValue(MinimumProperty))
                    error = true;

                SetValue(ValueProperty, value);

                if (!error)
                {
                    SetValue(ValueOkProperty, value);
                    VisualStateManager.GoToState(this, "DefaultLayout", false);
                }
                else
                    VisualStateManager.GoToState(this, "NotAcceptedLayout", false);
            }
        }

        public int ValueOk
        {
            get { return (int)GetValue(ValueOkProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set
            {
                int _max = (int)GetValue(MaximumProperty);
                if (Value > _max)
                    Value = _max;

                SetValue(MaximumProperty, value);
            }
        }

        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set
            {
                int _min = (int)GetValue(MinimumProperty);
                if (Value < _min)
                    Value = _min;

                SetValue(MinimumProperty, value);
            }
        }
        
        private string Tooltip { get { return Minimum + " - " + Maximum; } }



        public NumberBox()
        {
            this.InitializeComponent();
            DataGrid.DataContext = this;
        }
        private void InputBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Number0:
                case Windows.System.VirtualKey.Number1:
                case Windows.System.VirtualKey.Number2:
                case Windows.System.VirtualKey.Number3:
                case Windows.System.VirtualKey.Number4:
                case Windows.System.VirtualKey.Number5:
                case Windows.System.VirtualKey.Number6:
                case Windows.System.VirtualKey.Number7:
                case Windows.System.VirtualKey.Number8:
                case Windows.System.VirtualKey.Number9:
                case Windows.System.VirtualKey.Delete:
                case Windows.System.VirtualKey.Clear:
                case Windows.System.VirtualKey.Back:
                    break;
                default:
                    e.Handled = true;
                    break;
            }
        }

        private void GoUp(object sender, RoutedEventArgs e)
        {
            Value++;
        }

        private void GoDown(object sender, RoutedEventArgs e)
        {
            Value--;
        }

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int outx;
            bool success = int.TryParse(InputBox.Text, out outx);
            if(success)
                Value = outx;
        }
    }
}
