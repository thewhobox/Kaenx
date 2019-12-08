using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace METS.Context.Project
{
    public class LineModel
    {
        [Key]
        public int UId { get; set; }
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public bool IsExpanded { get; set; }

        public LineModel() { }
        public LineModel(int projId) { ProjectId = projId; }
    }
}


