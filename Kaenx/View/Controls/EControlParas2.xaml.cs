using Kaenx.Classes;
using Kaenx.Classes.Controls.Paras;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using Kaenx.View.Controls;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace Kaenx.Views.Easy.Controls
{
    public sealed partial class EControlParas2 : UserControl
    {
        public ObservableCollection<ListChannelModel> ListNavChannel { get; set; } = new ObservableCollection<ListChannelModel>();
        public ObservableCollection<ListBlockModel> ListNavBlock { get; set; } = new ObservableCollection<ListBlockModel>();
        private List<DeviceComObject> comObjects { get; set; }

        public LineDevice device { get; }
        private CatalogContext _context = new CatalogContext();
        private ProjectContext _contextP = new ProjectContext();

        private List<BlockVisHelper> helperBlock = new List<BlockVisHelper>();
        private List<Binding> bindings = new List<Binding>();
        private Dictionary<string, IParam> Params = new Dictionary<string, IParam>();
        private List<ParamVisHelper> conditions;
        private Dictionary<string, AppParameter> AppParas = new Dictionary<string, AppParameter>();
        private Dictionary<string, AppParameterTypeViewModel> AppParaTypess = new Dictionary<string, AppParameterTypeViewModel>();
        Stopwatch watch = new Stopwatch();

        public EControlParas2(LineDevice dev)
        {
            this.InitializeComponent();
            device = dev;
            this.DataContext = this;

            if (!_context.Applications.Any(a => a.Id == device.ApplicationId))
            {
                LoadRing.Visibility = Visibility.Collapsed;
                ViewHelper.Instance.ShowNotification("Achtung!!! Applikation konnte nicht gefunden werden. Bitte importieren Sie das Produkt erneut.", 4000, ViewHelper.MessageType.Error);
                return;
            }
        }

        public void Start()
        {
            watch.Start();
            foreach (AppParameter para in _context.AppParameters.Where(p => p.ApplicationId == device.ApplicationId))
                AppParas.Add(para.Id, para);


            if (_contextP.ChangesParam.Any(c => c.DeviceId == device.UId))
            {
                List<string> updated = new List<string>();
                var changes = _contextP.ChangesParam.Where(c => c.DeviceId == device.UId).OrderByDescending(c => c.StateId);
                foreach (ChangeParamModel model in changes)
                {
                    if (updated.Contains(model.ParamId)) continue;
                    AppParas[model.ParamId].Value = model.Value;
                    updated.Add(model.ParamId);
                }
            }

            _ = Load();
        }

        private async Task Load()
        {
            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("Dynamic");
            StorageFile file = await folder.GetFileAsync(device.ApplicationId + ".xml");
            StorageFile filePA = await folder.GetFileAsync(device.ApplicationId + "-PA-All.json");

            XDocument dynamic = XDocument.Load(await file.OpenStreamForReadAsync());


            await PrepareComObject(dynamic.Root);

            conditions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ParamVisHelper>>(await FileIO.ReadTextAsync(filePA));
            try
            {
                await ParseRoot(dynamic.Root, Visibility.Visible);
            } catch
            {

            }

            NavChannel.SelectedIndex = 0;

            if (ListNavChannel.Count != 1)
            {
                NavChannel.Visibility = Visibility.Visible;
                Grid.SetColumn(NavBlock, 1);
                Grid.SetColumnSpan(NavBlock, 1);
            }

            LoadRing.Visibility = Visibility.Collapsed;

            watch.Stop();
            ViewHelper.Instance.ShowNotification("Geladen nach: " + watch.Elapsed.TotalSeconds + "s", 3000);
        }


        private async Task ParseRoot(XElement xparent, Visibility visibility, ListChannelModel parentChannel = null)
        {
            foreach (XElement xele in xparent.Elements())
            {
                switch (xele.Name.LocalName)
                {
                    case "Dynamic":
                        await ParseRoot(xele, visibility);
                        break;
                    case "Channel":
                        ListChannelModel mChannel = new ListChannelModel();
                        mChannel.Name = string.IsNullOrEmpty(xele.Attribute("Text")?.Value) ? "Allgemein" : xele.Attribute("Text").Value;
                        ListNavChannel.Add(mChannel);
                        await ParseRoot(xele, visibility, mChannel);
                        break;
                    case "ParameterBlock":
                        ListBlockModel mBlock = new ListBlockModel();
                        mBlock.Visible = vis2vis(visibility);
                        mBlock.Id = xele.Attribute("Id").Value;
                        if(xele.Attribute("ParamRefId") != null)
                        {
                            AppParameter para = AppParas[xele.Attribute("ParamRefId").Value];
                            mBlock.Name = para.Text;
                        }
                        else
                            mBlock.Name = xele.Attribute("Text").Value;

                        List<ParamCondition> conds = SaveHelper.GetConditions(xele);
                        if(conds.Count != 0)
                        {
                            helperBlock.Add(new BlockVisHelper(mBlock) { Conditions = conds });
                        }
                        mBlock.Panel = new StackPanel();
                        parentChannel.Blocks.Add(mBlock);
                        await ParseBlock(xele, mBlock.Panel, Visibility.Visible);
                        break;
                    case "ChannelIndependentBlock":
                        NavChannel.IsEnabled = false;
                        ListChannelModel mChannelIB = new ListChannelModel();
                        mChannelIB.Name = "Allgemein";
                        ListNavChannel.Add(mChannelIB);
                        await ParseRoot(xele, visibility, mChannelIB);
                        break;
                    case "choose":
                        AppParameter choosePara = AppParas[xele.Attribute("ParamRefId").Value];
                        List<string> allVals = new List<string>();
                        int tempOut;
                        foreach (XElement xwhen in xele.Elements())
                        {
                            if (xwhen.Attribute("test")?.Value.Contains(" ") == true || int.TryParse(xwhen.Attribute("test")?.Value, out tempOut))
                            {
                                allVals.AddRange(xwhen.Attribute("test").Value.Split(" "));
                            }
                            else if (xwhen.Attribute("default") != null)
                            {

                            }
                            else
                            {

                            }
                        }

                        foreach (XElement xwhen in xele.Elements())
                        {
                            Visibility visible = Visibility.Collapsed;

                            if (visibility == Visibility.Visible)
                            {
                                if (xwhen.Attribute("default") != null && !allVals.Contains(choosePara.Value))
                                {
                                    visible = Visibility.Visible;
                                }
                                else if (xwhen.Attribute("test")?.Value.Contains(" ") == true || int.TryParse(xwhen.Attribute("test")?.Value, out tempOut))
                                {
                                    if (xwhen.Attribute("test").Value.Split(" ").Contains(choosePara.Value))
                                        visible = Visibility.Visible;
                                }
                                else
                                {

                                }
                            }

                            await ParseRoot(xwhen, visible, parentChannel);
                        }
                        break;
                    default:
                        break;
                }
            }
        }


        private Visibility vis2vis(Visibility input)
        {
            if (input == Visibility.Visible) return Visibility.Visible;
            return Visibility.Collapsed;
        }


        private async Task ParseBlock(XElement xparent, StackPanel parent, Visibility visibility)
        {
            foreach (XElement xele in xparent.Elements())
            { 
                switch(xele.Name.LocalName)
                {
                    case "ParameterRefRef":
                        AppParameter para = AppParas[xele.Attribute("RefId").Value];
                        if (para.Access == AccessType.None) continue;
                        //TODO überprüfen
                        AppParameterTypeViewModel paraType = GetParamType(para.ParameterTypeId);


                        string ids = para.Id;
                        bool stopper = false;
                        bool finished = false;
                        XElement xtemp = xele;
                        while (!stopper)
                        {
                            xtemp = xtemp.Parent;

                            switch (xtemp.Name.LocalName)
                            {
                                case "when":
                                    if (finished) continue;
                                    ParamCondition cond = new ParamCondition();
                                    int tempOut2;
                                    if (xtemp.Attribute("default")?.Value == "true")
                                    {
                                        ids = "d" + ids;
                                        List<string> values = new List<string>();
                                        IEnumerable<XElement> whens = xtemp.Parent.Elements();
                                        foreach (XElement w in whens)
                                        {
                                            if (w == xtemp)
                                                continue;

                                            try
                                            {
                                                values.AddRange(w.Attribute("test").Value.Split(" "));
                                            } catch
                                            {

                                            }
                                        }
                                        cond.Values = string.Join(",", values);
                                    }
                                    else if (xtemp.Attribute("test")?.Value.Contains(" ") == true || int.TryParse(xtemp.Attribute("test")?.Value, out tempOut2))
                                    {
                                        cond.Values = string.Join(",", xtemp.Attribute("test").Value.Split(" "));
                                    }
                                    else
                                    {
                                        Log.Warning("Unbekanntes when! " + xele.ToString());
                                    }

                                    ids = "|" + xtemp.Parent.Attribute("ParamRefId").Value + "." + cond.Values + "|" + ids;
                                    break;

                                case "Channel":
                                case "ParameterBlock":
                                    ids = xtemp.Attribute("Id").Value + "|" + ids;
                                    finished = true;
                                    break;

                                case "Dynamic":
                                    stopper = true;
                                    break;
                            }
                        }

                        string hash = Convert.ToBase64String(Encoding.UTF8.GetBytes(ids));

                        switch (paraType.Type)
                        {
                            case ParamTypes.Picture:
                                ParamPicture paraviewP = new ParamPicture(para, paraType);
                                paraviewP.hash = hash;
                                paraviewP.SetVisibility(vis2vis(visibility));
                                parent.Children.Add(paraviewP);
                                Params.Add(hash, paraviewP);
                                break;
                            case ParamTypes.NumberInt:
                            case ParamTypes.NumberUInt:
                                ParamNumber paraviewN = new ParamNumber(para, paraType);
                                paraviewN.hash = hash;
                                paraviewN.SetVisibility(vis2vis(visibility));
                                paraviewN.ParamChanged += ParamChanged;
                                parent.Children.Add(paraviewN);
                                Params.Add(hash, paraviewN);
                                break;
                            case ParamTypes.Text:
                                if (para.Access == AccessType.Read)
                                {
                                    ParamText paraviewT = new ParamText(para, paraType);
                                    paraviewT.SetVisibility(vis2vis(visibility));
                                    parent.Children.Add(paraviewT);
                                    Params.Add(hash, paraviewT);
                                }
                                else
                                {
                                    ParamInput paraviewI = new ParamInput(para, paraType) { Name = para.Id };
                                    paraviewI.hash = hash;
                                    paraviewI.ParamChanged += ParamChanged;
                                    paraviewI.SetVisibility(vis2vis(visibility));
                                    parent.Children.Add(paraviewI);
                                    Params.Add(hash, paraviewI);
                                }
                                break;
                            case ParamTypes.Enum:
                                IEnumerable<AppParameterTypeEnumViewModel> enums = _context.AppParameterTypeEnums.Where(e => e.ParameterId == paraType.Id).OrderBy(e => e.Order).ToList();

                                if (enums.Count() == 0)
                                {
                                    Log.Warning("ParameterTyp Enum hat keine Enums! " + paraType.Id);
                                    Border b2 = new Border();
                                    b2.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Red);
                                    b2.BorderThickness = new Thickness(0, 1, 0, 0);
                                    b2.Margin = new Thickness(10, 20, 10, 20);
                                    b2.Child = new TextBlock() { Text = para.Text + " - Keine Enums" };
                                    b2.Visibility = vis2vis(visibility);
                                    parent.Children.Add(b2);
                                }
                                else if (enums.Count() > 2)
                                {
                                    ParamEnum paraviewE = new ParamEnum(para, paraType, enums) { Name = para.Id };
                                    paraviewE.hash = hash;
                                    paraviewE.ParamChanged += ParamChanged;
                                    paraviewE.SetVisibility(vis2vis(visibility));
                                    parent.Children.Add(paraviewE);
                                    Params.Add(hash, paraviewE);
                                }
                                else
                                {
                                    ParamEnum2 paraviewE2 = new ParamEnum2(para, paraType, enums) { Name = para.Id };
                                    paraviewE2.hash = hash;
                                    paraviewE2.ParamChanged += ParamChanged;
                                    paraviewE2.SetVisibility(vis2vis(visibility));
                                    parent.Children.Add(paraviewE2);
                                    Params.Add(hash, paraviewE2);
                                }
                                break;

                            case ParamTypes.None:
                                ParamNone paraviewNo = new ParamNone(para, paraType);
                                paraviewNo.SetVisibility(vis2vis(visibility));
                                parent.Children.Add(paraviewNo);
                                Params.Add(hash, paraviewNo);
                                break;
                        }
                        break;


                    case "ParameterSeparator":
                        Border b = new Border();
                        b.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Gray);
                        b.BorderThickness = new Thickness(0, 1, 0, 0);
                        b.Margin = new Thickness(10, 20, 10, 20);
                        if (xele.Attribute("Text")?.Value != "")
                            b.Child = new TextBlock() { Text = xele.Attribute("Text").Value };
                        else
                            b.Height = 1;
                        b.Visibility = vis2vis(visibility);
                        parent.Children.Add(b);
                        //Params.Add(xele.Attribute("Id").Value, b); /TODO add seperator
                        //Todo conditions for seperators
                        break;

                    case "choose":
                        AppParameter choosePara = AppParas[xele.Attribute("ParamRefId").Value];
                        List<string> allVals = new List<string>();
                        int tempOut;
                        foreach(XElement xwhen in xele.Elements())
                        {
                            if(xwhen.Attribute("test")?.Value.Contains(" ") == true || int.TryParse(xwhen.Attribute("test")?.Value, out tempOut))
                            {
                                allVals.AddRange(xwhen.Attribute("test").Value.Split(" "));
                            } else if(xwhen.Attribute("default") != null)
                            {

                            }
                            else
                            {

                            }
                        }

                        foreach (XElement xwhen in xele.Elements())
                        {
                            Visibility visible = Visibility.Visible;

                            if (visibility == Visibility.Visible)
                            {
                                if (xwhen.Attribute("default") != null && !allVals.Contains(choosePara.Value))
                                {
                                    visible = Visibility.Visible;
                                }
                                else if (xwhen.Attribute("test")?.Value.Contains(" ") == true || int.TryParse(xwhen.Attribute("test")?.Value, out tempOut))
                                {
                                    if (!xwhen.Attribute("test").Value.Split(" ").Contains(choosePara.Value))
                                        visible = Visibility.Collapsed;
                                }
                                else
                                {

                                }
                            }
                            else
                            {
                                visible = vis2vis(visibility);
                            }

                            await ParseBlock(xwhen, parent, visible);
                        }

                        break;
                    default:
                        break;
                }
            }
        }

        private void ParamChanged(string source, string value, string hash)
        {
            AppParameter para = AppParas[source];
            para.Value = value;
            IEnumerable<ParamVisHelper> helpers = conditions.Where(h => h.Conditions.Any(c => c.SourceId == source));

            foreach(ParamVisHelper helper in helpers)
            {
                bool flag = true;

                foreach(ParamCondition cond in helper.Conditions)
                {
                    if (flag == false) break;
                    AppParameter paraCond = AppParas[cond.SourceId];
                    switch (cond.Operation)
                    {
                        case ConditionOperation.IsInValue:
                            if (!cond.Values.Split(",").Contains(paraCond.Value))
                                flag = false;
                            break;

                        case ConditionOperation.Default:
                            if (cond.Values.Split(",").Contains(paraCond.Value))
                                flag = false;
                            break;
                        default:
                            break;
                    }
                }

                if(Params.ContainsKey(helper.Hash))
                {
                    Params[helper.Hash].SetVisibility(flag ? Visibility.Visible : Visibility.Collapsed);
                }
            }

            IEnumerable<BlockVisHelper> helpersB = helperBlock.Where(h => h.Conditions.Any(c => c.SourceId == source));
            foreach(BlockVisHelper helper in helpersB)
            {
                bool flag = true;

                foreach (ParamCondition cond in helper.Conditions)
                {
                    if (flag == false) break;
                    AppParameter paraCond = AppParas[cond.SourceId];
                    switch (cond.Operation)
                    {
                        case ConditionOperation.IsInValue:
                            if (!cond.Values.Split(",").Contains(paraCond.Value))
                                flag = false;
                            break;

                        case ConditionOperation.Default:
                            if (cond.Values.Split(",").Contains(paraCond.Value))
                                flag = false;
                            break;
                        default:
                            break;
                    }
                }

                helper.Block.Visible = flag ? Visibility.Visible : Visibility.Collapsed;
            }

            if (helpersB.Count() > 0)
                filterBlocks();

            ChangeParamModel change = new ChangeParamModel
            {
                DeviceId = device.UId,
                ParamId = source,
                Value = value
            };

            device.LoadedApplication = false;
            ChangeHandler.Instance.ChangedParam(change);
            CheckComObjects();
            //SaveHelper.SaveProject();


            //TODO set para value if no connected comobj is affected
        }

        private AppParameterTypeViewModel GetParamType(string id)
        {
            try
            {
                if (AppParaTypess.ContainsKey(id))
                    return AppParaTypess[id];

                AppParameterTypeViewModel type = _context.AppParameterTypes.Single(pt => pt.Id == id);
                AppParaTypess.Add(id, type);
                return type;
            } catch(Exception e)
            {
                Log.Error(e, "GetParamType Fehler!");
            }
            return null;
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
                    string val = AppParas[cond.SourceId].Value;
                    //if (tempValues.ContainsKey(cond.SourceId))
                    //    val = tempValues[cond.SourceId];
                    //else
                    //{
                    //    AppParameter pbPara = _context.AppParameters.Single(p => p.Id == cond.SourceId);
                    //    ChangeParamModel change = null;
                    //    try
                    //    {
                    //        change = _contextP.ChangesParam.Where(c => c.DeviceId == device.UId && c.ParamId == pbPara.Id).OrderByDescending(c => c.StateId).First();
                    //    }
                    //    catch { }
                    //    if (change == null)
                    //        val = pbPara.Value;
                    //    else
                    //        val = change.Value;

                    //    tempValues.Add(cond.SourceId, val);
                    //}

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

        private async Task PrepareComObject(XElement xele)
        {
            try
            {

            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("Dynamic");
            StorageFile fileJSON = await folder.GetFileAsync(device.ApplicationId + "-CO-All.json");

            string json = await FileIO.ReadTextAsync(fileJSON);
            comObjects = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DeviceComObject>>(json);


            foreach (DeviceComObject com in comObjects)
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
            }catch(Exception e)
            {
                Log.Error(e, "PrepareComObject Fehler!");
            }
        }

        private void NavChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavChannel.SelectedItem == null) return;
            filterBlocks();
            NavBlock.SelectedIndex = 0;
        }

        private void filterBlocks()
        {
            ListChannelModel selectedItem = (ListChannelModel)NavChannel.SelectedItem;


            foreach (ListBlockModel block in selectedItem.Blocks)
            {
                if (block.Visible == Visibility.Visible || !ListNavBlock.Any(b => ((ListBlockModel)b).Id == block.Id)) continue;

                ListBlockModel helper = ListNavBlock.Single(b => b.Id == block.Id);
                ListNavBlock.Remove(helper);
            }



            foreach (ListBlockModel block in selectedItem.Blocks)
            {
                if (block.Visible == Visibility.Collapsed || ListNavBlock.Any(b => ((ListBlockModel)b).Id == block.Id)) continue;
                ListNavBlock.Add(block);
            }
        }
    }
}
