using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class ApplicationViewModel
    {
        [Key]
        [MaxLength(255)]
        public string Id { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }
        public int Version { get; set; }
        public int Number { get; set; }
        [MaxLength(7)]
        public string Mask { get; set; }
        public int Manufacturer { get; set; }

        [MaxLength(40)]
        public string Table_Object { get; set; }
        public int Table_Object_Offset { get; set; }
        [MaxLength(40)]
        public string Table_Group { get; set; }
        public int Table_Group_Offset { get; set; }
        public int Table_Group_Max { get; set; }
        [MaxLength(40)]
        public string Table_Assosiations { get; set; }
        public int Table_Assosiations_Offset { get; set; }
        public int Table_Assosiations_Max { get; set; }


        public string VersionString
        {
            get
            {
                int rest = Version % 16;
                int full = (Version - rest) / 16;
                return "V" + full.ToString() + "." + rest.ToString();
            }
        }
    }
}
