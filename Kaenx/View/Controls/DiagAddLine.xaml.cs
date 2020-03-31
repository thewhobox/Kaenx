using Kaenx.Classes;
using Kaenx.Classes.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
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

namespace Kaenx.View.Controls
{
    public sealed partial class DiagAddLine : ContentDialog, INotifyPropertyChanged
    {
        public ObservableCollection<Line> Lines { get; set; } = new ObservableCollection<Line>();

        public ObservableCollection<TopologieBase> AddedLines { get; set; } = new ObservableCollection<TopologieBase>();

        public event PropertyChangedEventHandler PropertyChanged;


        private Line _selectedLine;
        private ResourceLoader loader = ResourceLoader.GetForCurrentView("Dialogs");

        public Line SelectedLine
        {
            get { return _selectedLine; }
            set { _selectedLine = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedLine")); }
        }


        public DiagAddLine()
        {
            this.InitializeComponent();
            this.DataContext = this;
            Line bb = new Line(0, "Backbone");
            Lines.Add(bb);

            foreach (Line line in SaveHelper._project.Lines)
            {
                Lines.Add(line);
            }
        }



        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            AddedLines.Clear();
        }


        private void Add(object sender, RoutedEventArgs e)
        {
            if (SelectedLine == null)
            {
                Notify.Show("Bitte wählen Sie erst eine Linie aus.", 3000);
                return;
            }

            if (SelectedLine.Id == 0)
            {
                Line area = new Line(getFirstFreeIdSub(), loader.GetString("AddLineNewLineName"));
                AddedLines.Add(area);
                Lines.Add(area);
            }
            else
                AddedLines.Add(new LineMiddle(getFirstFreeIdSub(), loader.GetString("AddLineNewLineName"), SelectedLine));

            AddedLines.Sort(l => l.LineName);
        }


        private int getFirstFreeIdSub()
        {
            if (SelectedLine.Id == 0)
            {
                for (int i = 1; i < 256; i++)
                    if (!Lines.Any(l => l.Id == i) && !AddedLines.Any(l => l is Line && l.Id == i))
                        return i;
            }
            else
            {
                for (int i = 1; i < 256; i++)
                    if (!SelectedLine.Subs.Any(l => l.Id == i) && !AddedLines.Any(l => l.Id == i))
                        return i;
            }


            return -1;
        }

        private void Remove(object sender, RoutedEventArgs e)
        {
            object data = (sender as Button).DataContext;
            if(data is Line)
            {
                Line line = data as Line;
                foreach(LineMiddle lm in line.Subs)
                {
                    AddedLines.Remove(lm);
                }
                AddedLines.Remove(line);
                Lines.Remove(line);
            } else if(data is LineMiddle)
            {
                LineMiddle line = data as LineMiddle;
                AddedLines.Remove(line);
                line.Parent.Subs.Remove(line);
            }
        }
    }
}
