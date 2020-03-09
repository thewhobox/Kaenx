using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class DeviceViewModel
    {
        [Key]
        public string Id { get; set; }
        [MaxLength(7)]
        public string ManufacturerId { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }
        [MaxLength(300)]
        public string VisibleDescription { get; set; }
        [MaxLength(100)]
        public string OrderNumber { get; set; }

        public bool HasIndividualAddress { get; set; }
        public bool HasApplicationProgram { get; set; }
        public bool IsPowerSupply { get; set; }
        public bool IsRailMounted { get; set; }
        public bool IsCoupler { get; set; }
        public int BusCurrent { get; set; }

        [MaxLength(100)]
        public string CatalogId { get; set; }
        [MaxLength(100)]
        public string HardwareId { get; set; }
    }
}
