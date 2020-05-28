using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes
{
    public class DataPointSubType
    {
        public string Name { get; set; }
        public string MainNumber { get; set; }
        public string Number { get; set; }
        public bool Default { get; set; } = false;
        public int SizeInBit { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public string TypeNumbers
        {
            get
            {
                if (Name == "...") return "";
                string x = MainNumber + ".";
                if(Number == "xxx")
                {
                    x += Number;
                    return x;
                }
                int y = int.Parse(Number);
                if (y < 10) x += "00" + Number;
                else if (y < 100) x += "0" + Number;
                else x += Number;
                return x;
            }
        }
    }
}
