using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Kaenx.DataContext.Project
{
    public class ProjectModel
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] Image { get; set; }
        public byte[] Area { get; set; }

        public ObservableCollection<LineModel> Lines = new ObservableCollection<LineModel>();
        //public ObservableCollection<Group> Groups = new ObservableCollection<Group>();
    }
}
