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
        public string Name { get; set; }
    }
}
