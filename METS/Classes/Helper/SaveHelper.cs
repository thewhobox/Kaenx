﻿using METS.Classes.Controls.Paras;
using METS.Classes.Project;
using METS.Context.Catalog;
using METS.Context.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;

namespace METS.Classes.Helper
{
    public class SaveHelper
    {
        private static Project.Project _project;
        private static ProjectContext context = new ProjectContext();
        private static CatalogContext contextC = new CatalogContext();

        public static List<ProjectModel> GetProjects()
        {
            return context.Projects.ToList();
        }

        public static ProjectModel SaveProject(Project.Project _pro = null)
        {
            if(_pro != null)
            {
                _project = _pro;
            }
            if(_project == null)
                return null;

            ProjectModel model;

            if(!context.Projects.Any(p => p.Id == _project.Id))
            {
                model = new ProjectModel();
                context.Projects.Add(model);
                context.SaveChanges();
            } else
            {
                model = context.Projects.Single(p => p.Id == _project.Id);
            }

            model.Name = _project.Name;


            foreach(Line line in _project.Lines)
            {
                LineModel linemodel;
                if(context.LinesMain.Any(l => l.UId == line.UId && l.ProjectId == model.Id))
                {
                    linemodel = context.LinesMain.Single(l => l.UId == line.UId && l.ProjectId == model.Id);
                } else
                {
                    linemodel = new LineModel(model.Id);
                    context.LinesMain.Add(linemodel);
                    context.SaveChanges();
                    line.UId = linemodel.UId;
                }
                linemodel.Id = line.Id;
                linemodel.Name = line.Name;
                linemodel.IsExpanded = line.IsExpanded;
                context.LinesMain.Update(linemodel);

                foreach(LineMiddle linem in line.Subs)
                {
                    LineMiddleModel linemiddlemodel;
                    if(context.LinesMiddle.Any(l => l.UId == linem.UId && l.ProjectId == model.Id))
                    {
                        linemiddlemodel = context.LinesMiddle.Single(l => l.UId == linem.UId && l.ProjectId == model.Id);
                    } else
                    {
                        linemiddlemodel = new LineMiddleModel(model.Id);
                        context.LinesMiddle.Add(linemiddlemodel);
                        context.SaveChanges();
                        linem.UId = linemiddlemodel.UId;
                    }
                    linemiddlemodel.Id = linem.Id;
                    linemiddlemodel.Name = linem.Name;
                    linemiddlemodel.IsExpanded = linem.IsExpanded;
                    linemiddlemodel.ParentId = line.Id;
                    context.LinesMiddle.Update(linemiddlemodel);


                    foreach(LineDevice linedev in linem.Subs)
                    {
                        LineDeviceModel linedevmodel;
                        if(context.LineDevices.Any(l => l.UId == linedev.UId && l.ProjectId == model.Id))
                        {
                            linedevmodel = context.LineDevices.Single(l => l.UId == linedev.UId && l.ProjectId == model.Id);
                        } else
                        {
                            linedevmodel = new LineDeviceModel(model.Id);
                            context.LineDevices.Add(linedevmodel);
                            context.SaveChanges();
                            linedev.UId = linedevmodel.UId;
                        }
                        linedevmodel.Id = linedev.Id;
                        linedevmodel.ParentId = linem.Id;
                        linedevmodel.Name = linedev.Name;
                        linedevmodel.ApplicationId = linedev.ApplicationId;
                        linedevmodel.DeviceId = linedev.DeviceId;

                        IEnumerable<ComObject> removeComs = context.ComObjects.Where(co => co.DeviceId == linedev.UId).ToList();
                        context.ComObjects.RemoveRange(removeComs);
                        foreach (DeviceComObject comObj in linedev.ComObjects)
                        {
                            List<int> groupIds = new List<int>();

                            foreach (GroupAddress ga in comObj.Groups)
                                groupIds.Add(ga.UId);

                            ComObject com;
                            if (context.ComObjects.Any(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId))
                            {
                                com = context.ComObjects.Single(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId);
                                com.Groups = string.Join(",", groupIds);
                                context.ComObjects.Update(com);
                            }
                            else
                            {
                                com = new ComObject();
                                com.ComId = comObj.Id;
                                com.DeviceId = linedev.UId;
                                com.Groups = string.Join(",", groupIds);
                                context.ComObjects.Add(com);
                            }
                        }

                        context.LineDevices.Update(linedevmodel);
                    }

                    IEnumerable<LineDeviceModel> dev2delete = context.LineDevices.Where(d => d.ProjectId == model.Id && d.ParentId == linem.Id && !linem.Subs.Any(lm => lm.UId == d.UId)).ToList();
                    context.LineDevices.RemoveRange(dev2delete);
                }


                IEnumerable<LineMiddleModel> linem2delete = context.LinesMiddle.Where(d => d.ProjectId == model.Id && d.ParentId == line.Id && !line.Subs.Any(lm => lm.UId == d.UId)).ToList();
                context.LinesMiddle.RemoveRange(linem2delete);
            }


            IEnumerable<LineModel> line2delete = context.LinesMain.Where(d => d.ProjectId == model.Id && !_project.Lines.Any(lm => lm.UId == d.UId)).ToList();
            context.LinesMain.RemoveRange(line2delete);


            context.Projects.Update(model);
            context.SaveChanges();

            return model;
        }
        
