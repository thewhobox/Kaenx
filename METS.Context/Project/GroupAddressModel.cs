using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace METS.Context.Project
{
    public class GroupAddressModel
    {
        [Key]
        public int UId { get; set; }
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int ParentId { get; set; }
        public string Name { get; set; }
    }
}
