using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS.Context.Catalog
{
    public class CatalogViewModel
    {
        [Key]
        public string Id { get; set; }
        [MaxLength(100)]
        public string ParentId { get; set; }
        [MaxLength(100)]
        public string Name_DE { get; set; }
        [MaxLength(100)]
        public string Name_EN { get; set; }

        public string GetName()
        {
            if (System.Globalization.CultureInfo.CurrentCulture.Parent.Name == "de")
                return Name_DE;
            else
                return Name_EN;
        }
    }
}