        public static void SaveGroups()
        {
            foreach(Group g in _project.Groups)
            {
                GroupMainModel gmain = context.GroupMain.Single(gm => gm.UId == g.UId);
                gmain.Name = g.Name;
                gmain.Id = g.Id;
                context.GroupMain.Update(gmain);

                foreach(GroupMiddle gm in g.Subs)
                {
                    GroupMiddleModel gmiddle = new GroupMiddleModel();
                    gmiddle.Name = gm.Name;
                    gmiddle.Id = gm.Id;
                    gmiddle.ParentId = gmain.UId;
                    context.GroupMiddle.Add(gmiddle);

                    foreach(GroupAddress ga in gm.Subs)
                    {
                        GroupAddressModel gaddress = context.GroupAddress.Single(g => g.UId == ga.UId);
                        gaddress.Name = ga.Name;
                        gaddress.Id = ga.Id;
                        gaddress.ParentId = gmiddle.UId;
                        context.GroupAddress.Update(gaddress);
                    }
                }
            }

            context.SaveChanges();
        }

        public static void SaveAssociations(LineDevice linedev)
        {
            IEnumerable<ComObject> removeComs = context.ComObjects.Where(co => co.DeviceId == linedev.UId).ToList();
            context.ComObjects.RemoveRange(removeComs);

            foreach (DeviceComObject comObj in linedev.ComObjects)
            {
                List<int> groupIds = new List<int>();

                foreach (GroupAddress ga in comObj.Groups)
                    groupIds.Add(ga.UId);

                ComObject com;
                if (context.ComObjects.Any(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId))
                {
                    com = context.ComObjects.Single(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId);
                    com.Groups = string.Join(",", groupIds);
                    context.ComObjects.Update(com);
                }
                else
                {
                    com = new ComObject();
                    com.ComId = comObj.Id;
                    com.DeviceId = linedev.UId;
                    com.Groups = string.Join(",", groupIds);
                    context.ComObjects.Add(com);
                }
            }
            context.SaveChanges();
        }

