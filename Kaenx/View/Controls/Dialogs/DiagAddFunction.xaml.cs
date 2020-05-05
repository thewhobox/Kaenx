using Kaenx.Classes.Buildings;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Kaenx.View.Controls.Dialogs
{
    public sealed partial class DiagAddFunction : ContentDialog
    {
        public ObservableCollection<FunctionGroup> Groups { get; set; } = new ObservableCollection<FunctionGroup>();

        public DiagAddFunction()
        {
            this.InitializeComponent();
            OutGroups.ItemsSource = Groups;
            Load();
        }

        private async void Load()
        {
            StorageFile file;

            if (await ApplicationData.Current.RoamingFolder.FileExistsAsync("Functions.json"))
            {
                file = await ApplicationData.Current.RoamingFolder.GetFileAsync("Functions.json");
            }
            else
            {
                file = await ApplicationData.Current.RoamingFolder.CreateFileAsync("Functions.json");

                List<Function> functions = new List<Function>();
                Function f = new Function() { Name = "Schalten" };
                f.Subs.Add(new FunctionGroup(f) { Name = "Schalten" });
                f.Subs.Add(new FunctionGroup(f) { Name = "Status" });
                functions.Add(f);

                f = new Function() { Name = "Dimmen" };
                f.Subs.Add(new FunctionGroup(f) { Name = "Schalten" });
                f.Subs.Add(new FunctionGroup(f) { Name = "Schalten Status" });
                f.Subs.Add(new FunctionGroup(f) { Name = "Dimmen Relativ" });
                f.Subs.Add(new FunctionGroup(f) { Name = "Dimmen Absolut" });
                f.Subs.Add(new FunctionGroup(f) { Name = "Dimmen Wert" });
                functions.Add(f);

                string jsonList = Newtonsoft.Json.JsonConvert.SerializeObject(functions);
                await FileIO.WriteTextAsync(file, jsonList);
            }

            string json = await FileIO.ReadTextAsync(file);

            InFunc.ItemsSource = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Function>>(json); ;
        }

        public string GetName()
        {
            return InName.Text;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Groups.Clear();
        }

        private void InFunc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Function f = InFunc.SelectedItem as Function;
            Groups.Clear();

            foreach (FunctionGroup g in f.Subs)
                Groups.Add(g);
        }
    }
}
