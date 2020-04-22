﻿using Kaenx.Classes.Controls.Paras;
using Kaenx.Classes.Dynamic;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Local;
using Kaenx.DataContext.Project;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Kaenx.Classes.Helper
{
    public class SaveHelper 
    {
        public static Project.Project _project;
        public static ProjectContext contextProject;
        private static CatalogContext contextC = new CatalogContext(new LocalConnectionCatalog() { DbHostname = "Catalog.db", Type = LocalConnectionCatalog.DbConnectionType.SqlLite });

        private static Dictionary<string, AppParameter> AppParas;
        private static Dictionary<string, AppParameterTypeViewModel> AppParaTypes;
        private static Dictionary<string, AppComObject> ComObjects;

        public static ProjectModel SaveProject(Project.Project _pro = null)
        {
            if (_pro != null)
            {
                _project = _pro;

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
            model.ImageH = _project.ImageH;
            model.ImageW = _project.ImageW;


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
                    linemiddlemodel.ParentId = line.Id;
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
                        linedevmodel.ParentId = linem.Id;
                        linedevmodel.Name = linedev.Name;
                        linedevmodel.ApplicationId = linedev.ApplicationId;
                        linedevmodel.DeviceId = linedev.DeviceId;

                        IEnumerable<ComObject> removeComs = contextProject.ComObjects.Where(co => co.DeviceId == linedev.UId).ToList();
                        contextProject.ComObjects.RemoveRange(removeComs);
                        foreach (DeviceComObject comObj in linedev.ComObjects)
                        {
                            List<int> groupIds = new List<int>();

                            foreach (GroupAddress ga in comObj.Groups)
                                groupIds.Add(ga.UId);

                            ComObject com;
                            if (contextProject.ComObjects.Any(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId))
                            {
                                com = contextProject.ComObjects.Single(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId);
                                com.Groups = string.Join(",", groupIds);
                                contextProject.ComObjects.Update(com);
                            }
                            else
                            {
                                com = new ComObject();
                                com.ComId = comObj.Id;
                                com.DeviceId = linedev.UId;
                                com.Groups = string.Join(",", groupIds);
                                contextProject.ComObjects.Add(com);
                            }
                        }

                        contextProject.LineDevices.Update(linedevmodel);
                    }

                    IEnumerable<LineDeviceModel> dev2delete = contextProject.LineDevices.Where(d => d.ProjectId == model.Id && d.ParentId == linem.Id && !linem.Subs.Any(lm => lm.UId == d.UId)).ToList();
                    contextProject.LineDevices.RemoveRange(dev2delete);
                }


                IEnumerable<LineMiddleModel> linem2delete = contextProject.LinesMiddle.Where(d => d.ProjectId == model.Id && d.ParentId == line.Id && !line.Subs.Any(lm => lm.UId == d.UId)).ToList();
                contextProject.LinesMiddle.RemoveRange(linem2delete);
            }


            IEnumerable<LineModel> line2delete = contextProject.LinesMain.Where(d => d.ProjectId == model.Id && !_project.Lines.Any(lm => lm.UId == d.UId)).ToList();
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

            contextProject.LineDevices.Update(model);
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
            linemodel.ParentId = line.Parent.Id;
            contextProject.LinesMiddle.Update(linemodel);
            contextProject.SaveChanges();
        }

        public static void SaveGroups()
        {
            contextProject.SaveChanges();

            foreach (Project.Group g in _project.Groups)
            {
                GroupMainModel gmain = contextProject.GroupMain.Single(gm => gm.UId == g.UId);
                gmain.Name = g.Name;
                gmain.Id = g.Id;
                contextProject.GroupMain.Update(gmain);

                foreach (GroupMiddle gm in g.Subs)
                {
                    GroupMiddleModel gmiddle = contextProject.GroupMiddle.Single(gm2 => gm2.UId == gm.UId);
                    gmiddle.Name = gm.Name;
                    gmiddle.Id = gm.Id;
                    gmiddle.ParentId = gmain.UId;
                    contextProject.GroupMiddle.Update(gmiddle);

                    foreach (GroupAddress ga in gm.Subs)
                    {
                        GroupAddressModel gaddress = contextProject.GroupAddress.Single(g => g.UId == ga.UId);
                        gaddress.Name = ga.Name;
                        gaddress.Id = ga.Id;
                        gaddress.ParentId = gmiddle.UId;
                        contextProject.GroupAddress.Update(gaddress);
                    }
                }
            }

            contextProject.SaveChanges();
        }

        public static void SaveAssociations(LineDevice linedev)
        {
            IEnumerable<ComObject> removeComs = contextProject.ComObjects.Where(co => co.DeviceId == linedev.UId).ToList();
            contextProject.ComObjects.RemoveRange(removeComs);

            foreach (DeviceComObject comObj in linedev.ComObjects)
            {
                List<int> groupIds = new List<int>();

                foreach (GroupAddress ga in comObj.Groups)
                    groupIds.Add(ga.UId);

                ComObject com;
                if (contextProject.ComObjects.Any(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId))
                {
                    com = contextProject.ComObjects.Single(co => co.ComId == comObj.Id && co.DeviceId == linedev.UId);
                    com.Groups = string.Join(",", groupIds);
                    contextProject.ComObjects.Update(com);
                }
                else
                {
                    com = new ComObject();
                    com.ComId = comObj.Id;
                    com.DeviceId = linedev.UId;
                    com.Groups = string.Join(",", groupIds);
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

        public static Project.Project LoadProject(ProjectViewHelper helper)
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
                    ViewHelper.Instance.ShowNotification("all", "Die Verbindung wo das Projekt gespeichert sein soll konnt enicht gefunden werden.", 3000, ViewHelper.MessageType.Error);
                    return null;
                }
                contextProject = new ProjectContext(lconn);
                contextProject.Database.Migrate();
                project.Connection = lconn;
            }

            //Catalog mit in das Project machen!
            contextC = new CatalogContext(new LocalConnectionCatalog() { DbHostname = "Catalog.db", Type = LocalConnectionCatalog.DbConnectionType.SqlLite });
            contextC.Database.Migrate();
            //TODO do when project is opening

            ProjectModel pm = contextProject.Projects.Single(p => p.Id == helper.ProjectId);
            project.Name = pm.Name;
            project.Id = pm.Id;
            project.Image = pm.Image;
            project.ImageH = pm.ImageH;
            project.ImageW = pm.ImageW;

            Dictionary<int, GroupAddress> groups = new Dictionary<int, GroupAddress>();

            foreach (GroupMainModel gmain in contextProject.GroupMain.Where(g => g.ProjectId == project.Id))
            {
                Project.Group groupMain = new Project.Group(gmain);
                project.Groups.Add(groupMain);

                foreach (GroupMiddleModel gmiddle in contextProject.GroupMiddle.Where(g => g.ParentId == groupMain.UId))
                {
                    GroupMiddle groupMiddle = new GroupMiddle(gmiddle, groupMain);
                    groupMain.Subs.Add(groupMiddle);

                    foreach (GroupAddressModel gaddress in contextProject.GroupAddress.Where(g => g.ParentId == groupMiddle.UId))
                    {
                        GroupAddress groupAddress = new GroupAddress(gaddress, groupMiddle);
                        groupMiddle.Subs.Add(groupAddress);
                        groups.Add(groupAddress.UId, groupAddress);
                    }
                }
            }

            foreach (LineModel lmodel in contextProject.LinesMain.Where(l => l.ProjectId == helper.Id))
            {
                Line line = new Line(lmodel);
                project.Lines.Add(line);

                foreach (LineMiddleModel lmm in contextProject.LinesMiddle.Where(l => l.ProjectId == helper.Id && l.ParentId == line.Id))
                {
                    LineMiddle lm = new LineMiddle(lmm, line);
                    line.Subs.Add(lm);

                    foreach (LineDeviceModel ldm in contextProject.LineDevices.Where(l => l.ProjectId == helper.Id && l.ParentId == lm.Id).OrderBy(l => l.Id))
                    {
                        LineDevice ld = new LineDevice(ldm, lm, true);
                        ld.DeviceId = ldm.DeviceId;


                        foreach (ComObject com in contextProject.ComObjects.Where(co => co.DeviceId == ld.UId))
                        {
                            AppComObject comObj = contextC.AppComObjects.Single(c => c.Id == com.ComId);
                            DeviceComObject dcom = new DeviceComObject(comObj);

                            if (!string.IsNullOrEmpty(com.Groups))
                            {
                                string[] ids = com.Groups.Split(",");
                                foreach (string id_str in ids)
                                {
                                    int id = int.Parse(id_str);
                                    GroupAddress ga = groups[id];
                                    dcom.Groups.Add(ga);
                                    ga.ComObjects.Add(dcom);
                                }
                            }

                            if (!string.IsNullOrEmpty(dcom.BindedId))
                            {
                                Regex reg = new Regex("{{((.+):(.+))}}");
                                Match m = reg.Match(dcom.Name);
                                if (m.Success)
                                {
                                    string value = "";
                                    try
                                    {
                                        ChangeParamModel changeB = contextProject.ChangesParam.Where(c => c.DeviceId == ld.UId && c.ParamId == dcom.BindedId).OrderByDescending(c => c.StateId).First();
                                        value = changeB.Value;
                                    }
                                    catch { }

                                    if (value == "")
                                        dcom.DisplayName = reg.Replace(dcom.Name, m.Groups[3].Value);
                                    else
                                        dcom.DisplayName = reg.Replace(dcom.Name, value);
                                }
                            } else
                            {
                                dcom.DisplayName = dcom.Name;
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
                    ViewHelper.Instance.ShowNotification("all", "Die Verbindung wo das Projekt gespeichert sein soll konnt enicht gefunden werden.", 3000, ViewHelper.MessageType.Error);
                    return;
                }
                contextProject = new ProjectContext(lconn);
                contextProject.Database.Migrate();
            }

            List<ProjectModel> ps = contextProject.Projects.Where(p => p.Id == helper.ProjectId).ToList();
            contextProject.Projects.RemoveRange(ps);

            List<LineModel> ls = contextProject.LinesMain.Where(l => l.ProjectId == helper.ProjectId).ToList();
            contextProject.LinesMain.RemoveRange(ls);

            List<LineMiddleModel> lms = contextProject.LinesMiddle.Where(l => l.ProjectId == helper.ProjectId).ToList();
            contextProject.LinesMiddle.RemoveRange(lms);

            List<LineDeviceModel> lds = contextProject.LineDevices.Where(l => l.ProjectId == helper.ProjectId).ToList();
            contextProject.LineDevices.RemoveRange(lds);

            contextProject.SaveChanges();
        }


        public static void GenerateDynamic(AppAdditional adds)
        {
            XDocument dynamic = XDocument.Parse(System.Text.Encoding.UTF8.GetString(adds.Dynamic));
            XmlReader reader = dynamic.CreateReader();

            Dictionary<string, XElement> Id2Element = new Dictionary<string, XElement>();
            Dictionary<string, ParameterBlock> Id2ParamBlock = new Dictionary<string, ParameterBlock>();
            List<IDynChannel> Channels = new List<IDynChannel>();
            IDynChannel currentChannel = null;


            foreach(XElement ele in dynamic.Root.Descendants(XName.Get("ParameterBlock", dynamic.Root.Name.NamespaceName)))
            {
                Id2Element.Add(ele.Attribute("Id").Value, ele);
            }
            foreach(XElement ele in dynamic.Root.Descendants(XName.Get("Channel", dynamic.Root.Name.NamespaceName)))
            {
                Id2Element.Add(ele.Attribute("Id").Value, ele);
            }

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement) continue;

                switch (reader.LocalName)
                {
                    case "ChannelIndependentBlock":
                        ChannelIndependentBlock cib = new ChannelIndependentBlock();
                        currentChannel = cib;
                        Channels.Add(cib);
                        break;

                    case "Channel":
                        if(reader.GetAttribute("Text") == "")
                        {
                            ChannelIndependentBlock cib2 = new ChannelIndependentBlock();
                            currentChannel = cib2;
                            Channels.Add(cib2);
                        } else
                        {
                            ChannelBlock cb = new ChannelBlock();
                            cb.Id = reader.GetAttribute("Id");
                            cb.Name = reader.GetAttribute("Name");
                            cb.Text = reader.GetAttribute("Text");
                            cb.Conditions = GetConditions(Id2Element[cb.Id]);
                            Channels.Add(cb);
                            currentChannel = cb;
                        }
                        break;


                    case "ParameterBlock":
                        ParameterBlock pb = new ParameterBlock();
                        pb.Id = reader.GetAttribute("Id");
                        if (reader.GetAttribute("ParamRefId") != null)
                        {
                            try
                            {
                                string paramId = reader.GetAttribute("ParamRefId");
                                AppParameter para = contextC.AppParameters.Single(p => p.Id == paramId);
                                pb.Text = para.Text;
                            }
                            catch
                            {

                            }
                        }
                        else
                            pb.Text = reader.GetAttribute("Text");

                        pb.Conditions = GetConditions(Id2Element[pb.Id]);
                        currentChannel.Blocks.Add(pb);
                        Id2ParamBlock.Add(pb.Id, pb);
                        break;

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

            string appId = Id2Element.Keys.ElementAt(0).Substring(0, 21);
            AppParas = new Dictionary<string, AppParameter>();
            AppParaTypes = new Dictionary<string, AppParameterTypeViewModel>();
            ComObjects = new Dictionary<string, AppComObject>();

            foreach (AppParameter para in contextC.AppParameters.Where(p => p.ApplicationId == appId))
                AppParas.Add(para.Id, para);

            foreach (AppParameterTypeViewModel type in contextC.AppParameterTypes.Where(t => t.ApplicationId == appId))
                AppParaTypes.Add(type.Id, type);

            foreach (AppComObject co in contextC.AppComObjects.Where(t => t.ApplicationId == appId))
                ComObjects.Add(co.Id, co);


            foreach (XElement elePB in dynamic.Root.Descendants(XName.Get("ParameterBlock", dynamic.Root.Name.NamespaceName)))
            {
                ParameterBlock block = Id2ParamBlock[elePB.Attribute("Id").Value];
                GetChildItems(elePB, block);
            }

            List<string> updatedComs = new List<string>();
            foreach (XElement eleCH in dynamic.Root.Descendants(XName.Get("Channel", dynamic.Root.Name.NamespaceName)).Where(ch => !string.IsNullOrEmpty(ch.Attribute("Text")?.Value)))
            {
                foreach(XElement co in eleCH.Descendants(XName.Get("ComObjectRefRef", dynamic.Root.Name.NamespaceName)))
                {
                    if (updatedComs.Contains(co.Attribute("RefId").Value)) continue;
                    AppComObject aco = ComObjects[co.Attribute("RefId").Value];
                    aco.Group = eleCH.Attribute("Text").Value;
                    contextC.AppComObjects.Update(aco);
                    updatedComs.Add(aco.Id);
                }
            }
            contextC.SaveChanges();


            adds.ParamsHelper = ObjectToByteArray(Channels, true);
        }

        private static void GetChildItems(XElement parent, ParameterBlock block)
        {
            foreach(XElement ele in parent.Elements())
            {
                switch (ele.Name.LocalName)
                {
                    case "when":
                    case "choose":
                        GetChildItems(ele, block);
                        break;
                    case "ParameterRefRef":
                        ParseParameterRefRef(ele, block);
                        break;
                    case "ParameterSeparator":
                        ParseSeparator(ele, block);
                        break;
                    case "ComObjectRefRef":
                        //Todo get BindId!!!
                        break;
                }
            }
        }

        private static void ParseSeparator(XElement xele, ParameterBlock block)
        {
            int vers = int.Parse(xele.Name.NamespaceName.Substring(xele.Name.NamespaceName.LastIndexOf("/") + 1));

            (List<ParamCondition> Conds, string Hash) list = GetConditions(xele, true);

            if(vers < 14)
            {
                ParamSeperator sepe = new ParamSeperator();
                sepe.Id = xele.Attribute("Id").Value;
                sepe.Text = xele.Attribute("Text").Value;
                if(string.IsNullOrEmpty(sepe.Text))
                    sepe.Hint = "HorizontalRuler";
                sepe.Conditions = list.Conds;
                sepe.Hash = list.Hash;
                block.Parameters.Add(sepe);
                return;
            }

            IDynParameter sep = null;

            string hint = xele.Attribute("UIHint")?.Value;
            switch (hint)
            {
                case null:
                case "HeadLine":
                case "HorizontalRuler":
                    sep = new ParamSeperator() { Hint = hint };
                    break;

                case "Error":
                case "Information":
                    sep = new ParamSeperatorBox() { Hint = hint };
                    break;

                default:
                    Log.Error("Unbekannter UIHint: " + hint);
                    return;
            }

            sep.Conditions = list.Conds;
            sep.Hash = list.Hash;
            sep.Id = xele.Attribute("Id").Value;
            sep.Text = xele.Attribute("Text").Value;
            block.Parameters.Add(sep);
        }

        private static void ParseParameterRefRef(XElement xele, ParameterBlock block)
        {
            AppParameter para = AppParas[xele.Attribute("RefId").Value];
            if (para.Access == AccessType.None) return;
            //TODO überprüfen
            AppParameterTypeViewModel paraType = AppParaTypes[para.ParameterTypeId];
            var conds = GetConditions(xele, true);

            switch (paraType.Type)
            {
                case ParamTypes.NumberInt:
                case ParamTypes.NumberUInt:
                case ParamTypes.Float9:
                    Dynamic.ParamNumber pnu = new Dynamic.ParamNumber();
                    pnu.Id = para.Id;
                    pnu.Text = para.Text;
                    pnu.SuffixText = para.SuffixText;
                    try
                    {
                        pnu.Minimum = int.Parse(paraType.Tag1);
                        pnu.Maximum = int.Parse(paraType.Tag2);
                    }
                    catch
                    {

                    }
                    pnu.Value = para.Value;
                    pnu.Default = para.Value;
                    pnu.Conditions = conds.paramList;
                    pnu.Hash = conds.hash;
                    block.Parameters.Add(pnu);
                    break;

                case ParamTypes.Text:
                    Dynamic.ParamText pte = new Dynamic.ParamText();
                    pte.Id = para.Id;
                    pte.Text = para.Text;
                    pte.SuffixText = para.SuffixText;
                    pte.Default = para.Value;
                    pte.Value = para.Value;
                    pte.Conditions = conds.paramList;
                    pte.Hash = conds.hash;
                    block.Parameters.Add(pte);
                    break;

                case ParamTypes.Enum:
                    List<ParamEnumOption> options = new List<ParamEnumOption>();
                    foreach(AppParameterTypeEnumViewModel enu in contextC.AppParameterTypeEnums.Where(e => e.ParameterId == paraType.Id).OrderBy(e => e.Order))
                    {
                        options.Add(new ParamEnumOption() { Text = enu.Text, Value = enu.Value });
                    }
                    int count = options.Count();

                    if (count > 2 || count == 1)
                    {
                        Dynamic.ParamEnum pen = new Dynamic.ParamEnum();
                        pen.Id = para.Id;
                        pen.Text = para.Text;
                        pen.SuffixText = para.SuffixText;
                        pen.Default = para.Value;
                        pen.Value = para.Value;
                        pen.Options = options;
                        pen.Conditions = conds.paramList;
                        pen.Hash = conds.hash;
                        block.Parameters.Add(pen);
                    } else
                    {
                        Dynamic.ParamEnumTwo pent = new ParamEnumTwo();
                        pent.Id = para.Id;
                        pent.Text = para.Text;
                        pent.SuffixText = para.SuffixText;
                        pent.Default = para.Value;
                        pent.Value = para.Value;
                        pent.Option1 = options[0];
                        pent.Option2 = options[1];
                        pent.Conditions = conds.paramList;
                        pent.Hash = conds.hash;
                        block.Parameters.Add(pent);
                    }
                    break;

                case ParamTypes.CheckBox:
                    ParamCheckBox pch = new ParamCheckBox();
                    pch.Id = para.Id;
                    pch.Text = para.Text;
                    pch.SuffixText = para.SuffixText;
                    pch.Default = para.Value;
                    pch.Value = para.Value;
                    pch.Conditions = conds.paramList;
                    pch.Hash = conds.hash;
                    block.Parameters.Add(pch);
                    break;

                case ParamTypes.Color:
                    ParamColor pco = new ParamColor();
                    pco.Id = para.Id;
                    pco.Text = para.Text;
                    pco.SuffixText = para.SuffixText;
                    pco.Default = para.Value;
                    pco.Value = para.Value;
                    pco.Conditions = conds.paramList;
                    pco.Hash = conds.hash;
                    block.Parameters.Add(pco);
                    break;
            }
        }


        public static void GenerateDefaultComs(AppAdditional adds)
        {
            List<DeviceComObject> comObjects = new List<DeviceComObject>();
            XDocument dynamic = XDocument.Parse(System.Text.Encoding.UTF8.GetString(adds.Dynamic));
            IEnumerable<XElement> elements = dynamic.Root.Descendants(XName.Get("ComObjectRefRef", dynamic.Root.Name.NamespaceName));
            Dictionary<string, AppComObject> comobjects = new Dictionary<string, AppComObject>();
            
            foreach (AppComObject com in contextC.AppComObjects)
                comobjects.Add(com.Id, com);

            foreach (XElement xcom in elements)
            {
                AppComObject appCom = comobjects[xcom.Attribute("RefId").Value];
                if (appCom.Text == "Dummy") continue;

                DeviceComObject comobject = new DeviceComObject(appCom);
                comobject.Conditions = GetConditions(xcom);
                comObjects.Add(comobject);
            }

            adds.ComsAll = ObjectToByteArray(comObjects);
            adds.ComsDefault = ObjectToByteArray(GetDefaultComs(comObjects));
        }

        public static void GenerateVisibleProps(AppAdditional adds)
        {
            List<ParamVisHelper> paras = new List<ParamVisHelper>();
            XDocument dynamic = XDocument.Parse(System.Text.Encoding.UTF8.GetString(adds.Dynamic));
            IEnumerable<XElement> elements = dynamic.Root.Descendants(XName.Get("ParameterRefRef", dynamic.Root.Name.NamespaceName));

            foreach (XElement xpara in elements)
            {
                ParamVisHelper para = new ParamVisHelper(xpara.Attribute("RefId").Value);
                (List<ParamCondition> conds, string hash) result = GetConditions(xpara, true);
                para.Conditions = result.conds;
                para.Hash = result.hash;
                paras.Add(para);
            }

            adds.ParameterAll = ObjectToByteArray(paras);
        }



        public static bool CheckConditions(List<ParamCondition> conds, Dictionary<string, ViewParamModel> Id2Param = null)
        {
            Dictionary<string, string> tempValues = new Dictionary<string, string>();
            bool flag = true;

            foreach (ParamCondition cond in conds)
            {
                if (flag == false) break;
                string paraValue = "";
                if (Id2Param != null && Id2Param.ContainsKey(cond.SourceId))
                {
                    paraValue = Id2Param[cond.SourceId].Value;
                }
                else
                {
                    if (tempValues.ContainsKey(cond.SourceId))
                        paraValue = tempValues[cond.SourceId];
                    else
                    {
                        AppParameter pbPara = contextC.AppParameters.Single(p => p.Id == cond.SourceId);
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
                        //if(!checkDefault)
                        //{

                        //}
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
                string ids = xele.Attribute("RefId")?.Value;
                if (ids == null) ids = xele.Attribute("Id").Value;
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

                            cond.SourceId = xele.Parent.Attribute("ParamRefId").Value;
                            conds.Add(cond);

                            ids = "|" + cond.SourceId + "." + cond.Values + "|" + ids;
                            break;

                        case "Channel":
                        case "ParameterBlock":
                            ids = xele.Attribute("Id").Value + "|" + ids;
                            finished = true;
                            break;

                        case "Dynamic":
                            //return (conds, ids);
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

                
                if (CheckConditions(obj.Conditions))
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
                if (!noNotify) ViewHelper.Instance.ShowNotification("main", "Die Spannungsquelle der Linie ist möglicherweise nicht ausreichend.\r\n(Verfügbar: " + maxCurrent + " Berechnet: " + current, 5000, ViewHelper.MessageType.Warning);
            }
            else if ((maxCurrent - current) < 80)
            {
                line.CurrentBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Orange);
                if (!noNotify) ViewHelper.Instance.ShowNotification("main", "In der Linie sind nur noch " + (maxCurrent - current) + " mA Reserve verfügbar.", 5000, ViewHelper.MessageType.Info);
            }
            else
                line.CurrentBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.White);

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



        public static byte[] ObjectToByteArray(object obj, bool full = false)
        {
            string text;

            if (full)
            {
                text = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings
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
                    TypeNameAssemblyFormatHandling = Newtonsoft.Json.TypeNameAssemblyFormatHandling.Simple
                });
            }
            else
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(text);
            }

        }

    }
}
