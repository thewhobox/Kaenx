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
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.ApplicationModel.Background;

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

            try
            {
                _ = Load();
            }catch(Exception ex)
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
            AppAdditional adds = _context.AppAdditionals.Single(a => a.Id == device.ApplicationId);
            comObjects = SaveHelper.ByteArrayToObject<List<DeviceComObject>>(adds.ComsAll);
            Channels = SaveHelper.ByteArrayToObject<List<IDynChannel>>(adds.ParamsHelper, true);
            Bindings = SaveHelper.ByteArrayToObject<List<ParamBinding>>(adds.Bindings);

            foreach (IDynChannel ch in Channels)
            {
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
                    if (block.Text?.Contains("{{") == true)
                        block.DisplayText = block.Text.Replace("{{dyn}}", block.DefaultText);
                    else
                        block.DisplayText = block.Text;

                    foreach (IDynParameter para in block.Parameters)
                    {
                        if (ParaChanges.ContainsKey(para.Id))
                            para.Value = ParaChanges[para.Id].Value;

                        if (!Id2Param.ContainsKey(para.Id))
                            Id2Param.Add(para.Id, new ViewParamModel(para.Value));

                        Id2Param[para.Id].Parameters.Add(para);
                        Hash2Param.Add(para.Hash, para);
                        para.PropertyChanged += Para_PropertyChanged;
                    }
                }
            }

            foreach(ChangeParamModel change in ParaChanges.Values)
            {
                try
                {
                    IDynParameter para = Id2Param[change.ParamId].Parameters[0];
                    Para_PropertyChanged(para);
                }
                catch { }
            }

            _= CheckComObjects();


            LoadRing.Visibility = Visibility.Collapsed;
            watch.Stop();
            ViewHelper.Instance.ShowNotification("main", "Geladen nach: " + watch.Elapsed.TotalSeconds + "s", 3000);
        }

        private void Para_PropertyChanged(object sender, PropertyChangedEventArgs e = null)
        {
            if (e != null && e.PropertyName != "Value") return;

            IDynParameter para = sender as IDynParameter;
            Debug.WriteLine("Wert geändert! " + para.Id + " -> " + para.Value);

            string oldValue = Id2Param[para.Id].Value;

            Id2Param[para.Id].Value = para.Value;

            (List<DeviceComObject> allNew, List<DeviceComObject> toDelete) comObjs = (null, null);
            if (e != null)
            {
               comObjs = CheckRemoveComObjects(para.Id, para.Value);
            }




            #region Parameter neu berechnen
            List<ChannelBlock> list = new List<ChannelBlock>();

            List<ParameterBlock> list2 = new List<ParameterBlock>();
            foreach (IDynChannel ch in Channels)
            {
                if (ch is ChannelBlock && ch.Conditions.Any(c => c.SourceId == para.Id)) list.Add(ch as ChannelBlock);

                list2.AddRange(ch.Blocks.Where(b => b.Conditions.Any(c => c.SourceId == para.Id)));
            }
            foreach (ChannelBlock block in list)
            {
                block.Visible = SaveHelper.CheckConditions(block.Conditions, Id2Param) ? Visibility.Visible : Visibility.Collapsed;
            }

            foreach(ParameterBlock block in list2)
            {
                block.Visible = SaveHelper.CheckConditions(block.Conditions, Id2Param) ? Visibility.Visible : Visibility.Collapsed;
            }

            IEnumerable <IDynParameter> list3 = Hash2Param.Values.Where(p => p.Conditions.Any(c => c.SourceId == para.Id));
            foreach(IDynParameter par in list3)
            {
                par.Visible = SaveHelper.CheckConditions(par.Conditions, Id2Param) ? Visibility.Visible : Visibility.Collapsed;
            }

            IEnumerable<ParamBinding> list4 = Bindings.Where(b => b.SourceId == para.Id);
            foreach(ParamBinding bind in list4)
            {
                string[] ids = bind.Hash.Split(":");

                switch (ids[0])
                {
                    case "CB":
                        IDynChannel ch = Channels.Single(c => c.Id == ids[1]);
                        if(ch is ChannelBlock)
                        {
                            ChannelBlock chb = ch as ChannelBlock;
                            if (string.IsNullOrEmpty(para.Value))
                                chb.DisplayText = chb.Text.Replace("{{dyn}}", bind.DefaultText);
                            else
                                chb.DisplayText = chb.Text.Replace("{{dyn}}", para.Value);
                        }
                        break;

                    case "PB":
                        foreach(IDynChannel ch2 in Channels)
                        {
                            if(ch2.Blocks.Any(b => b.Id == ids[1]))
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
                            DeviceComObject com = device.ComObjects.Single(c => c.Id == ids[1]);
                            if (string.IsNullOrEmpty(para.Value))
                                com.DisplayName = com.Name.Replace("{{dyn}}", bind.DefaultText);
                            else
                                com.DisplayName = com.Name.Replace("{{dyn}}", para.Value);
                        }
                        catch {
                        }
                        break;
                }
            }
            #endregion

            if (e == null) return;

            _= CheckComObjects(comObjs.allNew);

            ChangeParamModel change = new ChangeParamModel
            {
                DeviceId = device.UId,
                ParamId = para.Id,
                Value = para.Value
            };

            device.LoadedApplication = false;
            ChangeHandler.Instance.ChangedParam(change);
        }


        private (List<DeviceComObject> allNew, List<DeviceComObject> toDelete) CheckRemoveComObjects(string paraId, string paraValue)
        {
            List<DeviceComObject> newObjs = new List<DeviceComObject>();

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
            foreach (DeviceComObject cobj in device.ComObjects)
                if (!newObjs.Any(co => co.Id == cobj.Id) && cobj.Groups.Count != 0)
                    toDelete.Add(cobj);

            return (newObjs, toDelete);
        }


        private async Task CheckComObjects(List<DeviceComObject> newObjs = null)
        {
            if(newObjs == null)
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
                if (!coms.ContainsKey(com.ComId))
                    coms.Add(com.ComId, com);

            foreach (DeviceComObject cobj in toDelete)
            {
                ComObject com = coms[cobj.Id];
                _contextP.ComObjects.Remove(com);
                device.ComObjects.Remove(cobj);
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
                device.ComObjects.Add(dcom);


                ComObject com = new ComObject();
                com.ComId = dcom.Id;
                com.DeviceId = device.UId;
                _contextP.ComObjects.Add(com);
            }

            device.ComObjects.Sort(s => s.Number);
            _contextP.SaveChanges();

            await Task.Delay(1);
        }

    }
}
