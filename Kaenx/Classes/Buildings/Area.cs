using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Buildings
{
    public class Area
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ObservableCollection<Building> Buildings { get; set; } = new ObservableCollection<Building>();
    }
}
