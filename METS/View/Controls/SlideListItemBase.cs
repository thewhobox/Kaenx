using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace METS.Classes.Controls
{
    public class SlideListItemBase : INotifyPropertyChanged
    {
        private bool isSelected = false;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                Update("IsSelected");
            }
        }

        public Symbol LeftSymbol { get; set; }
        public Symbol RightSymbol { get; set; }

        public ICommand LeftCommand { get; set; }
        public ICommand RightCommand { get; set; }

        public SolidColorBrush LeftBackground { get; set; }
        public SolidColorBrush RightBackground { get; set; }

        public SolidColorBrush LeftForeground { get; set; }
        public SolidColorBrush RightForeground { get; set; }

        public double ActivationWidth { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        
        private void Update(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
