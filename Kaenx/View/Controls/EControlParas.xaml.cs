using Kaenx.Classes;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Import;
using Kaenx.DataContext.Import.Dynamic;
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

        private List<ComBinding> comObjects { get; set; }



        public LineDevice Device { get; }
        private CatalogContext _context = new CatalogContext();
        private ProjectContext _contextP = new ProjectContext(SaveHelper._project.Connection);

        private Dictionary<int, ChangeParamModel> ParaChanges = new Dictionary<int, ChangeParamModel>();
        private Dictionary<string, AppParameterTypeViewModel> AppParaTypess = new Dictionary<string, AppParameterTypeViewModel>();
        Stopwatch watch = new Stopwatch();

        public event PropertyChangedEventHandler PropertyChanged;





        private List<IDynChannel> _channels;
        public List<IDynChannel> Channels
        {
            get { return _channels; }
            set { _channels = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Channels")); }
        }


        Dictionary<int, ViewParamModel> Id2Param = new Dictionary<int, ViewParamModel>();
        Dictionary<string, IDynParameter> Hash2Param = new Dictionary<string, IDynParameter>();
        List<ParamBinding> Bindings;
        List<AssignParameter> Assignments;
        Dictionary<int, string> values = new Dictionary<int, string>();


        public EControlParas(LineDevice dev)
        {
            this.InitializeComponent();
            Device = dev;
            this.DataContext = this;

            if (!_context.Applications.Any(a => a.Id == Device.ApplicationId))
            {
                LoadRing.Visibility = Visibility.Collapsed;
                ViewHelper.Instance.ShowNotification("main", "Achtung!!! Applikation konnte nicht gefunden werden. Bitte importieren Sie das Produkt erneut.", 4000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                return;
            }

            this.SizeChanged += EControlParas2_SizeChanged;
        }

        //public EControlParas(Classes.Bus.Data.DeviceConfigData data)
        //{
        //    this.InitializeComponent();
        //    Device = data.Device;
        //    this.DataContext = this;

        //    //TODO check change
        //    ApplicationViewModel app = _context.Applications.Single(a => a.Hash == data.ApplicationId);

        //    Device.ApplicationId = app.Id;

        //    this.SizeChanged += EControlParas2_SizeChanged;
        //}

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

            _ = Load();
        }

        //public void StartRead()
        //{
        //    _ = Load();
        //}



        private async Task Load()
        {
            try
            {

                await Task.Delay(1);
                AppAdditional adds = _context.AppAdditionals.Single(a => a.ApplicationId == Device.ApplicationId);
                comObjects = FunctionHelper.ByteArrayToObject<List<ComBinding>>(adds.ComsAll, true);
                Channels = FunctionHelper.ByteArrayToObject<List<Kaenx.DataContext.Import.Dynamic.IDynChannel>>(adds.ParamsHelper, true, "Kaenx.DataContext.Import.Dynamic");
                Bindings = FunctionHelper.ByteArrayToObject<List<ParamBinding>>(adds.Bindings, true);
                Assignments = FunctionHelper.ByteArrayToObject<List<AssignParameter>>(adds.Assignments, true);


                foreach (IDynChannel ch in Channels)
                {
                    if (!ch.HasAccess)
                    {
                        ch.IsVisible = false;
                        continue;
                    }

                    foreach (ParameterBlock block in ch.Blocks)
                    {
                        if (!block.HasAccess)
                        {
                            block.IsVisible = false;
                            continue;
                        }

                        foreach (IDynParameter para in block.Parameters)
                        {
                            try
                            {

                                Hash2Param.Add(para.Hash, para);
                            }
                            catch
                            {

                            }
                            if (!para.HasAccess)
                            {
                                para.IsVisible = false;
                                continue;
                            }

                            if (ParaChanges.ContainsKey(para.Id))
                                para.Value = ParaChanges[para.Id].Value;

                            if (!Id2Param.ContainsKey(para.Id))
                                Id2Param.Add(para.Id, new ViewParamModel(para.Value));

                            Id2Param[para.Id].Parameters.Add(para);
                            
                            //para.PropertyChanged += Para_PropertyChanged;
                        }
                    }
                }

                List<int> testL = new List<int>();
                foreach (ChangeParamModel change in ParaChanges.Values)
                {
                    Id2Param[change.ParamId].Value = change.Value;
                    testL.Add(change.ParamId);
                }


                CatalogContext co = new CatalogContext();
                foreach (AppParameter para in co.AppParameters.Where(p => p.ApplicationId == adds.ApplicationId))
                {
                    if (!values.ContainsKey(para.ParameterId))
                    {
                        ViewParamModel model = new ViewParamModel(para.Value);
                        ParamText p = new ParamText();
                        p.Value = para.Value;
                        p.Id = para.Id;
                        p.IsVisible = true;
                        model.Parameters.Add(p);
                        values.Add(para.ParameterId, para.Value);
                        if(!Id2Param.ContainsKey(para.Id))
                            Id2Param.Add(para.Id, model);
                    }
                }


                foreach (AssignParameter assign in Assignments)
                {
                    bool test = FunctionHelper.CheckConditions(assign.Conditions, values);
                    try
                    {
                        Id2Param[assign.Target].Assign = test ? assign : null;
                        if (test)
                        {
                            values[assign.Target] = assign.Value;
                            testL.Add(assign.Target);
                        }
                    }
                    catch
                    {

                    }
                }


                foreach (int id in testL)
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




                LoadRing.Visibility = Visibility.Collapsed;
                watch.Stop();
                ViewHelper.Instance.ShowNotification("main", "Geladen nach: " + watch.Elapsed.TotalSeconds + "s", 3000);

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Laden der Parameter fehlgeschlagen!");
                ViewHelper.Instance.ShowNotification("main", ex.Message, 4000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
            }
        }

        private async void Para_PropertyChanged(object sender, PropertyChangedEventArgs e = null)
        {
            if (e != null && e.PropertyName != "Value") return;

            IDynParameter para = sender as IDynParameter;
            Debug.WriteLine("Wert geändert! " + para.Id + " -> " + para.Value);

            string oldValue = values[para.Id];
            values[para.Id] = para.Value;


            List<DeviceComObject> toRemove = CheckRemovingComs(para);

            if(toRemove.Count > 0)
            {
                DiagComsDeleted diag = new DiagComsDeleted();
                diag.SetComs(toRemove);
                await diag.ShowAsync();
                if(!diag.DoDelete)
                {
                    values[para.Id] = oldValue;
                    para.Value = oldValue;
                    return;
                }
            }


            CalculateVisibilityParas(para);

            CalculateVisibilityComs(para);


            ChangeParamModel change = new ChangeParamModel
            {
                DeviceId = Device.UId,
                ParamId = para.Id,
                Value = para.Value
            };

            Device.LoadedApplication = false;
            _= App._dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ChangeHandler.Instance.ChangedParam(change);
            });

        }

        private List<DeviceComObject> CheckRemovingComs(IDynParameter para)
        {
            IEnumerable<ComBinding> list = comObjects.Where(co => co.Conditions.Any(c => c.SourceId == para.Id));
            List<DeviceComObject> toRemove = new List<DeviceComObject>();

            foreach (ComBinding com in list)
            {
                if (!FunctionHelper.CheckConditions(com.Conditions, values))
                {
                    if (Device.ComObjects.Any(c => c.Id == com.ComId))
                    {
                        DeviceComObject dcom = Device.ComObjects.Single(co => co.Id == com.ComId);
                        if(dcom.Groups.Count > 0)
                             toRemove.Add(dcom);
                    }
                }
            }
            return toRemove;
        }

        private void CalculateVisibilityComs(IDynParameter para)
        {
            IEnumerable<ComBinding> list = comObjects.Where(co => co.Conditions.Any(c => c.SourceId == para.Id));

            foreach(IGrouping<int, ComBinding> bindings in list.GroupBy(cb => cb.ComId))
            {
                if(bindings.Any(cond => FunctionHelper.CheckConditions(cond.Conditions, values)))
                {
                    if (!Device.ComObjects.Any(c => c.Id == bindings.Key))
                    {
                        AppComObject acom = _context.AppComObjects.Single(a => a.ApplicationId == Device.ApplicationId && a.Id == bindings.Key);
                        Device.ComObjects.Add(new DeviceComObject(acom));
                    }
                } else
                {
                    if (Device.ComObjects.Any(c => c.Id == bindings.Key))
                    {
                        DeviceComObject dcom = Device.ComObjects.Single(co => co.Id == bindings.Key);
                        Device.ComObjects.Remove(dcom);
                    }
                }
            }

            //TODO allow to sort for name, function, etc
            Device.ComObjects.Sort(c => c.Number);
        }

        private void CalculateVisibilityParas(IDynParameter para)
        {
            List<ChannelBlock> list = new List<ChannelBlock>();
            List<ParameterBlock> list2 = new List<ParameterBlock>();
            List<int> list5 = new List<int>();


            foreach (AssignParameter assign in Assignments)
            {
                bool test = SaveHelper.CheckConditions(Device.ApplicationId, assign.Conditions, Id2Param);
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
                if(par.HasAccess)
                    par.IsVisible = SaveHelper.CheckConditions(Device.ApplicationId, par.Conditions, Id2Param);

            foreach (IDynChannel ch in Channels)
            {
                if(ch.HasAccess)
                    ch.IsVisible = SaveHelper.CheckConditions(Device.ApplicationId, ch.Conditions, Id2Param);

                foreach (ParameterBlock block in ch.Blocks)
                    if (block.HasAccess)
                        block.IsVisible = SaveHelper.CheckConditions(Device.ApplicationId, block.Conditions, Id2Param);
                    else
                        block.IsVisible = false;
            }


            IEnumerable<ParamBinding> list4 = Bindings.Where(b => b.SourceId == para.Id);
            foreach (ParamBinding bind in list4)
            {
                switch (bind.Type)
                {
                    case BindingTypes.Channel:
                        IDynChannel ch = Channels.Single(c => c.Id == bind.TargetId);
                        if (ch is ChannelBlock)
                        {
                            ChannelBlock chb = ch as ChannelBlock;
                            if (string.IsNullOrEmpty(para.Value))
                                chb.Text = bind.FullText.Replace("{{dyn}}", bind.DefaultText);
                            else
                                chb.Text = bind.FullText.Replace("{{dyn}}", para.Value);
                        }
                        break;

                    case BindingTypes.ParameterBlock:
                        foreach (IDynChannel ch2 in Channels)
                        {
                            if (ch2.Blocks.Any(b => b.Id == bind.TargetId))
                            {
                                ParameterBlock bl = ch2.Blocks.Single(b => b.Id == bind.TargetId);
                                if (string.IsNullOrEmpty(para.Value) || string.IsNullOrWhiteSpace(para.Value))
                                    bl.Text = bind.FullText.Replace("{{dyn}}", bind.DefaultText);
                                else
                                    bl.Text = bind.FullText.Replace("{{dyn}}", para.Value);
                            }
                        }
                        break;

                    case BindingTypes.ComObject:
                        try
                        {
                            //TODO check what to do
                            //DeviceComObject com = Device.ComObjects.Single(c => c.Id == bind.TargetId);
                            //if (string.IsNullOrEmpty(para.Value))
                            //    com.DisplayName = com.Name.Replace("{{dyn}}", bind.DefaultText);
                            //else
                            //    com.DisplayName = com.Name.Replace("{{dyn}}", para.Value);
                        }
                        catch
                        {
                        }
                        break;
                }
            }


            foreach (IDynChannel ch in Channels)
            {
                if (ch.IsVisible)
                {
                    ch.IsVisible = ch.Blocks.Any(b => b.IsVisible);
                }
            }
        }





        private void ShowComsToggler_Toggled(object sender, RoutedEventArgs e)
        {
            if (ShowComsToggler.IsOn)
            {
                //TODO: Über Properties die SIchtbarkeit der Spalten ändern?
                VisualStateManager.GoToState(this, "ShowComs", true);
                //ColsPara.Width = new GridLength(0, GridUnitType.Pixel);
                //ColsComs.Width = new GridLength(0, GridUnitType.Auto);
            } else
            {
                VisualStateManager.GoToState(this, "Default", true);
                //ColsPara.Width = new GridLength(0, GridUnitType.Auto);
                //ColsComs.Width = new GridLength(0, GridUnitType.Pixel);
            }
        }
    }
}
