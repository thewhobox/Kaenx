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
            }

            return NotFound;
        }
    }
}
