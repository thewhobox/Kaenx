using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class AppParameterTypeEnumViewModel
    {
        [Key]
        [MaxLength(255)]
        public string Id { get; set; }
        [MaxLength(100)]
        public string ParameterId { get; set; }
        [MaxLength(100)]
        public string Text { get; set; }
        [MaxLength(100)]
        public string Value { get; set; }
        public int Order { get; set; }
    }
}
