using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Project
{
    public class ChangeHandler : INotifyPropertyChanged
    {
        private ProjectContext _context = Helper.SaveHelper.contextProject;
        private CatalogContext _contextC = new CatalogContext();
        private int _currentStateId = 0;
        private int _projectId;
        private int _count = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public static ChangeHandler Instance { get; set; }
        public int Count
        {
            get { return _count; }
            set { _count = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count")); }
        }


        public ChangeHandler(int projectId)
        {
            _projectId = projectId;
            try
            {
                StateModel state = _context.States.Where(s => s.ProjectId == projectId).OrderByDescending(s => s.Id).First();
                _currentStateId = state.Id;
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                StateModel state = new StateModel();
                state.Name = "Neuer State";
                state.Description = "Keine Beschreibung";
                state.ProjectId = _projectId;
                _context.States.Add(state);
                _context.SaveChanges();
                _currentStateId = state.Id;
            }
            CalcCount();
        }

        public void ChangedParam(ChangeParamModel change)
        {
            change.StateId = _currentStateId;

            if (_context.ChangesParam.Any(c => c.DeviceId == change.DeviceId && c.ParamId == change.ParamId && c.StateId == _currentStateId))
            {
                ChangeParamModel changeOld = _context.ChangesParam.Single(s => s.DeviceId == change.DeviceId && s.ParamId == change.ParamId && s.StateId == _currentStateId);
                string oldVal = "";

                try {
                    ChangeParamModel changeOld2 = _context.ChangesParam.Where(s => s.DeviceId == change.DeviceId && s.ParamId == change.ParamId && s.Id != changeOld.Id).OrderByDescending(c => c.StateId).First();
                    oldVal = changeOld2.Value;
                } catch
                {
                    AppParameter para = _contextC.AppParameters.Single(p => p.Id == change.ParamId);
                    oldVal = para.Value;
                }

                if (oldVal == change.Value)
                {
                    _context.ChangesParam.Remove(changeOld);
                } else
                {
                    //if (change.Value == change.Value) return;
                    changeOld.Value = change.Value;
                    _context.ChangesParam.Update(changeOld);
                }
            }
            else
            {
                _context.ChangesParam.Add(change);
            }
            _context.SaveChanges();

            CalcCount();
        }



        private void CalcCount()
        {
            int cp = _context.ChangesParam.Where(c => c.StateId == _currentStateId).Count();
            Count = cp;
        }
    }
}