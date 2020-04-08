using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Kaenx.View.Controls
{
    public sealed partial class NumberBox : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(int), typeof(NumberBox), new PropertyMetadata(null));
        public static readonly DependencyProperty ValueOkProperty = DependencyProperty.Register("ValueOk", typeof(int), typeof(NumberBox), new PropertyMetadata(null, new PropertyChangedCallback(TextProperty_PropertyChanged)));
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(int), typeof(NumberBox), new PropertyMetadata(null));
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(int), typeof(NumberBox), new PropertyMetadata(null));


        public delegate string PreviewChangedHandler(NumberBox sender, int Value);
        public event PreviewChangedHandler PreviewChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private static void TextProperty_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            obj.SetValue(ValueProperty, e.NewValue);
        }


        private string _errMessage;
        public string ErrMessage
        {
            get { return _errMessage; }
            set { _errMessage = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ErrMessage")); }
        }

        public int Value { 
            get { return (int)GetValue(ValueProperty); }
            set
            {
                bool error = false;
                string handled = PreviewChanged?.Invoke(this, value);
                if (!string.IsNullOrEmpty(handled))
                {
                    error = true;
                    ErrMessage = handled;
                }

                BtnUp.IsEnabled = value < (int)GetValue(MaximumProperty);
                BtnDown.IsEnabled = value > (int)GetValue(MinimumProperty);

                if (value > (int)GetValue(MaximumProperty))
                {
                    error = true;
                    ErrMessage = "Zahl größer als Maximum von " + (int)GetValue(MaximumProperty);
                }

                if (value < (int)GetValue(MinimumProperty))
                {
                    error = true;
                    ErrMessage = "Zahl kleiner als Minimum  von " + (int)GetValue(MinimumProperty);
                }

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
            set { SetValue(ValueProperty, value); SetValue(ValueOkProperty, value); }
        }

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set
            {
                SetValue(MaximumProperty, value);

                int _val = (int)GetValue(ValueProperty);
                bool error = false;

                BtnUp.IsEnabled = _val < value;
                if (_val > value)
                    error = true;

                int min = (int)GetValue(MinimumProperty);
                BtnDown.IsEnabled = _val > min;
                if (_val < min)
                    error = true;

                if (!error)
                {
                    SetValue(ValueOkProperty, _val);
                    VisualStateManager.GoToState(this, "DefaultLayout", false);
                }
                else
                    VisualStateManager.GoToState(this, "NotAcceptedLayout", false);
            }
        }

        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set
            {
                SetValue(MinimumProperty, value);

                int _val = (int)GetValue(ValueProperty);
                bool error = false;

                int max = (int)GetValue(MaximumProperty);
                BtnUp.IsEnabled = _val < max;
                if (_val > max)
                    error = true;

                BtnDown.IsEnabled = _val > value;
                if (_val < value)
                    error = true;

                if (!error)
                {
                    SetValue(ValueOkProperty, _val);
                    VisualStateManager.GoToState(this, "DefaultLayout", false);
                }
                else
                    VisualStateManager.GoToState(this, "NotAcceptedLayout", false);
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
                case Windows.System.VirtualKey.NumberPad0:
                case Windows.System.VirtualKey.NumberPad1:
                case Windows.System.VirtualKey.NumberPad2:
                case Windows.System.VirtualKey.NumberPad3:
                case Windows.System.VirtualKey.NumberPad4:
                case Windows.System.VirtualKey.NumberPad5:
                case Windows.System.VirtualKey.NumberPad6:
                case Windows.System.VirtualKey.NumberPad7:
                case Windows.System.VirtualKey.NumberPad8:
                case Windows.System.VirtualKey.NumberPad9:
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