        public static Project.Project LoadProject(int projectId)
        {
            Project.Project project = new Project.Project();

            ProjectModel pm = context.Projects.Single(p => p.Id == projectId);
            project.Name = pm.Name;
            project.Id = pm.Id;

            Dictionary<int, GroupAddress> groups = new Dictionary<int, GroupAddress>();

            foreach (GroupMainModel gmain in context.GroupMain.Where(g => g.ProjectId == project.Id))
            {
                Group groupMain = new Group(gmain);
                project.Groups.Add(groupMain);

                foreach (GroupMiddleModel gmiddle in context.GroupMiddle.Where(g => g.ParentId == groupMain.UId))
                {
                    GroupMiddle groupMiddle = new GroupMiddle(gmiddle, groupMain);
                    groupMain.Subs.Add(groupMiddle);

                    foreach (GroupAddressModel gaddress in context.GroupAddress.Where(g => g.ParentId == groupMiddle.UId))
                    {
                        GroupAddress groupAddress = new GroupAddress(gaddress, groupMiddle);
                        groupMiddle.Subs.Add(groupAddress);
                        groups.Add(groupAddress.UId, groupAddress);
                    }
                }
            }

            foreach (LineModel lmodel in context.LinesMain.Where(l => l.ProjectId == projectId))
            {
                Line line = new Line(lmodel);
                project.Lines.Add(line);

                foreach(LineMiddleModel lmm in context.LinesMiddle.Where(l => l.ProjectId == projectId && l.ParentId == line.Id))
                {
                    LineMiddle lm = new LineMiddle(lmm, line);
                    line.Subs.Add(lm);

                    foreach(LineDeviceModel ldm in context.LineDevices.Where(l => l.ProjectId == projectId && l.ParentId == lm.Id).OrderBy(l => l.Id))
                    {
                        LineDevice ld = new LineDevice(ldm, lm);
                        ld.DeviceId = ldm.DeviceId;


                        foreach(ComObject com in context.ComObjects.Where(co => co.DeviceId == ld.UId))
                        {
                            AppComObject comObj = contextC.AppComObjects.Single(c => c.Id == com.ComId);
                            DeviceComObject dcom = new DeviceComObject(comObj);
                            string[] ids = com.Groups.Split(",");

                            if(com.Groups != "")
                            {
                                foreach (string id_str in ids)
                                {
                                    int id = int.Parse(id_str);
                                    GroupAddress ga = groups[id];
                                    dcom.Groups.Add(ga);
                                    ga.ComObjects.Add(dcom);
                                }
                            }

                            ld.ComObjects.Add(dcom);
                        }
                        ld.ComObjects.Sort(co => co.Number);
                        lm.Subs.Add(ld);
                    }

                    CalculateLineCurrent(lm);
                }
            }


            _project = project;

            return project;
        }
        
        public static void DeleteProject(int id)
        {
            List<ProjectModel> ps = context.Projects.Where(p => p.Id == id).ToList();
            context.Projects.RemoveRange(ps);

            List<LineModel> ls = context.LinesMain.Where(l => l.ProjectId == id).ToList();
            context.LinesMain.RemoveRange(ls);

            List<LineMiddleModel> lms = context.LinesMiddle.Where(l => l.ProjectId == id).ToList();
            context.LinesMiddle.RemoveRange(lms);

            List<LineDeviceModel> lds = context.LineDevices.Where(l => l.ProjectId == id).ToList();
            context.LineDevices.RemoveRange(lds);

            context.SaveChanges();
        }
    


        public static async Task GenerateDefaultComs(string appId)
        {
            List<DeviceComObject> comObjects = new List<DeviceComObject>();

            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("Dynamic");
            StorageFile fileDEF = await folder.CreateFileAsync(appId + "-CO-Default.json", CreationCollisionOption.ReplaceExisting);
            StorageFile fileJSON = await folder.CreateFileAsync(appId + "-CO-All.json", CreationCollisionOption.ReplaceExisting);
            StorageFile file = await folder.GetFileAsync(appId + ".xml");

            XDocument dynamic = XDocument.Load(await file.OpenStreamForReadAsync());
            string xmlns = dynamic.Root.Name.NamespaceName;

            IEnumerable<XElement> elements = dynamic.Root.Descendants(XName.Get("ComObjectRefRef", xmlns));

            foreach (XElement xcom in elements)
            {
                AppComObject appCom = contextC.AppComObjects.Single(c => c.Id == xcom.Attribute("RefId").Value);
                if (appCom.Text_DE == "Dummy") continue;

                DeviceComObject comobject = new DeviceComObject(appCom);
                comobject.Conditions = GetComConditions(xcom);
                comObjects.Add(comobject);
            }

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(comObjects);
            await FileIO.WriteTextAsync(fileJSON, json);

            json = Newtonsoft.Json.JsonConvert.SerializeObject(GetDefaultComs(comObjects));
            await FileIO.WriteTextAsync(fileDEF, json);
        }

