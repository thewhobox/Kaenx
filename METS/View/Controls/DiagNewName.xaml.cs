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

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace METS.Classes.Controls
{
    public sealed partial class DiagNewName : ContentDialog
    {
        private string _name;
        public string NewName
        {
            get { return _name; }
            set { _name = value; if (value == null) return;  Input.Text = ""; Input.SelectedText = value; }
        }

        public DiagNewName()
        {
            this.InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            NewName = Input.Text;
            //this.Hide();
        }

        private void Input_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter && Input.Text != "")
            {
                NewName = Input.Text;
                this.Hide();
            }

            if (Input.Text == "")
                this.IsPrimaryButtonEnabled = false;
            else
                this.IsPrimaryButtonEnabled = true;
        }
    }
}
