using Kaenx.Classes.Buildings;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Import;
using Kaenx.DataContext.Import.Dynamic;
using Kaenx.DataContext.Local;
using Kaenx.DataContext.Project;
using Kaenx.Views.Easy.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Kaenx.Classes.Helper
{
    public class SaveHelper 
    {
        public static Project.Project _project;
        private static ProjectContext contextProject;
        public static LocalConnectionProject connProject;
        private static CatalogContext contextC = new CatalogContext(new LocalConnectionCatalog() { DbHostname = "Catalog.db", Type = LocalConnectionCatalog.DbConnectionType.SqlLite });

        //public static ProjectModel SaveProject(Project.Project _pro = null)
        //{
        //    if (_pro != null)
        //    {
        //        _project = _pro;

        //        connProject = _project.Connection;
        //        contextProject = new ProjectContext(_project.Connection);
        //        contextProject.Database.Migrate();
        //    }
        //    if (_project == null)
        //        return null;


        //    ProjectModel model;

        //    if (!contextProject.Projects.Any(p => p.Id == _project.Id))
        //    { 
        //        model = new ProjectModel();
        //        contextProject.Projects.Add(model);
        //        contextProject.SaveChanges();
        //    }
        //    else
        //    {
        //        model = contextProject.Projects.Single(p => p.Id == _project.Id);
        //    }

        //    model.Name = _project.Name;
        //    model.Image = _project.Image;
        //    model.Area = FunctionHelper.ObjectToByteArray(_project.Area);

        //    foreach (Line line in _project.Lines)
        //    {
        //        LineModel linemodel;
        //        if (contextProject.LinesMain.Any(l => l.UId == line.UId && l.ProjectId == model.Id))
        //        {
        //            linemodel = contextProject.LinesMain.Single(l => l.UId == line.UId && l.ProjectId == model.Id);
        //        }
        //        else
        //        {
        //            linemodel = new LineModel(model.Id);
        //            contextProject.LinesMain.Add(linemodel);
        //            contextProject.SaveChanges();
        //            line.UId = linemodel.UId;
        //        }
        //        linemodel.Id = line.Id;
        //        linemodel.Name = line.Name;
        //        linemodel.IsExpanded = line.IsExpanded;
        //        contextProject.LinesMain.Update(linemodel);

        //        foreach (LineMiddle linem in line.Subs)
        //        {
        //            LineMiddleModel linemiddlemodel;
        //            if (contextProject.LinesMiddle.Any(l => l.UId == linem.UId && l.ProjectId == model.Id))
        //            {
        //                linemiddlemodel = contextProject.LinesMiddle.Single(l => l.UId == linem.UId && l.ProjectId == model.Id);
        //            }
        //            else
        //            {
        //                linemiddlemodel = new LineMiddleModel(model.Id);
        //                contextProject.LinesMiddle.Add(linemiddlemodel);
        //                contextProject.SaveChanges();
        //                linem.UId = linemiddlemodel.UId;
        //            }
        //            linemiddlemodel.Id = linem.Id;
        //            linemiddlemodel.Name = linem.Name;
        //            linemiddlemodel.IsExpanded = linem.IsExpanded;
        //            linemiddlemodel.ParentId = line.UId;
        //            contextProject.LinesMiddle.Update(linemiddlemodel);


        //            foreach (LineDevice linedev in linem.Subs)
        //            {
        //                LineDeviceModel linedevmodel;
        //                if (contextProject.LineDevices.Any(l => l.UId == linedev.UId && l.ProjectId == model.Id))
        //                {
        //                    linedevmodel = contextProject.LineDevices.Single(l => l.UId == linedev.UId && l.ProjectId == model.Id);
        //                }
        //                else
        //                {
        //                    linedevmodel = new LineDeviceModel(model.Id);
        //                    contextProject.LineDevices.Add(linedevmodel);
        //                    contextProject.SaveChanges();
        //                    linedev.UId = linedevmodel.UId;
        //                }
        //                linedevmodel.Id = linedev.Id;
        //                linedevmodel.ParentId = linem.UId;
        //                linedevmodel.Name = linedev.Name;
        //                linedevmodel.Serial = linedev.Serial;
        //                linedevmodel.ApplicationId = linedev.ApplicationId;
        //                linedevmodel.DeviceId = linedev.DeviceId;

        //                IEnumerable<ComObject> removeComs = contextProject.ComObjects.Where(co => co.DeviceId == linedev.UId).ToList();
        //                contextProject.ComObjects.RemoveRange(removeComs);
        //                foreach (DeviceComObject comObj in linedev.ComObjects)
        //                {
        //                    List<string> groupIds = new List<string>();

        //                    foreach (FunctionGroup ga in comObj.Groups)
        //                        groupIds.Add(ga.Address.ToString());

        //                    //TODO reimplement
        //                    //ComObject com;
        //                    //if (contextProject.ComObjects.Any(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId))
        //                    //{
        //                    //    com = contextProject.ComObjects.Single(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId);
        //                    //    com.Groups = string.Join(",", groupIds);
        //                    //    contextProject.ComObjects.Update(com);
        //                    //}
        //                    //else
        //                    //{
        //                    //    com = new ComObject
        //                    //    {
        //                    //        ComId = comObj.Id,
        //                    //        DeviceId = linedev.UId,
        //                    //        Groups = string.Join(",", groupIds)
        //                    //    };
        //                    //    contextProject.ComObjects.Add(com);
        //                    //}
        //                }

        //                contextProject.LineDevices.Update(linedevmodel);
        //            }

        //            //List<LineDeviceModel> dev2delete = contextProject.LineDevices.Where(d => d.ProjectId == model.Id && d.ParentId == linem.Id && !linem.Subs.Any(lm => lm.UId == d.UId)).ToList();
        //            IEnumerable<LineDeviceModel> dev2delete = contextProject.LineDevices.AsEnumerable().Where(d => d.ProjectId == model.Id && d.ParentId == linem.UId && !linem.Subs.Any(lm => lm.UId == d.UId)).AsEnumerable();
        //            contextProject.LineDevices.RemoveRange(dev2delete);
        //        }


        //        IEnumerable<LineMiddleModel> linem2delete = contextProject.LinesMiddle.AsEnumerable().Where(d => d.ProjectId == model.Id && d.ParentId == line.Id && !line.Subs.Any(lm => lm.UId == d.UId)).AsEnumerable();
        //        contextProject.LinesMiddle.RemoveRange(linem2delete);
        //    }


        //    IEnumerable<LineModel> line2delete = contextProject.LinesMain.AsEnumerable().Where(d => d.ProjectId == model.Id && !_project.Lines.Any(lm => lm.UId == d.UId)).AsEnumerable();
        //    contextProject.LinesMain.RemoveRange(line2delete);


        //    contextProject.Projects.Update(model);
        //    contextProject.SaveChanges();

        //    return model;
        //}

        public static void UpdateDevice(LineDevice dev)
        {
            LineDeviceModel model = contextProject.LineDevices.Single(d => d.UId == dev.UId);
            model.ApplicationId = dev.ApplicationId;
            model.DeviceId = dev.DeviceId;
            model.Id = dev.Id;
            model.Name = dev.Name;
            model.ParentId = dev.Parent.UId;
            model.LoadedApp = dev.LoadedApplication;
            model.LoadedGA = dev.LoadedGroup;
            model.LoadedPA = dev.LoadedPA;
            model.Serial = dev.Serial;
            model.IsDeactivated = dev.IsDeactivated;
            model.LastGroupCount = dev.LastGroupCount;

            contextProject.LineDevices.Update(model);
            contextProject.SaveChanges();
        }

        public static void SaveStructure()
        {
            ProjectModel model = contextProject.Projects.Single(p => p.Id == _project.Id);
            model.Area = FunctionHelper.ObjectToByteArray(_project.Area);
            contextProject.Update(model);
            contextProject.SaveChanges();
        }

        public static void SaveLine(Line line)
        {
            LineModel linemodel;
            if (contextProject.LinesMain.Any(l => l.UId == line.UId && l.ProjectId == _project.Id))
            {
                linemodel = contextProject.LinesMain.Single(l => l.UId == line.UId && l.ProjectId == _project.Id);
            }
            else
            {
                linemodel = new LineModel(_project.Id);
                contextProject.LinesMain.Add(linemodel);
                contextProject.SaveChanges();
                line.UId = linemodel.UId;
            }
            linemodel.Id = line.Id;
            linemodel.Name = line.Name;
            linemodel.IsExpanded = line.IsExpanded;
            contextProject.LinesMain.Update(linemodel);
            contextProject.SaveChanges();
        }

        public static void SaveLine(LineMiddle line)
        {
            LineMiddleModel linemodel;
            if (contextProject.LinesMiddle.Any(l => l.UId == line.UId && l.ProjectId == _project.Id))
            {
                linemodel = contextProject.LinesMiddle.Single(l => l.UId == line.UId && l.ProjectId == _project.Id);
            }
            else
            {
                linemodel = new LineMiddleModel(_project.Id);
                contextProject.LinesMiddle.Add(linemodel);
                contextProject.SaveChanges();
                line.UId = linemodel.UId;
            }
            linemodel.Id = line.Id;
            linemodel.Name = line.Name;
            linemodel.IsExpanded = line.IsExpanded;
            linemodel.ParentId = line.Parent.UId;
            contextProject.LinesMiddle.Update(linemodel);
            contextProject.SaveChanges();
        }

        public static void SaveAssociations(LineDevice linedev)
        {
            IEnumerable<ComObject> removeComs = contextProject.ComObjects.Where(co => co.DeviceId == linedev.UId).ToList();
            contextProject.ComObjects.RemoveRange(removeComs);

            foreach (DeviceComObject comObj in linedev.ComObjects)
            {
                List<string> groupIds = new List<string>();

                foreach (FunctionGroup ga in comObj.Groups)
                    groupIds.Add(ga.Address.ToString());

                ComObject com;
                if (contextProject.ComObjects.Any(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId))
                {
                    try
                    {
                        com = contextProject.ComObjects.Single(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId);
                    }
                    catch
                    {
                        List<ComObject> objs = contextProject.ComObjects.Where(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId).ToList();
                        Debug.WriteLine("Es waren " + objs.Count + " ComObjects vorhanden");
                        com = objs[0];
                        objs.Remove(com);
                        contextProject.ComObjects.RemoveRange(objs);
                    }
                    com.Groups = string.Join(",", groupIds);
                    contextProject.ComObjects.Update(com);
                }
                else
                {
                    com = new ComObject
                    {
                        ComId = comObj.Id,
                        DeviceId = linedev.UId,
                        Groups = string.Join(",", groupIds)
                    };
                    contextProject.ComObjects.Add(com);
                }
            }
            contextProject.SaveChanges();
        }

        public static async Task<Project.Project> LoadProject(ProjectViewHelper helper)
        {
            Project.Project project = new Project.Project();

            using(LocalContext con = new LocalContext())
            {
                LocalConnectionProject lconn;
                try
                {
                    lconn = con.ConnsProject.Single(p => p.Id == helper.Local.ConnectionId);
                }
                catch
                {
                    Serilog.Log.Error($"Project-Verbindung {helper.Local.ConnectionId} konnte nicht gefunden werden.");
                    ViewHelper.Instance.ShowNotification("all", "Die Verbindung wo das Projekt gespeichert sein soll konnt enicht gefunden werden.", 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                    return null;
                }
                connProject = lconn;
                contextProject = new ProjectContext(lconn);
                contextProject.Database.Migrate();
                project.Connection = lconn;
                connProject = lconn;
            }

            //Catalog mit in das Project machen!
            contextC = new CatalogContext(new LocalConnectionCatalog() { DbHostname = "Catalog.db", Type = LocalConnectionCatalog.DbConnectionType.SqlLite });
            contextC.Database.Migrate();
            //TODO do when project is opening

            ProjectModel pm = contextProject.Projects.Single(p => p.Id == helper.ProjectId);
            project.Name = pm.Name;
            project.Id = pm.Id;
            project.Image = pm.Image;

            Dictionary<string, FunctionGroup> groups = new Dictionary<string, FunctionGroup>();

            if(pm.Area != null)
            {
                project.Area = FunctionHelper.ByteArrayToObject<Area>(pm.Area);
                foreach (Building b in project.Area.Buildings)
                {
                    b.ParentArea = project.Area;
                    foreach(Floor fl in b.Floors)
                    {
                        fl.ParentBuilding = b;
                        foreach(Room ro in fl.Rooms)
                        {
                            ro.ParentFloor = fl;
                            foreach(Function f in ro.Functions)
                            {
                                f.ParentRoom = ro;
                                foreach (FunctionGroup fg in f.Subs)
                                {
                                    fg.ParentFunction = f;
                                    groups.Add(fg.Address.ToString(), fg);
                                }
                            }
                        }
                    }
                }
            }



            //Hier DPS laden
            Dictionary<string, Dictionary<string, DataPointSubType>> DPSTs = await SaveHelper.GenerateDatapoints();


            foreach (LineModel lmodel in contextProject.LinesMain.AsEnumerable().Where(l => l.ProjectId == helper.ProjectId).OrderBy(l => l.Id))
            {
                Line line = new Line(lmodel);
                project.Lines.Add(line);

                foreach (LineMiddleModel lmm in contextProject.LinesMiddle.AsEnumerable().Where(l => l.ProjectId == helper.ProjectId && l.ParentId == line.UId).OrderBy(l => l.Id))
                {
                    LineMiddle lm = new LineMiddle(lmm, line);
                    line.Subs.Add(lm);

                    foreach (LineDeviceModel ldm in contextProject.LineDevices.AsEnumerable().Where(l => l.ProjectId == helper.ProjectId && l.ParentId == lm.UId).OrderBy(l => l.Id))
                    {
                        LineDevice ld = new LineDevice(ldm, lm, true) { DeviceId = ldm.DeviceId };


                        foreach (ComObject com in contextProject.ComObjects.Where(co => co.DeviceId == ld.UId))
                        {
                            AppComObject comObj = contextC.AppComObjects.Single(c => c.Id == com.ComId && c.ApplicationId == ldm.ApplicationId);
                            DeviceComObject dcom = new DeviceComObject(comObj) { ParentDevice = ld };

                            if (!string.IsNullOrEmpty(com.Groups))
                            {
                                string[] ids = com.Groups.Split(",");
                                foreach (string id_str in ids)
                                {
                                    FunctionGroup ga = groups[id_str];
                                    dcom.Groups.Add(ga); 
                                    ga.ComObjects.Add(dcom);
                                }
                            }

                            //TODO check what to do if binding exists
                            //if (dcom.BindedId != -2 && dcom.Name.Contains("{{dyn}}"))
                            //{
                            //    string value = "";
                            //    try
                            //    {
                            //        ChangeParamModel changeB = contextProject.ChangesParam.Where(c => c.DeviceId == ld.UId && c.ParamId == dcom.BindedId).OrderByDescending(c => c.StateId).First();
                            //        value = changeB.Value;
                            //    }
                            //    catch { }

                            //    if (value == "")
                            //        dcom.DisplayName = dcom.Name.Replace("{{dyn}}", comObj.BindedDefaultText);
                            //    else
                            //        dcom.DisplayName = dcom.Name.Replace("{{dyn}}", value);
                            //} else
                            //{
                            //    dcom.DisplayName = dcom.Name;
                            //}


                            if (comObj.Datapoint == -1)
                            {
                                dcom.DataPointSubType = new DataPointSubType() { SizeInBit = comObj.Size, Name = "x Bytes", Number = "..." };
                            }
                            else
                            {
                                if (comObj.DatapointSub == -1)
                                {
                                    dcom.DataPointSubType = DPSTs[comObj.Datapoint.ToString()]["xxx"];
                                }
                                else
                                {
                                    dcom.DataPointSubType = DPSTs[comObj.Datapoint.ToString()][comObj.DatapointSub.ToString()];
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

        public static void DeleteProject(ProjectViewHelper helper)
        {
            using (LocalContext con = new LocalContext())
            {
                LocalConnectionProject lconn;
                try
                {
                    lconn = con.ConnsProject.Single(p => p.Id == helper.Local.ConnectionId);
                }
                catch
                {
                    Serilog.Log.Error($"Project-Verbindung {helper.Local.ConnectionId} konnte nicht gefunden werden.");
                    ViewHelper.Instance.ShowNotification("all", "Die Verbindung wo das Projekt gespeichert sein soll konnt enicht gefunden werden.", 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                    return;
                }
                contextProject = new ProjectContext(lconn);
                contextProject.Database.Migrate();
            }

            List<ProjectModel> ps = contextProject.Projects.Where(p => p.Id == helper.ProjectId).ToList();
            contextProject.Projects.RemoveRange(ps);
            ps = null;

            List<LineModel> ls = contextProject.LinesMain.Where(l => l.ProjectId == helper.ProjectId).ToList();
            contextProject.LinesMain.RemoveRange(ls);
            ls = null;

            List<LineMiddleModel> lms = contextProject.LinesMiddle.Where(l => l.ProjectId == helper.ProjectId).ToList();
            contextProject.LinesMiddle.RemoveRange(lms);
            lms = null;

            List<LineDeviceModel> lds = contextProject.LineDevices.Where(l => l.ProjectId == helper.ProjectId).ToList();
            contextProject.LineDevices.RemoveRange(lds);
            foreach (LineDeviceModel dev in lds)
            {
                IEnumerable<ChangeParamModel> changes = contextProject.ChangesParam.Where(c => c.DeviceId == dev.UId);
                contextProject.ChangesParam.RemoveRange(changes);
                changes = null;
                IEnumerable<ComObject> comobjs = contextProject.ComObjects.Where(c => c.DeviceId == dev.UId);
                contextProject.ComObjects.RemoveRange(comobjs);
            }
            lds = null;

            IEnumerable<StateModel> states = contextProject.States.Where(s => s.ProjectId == helper.ProjectId);
            contextProject.States.RemoveRange(states);
            states = null;

            contextProject.SaveChanges();
        }


        public static async Task<Dictionary<string, Dictionary<string, DataPointSubType>>> GenerateDatapoints()
        {
            Dictionary<string, Dictionary<string, DataPointSubType>> DPSTs = new Dictionary<string, Dictionary<string, DataPointSubType>>();

            if (await ApplicationData.Current.LocalFolder.FileExistsAsync("DataPoints.json"))
            {
                string json2 = await FileIO.ReadTextAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("DataPoints.json"));
                DPSTs = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, DataPointSubType>>>(json2);
                return DPSTs;
            }
            else
            {
                DPSTs = new Dictionary<string, Dictionary<string, DataPointSubType>>();
            }

            StorageFile defaultFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Data/knx_dps.xml"));
            XElement xml = XDocument.Parse(await FileIO.ReadTextAsync(defaultFile)).Root;


            string current = System.Globalization.CultureInfo.CurrentCulture.Name;

            List<string> langs = new List<string>();
            IEnumerable<XElement> langsEle = xml.Descendants(XName.Get("Language", xml.Name.NamespaceName));
            foreach (XElement lang in langsEle)
                langs.Add(lang.Attribute("Identifier").Value);

            if (langs.Contains(current))
            {
                TranslateXml(xml, current);
            }
            else
            {
                current = current.Split("-")[0] + "-";
                if (langs.Any(l => l.StartsWith(current)))
                {
                    string x = langs.Single(l => l.StartsWith(current));
                    TranslateXml(xml, x);
                }
            }

            IEnumerable<XElement> dpts = xml.Descendants(XName.Get("DatapointType", xml.Name.NamespaceName));
            foreach (XElement dpt in dpts)
            {
                string numb = dpt.Attribute("Number").Value;
                DataPointSubType dpstd = new DataPointSubType
                {
                    Name = "",
                    Number = "xxx",
                    SizeInBit = int.Parse(dpt.Attribute("SizeInBit").Value),
                    MainNumber = numb
                };
                DPSTs.Add(numb, new Dictionary<string, DataPointSubType>());
                DPSTs[numb].Add(dpstd.Number, dpstd);

                foreach (XElement dpstE in dpt.Element(XName.Get("DatapointSubtypes", xml.Name.NamespaceName)).Elements())
                {
                    DataPointSubType dpst = new DataPointSubType
                    {
                        Name = dpstE.Attribute("Text").Value,
                        Number = dpstE.Attribute("Number").Value,
                        MainNumber = numb,
                        Default = dpstE.Attribute("Default")?.Value == "true",
                        SizeInBit = int.Parse(dpt.Attribute("SizeInBit").Value)
                    };

                    DPSTs[numb].Add(dpst.Number, dpst);
                }
            }

            StorageFile file2 = await ApplicationData.Current.LocalFolder.CreateFileAsync("DataPoints.json");
            string json3 = Newtonsoft.Json.JsonConvert.SerializeObject(DPSTs);
            await FileIO.WriteTextAsync(file2, json3);

            return DPSTs;
        }

        private static void TranslateXml(XElement xml, string selectedLang)
        {
            if (selectedLang == null) return;



            if (!xml.Descendants(XName.Get("Language", xml.Name.NamespaceName)).Any(l => l.Attribute("Identifier").Value.ToLower() == selectedLang.ToLower()))
            {
                return;
            }


            Dictionary<string, Dictionary<string, string>> transl = new Dictionary<string, Dictionary<string, string>>();

            var x = xml.Descendants(XName.Get("Language", xml.Name.NamespaceName)).Where(l => l.Attribute("Identifier").Value.ToLower() == selectedLang.ToLower());
            XElement lang = xml.Descendants(XName.Get("Language", xml.Name.NamespaceName)).Single(l => l.Attribute("Identifier").Value.ToLower() == selectedLang.ToLower());
            List<XElement> trans = lang.Descendants(XName.Get("TranslationElement", xml.Name.NamespaceName)).ToList();

            foreach (XElement translate in trans)
            {
                string id = translate.Attribute("RefId").Value;

                Dictionary<string, string> translations = new Dictionary<string, string>();

                foreach (XElement transele in translate.Elements())
                {
                    translations.Add(transele.Attribute("AttributeName").Value, transele.Attribute("Text").Value);
                }

                transl.Add(id, translations);
            }


            foreach (XElement ele in xml.Descendants())
            {
                if (ele.Attribute("Id") == null || !transl.ContainsKey(ele.Attribute("Id").Value)) continue;
                string eleId = ele.Attribute("Id").Value;


                foreach (string attr in transl[eleId].Keys)
                {
                    if (ele.Attribute(attr) != null)
                    {
                        ele.Attribute(attr).Value = transl[eleId][attr];
                    }
                    else
                    {
                        ele.Add(new XAttribute(XName.Get(attr), transl[eleId][attr]));
                    }
                }

            }
        }


        private static Dictionary<string, ParamBinding> Hash2Bindings;
        private static Dictionary<int, List<ParamBinding>> Ref2Bindings;
        private static List<int> updatedComs;
        private static List<AssignParameter> Assignments;

        //Nochmal in ImportHelper
        public static string ShortId(string id)
        {
            string temp = id.Substring(0, 16);


            if (id.Contains("_R-"))
            {
                temp += id.Substring(id.LastIndexOf("_"));
            }
            else
            {
                temp += id.Substring(id.IndexOf("_", 16));
            }

            return temp;
        }

        public static int GetItemId(string id)
        {
            return int.Parse(id.Substring(id.LastIndexOf("-") + 1));
        }

        //TODO Id2Param notwendig machen!
        public static bool CheckConditions(int applicationId, List<ParamCondition> conds, Dictionary<int, ViewParamModel> Id2Param)
        {
            Dictionary<int, string> tempValues = new Dictionary<int, string>();
            bool flag = true;

            foreach (ParamCondition cond in conds)
            {
                if (flag == false) break;
                string paraValue = "";
                if (Id2Param != null && Id2Param.ContainsKey(cond.SourceId))
                {
                    ViewParamModel model = Id2Param[cond.SourceId];

                    if(model.Assign == null)
                    {
                        paraValue = model.Value;
                        if (!model.Parameters.Any(p => p.IsVisible))
                        {
                            flag = false;
                            continue;
                        }
                    } else
                    {
                        if (model.Assign.Source == -1)
                            paraValue = model.Assign.Value;
                        else
                            paraValue = Id2Param[model.Assign.Source].Value;
                        if (!Id2Param[model.Assign.Source].Parameters.Any(p => p.IsVisible))
                        {
                            flag = false;
                            continue;
                        }
                    }



                    
                }
                else
                {
                    if (tempValues.ContainsKey(cond.SourceId))
                        paraValue = tempValues[cond.SourceId];
                    else
                    {
                        AppParameter pbPara = contextC.AppParameters.Single(p => p.ParameterId == cond.SourceId && p.ApplicationId == applicationId);
                        paraValue = pbPara.Value;
                        tempValues.Add(cond.SourceId, paraValue);
                    }
                }

                switch (cond.Operation)
                {
                    case ConditionOperation.IsInValue:
                        if (!cond.Values.Split(",").Contains(paraValue))
                            flag = false;
                        break;

                    case ConditionOperation.Default:
                        string[] defConds = cond.Values.Split(",");
                        int paraValInt = int.Parse(paraValue);

                        foreach(string defCond in defConds)
                        {
                            if (!flag) break;

                            if (defCond.StartsWith("<="))
                            {
                                int def = int.Parse(defCond.Substring(2));
                                if (paraValInt <= def) flag = false;
                            }
                            else if (defCond.StartsWith("<"))
                            {
                                int def = int.Parse(defCond.Substring(1));
                                if (paraValInt < def) flag = false;
                            }
                            else if (defCond.StartsWith(">="))
                            {
                                int def = int.Parse(defCond.Substring(2));
                                if (paraValInt >= def) flag = false;
                            }
                            else if (defCond.StartsWith(">"))
                            {
                                int def = int.Parse(defCond.Substring(1));
                                if (paraValInt > def) flag = false;
                            }
                            else
                            {
                                int def = int.Parse(defCond);
                                if (paraValInt == def) flag = false;
                            }
                        }
                        break;

                    case ConditionOperation.NotEqual:
                        if (cond.Values == paraValue)
                            flag = false;
                        break;

                    case ConditionOperation.Equal:
                        if (cond.Values != paraValue)
                            flag = false;
                        break;

                    case ConditionOperation.LowerThan:
                        int valLT = int.Parse(paraValue);
                        int valLTo = int.Parse(cond.Values);
                        if ((valLT < valLTo) == false)
                            flag = false;
                        break;

                    case ConditionOperation.LowerEqualThan:
                        int valLET = int.Parse(paraValue);
                        int valLETo = int.Parse(cond.Values);
                        if ((valLET <= valLETo) == false)
                            flag = false;
                        break;

                    case ConditionOperation.GreatherThan:
                        int valGT = int.Parse(paraValue);
                        int valGTo = int.Parse(cond.Values);
                        if ((valGT > valGTo) == false)
                            flag = false;
                        break;

                    case ConditionOperation.GreatherEqualThan:
                        int valGET = int.Parse(paraValue);
                        int valGETo = int.Parse(cond.Values);
                        if ((valGET >= valGETo) == false)
                            flag = false;
                        break;
                }
            }

            return flag;
        }


        public static void CalculateLineCurrent(LineMiddle line, bool noNotify = false)
        {
            if (!contextC.Devices.AsEnumerable().Any(d => d.IsPowerSupply && line.Subs.Any(l => l.DeviceId == d.Id)))
            {
                line.State = LineState.Normal;
                return;
            }

            int maxCurrent = CalculateLineCurrentAvailible(line);
            int current = CalculateLineCurrentUsed(line);

            //Todo schwelle EInstellbar machen


            ApplicationDataContainer container = ApplicationData.Current.LocalSettings;
            if(container.Values["minLineCurrent"] == null)
            {
                container.Values["minLineCurrent"] = 80;
            }
            int minCurrent = (int)container.Values["minLineCurrent"];

            if ((maxCurrent - current) <= 0)
            {
                line.State = LineState.Overloaded;
                if (!noNotify) ViewHelper.Instance.ShowNotification("main", $"Die Spannungsquelle der Linie {line.LineName} ist möglicherweise nicht ausreichend.\r\n(Verfügbar: {maxCurrent} Berechnet: {current}", 5000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning);
            }
            else if ((maxCurrent - current) < 80)
            {
                line.State = LineState.Warning;
                if (!noNotify) ViewHelper.Instance.ShowNotification("main", "In der Linie " + line.LineName + " sind nur noch " + (maxCurrent - current) + " mA Reserve verfügbar.", 5000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational);
            }
            else
                line.State = LineState.Normal;

        }

        public static int CalculateLineCurrentAvailible(LineMiddle line)
        {
            int maxCurrent = 0;

            foreach (LineDevice dev in line.Subs)
            {
                if (!contextC.Devices.Any(s => s.Id == dev.DeviceId)) continue;

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
                if (!contextC.Devices.Any(s => s.Id == dev.DeviceId)) continue;

                DeviceViewModel model = contextC.Devices.Single(s => s.Id == dev.DeviceId);

                if (!model.IsPowerSupply)
                    current += model.BusCurrent;
            }

            return current;
        }




        public static int StringToInt(string input, int def = 0)
        {
            return (int)StringToFloat(input, (float)def);
        }

        public static float StringToFloat(string input, float def = 0)
        {
            if (input == null) return def;

            if (input.ToLower().Contains("e+"))
            {
                float numb = float.Parse(input.Substring(0, 5).Replace('.', ','));
                int expo = int.Parse(input.Substring(input.IndexOf('+') + 1));
                if (expo == 0)
                    return int.Parse(numb.ToString());
                float res = numb * (10 * expo);
                return res;
            }

            try
            {
                return float.Parse(input);
            }
            catch
            {
                return def;
            }
        }

    }
}
