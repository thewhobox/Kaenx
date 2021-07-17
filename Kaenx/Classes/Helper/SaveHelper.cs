using Kaenx.Classes.Buildings;
using Kaenx.Classes.Dynamic;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
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

        private static Dictionary<int, AppParameter> AppParas;
        private static Dictionary<int, AppParameterTypeViewModel> AppParaTypes;
        private static Dictionary<int, AppComObject> ComObjects;

        public static ProjectModel SaveProject(Project.Project _pro = null)
        {
            if (_pro != null)
            {
                _project = _pro;

                connProject = _project.Connection;
                contextProject = new ProjectContext(_project.Connection);
                contextProject.Database.Migrate();
            }
            if (_project == null)
                return null;


            ProjectModel model;

            if (!contextProject.Projects.Any(p => p.Id == _project.Id))
            { 
                model = new ProjectModel();
                contextProject.Projects.Add(model);
                contextProject.SaveChanges();
            }
            else
            {
                model = contextProject.Projects.Single(p => p.Id == _project.Id);
            }

            model.Name = _project.Name;
            model.Image = _project.Image;
            model.Area = ObjectToByteArray(_project.Area);

            foreach (Line line in _project.Lines)
            {
                LineModel linemodel;
                if (contextProject.LinesMain.Any(l => l.UId == line.UId && l.ProjectId == model.Id))
                {
                    linemodel = contextProject.LinesMain.Single(l => l.UId == line.UId && l.ProjectId == model.Id);
                }
                else
                {
                    linemodel = new LineModel(model.Id);
                    contextProject.LinesMain.Add(linemodel);
                    contextProject.SaveChanges();
                    line.UId = linemodel.UId;
                }
                linemodel.Id = line.Id;
                linemodel.Name = line.Name;
                linemodel.IsExpanded = line.IsExpanded;
                contextProject.LinesMain.Update(linemodel);

                foreach (LineMiddle linem in line.Subs)
                {
                    LineMiddleModel linemiddlemodel;
                    if (contextProject.LinesMiddle.Any(l => l.UId == linem.UId && l.ProjectId == model.Id))
                    {
                        linemiddlemodel = contextProject.LinesMiddle.Single(l => l.UId == linem.UId && l.ProjectId == model.Id);
                    }
                    else
                    {
                        linemiddlemodel = new LineMiddleModel(model.Id);
                        contextProject.LinesMiddle.Add(linemiddlemodel);
                        contextProject.SaveChanges();
                        linem.UId = linemiddlemodel.UId;
                    }
                    linemiddlemodel.Id = linem.Id;
                    linemiddlemodel.Name = linem.Name;
                    linemiddlemodel.IsExpanded = linem.IsExpanded;
                    linemiddlemodel.ParentId = line.UId;
                    contextProject.LinesMiddle.Update(linemiddlemodel);


                    foreach (LineDevice linedev in linem.Subs)
                    {
                        LineDeviceModel linedevmodel;
                        if (contextProject.LineDevices.Any(l => l.UId == linedev.UId && l.ProjectId == model.Id))
                        {
                            linedevmodel = contextProject.LineDevices.Single(l => l.UId == linedev.UId && l.ProjectId == model.Id);
                        }
                        else
                        {
                            linedevmodel = new LineDeviceModel(model.Id);
                            contextProject.LineDevices.Add(linedevmodel);
                            contextProject.SaveChanges();
                            linedev.UId = linedevmodel.UId;
                        }
                        linedevmodel.Id = linedev.Id;
                        linedevmodel.ParentId = linem.UId;
                        linedevmodel.Name = linedev.Name;
                        linedevmodel.Serial = linedev.Serial;
                        linedevmodel.ApplicationId = linedev.ApplicationId;
                        linedevmodel.DeviceId = linedev.DeviceId;

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
                                com = contextProject.ComObjects.Single(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId);
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

                        contextProject.LineDevices.Update(linedevmodel);
                    }

                    //List<LineDeviceModel> dev2delete = contextProject.LineDevices.Where(d => d.ProjectId == model.Id && d.ParentId == linem.Id && !linem.Subs.Any(lm => lm.UId == d.UId)).ToList();
                    IEnumerable<LineDeviceModel> dev2delete = contextProject.LineDevices.AsEnumerable().Where(d => d.ProjectId == model.Id && d.ParentId == linem.Id && !linem.Subs.Any(lm => lm.UId == d.UId)).AsEnumerable();
                    contextProject.LineDevices.RemoveRange(dev2delete);
                }


                IEnumerable<LineMiddleModel> linem2delete = contextProject.LinesMiddle.AsEnumerable().Where(d => d.ProjectId == model.Id && d.ParentId == line.Id && !line.Subs.Any(lm => lm.UId == d.UId)).AsEnumerable();
                contextProject.LinesMiddle.RemoveRange(linem2delete);
            }


            IEnumerable<LineModel> line2delete = contextProject.LinesMain.AsEnumerable().Where(d => d.ProjectId == model.Id && !_project.Lines.Any(lm => lm.UId == d.UId)).AsEnumerable();
            contextProject.LinesMain.RemoveRange(line2delete);


            contextProject.Projects.Update(model);
            contextProject.SaveChanges();

            return model;
        }

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
            model.Area = ObjectToByteArray(_project.Area);
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

        //public static void SaveComObject(DeviceComObject com)
        //{
        //    ComObject com;
        //    if (contextProject.ComObjects.Any(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId))
        //    {
        //        com = contextProject.ComObjects.Single(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId);
        //        com.Groups = string.Join(",", groupIds);
        //        contextProject.ComObjects.Update(com);
        //    }
        //    else
        //    {
        //        com = new ComObject();
        //        com.ComId = comObj.Id;
        //        com.DeviceId = linedev.UId;
        //        com.Groups = string.Join(",", groupIds);
        //        contextProject.ComObjects.Add(com);
        //    }
        //}

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
                project.Area = ByteArrayToObject<Area>(pm.Area);
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

                            if (dcom.BindedId != -2 && dcom.Name.Contains("{{dyn}}"))
                            {
                                string value = "";
                                try
                                {
                                    ChangeParamModel changeB = contextProject.ChangesParam.Where(c => c.DeviceId == ld.UId && c.ParamId == dcom.BindedId).OrderByDescending(c => c.StateId).First();
                                    value = changeB.Value;
                                }
                                catch { }

                                if (value == "")
                                    dcom.DisplayName = dcom.Name.Replace("{{dyn}}", comObj.BindedDefaultText);
                                else
                                    dcom.DisplayName = dcom.Name.Replace("{{dyn}}", value);
                            } else
                            {
                                dcom.DisplayName = dcom.Name;
                            }


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
                        ld.IsInit = false;
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
                ImportHelper.TranslateXml(xml, current);
            }
            else
            {
                current = current.Split("-")[0] + "-";
                if (langs.Any(l => l.StartsWith(current)))
                {
                    string x = langs.Single(l => l.StartsWith(current));
                    ImportHelper.TranslateXml(xml, x);
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

        public static Dictionary<int, ViewParamModel> GenerateDynamic(AppAdditional adds)
        {
            XDocument dynamic = XDocument.Parse(Encoding.UTF8.GetString(adds.Dynamic));
            XmlReader reader = dynamic.CreateReader();

            updatedComs = new List<int>();
            Hash2Bindings = new Dictionary<string, ParamBinding>();
            Ref2Bindings = new Dictionary<int, List<ParamBinding>>();
            Assignments = new List<AssignParameter>();
            Dictionary<int, XElement> Id2Element = new Dictionary<int, XElement>();
            Dictionary<int, ParameterBlock> Id2ParamBlock = new Dictionary<int, ParameterBlock>();
            List<IDynChannel> Channels = new List<IDynChannel>();
            IDynChannel currentChannel = null;

            foreach(XElement ele in dynamic.Root.Descendants(XName.Get("ParameterBlock", dynamic.Root.Name.NamespaceName)))
            {
                Id2Element.Add(GetItemId(ele.Attribute("Id").Value), ele);
            }
            foreach(XElement ele in dynamic.Root.Descendants(XName.Get("Channel", dynamic.Root.Name.NamespaceName)))
            {
                Id2Element.Add(GetItemId(ele.Attribute("Id").Value), ele);
            }

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement) continue;
                string text = "";

                switch (reader.LocalName)
                {
                    case "ChannelIndependentBlock":
                        ChannelIndependentBlock cib = new ChannelIndependentBlock();
                        if (reader.GetAttribute("Access") == "None")
                        {
                            cib.HasAccess = false;
                            cib.Visible = Visibility.Collapsed;
                        }
                        currentChannel = cib;
                        Channels.Add(cib);
                        break;

                    case "Channel":
                        if(reader.GetAttribute("Text") == "")
                        {
                            ChannelIndependentBlock cib2 = new ChannelIndependentBlock();
                            if (reader.GetAttribute("Access") == "None")
                            {
                                cib2.HasAccess = false;
                                cib2.Visible = Visibility.Collapsed;
                            }
                            currentChannel = cib2;
                            Channels.Add(cib2);
                        } else
                        {
                            ChannelBlock cb = new ChannelBlock
                            {
                                Id = GetItemId(reader.GetAttribute("Id")),
                                Name = reader.GetAttribute("Name")
                            };
                            if (reader.GetAttribute("Access") == "None")
                            {
                                cb.HasAccess = false;
                                cb.Visible = Visibility.Collapsed;
                            }

                            text = reader.GetAttribute("Text");

                            if (text.Contains("{{"))
                            {
                                ParamBinding bind = new ParamBinding()
                                {
                                    Hash = "CB:" + cb.Id
                                };

                                Regex reg = new Regex("{{((.+):(.+))}}");
                                Match m = reg.Match(text);
                                if (m.Success)
                                {
                                    bind.DefaultText = m.Groups[3].Value;
                                    cb.DefaultText = m.Groups[3].Value;
                                    cb.Text = text.Replace(m.Value, "{{dyn}}");
                                    if (m.Groups[2].Value == "0")
                                    {
                                        string textId = reader.GetAttribute("TextParameterRefId");
                                        if (string.IsNullOrEmpty(textId)) bind.SourceId = -1;
                                        else
                                        {
                                            bind.SourceId = GetItemId(textId);
                                        }
                                    }
                                    else
                                    {
                                        string refid = m.Groups[2].Value;
                                        bind.SourceId = int.Parse(m.Groups[2].Value);
                                    }
                                }
                                else
                                {
                                    reg = new Regex("{{(.+)}}");
                                    m = reg.Match(text);
                                    if (m.Success)
                                    {
                                        bind.DefaultText = "";
                                        cb.Text = text.Replace(m.Value, "{{dyn}}");
                                        if (m.Groups[1].Value == "0")
                                        {
                                            string textId = reader.GetAttribute("TextParameterRefId");
                                            if (string.IsNullOrEmpty(textId)) bind.SourceId = -1;
                                            else
                                            {
                                                bind.SourceId = GetItemId(textId);
                                            }
                                        }
                                        else
                                        {
                                            string refid = m.Groups[2].Value;
                                            bind.SourceId = int.Parse(m.Groups[2].Value);
                                        }
                                    }
                                }

                                Hash2Bindings.Add(bind.Hash, bind);
                            }
                            else
                                cb.Text = text;

                            cb.Conditions = GetConditions(Id2Element[cb.Id]);
                            Channels.Add(cb);
                            currentChannel = cb;
                        }
                        break;


                    case "ParameterBlock":
                        ParameterBlock pb = new ParameterBlock { Id = GetItemId(reader.GetAttribute("Id")) };
                        if (reader.GetAttribute("Access") == "None")
                        {
                            pb.HasAccess = false;
                            pb.Visible = Visibility.Collapsed;
                        }
                        if (reader.GetAttribute("ParamRefId") != null)
                        {
                            try
                            {
                                int paramId = GetItemId(reader.GetAttribute("ParamRefId"));
                                AppParameter para = contextC.AppParameters.Single(p => p.ParameterId == paramId && p.ApplicationId == adds.ApplicationId);
                                text = para.Text;
                                if (para.Access == AccessType.None)
                                {
                                    pb.HasAccess = false;
                                    pb.Visible = Visibility.Collapsed;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Parameterblock TextRef Fehler!", ex);
                            }
                        }
                        else
                            text = reader.GetAttribute("Text");

                        if (text?.Contains("{{") == true)
                        {
                            ParamBinding bind = new ParamBinding()
                            {
                                Hash = "PB:" + pb.Id
                            };

                            Regex reg = new Regex("{{((.+):(.+))}}");
                            Match m = reg.Match(text);
                            if (m.Success)
                            {
                                bind.DefaultText = m.Groups[3].Value;
                                pb.DefaultText = m.Groups[3].Value;
                                pb.Text = text.Replace(m.Value, "{{dyn}}");
                                if (m.Groups[2].Value == "0")
                                {
                                    string textId = reader.GetAttribute("TextParameterRefId");
                                    if (string.IsNullOrEmpty(textId)) bind.SourceId = -1;
                                    else
                                    {
                                        bind.SourceId = GetItemId(textId);
                                    }
                                }
                                else
                                {
                                    bind.SourceId = int.Parse(m.Groups[2].Value);
                                }
                            }
                            else
                            {
                                reg = new Regex("{{(.+)}}");
                                m = reg.Match(text);
                                if (m.Success)
                                {
                                    bind.DefaultText = "";
                                    pb.Text = text.Replace(m.Value, "{{dyn}}");
                                    if (m.Groups[1].Value == "0")
                                    {
                                        string textId = reader.GetAttribute("TextParameterRefId");
                                        if (string.IsNullOrEmpty(textId)) bind.SourceId = -1;
                                        else
                                        {
                                            bind.SourceId = GetItemId(textId);
                                        }
                                    }
                                    else
                                    {
                                        bind.SourceId = int.Parse(m.Groups[1].Value);
                                    }
                                }
                            }

                            Hash2Bindings.Add(bind.Hash, bind);
                        }
                        else
                            pb.Text = text;

                        pb.Conditions = GetConditions(Id2Element[pb.Id]);
                        currentChannel.Blocks.Add(pb);
                        Id2ParamBlock.Add(pb.Id, pb);
                        break;

                    case "Assign":
                    case "choose":
                    case "when":
                    case "Dynamic":
                    case "ParameterRefRef":
                    case "ParameterSeparator":
                    case "ComObjectRefRef":
                        break;

                    default:
                        Log.Warning("Unbekanntes Element in Dynamic: " + reader.LocalName);
                        System.Diagnostics.Debug.WriteLine("Unbekanntest Element in Dynamic: " + reader.LocalName);
                        break;
                }
            }

            AppParas = new Dictionary<int, AppParameter>();
            AppParaTypes = new Dictionary<int, AppParameterTypeViewModel>();
            ComObjects = new Dictionary<int, AppComObject>();

            foreach (AppParameter para in contextC.AppParameters.Where(p => p.ApplicationId == adds.ApplicationId))
                AppParas.Add(para.ParameterId, para);

            foreach (AppParameterTypeViewModel type in contextC.AppParameterTypes.Where(t => t.ApplicationId == adds.ApplicationId))
                AppParaTypes.Add(type.Id, type);

            foreach (AppComObject co in contextC.AppComObjects.Where(t => t.ApplicationId == adds.ApplicationId))
                ComObjects.Add(co.Id, co);



            List<XElement> channels = dynamic.Root.Descendants(XName.Get("ChannelIndependentBlock", dynamic.Root.Name.NamespaceName)).ToList();
            channels.AddRange(dynamic.Root.Descendants(XName.Get("Channel", dynamic.Root.Name.NamespaceName)));
            foreach (XElement eleCH in channels)
            {
                string groupText = eleCH.Attribute("Text")?.Value;
                foreach (XElement elePB in eleCH.Descendants(XName.Get("ParameterBlock", dynamic.Root.Name.NamespaceName)))
                {
                    int textRefId = -2;
                    if (elePB.Attribute("TextParameterRefId") != null)
                    {
                        textRefId = GetItemId(elePB.Attribute("TextParameterRefId").Value);
                    }
                    else
                    {
                        XElement temp = elePB.Parent;
                        while (textRefId != -2 || temp.Name.LocalName == "Dynamic")
                        {
                            temp = temp.Parent;
                            textRefId = GetItemId(temp.Attribute("TextParameterRefId")?.Value);
                        }
                    }
                    ParameterBlock block = Id2ParamBlock[GetItemId(elePB.Attribute("Id").Value)];
                    GetChildItems(elePB, block, textRefId, groupText);
                }
            }
             contextC.SaveChanges();

            Id2Element.Clear();
            Id2ParamBlock.Clear();
            #region Brechne Standard sichtbarkeit

            Dictionary<int, ViewParamModel> Id2Param = new Dictionary<int, ViewParamModel>();

            foreach (IDynChannel ch in Channels)
            {
                foreach (ParameterBlock block in ch.Blocks)
                {
                    foreach (IDynParameter para in block.Parameters)
                    {
                        if (!Id2Param.ContainsKey(para.Id))
                            Id2Param.Add(para.Id, new ViewParamModel(para.Value));

                        Id2Param[para.Id].Parameters.Add(para);
                    }
                }
            }
            //Berechne Standard Sichtbarkeit:
            foreach (IDynChannel ch in Channels)
            {
                if(ch.HasAccess)
                    ch.Visible = SaveHelper.CheckConditions(adds.ApplicationId, ch.Conditions, Id2Param) ? Visibility.Visible : Visibility.Collapsed;

                foreach (ParameterBlock block in ch.Blocks)
                {
                    if(block.HasAccess)
                        block.Visible = SaveHelper.CheckConditions(adds.ApplicationId, block.Conditions, Id2Param) ? Visibility.Visible : Visibility.Collapsed;

                    foreach (IDynParameter para in block.Parameters)
                    {
                        if(block.HasAccess)
                            para.Visible = SaveHelper.CheckConditions(adds.ApplicationId, para.Conditions, Id2Param) ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
            #endregion


            adds.Bindings = ObjectToByteArray(Hash2Bindings.Values.ToList());
            adds.ParamsHelper = ObjectToByteArray(Channels, true);
            adds.Assignments = ObjectToByteArray(Assignments);

            Hash2Bindings.Clear();
            Hash2Bindings = null;
            Ref2Bindings.Clear();
            Ref2Bindings = null;
            Assignments.Clear();
            Assignments = null;
            updatedComs.Clear();
            updatedComs = null;

            return Id2Param;
        }

        private static void GetChildItems(XElement parent, ParameterBlock block, int textRefId, string groupText)
        {
            foreach(XElement ele in parent.Elements())
            {
                switch (ele.Name.LocalName)
                {
                    case "when":
                    case "choose":
                        GetChildItems(ele, block, textRefId, groupText);
                        break;
                    case "ParameterRefRef":
                        ParseParameterRefRef(ele, block, textRefId);
                        break;
                    case "ParameterSeparator":
                        ParseSeparator(ele, block);
                        break;
                    case "ComObjectRefRef":
                        ParseComObject(ele, textRefId, groupText);
                        break;
                    case "Assign":
                        AssignParameter assign = new AssignParameter
                        {
                            Target = GetItemId(ele.Attribute("TargetParamRefRef").Value),
                            Conditions = GetConditions(ele)
                        };
                        if (ele.Attribute("SourceParamRefRef") != null)
                        {
                            assign.Source = GetItemId(ele.Attribute("SourceParamRefRef").Value);
                        }
                        else
                        {
                            assign.Source = -1;
                            assign.Value = ele.Attribute("Value").Value;
                        }
                        Assignments.Add(assign);
                        break;
                }
            }
        }

        private static void ParseComObject(XElement xele, int textRefId, string groupText)
        {
            if (updatedComs.Contains(GetItemId(xele.Attribute("RefId").Value))) return;

            bool changed = false;
            AppComObject com = ComObjects[GetItemId(xele.Attribute("RefId").Value)];


            if (com.BindedId == -2 && textRefId == -2 && string.IsNullOrEmpty(groupText)) return;

            if (com.BindedId == -1)
            {
                com.BindedId = textRefId;
                changed = true;
            }

            if (!string.IsNullOrEmpty(groupText))
            {
                com.Group = groupText;
                changed = true;
            }

            if(com.BindedId != -2)
            {
                ParamBinding bind = new ParamBinding()
                {
                    Hash = "CO:" + com.Id,
                    SourceId = com.BindedId
                };

                Regex reg = new Regex("{{((.+):(.+))}}");
                Match m = reg.Match(com.Text);
                if (m.Success)
                {
                    bind.DefaultText = m.Groups[3].Value;
                    com.Text = com.Text.Replace(m.Value, "{{dyn}}");
                    changed = true;
                }
                else
                {
                    reg = new Regex("{{(.+)}}");
                    m = reg.Match(com.Text);
                    if (m.Success)
                    {
                        bind.DefaultText = "";
                        com.Text = com.Text.Replace(m.Value, "{{dyn}}");
                        changed = true;
                    }
                }
                Hash2Bindings.Add(bind.Hash, bind);
            }

            if (changed)
            {
                contextC.AppComObjects.Update(com);
                updatedComs.Add(com.Id);
            }
        }

        private static void ParseSeparator(XElement xele, ParameterBlock block)
        {
            int vers = int.Parse(xele.Name.NamespaceName.Substring(xele.Name.NamespaceName.LastIndexOf("/") + 1));

            (List<ParamCondition> Conds, string Hash) = GetConditions(xele, true);

            if(vers < 14)
            {
                ParamSeperator sepe = new ParamSeperator
                {
                    Id = GetItemId(xele.Attribute("Id").Value),
                    Text = xele.Attribute("Text").Value,
                    Conditions = Conds,
                    Hash = Hash
                };
                if (string.IsNullOrEmpty(sepe.Text))
                    sepe.Hint = "HorizontalRuler";
                block.Parameters.Add(sepe);
                return;
            }

            string hint = xele.Attribute("UIHint")?.Value;

            IDynParameter sep;
            switch (hint)
            {
                case null:
                case "HeadLine":
                case "HorizontalRuler":
                    sep = new ParamSeperator() { Hint = hint };
                    break;

                case "Error":
                case "Information":
                    sep = new ParamSeperatorBox()
                    {
                        Hint = hint,
                        IsError = (hint == "Error")
                    };
                    break;

                default:
                    Log.Error("Unbekannter UIHint: " + hint);
                    return;
            }

            sep.Conditions = Conds;
            sep.Hash = Hash;
            sep.Id = GetItemId(xele.Attribute("Id").Value);
            sep.Text = xele.Attribute("Text").Value;
            block.Parameters.Add(sep);
        }

        private static void ParseParameterRefRef(XElement xele, ParameterBlock block, int textRefId)
        {
            AppParameter para = AppParas[GetItemId(xele.Attribute("RefId").Value)];
            //TODO überprüfen
            AppParameterTypeViewModel paraType = AppParaTypes[para.ParameterTypeId];
            var (paramList, hash) = GetConditions(xele, true);

            int refid = para.Id;

            if (Ref2Bindings.ContainsKey(refid))
            {
                foreach (ParamBinding bind in Ref2Bindings[refid])
                {
                    if (bind.SourceId == -1)
                        bind.SourceId = textRefId;
                    else
                        bind.SourceId = para.Id;
                }
            }

            bool hasAccess = para.Access != AccessType.None;
            bool IsCtlEnabled = para.Access != AccessType.Read;

            switch (paraType.Type)
            {
                case ParamTypes.None:
                    IDynParameter paran = new ParamNone();
                    paran.Id = para.ParameterId;
                    paran.Text = para.Text;
                    paran.SuffixText = para.SuffixText;
                    paran.Default = para.Value;
                    paran.Value = para.Value;
                    paran.Conditions = paramList;
                    paran.Hash = hash;
                    paran.HasAccess = hasAccess;
                    paran.IsEnabled = IsCtlEnabled;
                    block.Parameters.Add(paran);
                    break;

                case ParamTypes.IpAdress:
                    IDynParameter pip;
                    if (para.Access == AccessType.Read)
                        pip = new Dynamic.ParamTextRead();
                    else
                        pip = new Dynamic.ParamText();
                    pip.Id = para.ParameterId;
                    pip.Text = para.Text;
                    pip.SuffixText = para.SuffixText;
                    pip.Default = para.Value;
                    pip.Value = para.Value;
                    pip.Conditions = paramList;
                    pip.Hash = hash;
                    pip.HasAccess = hasAccess;
                    pip.IsEnabled = IsCtlEnabled;
                    block.Parameters.Add(pip);
                    break;

                case ParamTypes.NumberInt:
                case ParamTypes.NumberUInt:
                case ParamTypes.Float9:
                    Dynamic.ParamNumber pnu = new Dynamic.ParamNumber
                    {
                        Id = para.ParameterId,
                        Text = para.Text,
                        SuffixText = para.SuffixText,
                        Value = para.Value,
                        Default = para.Value,
                        Conditions = paramList,
                        Hash = hash,
                        HasAccess = hasAccess,
                        IsEnabled = IsCtlEnabled
                    };
                    try
                    {
                        pnu.Minimum = StringToInt(paraType.Tag1);
                        pnu.Maximum = StringToInt(paraType.Tag2);
                    }
                    catch
                    {

                    }
                    block.Parameters.Add(pnu);
                    break;

                case ParamTypes.Text:
                    IDynParameter pte;
                    if (para.Access == AccessType.Read)
                        pte = new Dynamic.ParamTextRead();
                    else
                        pte = new Dynamic.ParamText();
                    pte.Id = para.ParameterId;
                    pte.Text = para.Text;
                    pte.SuffixText = para.SuffixText;
                    pte.Default = para.Value;
                    pte.Value = para.Value;
                    pte.Conditions = paramList;
                    pte.Hash = hash;
                    pte.HasAccess = hasAccess;
                    pte.IsEnabled = IsCtlEnabled;
                    block.Parameters.Add(pte);
                    break;

                case ParamTypes.Enum:
                    List<ParamEnumOption> options = new List<ParamEnumOption>();
                    foreach(AppParameterTypeEnumViewModel enu in contextC.AppParameterTypeEnums.Where(e => e.TypeId == paraType.Id).OrderBy(e => e.Order))
                    {
                        options.Add(new ParamEnumOption() { Text = enu.Text, Value = enu.Value });
                    }
                    int count = options.Count();

                    if (count > 2 || count == 1)
                    {
                        Dynamic.ParamEnum pen = new Dynamic.ParamEnum
                        {
                            Id = para.ParameterId,
                            Text = para.Text,
                            SuffixText = para.SuffixText,
                            Default = para.Value,
                            Value = para.Value,
                            Options = options,
                            Conditions = paramList,
                            Hash = hash,
                            HasAccess = hasAccess,
                            IsEnabled = IsCtlEnabled
                        };
                        block.Parameters.Add(pen);
                    } else
                    {
                        Dynamic.ParamEnumTwo pent = new ParamEnumTwo
                        {
                            Id = para.ParameterId,
                            Text = para.Text,
                            SuffixText = para.SuffixText,
                            Default = para.Value,
                            Value = para.Value,
                            Option1 = options[0],
                            Option2 = options[1],
                            Conditions = paramList,
                            Hash = hash,
                            HasAccess = hasAccess,
                            IsEnabled = IsCtlEnabled
                        };
                        block.Parameters.Add(pent);
                    }
                    break;

                case ParamTypes.CheckBox:
                    ParamCheckBox pch = new ParamCheckBox
                    {
                        Id = para.ParameterId,
                        Text = para.Text,
                        SuffixText = para.SuffixText,
                        Default = para.Value,
                        Value = para.Value,
                        Conditions = paramList,
                        Hash = hash,
                        HasAccess = hasAccess,
                        IsEnabled = IsCtlEnabled
                    };
                    block.Parameters.Add(pch);
                    break;

                case ParamTypes.Color:
                    ParamColor pco = new ParamColor
                    {
                        Id = para.ParameterId,
                        Text = para.Text,
                        SuffixText = para.SuffixText,
                        Default = para.Value,
                        Value = para.Value,
                        Conditions = paramList,
                        Hash = hash,
                        HasAccess = hasAccess,
                        IsEnabled = IsCtlEnabled
                    };
                    block.Parameters.Add(pco);
                    break;

                case ParamTypes.Time:
                    string[] tags = paraType.Tag1.Split(";");
                    ParamTime pti = new ParamTime()
                    {
                        Id = para.ParameterId,
                        Text = para.Text,
                        Default = para.Value,
                        Value = para.Value,
                        Conditions = paramList,
                        Hash = hash,
                        HasAccess = hasAccess,
                        IsEnabled = IsCtlEnabled,
                        Minimum = int.Parse(tags[0]),
                        Maximum = int.Parse(paraType.Tag2)
                    };

                    switch (tags[1])
                    {
                        case "Hours":
                            pti.SuffixText = "Stunden";
                            pti.Divider = 1;
                            break;
                        case "Seconds":
                            pti.SuffixText = "Sekunden";
                            pti.Divider = 1;
                            break;
                        case "TenSeconds":
                            pti.SuffixText = "Zehn Sekunden";
                            pti.Divider = 10;
                            break;
                        case "HundredSeconds":
                            pti.SuffixText = "Hundert Sekunden";
                            pti.Divider = 100;
                            break;
                        case "Milliseconds":
                            pti.SuffixText = "Millisekunden";
                            pti.Divider = 1;
                            break;
                        case "TenMilliseconds":
                            pti.SuffixText = "Zehn Millisekunden";
                            pti.Divider = 10;
                            break;
                        case "HundredMilliseconds":
                            pti.SuffixText = "Hundert Millisekunden";
                            pti.Divider = 100;
                            break;

                        //Todo 
                        /*
                         * PackedSecondsAndMilliseconds
                            Integer value in milliseconds
                            2 bytes:
                            (lo) lower 8 bits of milliseconds
                            (hi) ffssssss
                            (upper 2 bits of milliseconds + seconds)

                            Example: 2.500 seconds are encoded as F4h 42h
                            
                            11 110100  01000010

                            PackedDaysHoursMinutesAndSeconds
                            Integer value in seconds
                            3 bytes, same as DPT 10.001:

                            (lo) seconds
                            | minutes
                            (hi) dddhhhhh (days and hours)

                            Example: 2 days, 8 hours, 20 minutes and 10 seconds is encoded as 0Ah 14h 48h

                         */

                        default:
                            Log.Error("TypeTime Unit nicht unterstützt!! " + tags[1]);
                            throw new Exception("TypeTime Unit nicht unterstützt!! " + tags[1]);
                    }
                    block.Parameters.Add(pti);
                    break;

                default:
                    Serilog.Log.Error("Parametertyp nicht festgelegt!! " + paraType.Type.ToString());
                    Debug.WriteLine("Parametertyp nicht festgelegt!! " + paraType.Type.ToString());
                    throw new Exception("Parametertyp nicht festgelegt!! " + paraType.Type.ToString());
            }
        }


        public static async Task GenerateDefaultComs(AppAdditional adds, Dictionary<int, ViewParamModel> Id2Param)
        {
            List<DeviceComObject> comObjects = new List<DeviceComObject>();
            XDocument dynamic = XDocument.Parse(System.Text.Encoding.UTF8.GetString(adds.Dynamic));
            IEnumerable<XElement> elements = dynamic.Root.Descendants(XName.Get("ComObjectRefRef", dynamic.Root.Name.NamespaceName));
            Dictionary<int, AppComObject> comobjects = new Dictionary<int, AppComObject>();
            Dictionary<string, Dictionary<string, DataPointSubType>> DPST = await SaveHelper.GenerateDatapoints();

            foreach (AppComObject com in contextC.AppComObjects.Where(c => c.ApplicationId == adds.ApplicationId))
                comobjects.Add(com.Id, com);

            foreach (XElement xcom in elements)
            {
                AppComObject appCom = comobjects[GetItemId(xcom.Attribute("RefId").Value)];
                if (appCom.Text == "Dummy") continue;

                DeviceComObject comobject = new DeviceComObject(appCom);

                if(appCom.Datapoint == -1)
                {
                    comobject.DataPointSubType = new DataPointSubType() { SizeInBit = appCom.Size, Name = "x Bytes", Number = "..." };
                } else
                {
                    if (appCom.DatapointSub == -1)
                    {
                        comobject.DataPointSubType = DPST[appCom.Datapoint.ToString()]["xxx"];
                    }
                    else
                    {
                        comobject.DataPointSubType = DPST[appCom.Datapoint.ToString()][appCom.DatapointSub.ToString()];
                    }
                }


                

                comobject.Conditions = GetConditions(xcom);
                comObjects.Add(comobject);
            }

            adds.ComsAll = ObjectToByteArray(comObjects);
            adds.ComsDefault = ObjectToByteArray(GetDefaultComs(adds.ApplicationId, comObjects, Id2Param));
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
                        if (!model.Parameters.Any(p => p.Visible == Visibility.Visible))
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
                        if (!Id2Param[model.Assign.Source].Parameters.Any(p => p.Visible == Visibility.Visible))
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


        public static List<ParamCondition> GetConditions(XElement xele)
        {
            return GetConditions(xele, false).paramList;
        }

        public static (List<ParamCondition> paramList, string hash) GetConditions(XElement xele, bool isParam)
        {
            List<ParamCondition> conds = new List<ParamCondition>();
            try
            {
                string ids = xele.Attribute("RefId")?.Value ?? "";
                if (ids == "" && xele.Attribute("Id") != null) ids = xele.Attribute("Id").Value;

                bool finished = false;
                while (true)
                {
                    xele = xele.Parent;

                    switch (xele.Name.LocalName)
                    {
                        case "when":
                            if (finished && isParam) continue;
                            ParamCondition cond = new ParamCondition();
                            int tempOut;
                            if (xele.Attribute("default")?.Value == "true")
                            {
                                ids = "d" + ids;
                                List<string> values = new List<string>();
                                IEnumerable<XElement> whens = xele.Parent.Elements();
                                foreach (XElement w in whens)
                                {
                                    if (w == xele)
                                        continue;

                                    values.AddRange(w.Attribute("test").Value.Split(" "));
                                }
                                cond.Values = string.Join(",", values);
                                cond.Operation = ConditionOperation.Default;

                                if (cond.Values == "")
                                {
                                    ids = "|" + xele.Parent.Attribute("ParamRefId").Value + ".|" + ids;
                                    continue;
                                }
                            }
                            else if (xele.Attribute("test")?.Value.Contains(" ") == true || int.TryParse(xele.Attribute("test")?.Value, out tempOut))
                            {
                                cond.Values = string.Join(",", xele.Attribute("test").Value.Split(" "));
                                cond.Operation = ConditionOperation.IsInValue;
                            }
                            else if (xele.Attribute("test")?.Value.StartsWith("!=") == true)
                            {
                                cond.Values = xele.Attribute("test").Value.Substring(2);
                                cond.Operation = ConditionOperation.NotEqual;
                            }
                            else if (xele.Attribute("test")?.Value.StartsWith("<") == true)
                            {
                                if (xele.Attribute("test").Value.Contains("="))
                                {
                                    cond.Operation = ConditionOperation.LowerEqualThan;
                                    cond.Values = xele.Attribute("test").Value.Substring(2);
                                }
                                else
                                {
                                    cond.Operation = ConditionOperation.LowerThan;
                                    cond.Values = xele.Attribute("test").Value.Substring(1);
                                }
                            }
                            else if (xele.Attribute("test")?.Value.StartsWith(">") == true)
                            {
                                if (xele.Attribute("test").Value.Contains("="))
                                {
                                    cond.Operation = ConditionOperation.GreatherEqualThan;
                                    cond.Values = xele.Attribute("test").Value.Substring(2);
                                }
                                else
                                {
                                    cond.Operation = ConditionOperation.GreatherThan;
                                    cond.Values = xele.Attribute("test").Value.Substring(1);
                                }
                            }
                            else if (xele.Attribute("test")?.Value.StartsWith("!=") == true)
                            {
                                cond.Operation = ConditionOperation.NotEqual;
                                cond.Values = xele.Attribute("test").Value.Substring(2);
                            }
                            else if (xele.Attribute("test")?.Value.StartsWith("=") == true)
                            {
                                cond.Operation = ConditionOperation.Equal;
                                cond.Values = xele.Attribute("test").Value.Substring(1);
                            }
                            else {
                                string attrs = "";
                                foreach(XAttribute attr in xele.Attributes())
                                {
                                    attrs += attr.Name.LocalName + "=" + attr.Value + "  ";
                                }
                                Log.Warning("Unbekanntes when! " + attrs);
                            }

                            cond.SourceId = GetItemId(xele.Parent.Attribute("ParamRefId").Value);
                            conds.Add(cond);

                            ids = "|" + cond.SourceId + "." + cond.Values + "|" + ids;
                            break;

                        case "Channel":
                        case "ParameterBlock":
                            ids = xele.Attribute("Id").Value + "|" + ids;
                            finished = true;
                            break;

                        case "Dynamic":
                            return (conds, Convert.ToBase64String(Encoding.UTF8.GetBytes(ids)));
                    }
                }
            }
            catch(Exception e)
            {
                Log.Error(e, "Generiere Konditionen ist fehlgeschlagen");
            }
            return (conds, "");
        }

        private static List<DeviceComObject> GetDefaultComs(int applicationId, List<DeviceComObject> comObjects, Dictionary<int, ViewParamModel>  Id2Param)
        {
            ObservableCollection<DeviceComObject> defObjs = new ObservableCollection<DeviceComObject>();

            foreach (DeviceComObject obj in comObjects)
            {
                if (obj.Conditions.Count == 0)
                {
                    defObjs.Add(obj);
                    continue;
                }

                if (CheckConditions(applicationId, obj.Conditions, Id2Param)) //TODO ID2Param iwie generieren
                    defObjs.Add(obj);
            }
            defObjs.Sort(s => s.Number);
            return defObjs.ToList();
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



        public static byte[] ObjectToByteArray(object obj, bool full = false)
        {
            string text;

            if (full)
            {
                text = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.None, new Newtonsoft.Json.JsonSerializerSettings
                {
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects,
                    TypeNameAssemblyFormatHandling = Newtonsoft.Json.TypeNameAssemblyFormatHandling.Simple
                });
            }
            else
            {
                text = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            }
            return System.Text.Encoding.UTF8.GetBytes(text);
        }

        public static T ByteArrayToObject<T>(byte[] obj, bool full = false)
        {
            string text = Encoding.UTF8.GetString(obj);

            if (full)
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(text, new Newtonsoft.Json.JsonSerializerSettings
                {
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects,
                    TypeNameAssemblyFormatHandling = Newtonsoft.Json.TypeNameAssemblyFormatHandling.Simple,
                    Formatting = Newtonsoft.Json.Formatting.None
                });
            }
            else
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(text);
            }

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
