using Kaenx.Classes.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Kaenx.Classes.TemplateSelectors
{
    public class ControlsParamTypesSelector : DataTemplateSelector
    {
        public DataTemplate NotFound { get; set; }
        public DataTemplate Number { get; set; }
        public DataTemplate Text { get; set; }
        public DataTemplate TextRead { get; set; }
        public DataTemplate Enums { get; set; }
        public DataTemplate EnumsTwo { get; set; }
        public DataTemplate CheckBox { get; set; }
        public DataTemplate Color { get; set; }
        public DataTemplate Seperator { get; set; }
        public DataTemplate SeperatorBox { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            switch(item)
            {
                case ParamNumber pnu:
                    return Number;

                case ParamText pte: //TODO implement TextRead
                    return Text;

                case ParamEnum pen:
                    return Enums;

                case ParamEnumTwo pent:
                    return EnumsTwo;

                case ParamCheckBox pch:
                    return CheckBox;

                case ParamColor pco:
                    return Color;

                case ParamSeperator pse:
                    return Seperator;

                case ParamSeperatorBox psex:
                    return SeperatorBox;
            }

            return NotFound;
        }
    }
}
