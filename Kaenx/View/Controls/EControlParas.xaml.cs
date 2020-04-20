using Kaenx.Classes;
using Kaenx.Classes.Controls.Paras;
using Kaenx.Classes.Dynamic;
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
using System.Windows.Da;
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
using Microsoft.Toolkit.Uwp.UI.Controls;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace Kaenx.Views.Easy.Controls
{
    public sealed partial class EControlParas : UserControl, INotifyPropertyChanged
    {
        private bool _isBigView = false;
        public bool IsBigView
        {
            get { return _isBigView; }
            set { _isBigView = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsBigView")); }
        }

        private string _selectedParaBlockId;
        public string SelectedParaBlockId
        {
            get { return _selectedParaBlockId; }
            set
            {
                _selectedParaBlockId = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedParaBlockId"));
            }
        }

        private ParameterBlock _selectedParaBlock;
        public ParameterBlock SelectedParaBlock
        {
            get { return _selectedParaBlock; }
            set
            {
                _selectedParaBlock = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedParaBlock"));
            }
        }

        private List<DeviceComObject> comObjects { get; set; }



        public LineDevice device { get; }
        private CatalogContext _context = new CatalogContext();
        private ProjectContext _contextP = Classes.Helper.SaveHelper.contextProject;

        private Dictionary<string, ChangeParamModel> ParaChanges = new Dictionary<string, ChangeParamModel>();
        private Dictionary<string, AppParameterTypeViewModel> AppParaTypess = new Dictionary<string, AppParameterTypeViewModel>();
        Stopwatch watch = new Stopwatch();
        private XDocument dynamic;

        public event PropertyChangedEventHandler PropertyChanged;





        private List<IDynChannel> _channels;
        public List<IDynChannel> Channels
        {
            get { return _channels; }
            set { _channels = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Channels")); }
        }


        Dictionary<string, (string Value, List<IDynParameter> Paras)> Id2Param = new Dictionary<string, (string Value, List<IDynParameter> Paras)>();
        Dictionary<string, IDynParameter> Hash2Param = new Dictionary<string, IDynParameter>();



        public EControlParas(LineDevice dev)
        {
            this.InitializeComponent();
            device = dev;
            this.DataContext = this;

            if (!_context.Applications.Any(a => a.Id == device.ApplicationId))
            {
                LoadRing.Visibility = Visibility.Collapsed;
                ViewHelper.Instance.ShowNotification("main", "Achtung!!! Applikation konnte nicht gefunden werden. Bitte importieren Sie das Produkt erneut.", 4000, ViewHelper.MessageType.Error);
                return;
            }

            this.SizeChanged += EControlParas2_SizeChanged;
        }

        public EControlParas(Classes.Bus.Data.DeviceConfigData data)
        {
            this.InitializeComponent();
            device = data.Device;
            this.DataContext = this;

            device.ApplicationId = data.ApplicationId;

            this.SizeChanged += EControlParas2_SizeChanged;
        }

        private void EControlParas2_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            IsBigView = e.NewSize.Width > 1400;
        }

        public void Start()
        {
            watch.Start();

            if (_contextP.ChangesParam.Any(c => c.DeviceId == device.UId))
            {
                var changes = _contextP.ChangesParam.Where(c => c.DeviceId == device.UId).OrderByDescending(c => c.StateId);
                foreach (ChangeParamModel model in changes)
                {
                    if (ParaChanges.ContainsKey(model.ParamId)) continue;
                    ParaChanges.Add(model.ParamId, model);
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
            comObjects = SaveHelper.ByteArrayToObject<List<DeviceComObject>>(adds.ComsAll);
            Channels = SaveHelper.ByteArrayToObject<List<IDynChannel>>(adds.ParamsHelper, true);

            try
            {
                //Fülle Liste mit alle IDs und Hashes
                foreach (IDynChannel ch in Channels)
                {
                    foreach (ParameterBlock block in ch.Blocks)
                    {
                        foreach (IDynParameter para in block.Parameters)
                        {
                            if (ParaChanges.ContainsKey(para.Id))
                                para.Value = ParaChanges[para.Id].Value;

                            if (!Id2Param.ContainsKey(para.Id))
                                Id2Param.Add(para.Id, (para.Value, new List<IDynParameter>()));

                            Id2Param[para.Id].Paras.Add(para);
                            Hash2Param.Add(para.Hash, para);
                            para.PropertyChanged += Para_PropertyChanged;
                        }
                    }
                }
            }
            catch
            {

            }

            //Berechne ob Objekt sichtbar ist
            foreach (IDynChannel ch in Channels)
            {
                foreach (ParameterBlock block in ch.Blocks)
                {
                    block.Visible = CheckConditions(block.Conditions);

                    foreach (IDynParameter para in block.Parameters)
                    {
                        para.Visible = CheckConditions(para.Conditions);
                    }
                }
            }



            LoadRing.Visibility = Visibility.Collapsed;
            watch.Stop();
            ViewHelper.Instance.ShowNotification("main", "Geladen nach: " + watch.Elapsed.TotalSeconds + "s", 3000);
        }

        private void Para_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Value") return;

            IDynParameter para = sender as IDynParameter;
            Debug.WriteLine("Wert geändert! " + para.Id + " -> " + para.Value);
        }

        

        private Visibility CheckConditions(List<ParamCondition> conds)
        {
            bool flag = true;

            foreach (ParamCondition cond in conds)
            {
                if (flag == false) break;
                string paraValue = "";
                if (Id2Param.ContainsKey(cond.SourceId))
                {
                    paraValue = Id2Param[cond.SourceId].Value;
                } else
                {
                    AppParameter para = _context.AppParameters.Single(p => p.Id == cond.SourceId);
                    paraValue = para.Value;
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

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }
        /*

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
        }*/
    }
}
