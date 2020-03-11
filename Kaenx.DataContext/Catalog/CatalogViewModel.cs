using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class CatalogViewModel
    {
        [Key]
        [MaxLength(255)]
        public string Id { get; set; }
        [MaxLength(100)]
        public string ParentId { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }
    }
}
