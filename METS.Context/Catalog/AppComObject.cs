using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS.Context.Catalog
{
    public class AppComObject
    {
        [Key]
        public string Id { get; set; }
        [MaxLength(100)]
        public string ApplicationId { get; set; }

        [MaxLength(100)]
        public string Text_DE { get; set; }
        [MaxLength(100)]
        public string Text_EN { get; set; }
        [MaxLength(100)]
        public string FunctionText_DE { get; set; }
        [MaxLength(100)]
        public string FunctionText_EN { get; set; }

        public bool Flag_Read { get; set; }
        public bool Flag_Write { get; set; }
        public bool Flag_Communicate { get; set; }
        public bool Flag_Transmit { get; set; }
        public bool Flag_Update { get; set; }
        public bool Flag_ReadOnInit { get; set; }

        public int Number { get; set; }
        public int Size { get; set; }
        public int Datapoint { get; set; }
        public int DatapointSub { get; set; }

        public void LoadComp(AppComObject com)
        {
            Id = com.Id;
            ApplicationId = com.ApplicationId;
            Text_DE = com.Text_DE;
            Text_EN = com.Text_EN;
            FunctionText_DE = com.FunctionText_DE;
            FunctionText_EN = com.FunctionText_EN;
            Flag_Read = com.Flag_Read;
            Flag_Write = com.Flag_Write;
            Flag_Communicate = com.Flag_Communicate;
            Flag_Transmit = com.Flag_Transmit;
            Flag_Update = com.Flag_Update;
            Number = com.Number;
            Size = com.Size;
            Datapoint = com.Datapoint;
            DatapointSub = com.DatapointSub;
        }

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
            if (dp == null)
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

        public string GetText()
        {
            if (System.Globalization.CultureInfo.CurrentCulture.Parent.Name == "de")
                return Text_DE;
            else
                return Text_EN;
        }
        public string GetFunctionText()
        {
            if (System.Globalization.CultureInfo.CurrentCulture.Parent.Name == "de")
                return FunctionText_DE;
            else
                return FunctionText_EN;
        }
    }
}
