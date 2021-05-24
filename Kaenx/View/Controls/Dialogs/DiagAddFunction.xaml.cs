using Kaenx.Classes;
using Kaenx.Classes.Buildings;
using Kaenx.Classes.Helper;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
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
            Dictionary<string, Dictionary<string, DataPointSubType>> DPSTs = await SaveHelper.GenerateDatapoints();
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
                f.Subs.Add(new FunctionGroup(f) { Name = "Schalten", DPST = DPSTs["1"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Status", DPST = DPSTs["1"]["1"] });
                functions.Add(f);

                f = new Function() { Name = "Dimmen" };
                f.Subs.Add(new FunctionGroup(f) { Name = "Schalten", DPST = DPSTs["1"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Schalten Status", DPST = DPSTs["1"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Dimmen Relativ", DPST = DPSTs["3"]["7"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Dimmen Absolut", DPST = DPSTs["5"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Dimmen Wert", DPST = DPSTs["5"]["1"] });
                functions.Add(f);

                f = new Function() { Name = "Rollladen" };
                f.Subs.Add(new FunctionGroup(f) { Name = "Auf/Ab", DPST = DPSTs["1"]["8"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Stopp", DPST = DPSTs["1"]["17"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Position", DPST = DPSTs["5"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Position Status", DPST = DPSTs["5"]["1"] });
                functions.Add(f);

                f = new Function() { Name = "Jallousie" };
                f.Subs.Add(new FunctionGroup(f) { Name = "Auf/Ab", DPST = DPSTs["1"]["8"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Lamellen/Stopp", DPST = DPSTs["1"]["17"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Position", DPST = DPSTs["5"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Position Status", DPST = DPSTs["5"]["1"] });
                functions.Add(f);

                f = new Function() { Name = "Farbe RGB" };
                f.Subs.Add(new FunctionGroup(f) { Name = "Schalten", DPST = DPSTs["1"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Schalten Status", DPST = DPSTs["1"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Dimmen Relativ", DPST = DPSTs["3"]["7"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Dimmen Absolut", DPST = DPSTs["5"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Dimmen Wert", DPST = DPSTs["5"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "RGB", DPST = DPSTs["232"]["600"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "RGB Wert", DPST = DPSTs["232"]["600"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "HSV", DPST = DPSTs["232"]["600"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "HSV Wert", DPST = DPSTs["232"]["600"] });
                functions.Add(f);

                f = new Function() { Name = "Farbe RGB +Einzeln" };
                f.Subs.Add(new FunctionGroup(f) { Name = "Schalten", DPST = DPSTs["1"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Schalten Status", DPST = DPSTs["1"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Dimmen Relativ", DPST = DPSTs["3"]["7"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Dimmen Absolut", DPST = DPSTs["5"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "Dimmen Wert", DPST = DPSTs["5"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "RGB", DPST = DPSTs["232"]["600"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "RGB Wert", DPST = DPSTs["232"]["600"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "HSV", DPST = DPSTs["232"]["600"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "HSV Wert", DPST = DPSTs["232"]["600"] });

                f.Subs.Add(new FunctionGroup(f) { Name = "H (Farbton) Absolut", DPST = DPSTs["5"]["3"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "H (Farbton) Relativ", DPST = DPSTs["3"]["7"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "H (Farbton) Wert", DPST = DPSTs["5"]["3"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "S (Sättigung) Absolut", DPST = DPSTs["5"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "S (Sättigung) Relativ", DPST = DPSTs["3"]["7"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "S (Sättigung) Wert", DPST = DPSTs["5"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "V (Helligkeit) Absolut", DPST = DPSTs["5"]["1"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "V (Helligkeit) Relativ", DPST = DPSTs["3"]["7"] });
                f.Subs.Add(new FunctionGroup(f) { Name = "V (Helligkeit) Wert", DPST = DPSTs["5"]["1"] });
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
