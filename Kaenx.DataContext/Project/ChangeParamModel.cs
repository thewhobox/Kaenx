using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Project
{
    public class ChangeParamModel
    {
        [Key]
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public string ParamId { get; set; }
        public int StateId { get; set; }
        public string Value { get; set; }
    }

    public enum ChangeActionType
    {
        New,
        Changed,
        Deleted
    }
}
