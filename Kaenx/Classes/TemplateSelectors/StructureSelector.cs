using Kaenx.Classes.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Kaenx.Classes.TemplateSelectors
{
    public class StructureSelector : DataTemplateSelector
    {
        public DataTemplate TempBuilding { get; set; }
        public DataTemplate TempFloor { get; set; }
        public DataTemplate TempRoom { get; set; }
        public DataTemplate TempFunction { get; set; }
        public DataTemplate TempFunctionGroup { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            switch (item)
            {
                case Building bu:
                    return TempBuilding;

                case Floor fl:
                    return TempFloor;

                case Room ro:
                    return TempRoom;

                case Function fu:
                    return TempFunction;

                case FunctionGroup fug:
                    return TempFunctionGroup;

                default:
                    return null;
            }
        }
    }
}
