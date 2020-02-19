using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace METS.Context.Project
{
    public class ProjectModel
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] Image { get; set; }
        public int ImageH { get; set; }
        public int ImageW { get; set; }

        public ObservableCollection<LineModel> Lines = new ObservableCollection<LineModel>();
        //public ObservableCollection<Group> Groups = new ObservableCollection<Group>();
    }
}
