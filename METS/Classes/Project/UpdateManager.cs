using METS.Context.Catalog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS.Classes.Project
{
    public class UpdateManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int _count = 0;
        public int Count { 
            get { return _count; }
            set { _count = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count")); }
        }


        private static UpdateManager _instance;
        public static UpdateManager Instance
        {
            get
            {
                if (_instance == null) _instance = new UpdateManager();
                return _instance;
            }
        }

        private Project _project;
        private CatalogContext _context = new CatalogContext();

        public void SetProject(Project project)
        {
            _project = project;
        }

        public void CountUpdates()
        {
            int _c = 0;
            foreach (Line line in _project.Lines)
                foreach (LineMiddle lineM in line.Subs)
                    foreach (LineDevice device in lineM.Subs)
                        if (CheckDevice(device)) _c++;

            Count = _c;
        }

        public List<LineDevice> GetDevices()
        {
            List<LineDevice> devices = new List<LineDevice>();

            foreach (Line line in _project.Lines)
                foreach (LineMiddle lineM in line.Subs)
                    foreach (LineDevice device in lineM.Subs)
                        if (CheckDevice(device)) devices.Add(device);

            return devices;
        }

        private bool CheckDevice(LineDevice device)
        {
            Hardware2AppModel model = null;
            try
            {
                model = _context.Hardware2App.Single(h => h.ApplicationId == device.ApplicationId);
            } catch {
                return false;
            }

            Hardware2AppModel latestModel = _context.Hardware2App.Where(h => h.HardwareId == model.HardwareId && h.Number == model.Number).OrderByDescending(h => h.Version).First();

            if (latestModel.Version > model.Version)
                return true;
            else
                return false;
        }


    }
}