        private static List<ParamCondition> GetComConditions(XElement xele)
        {
            List<ParamCondition> conds = new List<ParamCondition>();
            while (true)
            {
                xele = xele.Parent;

                switch (xele.Name.LocalName)
                {
                    case "when":
                        ParamCondition cond = new ParamCondition();
                        if (xele.Attribute("default")?.Value == "true") continue;
                        cond.Values = xele.Attribute("test").Value.Split(" ").ToList();
                        xele = xele.Parent;
                        cond.SourceId = xele.Attribute("ParamRefId").Value;
                        conds.Add(cond);
                        break;

                    case "Dynamic":
                        return conds;
                }
            }
        }

        private static List<DeviceComObject> GetDefaultComs(List<DeviceComObject> comObjects)
        {
            Dictionary<string, string> tempValues = new Dictionary<string, string>();
            ObservableCollection<DeviceComObject> defObjs = new ObservableCollection<DeviceComObject>();
            
            foreach (DeviceComObject obj in comObjects)
            {
                if (obj.Conditions.Count == 0)
                {
                    defObjs.Add(obj);
                    continue;
                }

                bool flag = true;
                foreach (ParamCondition cond in obj.Conditions)
                {
                    string val;
                    if (tempValues.ContainsKey(cond.SourceId))
                        val = tempValues[cond.SourceId];
                    else
                    {
                        AppParameter pbPara = contextC.AppParameters.Single(p => p.Id == cond.SourceId);
                        val = pbPara.Value;
                        tempValues.Add(cond.SourceId, val);
                    }

                    if (!cond.Values.Contains(val))
                        flag = false;
                }

                if (flag)
                    defObjs.Add(obj);
            }
            defObjs.Sort(s => s.Number);
            return defObjs.ToList();
        }

        public static void CalculateLineCurrent(LineMiddle line, bool noNotify = false)
        {
            if (!contextC.Devices.Any(d => d.IsPowerSupply && line.Subs.Any(l => l.DeviceId == d.Id)))
                return;

            int maxCurrent = CalculateLineCurrentAvailible(line);
            int current = CalculateLineCurrentUsed(line);

            if ((maxCurrent - current) <= 0)
            {
                line.CurrentBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
                if (!noNotify) ViewHelper.Instance.ShowNotification("Die Spannungsquelle der Linie ist möglicherweise nicht ausreichend.\r\n(Verfügbar: " + maxCurrent + " Berechnet: " + current, 5000, ViewHelper.MessageType.Warning);
            }
            else if ((maxCurrent - current) < 80)
            {
                line.CurrentBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Orange);
                if (!noNotify) ViewHelper.Instance.ShowNotification("In der Linie sind nur noch " + (maxCurrent - current) + " mA Reserve verfügbar.", 5000, ViewHelper.MessageType.Info);
            }
            else
                line.CurrentBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Black);

        }

        public static int CalculateLineCurrentAvailible(LineMiddle line)
        {
            int maxCurrent = 0;

            foreach (LineDevice dev in line.Subs)
            {
                DeviceViewModel model = contextC.Devices.Single(s => s.Id == dev.DeviceId);

                if (model.IsPowerSupply)
                    maxCurrent += model.BusCurrent;
            }

            return maxCurrent;
        }

        public static int CalculateLineCurrentUsed(LineMiddle line)
        {
            int current = 0;

            foreach (LineDevice dev in line.Subs)
            {
                DeviceViewModel model = contextC.Devices.Single(s => s.Id == dev.DeviceId);

                if (!model.IsPowerSupply)
                    current += model.BusCurrent;
            }

            return current;
        }
    }
}
