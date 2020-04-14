using Kaenx.Classes;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
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
                Notify.Show(loader.GetString("AddLineErrMsg"), 3000);
                return;
            }

            if (SelectedLine.Id == 0)
            {
                Line area = new Line(getFirstFreeIdSub(), loader.GetString("AddLineNewAreaName"));
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
                    if (!SelectedLine.Subs.Any(l => l.Id == i) && !AddedLines.Any(l => l is LineMiddle && (l as LineMiddle).Parent == SelectedLine && l.Id == i))
                        return i;
            }


            return -1;
        }

        private void Remove(object sender, RoutedEventArgs e)
        {
            object data = (sender as Button).DataContext;

            switch((sender as Button).DataContext)
            {
                case Line line:
                    List<TopologieBase> toDelete = new List<TopologieBase>() { line };

                    foreach (TopologieBase topo in AddedLines)
                        if (topo is LineMiddle && (topo as LineMiddle).Parent == line)
                            toDelete.Add(topo);

                    foreach (TopologieBase lm in toDelete)
                        AddedLines.Remove(lm);

                    Lines.Remove(line);
                    break;

                case LineMiddle linem:
                    AddedLines.Remove(linem);
                    linem.Parent.Subs.Remove(linem);
                    break;
            }
        }

        private string NumberBox_PreviewChanged(NumberBox sender, int Value)
        {
            switch (sender.DataContext)
            {
                case Line line:
                    if (Lines.Any(l => l is Line && l != line && l.Id == Value))
                        return "schon vorhanden";
                    break;

                case LineMiddle linem:
                    if (linem.Parent.Subs.Any(l => l != linem && l.Id == Value) || AddedLines.Any(l => l != linem && l is LineMiddle && (l as LineMiddle).Parent == linem.Parent && l.Id == Value))
                        return "schon vorhanden 2";
                    break;
            }
            return null;
        }
    }
}
