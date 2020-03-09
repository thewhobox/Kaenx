using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Kaenx.DataContext.Project
{
    public class LineMiddleModel
    {
        [Key]
        public int UId { get; set; }
        public int Id { get; set; }
        public int ParentId { get; set; }
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public bool IsExpanded { get; set; }

        public LineMiddleModel() { }
        public LineMiddleModel(int projId) { ProjectId = projId; }
    }
}
