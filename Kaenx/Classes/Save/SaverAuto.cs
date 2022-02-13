using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Kaenx.Classes.Save
{
    public class SaverAuto : ISaveHelper
    {
        Project.Project _project;

        public void Init(Project.Project project)
        {
            _project = project;

            project.Lines.CollectionChanged += CollectionChanged;
            foreach(Line mainLine in project.Lines)
            {
                mainLine.Subs.CollectionChanged += CollectionChanged;
                mainLine.PropertyChanged += PropertyChanged;

                foreach(LineMiddle middleLine in mainLine.Subs)
                {
                    middleLine.Subs.CollectionChanged += CollectionChanged;
                    middleLine.PropertyChanged += PropertyChanged;

                    foreach(LineDevice deviceLine in middleLine.Subs)
                    {
                        deviceLine.PropertyChanged += PropertyChanged;
                        deviceLine.ComObjectsChanged += Device_ComObjectsChanged;
                    }
                }
            }
        }

        private void PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LineName"
                || e.PropertyName == "Subs") return;


            ProjectContext context = new ProjectContext(_project.Connection);


            if (sender is Line)
            {

            } else if(sender is LineMiddle)
            {

            } else if(sender is LineDevice ld)
            {
                LineDeviceModel model = context.LineDevices.Single(d => d.UId == ld.UId);
                model.Id = ld.Id;
                model.Name = ld.Name;
                model.LoadedApp = ld.LoadedPA;
                model.LoadedGA = ld.LoadedGroup;
                model.LoadedPA = ld.LoadedPA;
                model.Serial = ld.Serial;
                model.IsDeactivated = ld.IsDeactivated;
                model.LastGroupCount = ld.LastGroupCount;
                context.LineDevices.Update(model);
            }

            context.SaveChanges();
        }

        private void CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                return;


            ProjectContext context = new ProjectContext(_project.Connection);

            if(sender is ObservableCollection<Line>)
            {
                if(e.NewItems != null)
                {
                    foreach (Line line in e.NewItems)
                    {
                        LineModel linemodel = new LineModel(_project.Id);
                        context.LinesMain.Add(linemodel);
                        context.SaveChanges();
                        line.UId = linemodel.UId;
                        line.Subs.CollectionChanged += CollectionChanged;
                        linemodel.Id = line.Id;
                        linemodel.Name = line.Name;
                        linemodel.IsExpanded = line.IsExpanded;
                        context.LinesMain.Update(linemodel);
                    }
                }
                
                if(e.OldItems != null)
                {
                    foreach (Line line in e.OldItems)
                    {
                        if (context.LinesMain.Any(l => l.UId == line.UId && l.ProjectId == _project.Id))
                        {
                            LineModel linemodel = context.LinesMain.Single(l => l.UId == line.UId && l.ProjectId == _project.Id);
                            context.Remove(linemodel);
                        }
                        line.Subs.CollectionChanged -= CollectionChanged;
                        line.PropertyChanged -= PropertyChanged;
                    }
                }
                
            } else if(sender is ObservableCollection<LineMiddle>)
            {
                if(e.NewItems != null)
                {
                    foreach(LineMiddle line in e.NewItems)
                    {
                        LineMiddleModel linemodel = new LineMiddleModel(_project.Id);
                        context.LinesMiddle.Add(linemodel);
                        context.SaveChanges();
                        line.UId = linemodel.UId;
                        line.Subs.CollectionChanged += CollectionChanged;
                        linemodel.Id = line.Id;
                        linemodel.Name = line.Name;
                        linemodel.IsExpanded = line.IsExpanded;
                        linemodel.ParentId = line.Parent.UId;
                        context.LinesMiddle.Update(linemodel);
                    }
                }

                if (e.OldItems != null)
                {
                    foreach (LineMiddle line in e.OldItems)
                    {
                        if (context.LinesMiddle.Any(l => l.UId == line.UId && l.ProjectId == _project.Id))
                        {
                            LineMiddleModel linemodel = context.LinesMiddle.Single(l => l.UId == line.UId && l.ProjectId == _project.Id);
                            context.Remove(linemodel);
                        }
                        line.Subs.CollectionChanged -= CollectionChanged;
                        line.PropertyChanged -= PropertyChanged;
                    }
                }
            } else if(sender is ObservableCollection<LineDevice>)
            {            
                if (e.NewItems != null)
                {
                    foreach (LineDevice device in e.NewItems)
                    {
                        if (context.LineDevices.Any(d => d.UId == device.UId)) continue;

                        LineDeviceModel linedevmodel = new LineDeviceModel();
                        linedevmodel.Id = device.Id;
                        linedevmodel.ParentId = device.Parent.UId;
                        linedevmodel.Name = device.Name;
                        linedevmodel.ApplicationId = device.ApplicationId;
                        linedevmodel.DeviceId = device.DeviceId;
                        linedevmodel.HardwareId = device.HardwareId;
                        linedevmodel.ProjectId = SaveHelper._project.Id;
                        context.LineDevices.Add(linedevmodel);
                        context.SaveChanges();
                        device.UId = linedevmodel.UId;

                        device.PropertyChanged += PropertyChanged;
                        device.ComObjectsChanged += Device_ComObjectsChanged;
                    }
                }

                if (e.OldItems != null)
                {
                    foreach (LineDevice device in e.OldItems)
                    {
                        device.Parent.Subs.Remove(device);
                        if (!context.LineDevices.Any(d => d.UId == device.UId)) continue;

                        LineDeviceModel linedevmodel = context.LineDevices.Single(d => d.UId == device.UId);
                        context.LineDevices.Remove(linedevmodel);

                        IEnumerable<object> todelete = context.ComObjects.Where(c => c.DeviceId == device.UId);
                        context.RemoveRange(todelete);
                        //todelete = context.ChangesParam.Where(c => c.DeviceId == device.UId);
                        //context.Remove(todelete);
                        //TODO check why it doesnt work

                        device.PropertyChanged -= PropertyChanged;
                        device.ComObjectsChanged -= Device_ComObjectsChanged;
                    }
                }
            }



            context.SaveChanges();
        }

        private void Device_ComObjectsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                return;

            ProjectContext context = new ProjectContext(_project.Connection);
            LineDevice dev = sender as LineDevice;

            if (e.NewItems != null)
            {
                foreach (DeviceComObject com in e.NewItems)
                {
                    context.ComObjects.Add(new ComObject()
                    {
                        ComId = com.Id,
                        DeviceId = dev.UId
                    });
                }
            }

            if (e.OldItems != null)
            {
                foreach (DeviceComObject com in e.OldItems)
                {
                    ComObject oldCom = context.ComObjects.Single(c => c.DeviceId == dev.UId && c.ComId == com.Id);
                    context.ComObjects.Remove(oldCom);
                }
            }

            context.SaveChanges();
        }

        public void SaveLine()
        {
            throw new NotImplementedException();
        }
    }
}
