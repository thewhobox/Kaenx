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

        public static ObservableCollection<Device> GetDevicesFromCatalog(XElement catalogXML, XElement langXML)
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

            foreach (XElement lang in langXML.Elements())
            {
                if (lang.Attribute("Identifier").Value != System.Globalization.CultureInfo.CurrentCulture.Name) continue;


                IEnumerable<XElement> units = lang.Descendants(XName.Get("TranslationElement", catalogXML.Name.NamespaceName));

                foreach (XElement unit in units)
                {
                    if (!deviceList.Any(d => d.Id == unit.Attribute("RefId").Value)) continue;
                    Device model = deviceList.Single(d => d.Id == unit.Attribute("RefId").Value);

                    foreach (XElement trans in unit.Elements())
                    {
                        switch (trans.Attribute("AttributeName").Value)
                        {
                            case "Name":
                                model.Name = trans.Attribute("Text").Value;
                                break;

                            case "VisibleDescription":
                                model.VisibleDescription = trans.Attribute("Text").Value;
                                break;
                        }
                    }
                }
            }

            return deviceList;
        }
    }
}
