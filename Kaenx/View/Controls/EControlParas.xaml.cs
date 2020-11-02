using Kaenx.Classes;
using Kaenx.Classes.Dynamic;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using Kaenx.View.Controls;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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



        public LineDevice Device { get; }
        private CatalogContext _context = new CatalogContext();
        private ProjectContext _contextP = new ProjectContext(SaveHelper.connProject);

        private Dictionary<string, ChangeParamModel> ParaChanges = new Dictionary<string, ChangeParamModel>();
        private Dictionary<string, AppParameterTypeViewModel> AppParaTypess = new Dictionary<string, AppParameterTypeViewModel>();
        Stopwatch watch = new Stopwatch();

        public event PropertyChangedEventHandler PropertyChanged;





        private List<IDynChannel> _channels;
        public List<IDynChannel> Channels
        {
            get { return _channels; }
            set { _channels = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Channels")); }
        }


        Dictionary<string, ViewParamModel> Id2Param = new Dictionary<string, ViewParamModel>();
        Dictionary<string, IDynParameter> Hash2Param = new Dictionary<string, IDynParameter>();
        List<ParamBinding> Bindings;
        List<AssignParameter> Assignments;


        public EControlParas(LineDevice dev)
        {
            this.InitializeComponent();
            Device = dev;
            this.DataContext = this;

            if (!_context.Applications.Any(a => a.Id == Device.ApplicationId))
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
            Device = data.Device;
            this.DataContext = this;

            Device.ApplicationId = data.ApplicationId;

            this.SizeChanged += EControlParas2_SizeChanged;
        }

        private void EControlParas2_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            IsBigView = e.NewSize.Width > 1400;
        }

        public void Start()
        {
            watch.Start();

            if (_contextP.ChangesParam.Any(c => c.DeviceId == Device.UId))
            {
                var changes = _contextP.ChangesParam.Where(c => c.DeviceId == Device.UId).OrderByDescending(c => c.StateId);
                foreach (ChangeParamModel model in changes)
                {
                    if (ParaChanges.ContainsKey(model.ParamId)) continue;
                    ParaChanges.Add(model.ParamId, model);
                }
            }

            try
            {
                _ = Load();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Laden der Parameter fehlgeschlagen!");
                ViewHelper.Instance.ShowNotification("main", ex.Message, 4000, ViewHelper.MessageType.Error);
            }
        }

        public void StartRead()
        {
            _ = Load();
        }



        private async Task Load()
        {
            try
            {

                await Task.Delay(1);
                AppAdditional adds = _context.AppAdditionals.Single(a => a.Id == Device.ApplicationId);
                comObjects = SaveHelper.ByteArrayToObject<List<DeviceComObject>>(adds.ComsAll);
                Channels = SaveHelper.ByteArrayToObject<List<IDynChannel>>(adds.ParamsHelper, true);
                Bindings = SaveHelper.ByteArrayToObject<List<ParamBinding>>(adds.Bindings);
                Assignments = SaveHelper.ByteArrayToObject<List<AssignParameter>>(adds.Assignments);


                foreach (IDynChannel ch in Channels)
                {
                    if (!ch.HasAccess)
                    {
                        ch.Visible = Visibility.Collapsed;
                        continue;
                    }

                    if (ch is ChannelBlock)
                    {
                        ChannelBlock chb = ch as ChannelBlock;
                        if (chb.Text.Contains("{{"))
                            chb.DisplayText = chb.Text.Replace("{{dyn}}", chb.DefaultText);
                        else
                            chb.DisplayText = chb.Text;
                    }


                    foreach (ParameterBlock block in ch.Blocks)
                    {
                        if (!block.HasAccess)
                        {
                            block.Visible = Visibility.Collapsed;
                            continue;
                        }


                        if (block.Text?.Contains("{{") == true)
                            block.DisplayText = block.Text.Replace("{{dyn}}", block.DefaultText);
                        else
                            block.DisplayText = block.Text;

                        foreach (IDynParameter para in block.Parameters)
                        {
                            if (!para.HasAccess)
                            {
                                para.Visible = Visibility.Collapsed;
                                continue;
                            }

                            if (ParaChanges.ContainsKey(para.Id))
                                para.Value = ParaChanges[para.Id].Value;

                            if (!Id2Param.ContainsKey(para.Id))
                                Id2Param.Add(para.Id, new ViewParamModel(para.Value));

                            Id2Param[para.Id].Parameters.Add(para);
                            try
                            {

                                Hash2Param.Add(para.Hash, para);
                            }
                            catch
                            {

                            }
                            //para.PropertyChanged += Para_PropertyChanged;
                        }
                    }
                }

                List<string> testL = new List<string>();
                foreach (ChangeParamModel change in ParaChanges.Values)
                {
                    Id2Param[change.ParamId].Value = change.Value;
                    testL.Add(change.ParamId);
                }


                CatalogContext co = new CatalogContext();
                foreach (AppParameter para in co.AppParameters.Where(p => p.ApplicationId == adds.Id))
                {
                    if (!Id2Param.ContainsKey(para.Id))
                    {
                        ViewParamModel model = new ViewParamModel(para.Value);
                        ParamText p = new ParamText();
                        p.Value = para.Value;
                        p.Id = para.Id;
                        p.Visible = Visibility.Visible;
                        model.Parameters.Add(p);
                        Id2Param.Add(para.Id, model);
                    }
                }


                foreach (AssignParameter assign in Assignments)
                {
                    bool test = SaveHelper.CheckConditions(assign.Conditions, Id2Param);
                    try
                    {
                        Id2Param[assign.Target].Assign = test ? assign : null;
                        if (test) testL.Add(assign.Target);
                    }
                    catch
                    {

                    }
                }


                foreach (string id in testL)
                {
                    Id2Param[id].Parameters[0].Value = Id2Param[id].Value;
                    Para_PropertyChanged(Id2Param[id].Parameters[0]);
                }



                foreach(IDynChannel ch in Channels)
                {
                    foreach(ParameterBlock block in ch.Blocks)
                    {
                        foreach(IDynParameter para in block.Parameters)
                        {
                            para.PropertyChanged += Para_PropertyChanged;
                        }
                    }
                }


                _ = CheckComObjects();


                LoadRing.Visibility = Visibility.Collapsed;
                watch.Stop();
                ViewHelper.Instance.ShowNotification("main", "Geladen nach: " + watch.Elapsed.TotalSeconds + "s", 3000);

            }
            catch
            {

            }
        }

        private async void Para_PropertyChanged(object sender, PropertyChangedEventArgs e = null)
        {
            if (e != null && e.PropertyName != "Value") return;

            IDynParameter para = sender as IDynParameter;
            Debug.WriteLine("Wert geändert! " + para.Id + " -> " + para.Value);

            string oldValue = Id2Param[para.Id].Value;

            Id2Param[para.Id].Value = para.Value;




            CalculateVisibility(para);




            



            if (e == null) return;



            (List<DeviceComObject> allNew, List<DeviceComObject> toDelete) comObjs = (null, null);
            if (e != null)
            {
                comObjs = CheckRemoveComObjects();

                //TODO wenn welche löscht werden müssen abfragen ob das wirklich tun soll

                StringBuilder sb = new StringBuilder();
                foreach (DeviceComObject co in comObjs.allNew)
                    sb.AppendLine(co.Number + " " + co.DisplayName + " " + co.Function);
                Debug.WriteLine("Erstes dingens");
                Debug.WriteLine(sb.ToString());

                if (comObjs.toDelete.Count > 0)
                {
                    Debug.WriteLine("Es müssten Sachen gelöscht werden..");
                    DiagComsDeleted diag = new DiagComsDeleted();
                    diag.SetComs(comObjs.toDelete);
                    await diag.ShowAsync();
                    if (!diag.DoDelete)
                    {
                        Id2Param[para.Id].Value = oldValue;
                        para.Value = oldValue;
                        CalculateVisibility(para);
                        return;
                    }
                }
            }






            _ = CheckComObjects(comObjs.allNew);

            ChangeParamModel change = new ChangeParamModel
            {
                DeviceId = Device.UId,
                ParamId = para.Id,
                Value = para.Value
            };

            Device.LoadedApplication = false;
            ChangeHandler.Instance.ChangedParam(change);
        }


        private void CalculateVisibility(IDynParameter para)
        {
            List<ChannelBlock> list = new List<ChannelBlock>();
            List<ParameterBlock> list2 = new List<ParameterBlock>();
            List<string> list5 = new List<string>();


            foreach (AssignParameter assign in Assignments)
            {
                bool test = SaveHelper.CheckConditions(assign.Conditions, Id2Param);
                if (test)
                {
                    Id2Param[assign.Target].Assign = assign;
                    list5.Add(assign.Target);
                }
                else
                    Id2Param[assign.Target].Assign = null;
            }


            IEnumerable<IDynParameter> list3 = Hash2Param.Values.Where(p => p.Conditions.Any(c => c.SourceId == para.Id || list5.Contains(c.SourceId)));
            foreach (IDynParameter par in list3)
                par.Visible = SaveHelper.CheckConditions(par.Conditions, Id2Param) ? Visibility.Visible : Visibility.Collapsed;

            foreach (IDynChannel ch in Channels)
            {
                ch.Visible = SaveHelper.CheckConditions(ch.Conditions, Id2Param) ? Visibility.Visible : Visibility.Collapsed;

                foreach (ParameterBlock block in ch.Blocks)
                    if (block.HasAccess)
                        block.Visible = SaveHelper.CheckConditions(block.Conditions, Id2Param) ? Visibility.Visible : Visibility.Collapsed;
                    else
                        block.Visible = Visibility.Collapsed;
            }


            IEnumerable<ParamBinding> list4 = Bindings.Where(b => b.SourceId == para.Id);
            foreach (ParamBinding bind in list4)
            {
                string[] ids = bind.Hash.Split(":");

                switch (ids[0])
                {
                    case "CB":
                        IDynChannel ch = Channels.Single(c => c.Id == ids[1]);
                        if (ch is ChannelBlock)
                        {
                            ChannelBlock chb = ch as ChannelBlock;
                            if (string.IsNullOrEmpty(para.Value))
                                chb.DisplayText = chb.Text.Replace("{{dyn}}", bind.DefaultText);
                            else
                                chb.DisplayText = chb.Text.Replace("{{dyn}}", para.Value);
                        }
                        break;

                    case "PB":
                        foreach (IDynChannel ch2 in Channels)
                        {
                            if (ch2.Blocks.Any(b => b.Id == ids[1]))
                            {
                                ParameterBlock bl = ch2.Blocks.Single(b => b.Id == ids[1]);
                                if (string.IsNullOrEmpty(para.Value) || string.IsNullOrWhiteSpace(para.Value))
                                    bl.DisplayText = bl.Text.Replace("{{dyn}}", bind.DefaultText);
                                else
                                    bl.DisplayText = bl.Text.Replace("{{dyn}}", para.Value);
                            }
                        }
                        break;

                    case "CO":
                        try
                        {
                            DeviceComObject com = Device.ComObjects.Single(c => c.Id == ids[1]);
                            if (string.IsNullOrEmpty(para.Value))
                                com.DisplayName = com.Name.Replace("{{dyn}}", bind.DefaultText);
                            else
                                com.DisplayName = com.Name.Replace("{{dyn}}", para.Value);
                        }
                        catch
                        {
                        }
                        break;
                }
            }


            foreach (IDynChannel ch in Channels)
            {
                if (ch.Visible == Visibility.Visible)
                {
                    ch.Visible = ch.Blocks.Any(b => b.Visible == Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }


        private (List<DeviceComObject> allNew, List<DeviceComObject> toDelete) CheckRemoveComObjects()
        {
            List<DeviceComObject> newObjs = new List<DeviceComObject>();

            //foreach (DeviceComObject obj in comObjects)
            //{
            //    if (obj.Conditions.Count == 0)
            //    {
            //        newObjs.Add(obj);
            //        continue;
            //    }

            //    bool flag = SaveHelper.CheckConditions(obj.Conditions, Id2Param);
            //    if (flag)
            //        newObjs.Add(obj);
            //}

            foreach (DeviceComObject obj in comObjects)
            {
                if (obj.Conditions.Count == 0)
                {
                    newObjs.Add(obj);
                    continue;
                }

                bool flag = SaveHelper.CheckConditions(obj.Conditions, Id2Param);
                if (flag)
                    newObjs.Add(obj);
            }

            List<DeviceComObject> toDelete = new List<DeviceComObject>();
            foreach (DeviceComObject cobj in Device.ComObjects)
                if (!newObjs.Any(co => co.Id == cobj.Id) && cobj.Groups.Count != 0)
                    toDelete.Add(cobj);

            return (newObjs, toDelete);
        }


        private async Task CheckComObjects(List<DeviceComObject> newObjs = null)
        {
            //TODO check why it doesnt work with parameter new objs!
            if (newObjs == null)//|| true)
            {
                newObjs = new List<DeviceComObject>();

                foreach (DeviceComObject obj in comObjects)
                {
                    if (obj.Conditions.Count == 0)
                    {
                        newObjs.Add(obj);
                        continue;
                    }

                    bool flag = SaveHelper.CheckConditions(obj.Conditions, Id2Param);
                    if (flag)
                        newObjs.Add(obj);
                }
            }


            StringBuilder sb = new StringBuilder();
            foreach (DeviceComObject co in newObjs)
                sb.AppendLine(co.Number + " " + co.DisplayName + " " + co.Function);
            Debug.WriteLine("Zweites dingens");
            Debug.WriteLine(sb.ToString());



            List<DeviceComObject> toAdd = new List<DeviceComObject>();
            foreach (DeviceComObject cobj in newObjs)
            {
                if (!Device.ComObjects.Any(co => co.Id == cobj.Id))
                    toAdd.Add(cobj);
            }

            List<DeviceComObject> toDelete = new List<DeviceComObject>();
            foreach (DeviceComObject cobj in Device.ComObjects)
            {
                if (!newObjs.Any(co => co.Id == cobj.Id))
                    toDelete.Add(cobj);
            }

            Dictionary<string, ComObject> coms = new Dictionary<string, ComObject>();
            foreach (ComObject com in _contextP.ComObjects)
                if (!coms.ContainsKey(com.ComId))
                    coms.Add(com.ComId, com);


            //TODO check why every com gets deleted on first starts
            foreach (DeviceComObject cobj in toDelete)
            {
                ComObject com = coms[cobj.Id];
                _contextP.ComObjects.Remove(com);
                Device.ComObjects.Remove(cobj);
            }


            foreach (DeviceComObject dcom in toAdd)
            {
                if (dcom.Name.Contains("{{"))
                {
                    ParamBinding bind = Bindings.Single(b => b.Hash == "CO:" + dcom.Id);
                    string value = Id2Param[dcom.BindedId].Value;
                    if (string.IsNullOrEmpty(value))
                        dcom.DisplayName = dcom.Name.Replace("{{dyn}}", bind.DefaultText);
                    else
                        dcom.DisplayName = dcom.Name.Replace("{{dyn}}", value);
                }
                else
                {
                    dcom.DisplayName = dcom.Name;
                }
                Device.ComObjects.Add(dcom);


                ComObject com = new ComObject
                {
                    ComId = dcom.Id,
                    DeviceId = Device.UId
                };
                _contextP.ComObjects.Add(com);
            }

            Device.ComObjects.Sort(s => s.Number);
            _contextP.SaveChanges();

            await Task.Delay(1);
        }

   
    }
}
