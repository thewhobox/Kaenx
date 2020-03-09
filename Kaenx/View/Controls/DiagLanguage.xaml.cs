using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View.Controls
{
    public sealed partial class DiagLanguage : ContentDialog, INotifyPropertyChanged
    {
        private ObservableCollection<string> languages = new ObservableCollection<string>();
        public ObservableCollection<string> Languages
        {
            get
            {
                return languages;
            }
            set
            {
                languages = value;
                Update("Languages");
            }
        }

        private string selectedLang = "";

        public event PropertyChangedEventHandler PropertyChanged;

        public string SelectedLanguage
        {
            get
            {
                return selectedLang;
            }
            set
            {
                selectedLang = value;
                Update("SelectedLanguage");
            }
        }
        public DiagLanguage(ObservableCollection<string> langs)
        {
            this.InitializeComponent();
            this.DataContext = this;
            Languages = langs;

            string current = System.Globalization.CultureInfo.CurrentCulture.Name;

            if (Languages.Contains(current))
                SelectedLanguage = current;
            else
            {
                current = current.Substring(0, 2);
                if (Languages.Any(l => l.StartsWith(current)))
                    SelectedLanguage = Languages.First(l => l.StartsWith(current));
                else
                    SelectedLanguage = Languages[0];
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ApplicationDataContainer container = ApplicationData.Current.LocalSettings;
            container.Values["defaultLang"] = SelectedLanguage;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }



        private void Update(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
