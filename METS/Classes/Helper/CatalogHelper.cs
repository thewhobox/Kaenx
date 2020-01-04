using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace METS.Classes.Helper
{
    public class CatalogHelper
    {

        public static ObservableCollection<Device> GetDevicesFromCatalog(XElement catalogXML)
        {
            ObservableCollection<Device> deviceList = new ObservableCollection<Device>();
            IEnumerable<XElement> catalogItems = catalogXML.Descendants(XName.Get("CatalogItem", catalogXML.Name.NamespaceName));
            catalogItems = catalogItems.OrderBy(c => c.Attribute("Name").Value);

            foreach(XElement catalogItem in catalogItems)
            {
                Device device = new Device();
                device.Id = catalogItem.Attribute("Id").Value;
                device.Name = catalogItem.Attribute("Name").Value;
                device.VisibleDescription = catalogItem.Attribute("VisibleDescription")?.Value;
                device.ProductRefId = catalogItem.Attribute("ProductRefId").Value;
                device.Hardware2ProgramRefId = catalogItem.Attribute("Hardware2ProgramRefId").Value;

                deviceList.Add(device);
            }

            return deviceList;
        }
    }
}
