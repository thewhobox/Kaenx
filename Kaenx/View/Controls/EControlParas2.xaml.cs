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
using System.ComponentModel;
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
    public sealed partial class EControlParas2 : UserControl, INotifyPropertyChanged
    {
        private bool _isBigView = false;
        public bool IsBigView
        {
            get { return _isBigView; }
            set { _isBigView = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsBigView")); }
        }

        public ObservableCollection<ListChannelModel> ListNavChannel { get; set; } = new ObservableCollection<ListChannelModel>();
        public ObservableCollection<ListBlockModel> ListNavBlock { get; set; } = new ObservableCollection<ListBlockModel>();
        private List<DeviceComObject> comObjects { get; set; }

        public LineDevice device { get; }
        private CatalogContext _context = new CatalogContext();
        private ProjectContext _contextP = Classes.Helper.SaveHelper.contextProject;

        private List<BlockVisHelper> helperBlock = new List<BlockVisHelper>();
        private List<Binding> bindings = new List<Binding>();
        private List<Binding> bindingBlocks = new List<Binding>();
        private Dictionary<string, IParam> Params = new Dictionary<string, IParam>();
        private List<ParamVisHelper> conditions;
        private Dictionary<string, AppParameter> AppParas = new Dictionary<string, AppParameter>();
        private Dictionary<string, AppParameterTypeViewModel> AppParaTypess = new Dictionary<string, AppParameterTypeViewModel>();
        Stopwatch watch = new Stopwatch();
        private XDocument dynamic;

        public event PropertyChangedEventHandler PropertyChanged;

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

            this.SizeChanged += EControlParas2_SizeChanged;
        }

        public EControlParas2(Classes.Bus.Data.DeviceConfigData data)
        {
            this.InitializeComponent();
            device = data.Device;
            this.DataContext = this;

            AppParas = data.Parameters;
            device.ApplicationId = data.ApplicationId;

            this.SizeChanged += EControlParas2_SizeChanged;
        }

        private void EControlParas2_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            IsBigView = e.NewSize.Width > 850;
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

        public void StartRead()
        {
            _ = Load();
        }

        private async Task Load()
        {
            AppAdditional adds = _context.AppAdditionals.Single(a => a.Id == device.ApplicationId);
            dynamic = XDocument.Parse(Encoding.UTF8.GetString(adds.Dynamic));

            comObjects = SaveHelper.ByteArrayToObject<List<DeviceComObject>>(adds.ComsAll);
            conditions = SaveHelper.ByteArrayToObject<List<ParamVisHelper>>(adds.ParameterAll);

            try
            {
                await ParseRoot(dynamic.Root, Visibility.Visible);
            } catch(Exception e)
            { 
                watch.Stop();
                Log.Error(e, "Fehler beim Laden der ControlParas");
                LoadRing.Visibility = Visibility.Collapsed;
                ViewHelper.Instance.ShowNotification("Es trat ein Fehler beim Laden auf: " + e.Message, 3000, ViewHelper.MessageType.Error);
                return;
            }

            NavChannel.SelectedIndex = 0;

            if (ListNavChannel.Count != 1)
            {
                NavChannel.Visibility = Visibility.Visible;
                Grid.SetColumn(NavBlock, 1);
                Grid.SetColumnSpan(NavBlock, 1);
                NavChannel.CornerRadius = new CornerRadius(2, 0, 0, 0);
                NavBlock.CornerRadius = new CornerRadius(0, 2, 0, 0);
            }

            PrepareBindings();

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
                        //    NavChannel.IsEnabled = false;
                        //    ListChannelModel mChannelIB = new ListChannelModel();
                        //    mChannelIB.Name = "Allgemein";
                        //    ListNavChannel.Add(mChannelIB);
                        //    await ParseRoot(xele, visibility, mChannelIB);
                        ListChannelModel inChannel = new ListChannelModel();
                        inChannel.Name = "Allgemein";
                        ListNavChannel.Add(inChannel);
                        await ParseRoot(xele, visibility, inChannel);
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
                        }

                        foreach (XElement xwhen in xele.Elements())
                        {
                            Visibility visible = Visibility.Collapsed;

                            if (visibility == Visibility.Visible)
                            {
                                if (xwhen.Attribute("default")?.Value == "true" && !allVals.Contains(choosePara.Value))
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

                        string hash = GetHash(xele, para.Id);

                        switch (paraType.Type)
                        {
                            case ParamTypes.Picture:
                                ParamPicture paraviewP = new ParamPicture(para, paraType) { Hash = hash };
                                paraviewP.SetVisibility(vis2vis(visibility));
                                parent.Children.Add(paraviewP);
                                Params.Add(hash, paraviewP);
                                break;
                            case ParamTypes.NumberInt:
                            case ParamTypes.NumberUInt:
                                ParamNumber paraviewN = new ParamNumber(para, paraType) { Hash = hash };
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
                                    ParamInput paraviewI = new ParamInput(para, paraType) { Name = para.Id, Hash = hash };
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
                                    ParamEnum paraviewE = new ParamEnum(para, paraType, enums) { Name = para.Id, Hash = hash };
                                    paraviewE.ParamChanged += ParamChanged;
                                    paraviewE.SetVisibility(vis2vis(visibility));
                                    parent.Children.Add(paraviewE);
                                    Params.Add(hash, paraviewE);
                                }
                                else
                                {
                                    ParamEnum2 paraviewE2 = new ParamEnum2(para, paraType, enums) { Name = para.Id, Hash = hash };
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
                            }
                        }

                        foreach (XElement xwhen in xele.Elements())
                        {
                            Visibility visible = Visibility.Visible;

                            if (visibility == Visibility.Visible)
                            {
                                if (xwhen.Attribute("default")?.Value == "true" && allVals.Contains(choosePara.Value))
                                {
                                    visible = Visibility.Collapsed;
                                }
                                else if (xwhen.Attribute("test")?.Value.Contains(" ") == true || int.TryParse(xwhen.Attribute("test")?.Value, out tempOut))
                                {
                                    if (!xwhen.Attribute("test").Value.Split(" ").Contains(choosePara.Value))
                                        visible = Visibility.Collapsed;
                                }
                                else if (xwhen.Attribute("test")?.Value.StartsWith("<") == true)
                                {
                                    int val = int.Parse(choosePara.Value);
                                    if (xwhen.Attribute("test").Value.Contains("="))
                                    {
                                        if (val <= int.Parse(xwhen.Attribute("test").Value.Substring(2)) == false)
                                            visible = Visibility.Collapsed;
                                    }
                                    else
                                    {
                                        if (val < int.Parse(xwhen.Attribute("test").Value.Substring(1)) == false)
                                            visible = Visibility.Collapsed;
                                    }
                                }
                                else if (xwhen.Attribute("test")?.Value.StartsWith(">") == true)
                                {
                                    int val = int.Parse(choosePara.Value);
                                    if (xwhen.Attribute("test").Value.Contains("="))
                                    {
                                        if (val >= int.Parse(xwhen.Attribute("test").Value.Substring(2)) == false)
                                            visible = Visibility.Collapsed;
                                    }
                                    else
                                    {
                                        if (val > int.Parse(xwhen.Attribute("test").Value.Substring(1)) == false)
                                            visible = Visibility.Collapsed;
                                    }
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
        
        private string GetHash(XElement when, string paraId)
        {
            string ids = paraId;
            bool stopper = false;
            bool finished = false;
            XElement xtemp = when;
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
                                }
                                catch
                                {

                                }
                            }
                            cond.Values = string.Join(",", values);
                        }
                        else if (xtemp.Attribute("test")?.Value.Contains(" ") == true || int.TryParse(xtemp.Attribute("test")?.Value, out tempOut2))
                        {
                            cond.Values = string.Join(",", xtemp.Attribute("test").Value.Split(" "));
                        }
                        else if (xtemp.Attribute("test")?.Value.StartsWith("<") == true)
                        {
                            if (xtemp.Attribute("test").Value.Contains("="))
                                cond.Values = xtemp.Attribute("test").Value.Substring(2);
                            else
                                cond.Values = xtemp.Attribute("test").Value.Substring(1);
                        }
                        else if (xtemp.Attribute("test")?.Value.StartsWith(">") == true)
                        {
                            if (xtemp.Attribute("test").Value.Contains("="))
                                cond.Values = xtemp.Attribute("test").Value.Substring(2);
                            else
                                cond.Values = xtemp.Attribute("test").Value.Substring(1);
                        }
                        else
                        {
                            Log.Warning("Unbekanntes when! " + xtemp.ToString());
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

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(ids));
        }

        private bool CheckConditions(List<ParamCondition> conds)
        {
            bool flag = true;

            foreach (ParamCondition cond in conds)
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
                        //if(!checkDefault)
                        //{
                            
                        //}
                        break;

                    case ConditionOperation.LowerThan:
                        int valLT = int.Parse(paraCond.Value);
                        int valLTo = int.Parse(cond.Values);
                        if ((valLT < valLTo) == false)
                            flag = false;
                        break;

                    case ConditionOperation.LowerEqualThan:
                        int valLET = int.Parse(paraCond.Value);
                        int valLETo = int.Parse(cond.Values);
                        if ((valLET <= valLETo) == false)
                            flag = false;
                        break;

                    case ConditionOperation.GreatherThan:
                        int valGT = int.Parse(paraCond.Value);
                        int valGTo = int.Parse(cond.Values);
                        if ((valGT > valGTo) == false)
                            flag = false;
                        break;

                    case ConditionOperation.GreatherEqualThan:
                        int valGET = int.Parse(paraCond.Value);
                        int valGETo = int.Parse(cond.Values);
                        if ((valGET >= valGETo) == false)
                            flag = false;
                        break;
                }
            }

            return flag;
        }

        private async void ParamChanged(IParam param)
        {
            string source = param.ParamId;
            string value = param.GetValue();
            List<DeviceComObject> deleteList = CheckRemoveComObjects(source, value);

            if(deleteList.Count != 0)
            {
                DiagComsDeleted dcoms = new DiagComsDeleted();
                dcoms.SetComs(deleteList);
                await dcoms.ShowAsync();
                if (dcoms.DoDelete == false)
                {
                    param.SetValue(AppParas[param.ParamId].Value);
                    return;
                }
            }

            AppParameter para = AppParas[source];
            para.Value = value;
            IEnumerable<ParamVisHelper> helpers = conditions.Where(h => h.Conditions.Any(c => c.SourceId == source));

            foreach(ParamVisHelper helper in helpers)
            {
                if (!Params.ContainsKey(helper.Hash)) continue;

                bool flag = CheckConditions(helper.Conditions);
                Params[helper.Hash].SetVisibility(flag ? Visibility.Visible : Visibility.Collapsed);
            }



            IEnumerable<BlockVisHelper> helpersB = helperBlock.Where(h => h.Conditions.Any(c => c.SourceId == source));
            foreach(BlockVisHelper helper in helpersB)
            {
                bool flag = CheckConditions(helper.Conditions);
                helper.Block.Visible = flag ? Visibility.Visible : Visibility.Collapsed;
            }

            if (helpersB.Count() > 0)
                filterBlocks();


            foreach(Binding bind in bindings.Where(b => b.Id == source))
            {
                string newText = bind.TextPlaceholder.Replace("{{dyn}}", para.Value);

                if (bind.Item is ListChannelModel)
                {
                    (bind.Item as ListChannelModel).Name = newText;
                } else if(bind.Item is ListBlockModel)
                {
                    (bind.Item as ListBlockModel).Name = newText;
                } else if(bind.Item is DeviceComObject)
                {
                    (bind.Item as DeviceComObject).DisplayName = newText;
                }
            }


            ChangeParamModel change = new ChangeParamModel
            {
                DeviceId = device.UId,
                ParamId = source,
                Value = value
            };

            device.LoadedApplication = false;
            ChangeHandler.Instance.ChangedParam(change);
            CheckComObjects();
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




        private List<DeviceComObject> CheckRemoveComObjects(string paraId, string paraValue)
        {
            List<DeviceComObject> newObjs = new List<DeviceComObject>();

            foreach (DeviceComObject obj in comObjects)
            {
                if (obj.Conditions.Count == 0)
                {
                    newObjs.Add(obj);
                    continue;
                }

                bool flag = CheckConditions(obj.Conditions);
                if (flag)
                    newObjs.Add(obj);
            }

            List<DeviceComObject> toDelete = new List<DeviceComObject>();
            foreach (DeviceComObject cobj in device.ComObjects)
                if (!newObjs.Any(co => co.Id == cobj.Id) && cobj.Groups.Count != 0)
                    toDelete.Add(cobj);

            return toDelete;
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

                bool flag = CheckConditions(obj.Conditions);

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
                    toDelete.Add(cobj);
            }

            Dictionary<string, ComObject> coms = new Dictionary<string, ComObject>();
            foreach (ComObject com in _contextP.ComObjects)
                if(!coms.ContainsKey(com.ComId))
                    coms.Add(com.ComId, com);

            foreach (DeviceComObject cobj in toDelete)
            {
                ComObject com = coms[cobj.Id];
                _contextP.ComObjects.Remove(com);
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
                            DefaultText = m.Groups[3].Value,
                            TextPlaceholder = reg.Replace(cobj.Name, "{{dyn}}")
                        };
                        string rId = m.Groups[2].Value;
                        bind.Id = AppParas.Keys.Single(k => k.Contains("P-") && k.EndsWith("R-" + rId));
                        bind.Item = cobj;
                        bindings.Add(bind);

                        string value = AppParas[bind.Id].Value;

                        if (string.IsNullOrEmpty(value))
                            cobj.DisplayName = reg.Replace(cobj.Name, bind.DefaultText);
                        else
                            cobj.DisplayName = reg.Replace(cobj.Name, value);
                    }
                } else
                {
                    cobj.DisplayName = cobj.Name;
                }
                device.ComObjects.Add(cobj);


                ComObject com = new ComObject();
                com.ComId = cobj.Id;
                com.DeviceId = device.UId;
                _contextP.ComObjects.Add(com);
            }

            device.ComObjects.Sort(s => s.Number);
            _contextP.SaveChanges();
        }

        private void PrepareBindings()
        {
            foreach (DeviceComObject com in device.ComObjects)
            {
                if (com.Name.Contains("{{"))
                {
                    Regex reg = new Regex("{{((.+):(.+))}}");
                    Match m = reg.Match(com.Name);
                    if (m.Success)
                    {
                        Binding bind = new Binding()
                        {
                            DefaultText = m.Groups[3].Value,
                            TextPlaceholder = reg.Replace(com.Name, "{{dyn}}")
                        };
                        string rId = m.Groups[2].Value;
                        bind.Id = AppParas.Keys.Single(k => k.Contains("P-") && k.EndsWith("R-" + rId));
                        bind.Item = com;

                        string value = AppParas[bind.Id].Value;

                        if (!string.IsNullOrEmpty(value))
                            com.DisplayName = bind.TextPlaceholder.Replace("{{dyn}}", value);
                        else
                            com.DisplayName = bind.TextPlaceholder.Replace("{{dyn}}", bind.DefaultText);

                        bindings.Add(bind);
                    }
                }
            }
            

            foreach(ListChannelModel ch in ListNavChannel)
            {
                if (ch.Name.Contains("{{"))
                {
                    Regex reg = new Regex("{{((.+):(.+))}}");
                    Match m = reg.Match(ch.Name);
                    if (m.Success)
                    {
                        Binding bind = new Binding()
                        {
                            DefaultText = m.Groups[3].Value,
                            TextPlaceholder = reg.Replace(ch.Name, "{{dyn}}")
                        };
                        string rId = m.Groups[2].Value;
                        bind.Id = AppParas.Keys.Single(k => k.Contains("P-") && k.EndsWith("R-" + rId));
                        bind.Item = ch;

                        string value = AppParas[bind.Id].Value;
                        if (!string.IsNullOrEmpty(value))
                            ch.Name = bind.TextPlaceholder.Replace("{{dyn}}", value);
                        else
                            ch.Name = bind.TextPlaceholder.Replace("{{dyn}}", bind.DefaultText);

                        bindings.Add(bind);
                    }
                }

                foreach(ListBlockModel bl in ch.Blocks)
                {
                    if (bl.Name.Contains("{{"))
                    {
                        Regex reg = new Regex("{{((.+):(.+))}}");
                        Match m = reg.Match(bl.Name);
                        if (m.Success)
                        {
                            Binding bind = new Binding()
                            {
                                DefaultText = m.Groups[3].Value,
                                TextPlaceholder = reg.Replace(bl.Name, "{{dyn}}")
                            };
                            string rId = m.Groups[2].Value;
                            bind.Id = AppParas.Keys.Single(k => k.Contains("P-") && k.EndsWith("R-" + rId));
                            bind.Item = bl;

                            string value = AppParas[bind.Id].Value;
                            if (!string.IsNullOrEmpty(value))
                                bl.Name = bind.TextPlaceholder.Replace("{{dyn}}", value);
                            else
                                bl.Name = bind.TextPlaceholder.Replace("{{dyn}}", bind.DefaultText);

                            bindings.Add(bind);
                        }
                    }
                }
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
            List<ListBlockModel> toRemove = new List<ListBlockModel>();

            foreach (ListBlockModel block in ListNavBlock)
            {
                if (selectedItem.Blocks.Any(b => b.Id == block.Id && b.Visible == Visibility.Visible)) continue;
                toRemove.Add(block);
            }

            foreach (ListBlockModel model in toRemove)
                ListNavBlock.Remove(model);



            foreach (ListBlockModel block in selectedItem.Blocks)
            {
                if (block.Visible == Visibility.Collapsed || ListNavBlock.Any(b => ((ListBlockModel)b).Id == block.Id)) continue;
                ListNavBlock.Add(block);
            }
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
