using Kaenx.Classes.Buildings;
using Kaenx.Classes.Save;
using Kaenx.DataContext.Local;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Project
{
    public class Project : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] Image { get; set; }
        public LocalConnectionProject Connection { get; set; }
        public Area Area { get; set; } = new Area();
        public ObservableCollection<Line> Lines { get; set; } = new ObservableCollection<Line>();
        public LocalProject Local { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private ISaveHelper _saveHelper;
        public ISaveHelper SaveHelper
        {
            get { return _saveHelper; }
            set { _saveHelper = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SaveHelper")); }
        }


        public Project() { }

        public Project(string _name)
        {
            Name = _name;
        }

        public void InitSaver()
        {
            switch (Connection.Type)
            {
                case LocalConnectionProject.DbConnectionType.MySQL:
                    SaveHelper = new SaverManu();
                    break;
                case LocalConnectionProject.DbConnectionType.SqlLite:
                    SaveHelper = new SaverAuto();
                    break;
            }

            SaveHelper.Init(this);
        }
    }
}
