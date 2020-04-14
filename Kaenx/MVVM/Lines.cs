using Kaenx.Classes;
using Kaenx.Classes.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Kaenx.MVVM
{
    public class Lines
    {
        public static Lines Instance = new Lines();

        public ObservableCollection<Line> Main { get; set; } = new ObservableCollection<Line>();
        private ResourceLoader loader = ResourceLoader.GetForCurrentView();
        private int stackInt = 0;

        public void InitLines(int type, bool basement, int countMain, int countMiddle)
        {
            for (int a = 1; a < (countMain + 1); a++)
            {
                stackInt++;
                Line linea = new Line(stackInt, getMainName(type, a, basement));
                Main.Insert(0, linea);

                for(int b = 1; b < (countMiddle + 1); b++)
                {
                    stackInt++;
                    LineMiddle lineb = new LineMiddle(stackInt, getMiddleName(type, b, basement), linea);
                    linea.Subs.Insert(0, lineb);
                }
            }
        }


        private string getMainName(int type, int index, bool basement)
        {
            switch(type)
            {
                case 0:
                    return loader.GetString("LineName_flat");
                case 1:
                    return loader.GetString("LineName_house");
                case 2:
                    int offset = basement ? 2 : 1;
                    int index2 = index - offset;
                    if (index2 == -1) return loader.GetString("LineName_basement");
                    if (index2 == 0) return loader.GetString("LineName_ground");
                    return (index - offset).ToString() + loader.GetString("LineName_floor");
                case 3:
                    return loader.GetString("LineName_building") + " " + index.ToString();
                default:
                    return loader.GetString("LineName_defaultMain") + " " + index.ToString();
            }
        }

        private string getMiddleName(int type, int index, bool basement)
        {
            switch(type)
            {
                case 0:
                    return loader.GetString("LineName_floor");
                case 1:
                case 3:
                    int offset = basement ? 2 : 1;
                    int index2 = index - offset;
                    if (index2 == -1) return loader.GetString("LineName_basement");
                    if (index2 == 0) return loader.GetString("LineName_ground");
                    return (index - offset).ToString() + loader.GetString("LineName_floor");
                case 2:
                    return loader.GetString("LineName_section") + " " + index.ToString();
                default:
                    return loader.GetString("LineName_defaultMiddle") + " " + index.ToString();
            }
        }
    }
}
