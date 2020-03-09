using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Kaenx.DataContext.Project
{
    public class GroupMainModel
    {
        [Key]
        public int UId { get; set; }
        public int ProjectId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
