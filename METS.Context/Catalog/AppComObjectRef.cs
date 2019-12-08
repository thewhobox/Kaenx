using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS.Context.Catalog
{
    public class AppComObjectRef
    {
        public string Id { get; set; }
        public string RefId { get; set; }

        public string Text_DE { get; set; }
        public string Text_EN { get; set; }
        public string FunctionText_DE { get; set; }
        public string FunctionText_EN { get; set; }

        public bool? Flag_Read { get; set; }
        public bool? Flag_Write { get; set; }
        public bool? Flag_Communicate { get; set; }
        public bool? Flag_Transmit { get; set; }
        public bool? Flag_ReadOnInit { get; set; }
        public bool? Flag_Update { get; set; }

        public int Number { get; set; }
        public int Size { get; set; }
        public int Datapoint { get; set; }
        public int DatapointSub { get; set; }

        public void SetText(string text, string lang = null)
        {
            if (lang == "de" || lang == null)
                Text_DE = text;
            if (lang == "en" || lang == null)
                Text_EN = text;
        }

        public void SetFunction(string text, string lang = null)
        {
            if (lang == "de" || lang == null)
                FunctionText_DE = text;
            if (lang == "en" || lang == null)
                FunctionText_EN = text;
        }

        public void SetSize(string size)
        {
            if (size == null)
            {
                Size = -1;
                return;
            }
            string[] splitted = size.Split(' ');
            int i = int.Parse(splitted[0]);
            int m = (splitted[1] == "Byte") ? 8 : 1;
            Size = i * m;
        }

        public void SetDatapoint(string dp)
        {
            if (dp == null || dp == "")
            {
                Datapoint = -1;
                DatapointSub = -1;
                return;
            }

            if (dp.Contains(" "))
                dp = dp.Substring(0, dp.IndexOf(' '));

            string[] splitted = dp.Split('-');

            if (splitted[0] == "DPT")
            {
                Datapoint = int.Parse(splitted[1]);
                DatapointSub = -1;
            }
            else
            {
                Datapoint = int.Parse(splitted[1]);
                DatapointSub = int.Parse(splitted[2]);
            }
        }
    }
}
