using METS.Classes;
using METS.Classes.Controls.Paras;
using METS.Classes.Helper;
using METS.Classes.Project;
using METS.Context.Catalog;
using METS.Context.Project;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace METS.Views.Easy.Controls
{
    public sealed partial class EControlParas : UserControl
    {
        private int count = 1;
        private string xmlns;
        private XDocument dynamic;
        private LineDevice device { get; }
        private CatalogContext _context = new CatalogContext();
        private ProjectContext _contextP = new ProjectContext();
        private List<ParamCondition> conditions = new List<ParamCondition>();
        private Stopwatch sw = new Stopwatch();

        private List<Binding> bindings = new List<Binding>();

        private Dictionary<string, ParamAfterload> afterloads = new Dictionary<string, ParamAfterload>();
        private Dictionary<string, UIElement> dynUIs = new Dictionary<string, UIElement>();
        private Dictionary<string, string> tempValues = new Dictionary<string, string>();
        private List<DeviceComObject> comObjects = new List<DeviceComObject>();

        public EControlParas(LineDevice dev)
        {
            this.InitializeComponent();

            device = dev;
            this.DataContext = device;

            if (!_context.Applications.Any(a => a.Id == device.ApplicationId))
            {
                LoadRing.Visibility = Visibility.Collapsed;
                ViewHelper.Instance.ShowNotification("Achtung!!! Applikation konnte nicht gefunden werden. Bitte importieren Sie das Produkt erneut.", 4000, ViewHelper.MessageType.Error);
                return;
            }
        }

        public async void Load()
        {
            sw.Start();
            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("Dynamic");
            StorageFile file = await folder.GetFileAsync(device.ApplicationId + ".xml");

            dynamic = XDocument.Load(await file.OpenStreamForReadAsync());
            xmlns = dynamic.Root.Name.NamespaceName;
            PrepareComObject(dynamic.Root);
            ParseChilds(dynamic.Root);
            sw.Stop();
            LoadRing.Visibility = Visibility.Collapsed;
        }

        private List<string> ParseChilds(XElement xparent, string dynId = null, ParamCondition cond = null)
        {
            List<string> dynIds = new List<string>();
            int count3 = 1;

            foreach (XElement xele in xparent.Elements())
            {
                string mId;
                string mTitle;

                if (dynId == null)
                {
                    mId = xele.Attribute("Id")?.Value;
                }
                else
                {
                    mId = dynId + "." + count3;
                    dynIds.Add(mId);
                    count3++;
                }

                if (xele.Attribute("ParamRefId") != null)
                {
                    AppParameter pbPara = _context.AppParameters.Single(p => p.Id == xele.Attribute("ParamRefId").Value);
                    mTitle = pbPara.Text;
                }
                else
                {
                    mTitle = xele.Attribute("Text")?.Value;
                }

                switch (xele.Name.LocalName)
                {
                    case "Channel":
                        if (dynamic.Descendants(XName.Get("Channel", xmlns)).Count() > 1)
                        {
                            TabView tabview = new TabView();
                            tabview.SelectionChanged += tabview_SelectionChanged;

                            TabViewItem tabitem = new TabViewItem
                            {
                                Header = mTitle,
                                Name = mId,
                                Content = tabview
                            };

                            if (dynId != null)
                                tabitem.Visibility = Visibility.Collapsed;

                            if (cond != null)
                            {
                                //Add Logik
                            }

                            Channels.Items.Add(tabitem);
                            ParseChilds2(xele, tabview);

                        }
                        else
                        {
                            ParseChilds2(xele, Channels);
                        }
                        break;

                    case "ChannelIndependentBlock":
                        ParseChilds2(xele, Channels);
                        break;

                    case "choose":
                        break;

                    default:
                        break;
                }
            }

            return dynIds;
        }

        private List<string> ParseChilds2(XElement xparent, TabView parent, string dynId = null, string sourceId = null, string[] conds = null)
        {
            List<string> dynIds = new List<string>();
            int count2 = 1;

            foreach (XElement xele in xparent.Elements())
            {
                string mId = xele.Attribute("ParamRefId")?.Value;
                string mTitle;

                if (mId == null)
                    mId = xele.Attribute("Id")?.Value;

                if (dynId != null)
                {
                    mId = dynId + "." + count2;
                    dynIds.Add(mId);
                    count2++;
                }

                if (xele.Attribute("ParamRefId") != null)
                {
                    AppParameter pbPara = _context.AppParameters.Single(p => p.Id == xele.Attribute("ParamRefId").Value);
                    mTitle = pbPara.Text;
                }
                else
                {
                    mTitle = xele.Attribute("Text")?.Value;
                }

                switch (xele.Name.LocalName)
                {
                    case "ParameterBlock":
                        if (xele.Attribute("Access")?.Value == "None")
                            continue;

                        TabViewItem parablock = new TabViewItem();
                        ScrollViewer scroller = new ScrollViewer();
                        StackPanel panel = new StackPanel();

                        string value = "";
                        try
                        {
                            ChangeParamModel changeT = _contextP.ChangesParam.Where(c => c.DeviceId == device.UId && c.ParamId == sourceId).OrderByDescending(c => c.StateId).First();
                            value = changeT.Value;
                        }
                        catch
                        {
                            try
                            {
                                AppParameter pbParaT = _context.AppParameters.Single(p => p.Id == sourceId);
                                value = pbParaT.Value;
                            }
                            catch { }
                        }


                        if (mTitle.Contains("{{"))
                        {
                            Regex reg = new Regex("{{((.+):(.+))}}");
                            Match m = reg.Match(mTitle);
                            if (m.Success)
                            {
                                Binding bind = new Binding()
                                {
                                    Id = m.Groups[2].Value,
                                    DefaultText = m.Groups[3].Value,
                                    TextPlaceholder = reg.Replace(mTitle, "{{dyn}}"),
                                    Item = parablock
                                };
                                bindings.Add(bind);

                                try
                                {
                                    ChangeParamModel changeB = _contextP.ChangesParam.Where(c => c.DeviceId == device.UId && c.ParamId.EndsWith("R-" + bind.Id)).OrderByDescending(c => c.StateId).First();
                                    value = changeB.Value;
                                }
                                catch { }

                                if (value == "")
                                    parablock.Header = reg.Replace(mTitle, bind.DefaultText);
                                else
                                    parablock.Header = reg.Replace(mTitle, value);
                            }
                            else
                                parablock.Header = mTitle;
                        }
                        else
                            parablock.Header = mTitle;

                        if (mId != null)
                        {
                            parablock.Name = mId;
                        }
                        scroller.Content = panel;
                        parablock.Content = scroller;

                        if (dynId != null)
                        {
                            parablock.Visibility = Visibility.Collapsed;
                            dynUIs.Add(mId, parablock);
                        }

                        if (conds != null && conds.Contains(value))
                            parablock.Visibility = Visibility.Visible;



                        afterloads.Add(mId, new ParamAfterload(xele, panel));

                        parent.Items.Add(parablock);
                        //ParseChilds3(xele, panel);

                        break;

                    case "choose":
                        string id = xele.Attribute("ParamRefId").Value;

                        foreach (XElement xwhen in xele.Elements())
                        {
                            string destId = "dynamic-" + count;
                            count++;

                            string[] condVals = xwhen.Attribute("test").Value.Split(" ");

                            List<string> ids = ParseChilds2(xwhen, parent, destId, id, condVals);

                            foreach (string dynid in ids)
                            {
                                ParamCondition cond = new ParamCondition()
                                {
                                    SourceId = id,
                                    DestinationId = dynid
                                };
                                cond.Values = string.Join(",", condVals);
                                conditions.Add(cond);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            return dynIds;
        }

        private void ParseChilds3(XElement xparent, StackPanel parent)
        {
            foreach (XElement xele in xparent.Elements())
            {
                switch (xele.Name.LocalName)
                {
                    case "ParameterSeparator":
                        Border b = new Border();
                        b.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Gray);
                        b.BorderThickness = new Thickness(0, 1, 0, 0);
                        b.Margin = new Thickness(10, 20, 10, 20);
                        if (xele.Attribute("Text")?.Value != "")
                            b.Child = new TextBlock() { Text = xele.Attribute("Text").Value };
                        else
                            b.Height = 1;
                        parent.Children.Add(b);
                        break;

                    case "ParameterRefRef":
                        AppParameter pbPara = _context.AppParameters.Single(p => p.Id == xele.Attribute("RefId").Value);
                        if (pbPara.Access == AccessType.None) break;

                        AppParameterTypeViewModel pbType = _context.AppParameterTypes.Single(t => t.Id == pbPara.ParameterTypeId);
                        ChangeParamModel change = null;
                        try
                        {
                            change = _contextP.ChangesParam.Where(c => c.DeviceId == device.UId && c.ParamId == pbPara.Id).OrderByDescending(c => c.StateId).First();
                        }
                        catch { }

                        if (!tempValues.Keys.Contains(pbPara.Id))
                        {
                            if (change == null)
                                tempValues.Add(pbPara.Id, pbPara.Value);
                            else
                                tempValues.Add(pbPara.Id, change.Value);
                        }


                        switch (pbType.Type)
                        {
                            case ParamTypes.Picture:
                                ParamPicture paraviewP = new ParamPicture(pbPara, pbType);
                                parent.Children.Add(paraviewP);
                                break;
                            case ParamTypes.NumberInt:
                            case ParamTypes.NumberUInt:
                                ParamNumber paraviewN = new ParamNumber(pbPara, pbType);
                                parent.Children.Add(paraviewN);
                                break;
                            case ParamTypes.Text:
                                if (pbPara.Access == AccessType.Read)
                                {
                                    ParamText paraviewT = new ParamText(pbPara, pbType);
                                    parent.Children.Add(paraviewT);
                                }
                                else
                                {
                                    ParamInput paraviewI = new ParamInput(pbPara, pbType) { Name = pbPara.Id };
                                    paraviewI.ParamChanged += ParamChanged;
                                    parent.Children.Add(paraviewI);
                                }
                                break;
                            case ParamTypes.Enum:
                                IEnumerable<AppParameterTypeEnumViewModel> enums = _context.AppParameterTypeEnums.Where(e => e.ParameterId == pbType.Id).OrderBy(e => e.Order).ToList();

                                if (enums.Count() == 0)
                                {
                                    Log.Warning("ParameterTyp Enum hat keine Enums! " + pbType.Id);
                                    Border b2 = new Border();
                                    b2.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Red);
                                    b2.BorderThickness = new Thickness(0, 1, 0, 0);
                                    b2.Margin = new Thickness(10, 20, 10, 20);
                                    b2.Child = new TextBlock() { Text = pbPara.Text + " - Keine Enums" };
                                    parent.Children.Add(b2);
                                } else if(enums.Count() > 2)
                                {
                                    ParamEnum paraviewE = new ParamEnum(pbPara, pbType, enums) { Name = pbPara.Id };
                                    paraviewE.ParamChanged += ParamChanged;
                                    parent.Children.Add(paraviewE);
                                }
                                else
                                {
                                    ParamEnum2 paraviewE2 = new ParamEnum2(pbPara, pbType, enums) { Name = pbPara.Id };
                                    paraviewE2.ParamChanged += ParamChanged;
                                    parent.Children.Add(paraviewE2);
                                }
                                break;

                            case ParamTypes.None:
                                ParamNone paraviewNo = new ParamNone(pbPara, pbType);
                                parent.Children.Add(paraviewNo);
                                break;
                        }
                        break;

                    case "choose":
                        string id = xele.Attribute("ParamRefId").Value;

                        foreach (XElement xwhen in xele.Elements())
                        {
                            string destId = "dynamic-" + count;
                            count++;
                            ParamCondition cond = new ParamCondition()
                            {
                                SourceId = id,
                                DestinationId = destId
                            };

                            if (xwhen.Attribute("default") != null && xwhen.Attribute("default").Value == "true")
                            {
                                List<string> tempValues = new List<string>();
                                foreach(XElement w in xele.Elements())
                                {
                                    if (w == xwhen) continue;
                                    tempValues.AddRange(w.Attribute("test").Value.Split(" "));
                                }
                                cond.Values = string.Join(",", tempValues);
                            }
                            else if (xwhen.Attribute("test") != null)
                            {
                                cond.Values = string.Join(",", xwhen.Attribute("test").Value.Split(" "));
                            }
                            conditions.Add(cond);

                            StackPanel panelc = new StackPanel
                            {
                                Name = destId,
                                Visibility = Visibility.Collapsed
                            };

                            string value = "";
                            try
                            {
                                ChangeParamModel changeT = _contextP.ChangesParam.Where(c => c.DeviceId == device.UId && c.ParamId == cond.SourceId).OrderByDescending(c => c.StateId).First();
                                value = changeT.Value;
                            }
                            catch
                            {
                                AppParameter pbParaT = _context.AppParameters.Single(p => p.Id == cond.SourceId);
                                value = pbParaT.Value;
                            }

                            switch(cond.Operation)
                            {
                                case ConditionOperation.IsInValue:
                                    if (cond.Values.Contains(value))
                                        panelc.Visibility = Visibility.Visible;
                                    break;
                                case ConditionOperation.Default:
                                    if (!cond.Values.Contains(value))
                                        panelc.Visibility = Visibility.Visible;
                                    break;
                                default:
                                    break;
                            }
                            

                            if (!tempValues.Keys.Contains(cond.SourceId))
                            {
                                tempValues.Add(cond.SourceId, value);
                            }

                            dynUIs.Add(destId, panelc);
                            parent.Children.Add(panelc);
                            ParseChilds3(xwhen, panelc);
                        }
                        break;
                }
            }
        }

        private async void PrepareComObject(XElement xele)
        {
            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("Dynamic");
            StorageFile fileJSON = await folder.GetFileAsync(device.ApplicationId + "-CO-All.json");

            string json = await FileIO.ReadTextAsync(fileJSON);
            comObjects = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DeviceComObject>>(json);


            foreach(DeviceComObject com in comObjects)
            {
                if (com.Name.Contains("{{"))
                {
                    Regex reg = new Regex("{{((.+):(.+))}}");
                    Match m = reg.Match(com.Name);
                    if (m.Success)
                    {
                        Binding bind = new Binding()
                        {
                            Id = m.Groups[2].Value,
                            DefaultText = m.Groups[3].Value,
                            TextPlaceholder = reg.Replace(com.Name, "{{dyn}}")
                        };
                        if (device.ComObjects.Any(c => c.Id == com.Id))
                            bind.Item = device.ComObjects.Single(c => c.Id == com.Id);
                        bindings.Add(bind);
                    }
                }
            }

            //CheckComObjects();
            //SaveHelper.SaveProject();
        }

        private void CheckComObjects()
        {
            List<DeviceComObject> newObjs = new List<DeviceComObject>();

            foreach (DeviceComObject obj in comObjects)
            {
                if (obj.Conditions.Count == 0)
                {
                    newObjs.Add(obj);
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
                        AppParameter pbPara = _context.AppParameters.Single(p => p.Id == cond.SourceId);
                        ChangeParamModel change = null;
                        try
                        {
                            change = _contextP.ChangesParam.Where(c => c.DeviceId == device.UId && c.ParamId == pbPara.Id).OrderByDescending(c => c.StateId).First();
                        }
                        catch { }
                        if (change == null)
                            val = pbPara.Value;
                        else
                            val = change.Value;

                        tempValues.Add(cond.SourceId, val);
                    }

                    if (!cond.Values.Contains(val))
                        flag = false;
                }

                if (flag)
                    newObjs.Add(obj);
            }

            List<DeviceComObject> toAdd = new List<DeviceComObject>();
            foreach (DeviceComObject cobj in newObjs)
            {
                if (!device.ComObjects.Any(co => co.Id == cobj.Id))
                    toAdd.Add(cobj);
            }

            List<DeviceComObject> toDelete = new List<DeviceComObject>();
            foreach (DeviceComObject cobj in device.ComObjects)
            {
                if (!newObjs.Any(co => co.Id == cobj.Id))
                {
                    if (cobj.Groups.Count != 0)
                    {
                        //TODO frage ob verbundene Kommunikationsobjekte gelöscht werden sollen, sonst zurück setzen
                    }
                    toDelete.Add(cobj);
                }
            }
            foreach (DeviceComObject cobj in toDelete)
            {
                device.ComObjects.Remove(cobj);
            }
            foreach (DeviceComObject cobj in toAdd)
            {
                if (cobj.Name.Contains("{{"))
                {
                    Regex reg = new Regex("{{((.+):(.+))}}");
                    Match m = reg.Match(cobj.Name);
                    if (m.Success)
                    {
                        Binding bind = new Binding()
                        {
                            Id = m.Groups[2].Value,
                            DefaultText = m.Groups[3].Value,
                            TextPlaceholder = reg.Replace(cobj.Name, "{{dyn}}")
                        };
                        bind.Item = cobj;
                        bindings.Add(bind);

                        string value = "";
                        try
                        {
                            ChangeParamModel changeB = _contextP.ChangesParam.Where(c => c.DeviceId == device.UId && c.ParamId.EndsWith("R-" + bind.Id)).OrderByDescending(c => c.StateId).First();
                            value = changeB.Value;
                        }
                        catch { }

                        if (bind.Item == null) continue;
                        DeviceComObject dco = (DeviceComObject)bind.Item;

                        if (value == "")
                            dco.Name = reg.Replace(cobj.Name, bind.DefaultText);
                        else
                            dco.Name = reg.Replace(cobj.Name, value);
                    }
                }
                device.ComObjects.Add(cobj);
            }

            device.ComObjects.Sort(s => s.Number);
        }

        private void ParamChanged(string source, string value, string hash)
        {
            IEnumerable<ParamCondition> conds = conditions.Where(c => c.SourceId == source);

            foreach (ParamCondition cond in conds)
            {
                if (!dynUIs.ContainsKey(cond.DestinationId))
                    continue;
                UIElement dest = dynUIs[cond.DestinationId];
                if (dest == null)
                {
                    continue;
                }

                if (cond.Values.Split(",").Contains(value))
                    dest.Visibility = Visibility.Visible;
                else
                    dest.Visibility = Visibility.Collapsed;
            }

            IEnumerable<Binding> binds = bindings.Where(b => source.EndsWith("R-" + b.Id) && b.Item != null);

            foreach (Binding bind in binds)
            {
                string newName = bind.TextPlaceholder.Replace("{{dyn}}", value == "" ? bind.DefaultText : value);

                if(bind.Item is TabViewItem)
                {
                    ((TabViewItem)bind.Item).Header = bind.TextPlaceholder.Replace("{{dyn}}", newName);
                } else if(bind.Item is DeviceComObject)
                {
                    ((DeviceComObject)bind.Item).Name = bind.TextPlaceholder.Replace("{{dyn}}", newName);
                }

                    
            }

            ChangeParamModel change = new ChangeParamModel
            {
                DeviceId = device.UId,
                ParamId = source,
                Value = value
            };
            if (!tempValues.ContainsKey(source))
                tempValues.Add(source, value);

            tempValues[source] = value;

            device.LoadedApplication = false;
            ChangeHandler.Instance.ChangedParam(change);
            CheckComObjects();
            SaveHelper.SaveProject();
        }

        private async void tabview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabView view = (TabView)sender;
            TabViewItem item = (TabViewItem)view.SelectedItem;

            if (!afterloads.ContainsKey(item.Name)) return;
            ParamAfterload load = afterloads[item.Name];
            if (load.loaded) return;

            LoadRing.Visibility = Visibility.Visible;
            await Task.Delay(500);
            sw.Restart();
            ParseChilds3(load.element, load.parent);
            sw.Stop();
            LoadRing.Visibility = Visibility.Collapsed;
            ViewHelper.Instance.ShowNotification("Parameter geladen in: " + sw.Elapsed.TotalSeconds + "s", 4000);
            load.loaded = true;
        }
    }



    public class Binding
    {
        public string Id { get; set; }
        public string DefaultText { get; set; }
        public string TextPlaceholder { get; set; }
        public object Item { get; set; }
    }
}
