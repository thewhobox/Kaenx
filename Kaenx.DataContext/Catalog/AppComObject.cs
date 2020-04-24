using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class AppComObject
    {
        [Key]
        [MaxLength(255)]
        public string Id { get; set; }
        [MaxLength(255)]
        public string BindedId { get; set; }
        public string BindedDefaultText { get; set; }
        [MaxLength(100)]
        public string ApplicationId { get; set; }

        [MaxLength(100)]
        public string Text { get; set; }
        [MaxLength(100)]
        public string FunctionText { get; set; }

        public bool Flag_Read { get; set; }
        public bool Flag_Write { get; set; }
        public bool Flag_Communicate { get; set; }
        public bool Flag_Transmit { get; set; }
        public bool Flag_Update { get; set; }
        public bool Flag_ReadOnInit { get; set; }

        public string Group { get; set; }
        public int Number { get; set; }
        public int Size { get; set; }
        public int Datapoint { get; set; }
        public int DatapointSub { get; set; }

        public void LoadComp(AppComObject com)
        {
            Id = com.Id;
            BindedId = com.BindedId;
            Group = com.Group;
            ApplicationId = com.ApplicationId;
            Text = com.Text;
            FunctionText = com.FunctionText;
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
    }
}
