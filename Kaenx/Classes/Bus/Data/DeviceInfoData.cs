using Kaenx.Classes.Project;
using Kaenx.Konnect.Addresses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Kaenx.Classes.Bus.Data
{
    public class DeviceInfoData : IBusData
    {
        public string Type { get; set; } = "Info";
        public string MaskVersion { get; set; }
        public string SerialNumber { get; set; }
        public string ApplicationId { get; set; }
        public string ApplicationName { get; set; }
        public string Manufacturer { get; set; }
        public string DeviceName { get; set; }
        public string Description { get; set; }
        public string Additional { get; set; }
        public bool SupportsEF { get; set; }
        public LineDevice Device { get; set; }

        public Visibility ShowAdditional { get { return string.IsNullOrEmpty(Additional) ? Visibility.Collapsed : Visibility.Visible; } }


        public List<MulticastAddress> GroupTable { get; set; }

        public List<AssociationHelper> AssociationTable { get; set; }

        //public ICollectionView OtherRessources { get; set; }
        public List<OtherResource> OtherResources { get; set; }
    }


    public class GroupInfoCollection<T> : ObservableCollection<T>
    {
        public string Key { get; set; }

        public new IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)base.GetEnumerator();
        }
    }


    public class OtherResource
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string ValueRaw { get; set; }
    }

    public class AssociationHelper
    {
        public string GroupIndex { get; set; }
        public int ObjectIndex { get; set; }
        public string ObjectInfo { get; set; }
        public string ObjectFunc { get; set; }
    }
}
