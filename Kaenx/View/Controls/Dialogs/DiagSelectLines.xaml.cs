using Kaenx.Classes;
using Kaenx.Konnect.Addresses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Perception.Spatial;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View.Controls.Dialogs
{
    public sealed partial class DiagSelectLines : ContentDialog
    {
        public ObservableCollection<SearchPattern> Patterns { get; set; } = new ObservableCollection<SearchPattern>();


        public DiagSelectLines()
        {
            this.InitializeComponent();
            this.DataContext = this;
            Patterns.CollectionChanged += Patterns_CollectionChanged;
        }

        private void Patterns_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.IsPrimaryButtonEnabled = Patterns.Count > 0;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Patterns.Clear();
        }

        private void Add(object sender, RoutedEventArgs e)
        {
            Patterns.Add(new SearchPattern() { 
                Areas = InArea.Text, 
                Lines = InLine.Text, 
                Devices = InDevice.Text 
            });
            InArea.Text = "";
            InLine.Text = "";
            InDevice.Text = "";
        }

        private void Delete(object sender, RoutedEventArgs e)
        {
            SearchPattern pattern = (sender as Button).DataContext as SearchPattern;
            Patterns.Remove(pattern);
        }
    }
}
