using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
    public class LangHelper
    {
        public LangHelper(string code) => Code = code;
        public string Code { get; set; }
        public string Local { get { return new CultureInfo(Code).DisplayName; } }
    }

    public sealed partial class DiagLanguage : ContentDialog, INotifyPropertyChanged
    {
        private ObservableCollection<LangHelper> languages = new ObservableCollection<LangHelper>();
        public ObservableCollection<LangHelper> Languages
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

            foreach(string lang in langs)
            {
                Languages.Add(new LangHelper(lang));
            }

            string current = System.Globalization.CultureInfo.CurrentCulture.Name;

            if (Languages.Any(x => x.Code == current))
            {
                LangHelper h = Languages.First(l => l.Code == current);
                InSelectLang.SelectedItem = h;
                SelectedLanguage = h.Code;
            }
            else
            {
                current = current.Substring(0, 2);
                if (Languages.Any(l => l.Code.StartsWith(current)))
                {
                    LangHelper h = Languages.First(l => l.Code.StartsWith(current));
                    InSelectLang.SelectedItem = h;
                    SelectedLanguage = h.Code;
                }
                else
                {
                    LangHelper h = Languages[0];
                    InSelectLang.SelectedItem = h;
                    SelectedLanguage = h.Code;
                }
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

        private void InSelectLang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedLanguage = ((LangHelper)InSelectLang.SelectedItem).Code;
        }
    }
}
