using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes
{
    public class ComboItemSimple
    {
        public int Value { get; set; }
        public string Text { get; set; }

        public ComboItemSimple(int val, string text)
        {
            Value = val;
            Text = text;
        }
    }
}
