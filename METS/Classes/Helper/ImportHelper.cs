using METS.Context;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.Storage;
using METS.Context.Catalog;

namespace METS.Classes.Helper
{
    class ImportHelper
    {
        public delegate void ProgressChangedHandler(int count);
        public event ProgressChangedHandler ProgressChanged;
        public event ProgressChangedHandler ProgressMaxChanged;
        public event ProgressChangedHandler ProgressAppChanged;
        public event ProgressChangedHandler ProgressAppMaxChanged;

        public delegate void ValueHandler(string value);
        public event ValueHandler OnError;
        public event ValueHandler OnDeviceChanged;

        public string currentMan = "";

        private string currentNamespace;
        private List<ManufacturerViewModel> tempManus;
        private CatalogContext _context = new CatalogContext();

        private Dictionary<string, string> Prod2Section;
        private List<string> DeviceIds;
        private Dictionary<string, string> App2Hardware;
        private List<ImportError> AppErrors;
        private List<string> AppIds;

        private XElement transCatalog;
        private Dictionary<string, string> cat2prod = new Dictionary<string, string>();

        public async Task ImportCatalog(ZipArchiveEntry catalogEntry)
        {
            Prod2Section = new Dictionary<string, string>();

            XElement catXML = XDocument.Load(catalogEntry.Open()).Root;
            currentNamespace = catXML.Attribute("xmlns").Value;
            XElement catalog = catXML.Element(GetXName("ManufacturerData")).Element(GetXName("Manufacturer")).Element(GetXName("Catalog"));

            CatalogViewModel section = null;

            if (!_context.Sections.Any(s => s.Id == currentMan))
            {
                ManufacturerViewModel man = tempManus.Find(e => e.Id == currentMan);
                section = new CatalogViewModel();
                section.Id = currentMan;
                section.Name_DE = man.Name;
                section.Name_EN = man.Name;
                section.ParentId = "main";
                _context.Sections.Add(section);
            }
            _context.SaveChanges();

            ProgressMaxChanged(catalog.Elements().Count());
            
            int count = 0;

            Dictionary<string, CatalogViewModel> sections = new Dictionary<string, CatalogViewModel>();

            foreach (XElement sectionEle in catalog.Elements())
            {
                await Task.Delay(100);
                section = new CatalogViewModel();
                section.Id = sectionEle.Attribute("Id").Value;
                if (!_context.Sections.Any(s => s.Id == section.Id))
                {
                    section.Name_DE = sectionEle.Attribute("Name")?.Value;
                    section.ParentId = currentMan;
                    sections.Add(section.Id, section);
                }

                foreach (XElement subsectionEle in sectionEle.Elements())
                {
                    CatalogViewModel sectionSub = new CatalogViewModel();
                    sectionSub.Id = subsectionEle.Attribute("Id").Value;
                    if (!_context.Sections.Any(s => s.Id == sectionSub.Id))
                    {
                        sectionSub.Name_DE = subsectionEle.Attribute("Name")?.Value;
                        sectionSub.ParentId = section.Id;
                        sections.Add(sectionSub.Id, sectionSub);
                    }

                    foreach (XElement itemEle in subsectionEle.Elements())
                    {
                        cat2prod.Add(itemEle.Attribute("Id").Value, itemEle.Attribute("ProductRefId").Value);
                        if (!Prod2Section.Keys.Contains(itemEle.Attribute("ProductRefId").Value))
                            Prod2Section.Add(itemEle.Attribute("ProductRefId").Value, itemEle.Parent.Attribute("Id").Value);
                    }
                }

                count++;
                ProgressChanged(count);
            }


            List<XElement> langs = catXML.Descendants(GetXName("Language")).ToList();

            foreach(XElement lang in langs)
            {
                if (lang.Attribute("Identifier").Value != System.Globalization.CultureInfo.CurrentCulture.Name) continue;

                transCatalog = lang;

                IEnumerable<XElement> units = lang.Descendants(GetXName("TranslationElement"));

                foreach (XElement unit in units)
                {
                    if (!sections.ContainsKey(unit.Attribute("RefId").Value)) continue;
                    CatalogViewModel model = sections[unit.Attribute("RefId").Value];

                    foreach(XElement trans in unit.Elements())
                    {
                        if(trans.Attribute("AttributeName").Value == "Name")
                            model.Name_DE = trans.Attribute("Text").Value;
                    }

                    _context.Sections.Add(model);
                }
            }

            _context.SaveChanges();
        }

        public async Task ImportHardware(ZipArchiveEntry hardEntry, List<string> prods2load)
        {
            ProgressMaxChanged(100);

            AppIds = new List<string>();
            DeviceIds = new List<string>();
            App2Hardware = new Dictionary<string, string>();

            XElement hardXML = XDocument.Load(hardEntry.Open()).Root;
            currentNamespace = hardXML.Attribute("xmlns").Value;
            XElement hardware = hardXML.Element(GetXName("ManufacturerData")).Element(GetXName("Manufacturer")).Element(GetXName("Hardware"));

            IEnumerable<XElement> prods = hardware.Descendants(GetXName("Product"));
            int count = 0;

            Dictionary<string, DeviceViewModel> devices = new Dictionary<string, DeviceViewModel>();

            ProgressMaxChanged(prods.Count());

            foreach (XElement prodEle in prods)
            {
                await Task.Delay(100);
                string x = prodEle.Attribute("Id").Value;
                if (!prods2load.Contains(x))
                {
                    count++;
                    ProgressChanged(count);
                    continue;
                }

                XElement parent = prodEle.Parent.Parent;
                DeviceViewModel device = new DeviceViewModel();
                device.Id = prodEle.Attribute("Id").Value;
                DeviceIds.Add(device.Id);

                if (!_context.Devices.Any(d => d.Id == device.Id))
                {
                    device.ManufacturerId = currentMan;
                    device.Name = prodEle.Attribute("Text").Value;
                    device.VisibleDescription = prodEle.Attribute("VisibleDescription")?.Value; // GetAttributeTransalted(prodEle, "VisibleDescription", "DE");
                    device.OrderNumber = prodEle.Attribute("OrderNumber").Value;

                    device.BusCurrent = ConvertBusCurrent(parent.Attribute("BusCurrent")?.Value);
                    if (Prod2Section.Keys.Contains(device.Id))
                        device.CatalogId = Prod2Section[device.Id];
                    else
                        device.CatalogId = currentMan;
                    device.IsRailMounted = GetAttributeAsBool(prodEle, "IsRailMounted");
                    device.IsPowerSupply = GetAttributeAsBool(parent, "IsPowerSupply");
                    device.IsCoupler = GetAttributeAsBool(parent, "IsCoupler");
                    device.HasApplicationProgram = GetAttributeAsBool(parent, "HasApplicationProgram");
                    device.HasIndividualAddress = GetAttributeAsBool(parent, "HasIndividualAddress");
                    device.HardwareId = parent.Attribute("Id").Value;

                    devices.Add(device.Id, device);
                }

                IEnumerable<XElement> apps = parent.Descendants(GetXName("ApplicationProgramRef"));

                foreach (XElement app in apps)
                {
                    string appId = app.Attribute("RefId").Value;
                    string hardId = parent.Attribute("Id").Value;
                    if (!AppIds.Contains(appId))
                        AppIds.Add(appId);

                    if (!App2Hardware.Keys.Contains(appId)) 
                        App2Hardware.Add(appId, hardId);

                    if (!_context.Hardware2App.Any(h => h.HardwareId == hardId && h.ApplicationId == appId))
                        _context.Hardware2App.Add(new Hardware2AppModel { HardwareId = hardId, ApplicationId = appId });

                }

                count++;
                ProgressChanged(count);
            }



            IEnumerable<XElement> units = transCatalog?.Descendants(GetXName("TranslationElement"));

            if(units == null)
            {
                _context.SaveChanges();
                return;
            }

            foreach (XElement unit in units)
            {
                if (!cat2prod.ContainsKey(unit.Attribute("RefId").Value)) continue;
                string prodId = cat2prod[unit.Attribute("RefId").Value];
                if (!devices.ContainsKey(prodId)) continue;
                DeviceViewModel model = devices[prodId];

                foreach (XElement trans in unit.Elements())
                {
                    switch(trans.Attribute("AttributeName").Value)
                    {
                        case "Name":
                            model.Name = trans.Attribute("Text").Value;
                            break;

                        case "VisibleDescription":
                            model.VisibleDescription = trans.Attribute("Text").Value;
                            break;
                    }
                }

                _context.Devices.Add(model);
            }

            _context.SaveChanges();
        }

        public async Task ImportApplications(ZipArchive archive)
        {
            ProgressMaxChanged(AppIds.Count);
            AppErrors = new List<ImportError>();

            int count = 0;
            foreach (string appId in AppIds)
            {
                await Task.Delay(200);
                string hardId = App2Hardware[appId];
                string manuId = appId.Substring(0, appId.IndexOf('_'));

                ZipArchiveEntry appEntry = null;
                try
                {
                    appEntry = archive.GetEntry(manuId + "/" + appId + ".xml");
                } catch
                {

                }

                ApplicationViewModel app = new ApplicationViewModel();
                app.Id = appId;

                XDocument doc = XDocument.Load(appEntry.Open());
                XElement appele = doc.Descendants(XName.Get("ApplicationProgram", doc.Root.Name.Namespace.NamespaceName)).First();

                app.Number = int.Parse(appele.Attribute("ApplicationNumber").Value);
                app.Version = int.Parse(appele.Attribute("ApplicationVersion").Value);
                app.Mask = appele.Attribute("MaskVersion").Value;
                app.Name = appele.Attribute("Name").Value;

                Hardware2AppModel hard2App = _context.Hardware2App.Single(h => h.ApplicationId == app.Id);
                hard2App.Name = app.Name;
                hard2App.Version = app.Version;
                hard2App.Number = app.Number;
                _context.Hardware2App.Update(hard2App);
                _context.SaveChanges();


                int rest = app.Version % 16;
                int full = (app.Version - rest) / 16;

                OnDeviceChanged(app.Name + " " + "V" + full.ToString() + "." + rest.ToString());

                List<string> errs = CheckApplication(XmlReader.Create(appEntry.Open()));
                if (errs.Count > 0)
                {
                    ImportError err = new ImportError(app.Id);
                    err.Code = string.Join(",", errs);
                    err.Exception = "ApplicationCheck";
                    err.Message = app.Name + "Die Applikation hat die Überprüfung nicht bestanden. (evtl. Plugin benötigt) " + string.Join(",", errs);
                    AppErrors.Add(err);

                    OnError(app.Name + ": Die Applikation hat die Überprüfung nicht bestanden. (evtl. Plugin benötigt) " + string.Join(",", errs));

                    count++;
                    ProgressChanged(count);
                    continue;
                }
                
                int cmax = 0;
                using(XmlReader reader = XmlReader.Create(appEntry.Open()))
                {
                    while (reader.Read())
                    {
                        cmax++;

                        if (reader.NodeType == XmlNodeType.EndElement)
                            continue;
                        switch (reader.Name)
                        {
                            
                            case "ParameterType":
                                reader.ReadInnerXml();
                                break;

                            case "LoadProcedures":
                            case "ParameterRef":
                            case "AbsoluteSegment":
                            case "Union":
                            case "ComObject":
                            case "TranslationElement":
                            case "Dynamic":
                            case "ComObjectRef":
                            case "Parameter":
                                reader.ReadOuterXml();
                                break;
                        }
                    }
                }
                
                ProgressAppMaxChanged(cmax);



                await ReadApplication(XmlReader.Create(appEntry.Open()), app, cmax);

                

                count++;
                ProgressChanged(count);
            }

            OnDeviceChanged("");
        }



        public async Task UpdateManufacturers(XElement manXML)
        {
            tempManus = new List<ManufacturerViewModel>();
            currentNamespace = manXML.Attribute("xmlns").Value;
            XElement mans = manXML.Element(GetXName("MasterData")).Element(GetXName("Manufacturers"));

            ProgressMaxChanged(mans.Elements().Count());
            
            int count = 0;

            foreach (XElement manEle in mans.Elements())
            {
                ManufacturerViewModel man = new ManufacturerViewModel();
                man.Id = manEle.Attribute("Id").Value;
                man.Name = manEle.Attribute("Name").Value;
                man.KnxManufacturerId = int.Parse(manEle.Attribute("KnxManufacturerId").Value);

                tempManus.Add(man);

                count++;
                ProgressChanged(count);
                if(count % 4 == 0) await Task.Delay(1);
            }
        }

        public async Task CheckParams()
        {
            ProgressMaxChanged(App2Hardware.Count);
            int count = 0;
            int c = 0;
            foreach (string appId in App2Hardware.Keys)
            {
                if (AppErrors.Any(e => e.Id == appId)) continue;

                IEnumerable<AppAbsoluteSegmentViewModel> segmentsList = _context.AppAbsoluteSegments.Where(s => s.ApplicationId == appId);

                Dictionary<string, byte[]> segments = new Dictionary<string, byte[]>();
                //Dictionary<string, int> cachedTypeLength = new Dictionary<string, int>();
                //Dictionary<string, AppParameterTypeViewModel> cachedType = new Dictionary<string, AppParameterTypeViewModel>();

                foreach (AppAbsoluteSegmentViewModel s2p in segmentsList)
                {
                    if (s2p.Data == null) continue;
                    byte[] bytes = Convert.FromBase64String(s2p.Data);
                    segments.Add(s2p.Id, bytes);
                }
                segmentsList = null;

                IEnumerable<AppParameter> ps = _context.AppParameters.Where(p => p.ApplicationId == appId);

                ProgressAppMaxChanged(ps.Count());
                c = 0;

                #region Generate AppSegment
                //foreach (AppParameter para in ps)
                //{
                //    c++;
                //    ProgressAppChanged(c);
                //    if (c % 10 == 0) await Task.Delay(1);

                //    if (para.Access != AccessType.Full)
                //        continue;

                //    if (para.AbsoluteSegmentId == null || !segments.Keys.Contains(para.AbsoluteSegmentId))
                //        continue;

                //    AppParameterTypeViewModel paratype = null;


                //    if (!cachedType.Keys.Contains(para.ParameterTypeId))
                //    {
                //        paratype = _context.AppParameterTypes.Single(p => p.Id == para.ParameterTypeId);
                //        cachedType.Add(paratype.Id, paratype);
                //    }
                //    else
                //    {
                //        paratype = cachedType[para.ParameterTypeId];
                //    }

                //    if (paratype.Type != ParamTypes.Enum)
                //        continue;

                //    byte[] data = segments[para.AbsoluteSegmentId];
                //    byte[] readed = null;

                //    int length = 0;
                //    int lengthToRead = 0;

                //    if (!cachedTypeLength.Keys.Contains(para.ParameterTypeId))
                //    {
                //        AppParameterTypeViewModel paraType = _context.AppParameterTypes.Single(pt => pt.Id == para.ParameterTypeId);
                //        length = paraType.Size;
                //        cachedTypeLength.Add(para.ParameterTypeId, length);
                //    }
                //    else
                //    {
                //        length = cachedTypeLength[para.ParameterTypeId];
                //    }

                //    if (length < 7)
                //    {
                //        lengthToRead = 1;
                //        readed = new byte[1];
                //    }
                //    else
                //    {
                //        lengthToRead = (length / 8);
                //        readed = new byte[lengthToRead];
                //    }

                //    for (int i = para.Offset; i < para.Offset + lengthToRead; i++)
                //    {
                //        readed[i - para.Offset] = data[i];
                //    }


                //    string value = "";

                //    if (length < 7)
                //    {
                //        byte[] toread = new byte[1];
                //        toread[0] = readed[0];
                //        System.Collections.BitArray arr = new System.Collections.BitArray(toread);
                //        System.Collections.BitArray readedArr = new System.Collections.BitArray(8);
                //        byte[] readedBytes = new byte[2];
                //        try
                //        {
                //            for (int i = 0; i < length; i++)
                //            {
                //                readedArr[i] = arr[i + para.OffsetBit];
                //            }
                //        } catch(Exception e)
                //        {

                //        }

                //        byte a = 0;
                //        if (readedArr.Get(0)) a++;
                //        if (readedArr.Get(1)) a += 2;
                //        if (readedArr.Get(2)) a += 4;
                //        if (readedArr.Get(3)) a += 8;
                //        if (readedArr.Get(4)) a += 16;
                //        if (readedArr.Get(5)) a += 32;
                //        if (readedArr.Get(6)) a += 64;
                //        if (readedArr.Get(7)) a += 128;

                //        byte[] y = new byte[2];
                //        y[0] = a;

                //        uint a1 = BitConverter.ToUInt16(y, 0);

                //        value = a1.ToString();
                //    }
                //    else
                //    {
                //        if (readed.Count() < 2)
                //        {
                //            byte temp = readed[0];
                //            readed = new byte[2];
                //            readed[0] = temp;
                //        }
                //        readed = readed.Reverse().ToArray();
                //        value = BitConverter.ToUInt16(readed, 0).ToString();
                //    }

                //    bool exists = _context.AppParameterTypeEnums.Any(pe => pe.Value == value && pe.ParameterId == paratype.Id);

                //    if (!exists)
                //    {
                //        if (length < 8)
                //        {
                //            UInt16 numb = UInt16.Parse(para.Value);
                //            byte[] converted = BitConverter.GetBytes(numb);
                //            byte[] segBytes = new byte[1];
                //            segBytes[0] = segments[para.AbsoluteSegmentId][para.Offset];
                //            System.Collections.BitArray convArr = new System.Collections.BitArray(converted);
                //            System.Collections.BitArray segmArr = new System.Collections.BitArray(segBytes);
                //            for (int i = 0; i < length; i++)
                //            {
                //                segmArr[i + para.OffsetBit] = convArr[i];
                //            }
                //            byte a = 0;
                //            if (segmArr.Get(0)) a++;
                //            if (segmArr.Get(1)) a += 2;
                //            if (segmArr.Get(2)) a += 4;
                //            if (segmArr.Get(3)) a += 8;
                //            if (segmArr.Get(4)) a += 16;
                //            if (segmArr.Get(5)) a += 32;
                //            if (segmArr.Get(6)) a += 64;
                //            if (segmArr.Get(7)) a += 128;

                //            segments[para.AbsoluteSegmentId][para.Offset] = a;
                //        }
                //        else
                //        {
                //            byte[] toWrite = new byte[2];
                //            UInt16 numb = UInt16.Parse(para.Value);
                //            byte[] converted = BitConverter.GetBytes(numb);
                //            if (converted[1] != 0)
                //                converted = converted.Reverse().ToArray();

                //            for (int i1 = 0; i1 < lengthToRead; i1++)
                //            {
                //                segments[para.AbsoluteSegmentId][para.Offset + i1] = converted[i1];
                //            }
                //        }
                //    }
                //}
                #endregion

                foreach (string segId in segments.Keys)
                {
                    AppAbsoluteSegmentViewModel seg = _context.AppAbsoluteSegments.FirstOrDefault(a => a.Id == segId);
                    seg.Data = Convert.ToBase64String(segments[segId]);
                    _context.AppAbsoluteSegments.Update(seg);
                }

                count++;
                ProgressChanged(count);
                await Task.Delay(1000);
            }

            _context.SaveChanges();
        }

        public async Task<List<DeviceViewModel>> CheckDevices()
        {
            List<DeviceViewModel> AddedDevices = new List<DeviceViewModel>();
            ProgressMaxChanged(DeviceIds.Count);
            int count = 0;

            foreach (string deviceId in DeviceIds)
            {
                DeviceViewModel device = _context.Devices.FirstOrDefault(d => d.Id == deviceId);

                if (device == null)
                {
                    count++;
                    ProgressChanged(count);
                    await Task.Delay(500);
                    continue;
                }


                if (!device.HasApplicationProgram)
                {
                    AddedDevices.Add(device);
                    continue;
                }

                string hardwareId = device.HardwareId;

                if (_context.Hardware2App.Any(a => a.HardwareId == hardwareId))
                {
                    bool hasApp = false;
                    foreach(Hardware2AppModel model in _context.Hardware2App.Where(h => h.HardwareId == device.HardwareId))
                    {
                        if (_context.Applications.Any(a => a.Id == model.ApplicationId))
                            hasApp = true;
                    }

                    if(hasApp)
                        AddedDevices.Add(device);
                    else
                        _context.Devices.Remove(device);
                }
                else
                    _context.Devices.Remove(device);

                count++;
                ProgressChanged(count);
                await Task.Delay(500);
            }

            _context.SaveChanges();
            return AddedDevices;
        }


        private async Task ReadApplication(XmlReader reader, ApplicationViewModel app, int maxcount)
        {
            try
            {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Dynamic", CreationCollisionOption.OpenIfExists);
            } catch { }



            XElement temp = null;
            AppParameter param = null;
            List<AppError> Errors = new List<AppError>();
            Dictionary<string, AppParameter> Params = new Dictionary<string, AppParameter>();
            Dictionary<string, AppParameterRef> ParamRefs = new Dictionary<string, AppParameterRef>();
            Dictionary<string, AppComObject> ComObjects = new Dictionary<string, AppComObject>();
            Dictionary<string, AppComObjectRef> ComObjectRefs = new Dictionary<string, AppComObjectRef>();
            Dictionary<string, string> transParamBock = new Dictionary<string, string>();
            string currentLang = "";

            int c = 0;
            int c1 = 0;
            int c2 = 0;

            try
            {
                while (reader.Read())
                {
                    c++;
                    if (c % 60 == 0)
                    {
                        ProgressAppChanged(c);
                        await Task.Delay(1);
                    }

                    if (reader.NodeType == XmlNodeType.EndElement)
                        continue;
                    switch (reader.Name)
                    {
                        case "":
                        case "xml":
                        case "KNX":
                        case "ManufacturerData":
                        case "Manufacturer":
                        case "ApplicationPrograms":
                        case "Baggage":
                        case "Static":
                        case "Code":
                        case "Extension":
                        case "Languages":
                        case "Options":
                        case "Parameters":
                        case "ParameterTypes":
                        case "ParameterRefs":
                        case "TranslationUnit":
                        case "TypeNumber":
                        case "TypeRestriction":
                        case "TypePicture":
                        case "TypeFloat":
                        case "TypeText":
                        case "TypeNone":
                        case "TypeIPAddress":
                        case "Enumeration":
                        case "Translation":
                        case "ComObjectRefs":
                        case "ApplicationProgram":
                            continue;
                        case "LoadProcedures":
                            reader.ReadOuterXml();
                            break;
                        case "AbsoluteSegment":
                            temp = XDocument.Parse(reader.ReadOuterXml()).Root;
                            AppAbsoluteSegmentViewModel aas = new AppAbsoluteSegmentViewModel();
                            aas.Id = temp.Attribute("Id").Value;
                            aas.ApplicationId = app.Id;
                            if (!_context.AppAbsoluteSegments.Any(a => a.Id == aas.Id))
                            {
                                aas.Address = int.Parse(temp.Attribute("Address").Value);
                                aas.Size = int.Parse(temp.Attribute("Size").Value);
                                aas.Data = temp.Element(GetXName("Data"))?.Value;
                                aas.Mask = temp.Element(GetXName("Mask"))?.Value;
                                _context.AppAbsoluteSegments.Add(aas);
                            }
                            temp = null;
                            aas = null;
                            break;
                        case "ParameterType":
                            AppParameterTypeViewModel paramt = new AppParameterTypeViewModel();
                            paramt.Id = reader.GetAttribute("Id");
                            if (!_context.AppParameterTypes.Any(p => p.Id == paramt.Id))
                            {
                                temp = XDocument.Parse(reader.ReadInnerXml()).Root;
                                switch (temp.Name.LocalName)
                                {
                                    case "TypeNumber":
                                        switch (temp.Attribute("Type").Value)
                                        {
                                            case "signedInt":
                                                paramt.Type = ParamTypes.NumberInt;
                                                break;
                                            case "unsignedInt":
                                                paramt.Type = ParamTypes.NumberUInt;
                                                break;
                                            default:
                                                Errors.Add(new AppError(app.Id, "ParameterType", temp.Name.LocalName, temp.Attribute("Type").Value, "Unbekannter Nummerntype"));
                                                break;
                                        }
                                        paramt.Size = int.Parse(temp.Attribute("SizeInBit").Value);
                                        paramt.Tag1 = temp.Attribute("minInclusive").Value;
                                        paramt.Tag2 = temp.Attribute("maxInclusive").Value;
                                        break;
                                    case "TypeRestriction":
                                        paramt.Type = ParamTypes.Enum;
                                        paramt.Size = int.Parse(temp.Attribute("SizeInBit").Value);
                                        string _base = temp.Attribute("Base").Value;
                                        int cenu = 0;
                                        foreach (XElement en in temp.Elements())
                                        {
                                            AppParameterTypeEnumViewModel enu = new AppParameterTypeEnumViewModel();
                                            enu.ParameterId = paramt.Id;
                                            enu.Id = en.Attribute("Id").Value;
                                            if (!_context.AppParameterTypeEnums.Any(p => p.Id == enu.Id))
                                            {
                                                enu.Value = en.Attribute(_base).Value;
                                                enu.Text = en.Attribute("Text").Value;
                                                enu.Order = (en.Attribute("DisplayOrder") == null) ? cenu : int.Parse(en.Attribute("DisplayOrder").Value);
                                                _context.AppParameterTypeEnums.Add(enu);
                                                cenu++;
                                            }
                                        }
                                        break;
                                    case "TypeText":
                                        paramt.Type = ParamTypes.Text;
                                        paramt.Size = int.Parse(temp.Attribute("SizeInBit").Value);
                                        break;
                                    case "TypeFloat":
                                        switch (temp.Attribute("Encoding").Value)
                                        {
                                            case "DPT 9":
                                                paramt.Type = ParamTypes.Float9;
                                                break;
                                            default:
                                                Errors.Add(new AppError(app.Id, "ParameterType", "TypeFloat", temp.Attribute("Encoding").Value, "Unbekannter Floattype"));
                                                break;
                                        }
                                        paramt.Tag1 = temp.Attribute("minInclusive").Value;
                                        paramt.Tag2 = temp.Attribute("maxInclusive").Value;
                                        break;
                                    case "TypePicture":
                                        paramt.Type = ParamTypes.Picture;
                                        paramt.Tag1 = temp.Attribute("RefId").Value;
                                        break;
                                    case "TypeIPAddress":
                                        paramt.Type = ParamTypes.IpAdress;
                                        paramt.Tag1 = temp.Attribute("AddressType").Value;
                                        paramt.Size = 4 * 8;
                                        break;
                                    case "TypeNone":
                                        paramt.Type = ParamTypes.None;
                                        break;
                                    default:
                                        Errors.Add(new AppError(app.Id, "ParameterType", temp.Name.LocalName, "", "Unbekannter Parametertype"));
                                        break;
                                }
                                temp = null;
                                _context.AppParameterTypes.Add(paramt);
                            }
                            paramt = null;
                            break;
                        case "Parameter":
                            param = new AppParameter();
                            param.Id = reader.GetAttribute("Id");
                            temp = XDocument.Parse(reader.ReadOuterXml()).Root;
                            param.Text = temp.Attribute("Text").Value;
                            param.ParameterTypeId = temp.Attribute("ParameterType").Value;
                            param.Value = temp.Attribute("Value")?.Value;
                            switch(temp.Attribute("Access")?.Value)
                            {
                                case "None":
                                    param.Access = AccessType.None;
                                    break;
                                case "Read":
                                    param.Access = AccessType.Read;
                                    break;

                                default:
                                    param.Access = AccessType.Full;
                                    break;
                            }

                            if (temp.Elements(GetXName("Memory")).Count() > 0)
                            {
                                temp = temp.Elements(GetXName("Memory")).ElementAt(0);
                                param.AbsoluteSegmentId = temp.Attribute("CodeSegment").Value;
                                param.Offset = int.Parse(temp.Attribute("Offset").Value);
                                param.OffsetBit = int.Parse(temp.Attribute("BitOffset").Value);
                            }
                            Params.Add(param.Id, param);
                            param = null;
                            temp = null;
                            break;
                        case "Union":
                            temp = XDocument.Parse(reader.ReadOuterXml()).Root;
                            XElement mem = temp.Element(GetXName("Memory"));
                            string t1 = mem.Attribute("CodeSegment").Value;
                            int t2 = int.Parse(mem.Attribute("Offset").Value);
                            int t3 = int.Parse(mem.Attribute("BitOffset").Value);
                            int t4 = int.Parse(temp.Attribute("SizeInBit").Value);
                            mem = null;
                            foreach (XElement para in temp.Elements(GetXName("Parameter")))
                            {
                                param = new AppParameter();
                                param.Id = para.Attribute("Id").Value;
                                param.Text = para.Attribute("Text").Value;
                                param.ParameterTypeId = para.Attribute("ParameterType").Value;
                                param.AbsoluteSegmentId = t1;

                                int off = int.Parse(para.Attribute("Offset").Value);
                                int offb = int.Parse(para.Attribute("BitOffset").Value);
                                param.Offset = t2 + off;
                                param.OffsetBit = t3 + offb;
                                param.Value = para.Attribute("Value")?.Value;
                                if (para.Attribute("Access")?.Value == "None")
                                    param.Access = AccessType.None;
                                else
                                    param.Access = AccessType.Full;
                                Params.Add(param.Id, param);
                                param = null;
                            }
                            temp = null;
                            break;
                        case "ParameterRef":
                            AppParameterRef pref = new AppParameterRef();
                            temp = XDocument.Parse(reader.ReadOuterXml()).Root;
                            pref.Id = temp.Attribute("Id").Value;
                            pref.RefId = temp.Attribute("RefId").Value;
                            pref.Text = temp.Attribute("Text")?.Value;
                            pref.Access = (temp.Attribute("Access")) == null ? AccessType.Null : ((temp.Attribute("Access").Value == "None") ? AccessType.None : AccessType.Full);
                            pref.Value = temp.Attribute("Value")?.Value;
                            ParamRefs.Add(pref.Id, pref);
                            break;
                        case "AddressTable":
                            app.Table_Group = reader.GetAttribute("CodeSegment");
                            int offsetGroup;
                            int.TryParse(reader.GetAttribute("Offset"), out offsetGroup);
                            app.Table_Group_Offset = offsetGroup;
                            int maxEntries;
                            int.TryParse(reader.GetAttribute("MaxEntries"), out maxEntries);
                            app.Table_Group_Max = maxEntries;
                            break;
                        case "ComObjectTable":
                            app.Table_Object = reader.GetAttribute("CodeSegment");
                            int offsetObject;
                            int.TryParse(reader.GetAttribute("Offset"), out offsetObject);
                            app.Table_Object_Offset = offsetObject;
                            break;
                        case "AssociationTable":
                            app.Table_Assosiations = reader.GetAttribute("CodeSegment");
                            int offsetAssoc;
                            int.TryParse(reader.GetAttribute("Offset"), out offsetAssoc);
                            app.Table_Assosiations_Offset = offsetAssoc;
                            int maxEntriesA;
                            int.TryParse(reader.GetAttribute("MaxEntries"), out maxEntriesA);
                            app.Table_Assosiations_Max = maxEntriesA;
                            break;
                        case "ComObject":
                            AppComObject cobj = new AppComObject();
                            temp = XDocument.Parse(reader.ReadOuterXml()).Root;
                            cobj.Id = temp.Attribute("Id").Value;
                            cobj.SetText(temp.Attribute("Text")?.Value);
                            cobj.SetFunction(temp.Attribute("FunctionText")?.Value);
                            cobj.SetSize(temp.Attribute("ObjectSize")?.Value);
                            cobj.SetDatapoint(temp.Attribute("DatapointType")?.Value);
                            cobj.Number = int.Parse(temp.Attribute("Number").Value);

                            cobj.Flag_Communicate = temp.Attribute("CommunicationFlag")?.Value == "Enabled";
                            cobj.Flag_Read = temp.Attribute("ReadFlag")?.Value == "Enabled";
                            cobj.Flag_ReadOnInit = temp.Attribute("ReadOnInitFlag")?.Value == "Enabled";
                            cobj.Flag_Transmit = temp.Attribute("TransmitFlag")?.Value == "Enabled";
                            cobj.Flag_Update = temp.Attribute("UpdateFlag")?.Value == "Enabled";
                            cobj.Flag_Write = temp.Attribute("WriteFlag")?.Value == "Enabled";
                            // Todo: Add Flags
                            ComObjects.Add(cobj.Id, cobj);
                            break;
                        case "ComObjectRef":
                            AppComObjectRef cobjr = new AppComObjectRef();
                            temp = XDocument.Parse(reader.ReadOuterXml()).Root;
                            cobjr.Id = temp.Attribute("Id").Value;
                            cobjr.RefId = temp.Attribute("RefId").Value;

                            //TODO implement auto translate
                            cobjr.SetText(temp.Attribute("Text")?.Value);
                            cobjr.SetFunction(temp.Attribute("FunctionText")?.Value);
                            cobjr.SetSize(temp.Attribute("ObjectSize")?.Value);
                            cobjr.SetDatapoint(temp.Attribute("DatapointType")?.Value);
                            cobjr.Number = temp.Attribute("Number") == null ? -1 : int.Parse(temp.Attribute("Number").Value);

                            if (temp.Attribute("CommunicationFlag")?.Value == "Enabled")
                                cobjr.Flag_Communicate = true;
                            if (temp.Attribute("CommunicationFlag")?.Value == "Disabled")
                                cobjr.Flag_Communicate = false;
                            if (temp.Attribute("ReadFlag")?.Value == "Enabled")
                                cobjr.Flag_Read = true;
                            if (temp.Attribute("ReadFlag")?.Value == "Disabled")
                                cobjr.Flag_Read = false;
                            if (temp.Attribute("ReadOnInitFlag")?.Value == "Enabled")
                                cobjr.Flag_ReadOnInit = true;
                            if (temp.Attribute("ReadOnInitFlag")?.Value == "Disabled")
                                cobjr.Flag_ReadOnInit = false;
                            if (temp.Attribute("TransmitFlag")?.Value == "Enabled")
                                cobjr.Flag_Transmit = true;
                            if (temp.Attribute("TransmitFlag")?.Value == "Disabled")
                                cobjr.Flag_Transmit = false;
                            if (temp.Attribute("UpdateFlag")?.Value == "Enabled")
                                cobjr.Flag_Update = true;
                            if (temp.Attribute("UpdateFlag")?.Value == "Disabled")
                                cobjr.Flag_Update = false;
                            if (temp.Attribute("WriteFlag")?.Value == "Enabled")
                                cobjr.Flag_Write = true;
                            if (temp.Attribute("WriteFlag")?.Value == "Disabled")
                                cobjr.Flag_Write = false;

                            // Todo: Add Flags
                            ComObjectRefs.Add(cobjr.Id, cobjr);
                            break;
                        case "Dynamic":
                            string xml = reader.ReadOuterXml();
                            try
                            {
                                StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("Dynamic");
                                StorageFile file = await folder.CreateFileAsync(app.Id + ".xml", CreationCollisionOption.ReplaceExisting);
                                await FileIO.WriteTextAsync(file, xml);
                            } catch
                            {

                            }
                            break;
                        case "Language":
                            _context.SaveChanges();
                            string lang = reader.GetAttribute("Identifier").Substring(0, 2);

                            if (System.Globalization.CultureInfo.CurrentCulture.Parent.Name == lang)
                            {
                                currentLang = lang;
                            }
                            else
                            {
                                currentLang = null;
                            }

                            break;
                        case "TranslationElement":
                            if (currentLang == null) continue;
                            temp = XDocument.Parse(reader.ReadOuterXml()).Root;
                            IEnumerable<XElement> childs = temp.Elements();
                            string refId = temp.Attribute("RefId").Value;
                            if (refId == app.Id)
                            {
                                string appname = temp.Elements().ElementAt(0).Attribute("Text").Value;
                                app.Name = appname;
                                Hardware2AppModel hard2App = _context.Hardware2App.Single(h => h.ApplicationId == app.Id);
                                hard2App.Name = app.Name;
                                _context.Hardware2App.Update(hard2App);

                                break;
                            }
                            string type = refId.Split('_')[2];
                            type = type.Substring(0, type.IndexOf('-'));
                            foreach (XElement transele in childs)
                            {
                                switch (type)
                                {
                                    // Dynamic View
                                    case "PS": // ParaeterSeperator
                                    case "PR": // ParameterBlockRename
                                    case "CH": // Channel
                                        // ToDo: Translation einpflegen
                                        break;
                                    case "PB":
                                        transParamBock.Add(refId, transele.Attribute("Text").Value);
                                        break;
                                    case "PT":
                                        switch (transele.Attribute("AttributeName").Value)
                                        {
                                            case "Text":
                                                AppParameterTypeEnumViewModel en = _context.AppParameterTypeEnums.Single(e => e.Id == refId);
                                                en.Text = transele.Attribute("Text").Value;
                                                _context.AppParameterTypeEnums.Update(en);
                                                break;
                                            case "SuffixText": // ToDo: Iwann mal wirklich implementieren
                                                break;
                                            default:
                                                Errors.Add(new AppError(app.Id, "TranslationElement", type, transele.Attribute("AttributeName").Value, "Unbekannte Übersetzung 1"));
                                                break;
                                        }
                                        break;
                                    case "UP":
                                    case "P":
                                        if (refId.Split('_').Count() == 4 && refId.Split('_')[3].Substring(0, 1) == "R")
                                        {
                                            switch (transele.Attribute("AttributeName").Value)
                                            {
                                                case "Text":
                                                    ParamRefs[refId].Text = transele.Attribute("Text").Value;
                                                    break;
                                                case "InitialValue":
                                                case "SuffixText": // ToDo: Iwann mal wirklich implementieren
                                                    break;
                                                default:
                                                    Errors.Add(new AppError(app.Id, "TranslationElement", type, transele.Attribute("AttributeName").Value, "Unbekannte Übersetzung 2"));
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            switch (transele.Attribute("AttributeName").Value)
                                            {
                                                case "Text":
                                                    Params[refId].Text = transele.Attribute("Text").Value;
                                                    break;
                                                case "SuffixText": // ToDo: Iwann mal wirklich implementieren
                                                    break;
                                                default:
                                                    Errors.Add(new AppError(app.Id, "TranslationElement", type, transele.Attribute("AttributeName").Value, "Unbekannte Übersetzung 3"));
                                                    break;
                                            }
                                        }
                                        break;
                                    case "O":
                                        if (refId.Split('_').Count() == 4 && refId.Split('_')[3].Substring(0, 1) == "R")
                                        {
                                            switch (transele.Attribute("AttributeName").Value)
                                            {
                                                case "Text":
                                                    ComObjectRefs[refId].SetText(transele.Attribute("Text").Value, currentLang);
                                                    break;
                                                case "FunctionText":
                                                    ComObjectRefs[refId].SetFunction(transele.Attribute("Text").Value, currentLang);
                                                    break;
                                                case "VisibleDescription":
                                                    break;
                                                default:
                                                    Errors.Add(new AppError(app.Id, "TranslationElement", type, transele.Attribute("AttributeName").Value, "Unbekannte Übersetzung 4"));
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            switch (transele.Attribute("AttributeName").Value)
                                            {
                                                case "Text":
                                                    ComObjects[refId].SetText(transele.Attribute("Text").Value, currentLang);
                                                    break;
                                                case "FunctionText":
                                                    ComObjects[refId].SetFunction(transele.Attribute("Text").Value, currentLang);
                                                    break;
                                                case "VisibleDescription":
                                                    break;
                                                default:
                                                    Errors.Add(new AppError(app.Id, "TranslationElement", type, transele.Attribute("AttributeName").Value, "Unbekannte Übersetzung 5"));
                                                    break;
                                            }
                                        }
                                        break;

                                    default:
                                        Errors.Add(new AppError(app.Id, "TranslationElement", type, "", "Unbekannte Übersetzungstyp"));
                                        break;
                                }
                            }
                            break;
                        default:
                            Errors.Add(new AppError(app.Id, reader.Name, "", "", "Unbekannter Parameter"));
                            break;
                    }
                }

                ProgressAppChanged(c);

                if (!_context.Applications.Any(a => a.Id == app.Id))
                    _context.Applications.Add(app);

                if (Errors.Count != 0)
                {
                    OnError("Es traten " + Errors.Count.ToString() + " Fehler auf!");
                }

                Dictionary<string, AppParameterTypeViewModel> typeCache = new Dictionary<string, AppParameterTypeViewModel>();
                System.IO.MemoryStream defaultAbsoluteSegment = new System.IO.MemoryStream();
                int nullOffset = 0;

                List<byte[]> bytesToWrite = new List<byte[]>();

                foreach (AppParameterRef appref in ParamRefs.Values)
                {
                    if (!_context.AppParameters.Any(p => p.Id == appref.Id))
                    {
                        AppParameter final = new AppParameter();
                        final.LoadPara(Params[appref.RefId]);
                        final.Id = appref.Id;
                        final.ApplicationId = app.Id;
                        if (appref.Text != null) final.Text = appref.Text;
                        if (appref.Value != null) final.Value = appref.Value;
                        if (appref.Access != AccessType.Null) final.Access = appref.Access;


                        if (final.AbsoluteSegmentId == null && final.Access == AccessType.Full)
                        {
                            AppParameterTypeViewModel paratype;

                            if (typeCache.Keys.Contains(final.ParameterTypeId))
                            {
                                paratype = typeCache[final.ParameterTypeId];
                            }
                            else
                            {
                                paratype = _context.AppParameterTypes.Single(t => t.Id == final.ParameterTypeId);
                                typeCache.Add(final.ParameterTypeId, paratype);
                            }

                            if (paratype.Type != ParamTypes.None
                                && paratype.Type != ParamTypes.Picture)
                            {
                                final.AbsoluteSegmentId = $"{app.Id}_AS-default";
                                final.Offset = nullOffset;


                                int lengthInByte = 0;

                                if (paratype.Size < 8)
                                    lengthInByte = 1;
                                else
                                    lengthInByte = paratype.Size / 8;

                                byte[] toWrite = new byte[lengthInByte];

                                switch (paratype.Type)
                                {
                                    case ParamTypes.Enum:
                                    case ParamTypes.NumberUInt:
                                        UInt16 numb = UInt16.Parse(final.Value);
                                        byte[] converted = BitConverter.GetBytes(numb);
                                        converted = converted.Reverse().ToArray();
                                        for (int i1 = 0; i1 < converted.Length; i1++)
                                        {
                                            if (i1 > toWrite.Length - 1) break;
                                            toWrite[i1] = converted[i1];
                                        }
                                        break;
                                    case ParamTypes.NumberInt:
                                        int numb2 = int.Parse(final.Value);
                                        byte[] converted2 = BitConverter.GetBytes(numb2);
                                        converted2 = converted2.Reverse().ToArray();
                                        for (int i1 = 0; i1 < converted2.Length; i1++)
                                        {
                                            if (i1 > toWrite.Length - 1) break;
                                            toWrite[i1] = converted2[i1];
                                        }
                                        break;
                                    case ParamTypes.Text:
                                        byte[] converted3 = System.Text.Encoding.UTF8.GetBytes(final.Value);
                                        for (int i1 = 0; i1 < converted3.Length; i1++)
                                        {
                                            if (i1 > toWrite.Length - 1) break;
                                            toWrite[i1] = converted3[i1];
                                        }
                                        break;
                                    case ParamTypes.Float9:
                                        float numb4 = float.Parse(final.Value);
                                        byte[] converted4 = BitConverter.GetBytes(numb4);
                                        converted4 = converted4.Reverse().ToArray();
                                        for (int i1 = 0; i1 < converted4.Length; i1++)
                                        {
                                            if (i1 > toWrite.Length - 1) break;
                                            toWrite[i1] = converted4[i1];
                                        }
                                        break;
                                    case ParamTypes.None:
                                    case ParamTypes.Picture:
                                        break;
                                }

                                bytesToWrite.Add(toWrite);

                                nullOffset += lengthInByte;
                            }
                        }

                        _context.AppParameters.Add(final);
                    }
                    c1++;
                }


                if (!_context.AppAbsoluteSegments.Any(s => s.Id == $"{app.Id}_AS-default"))
                {
                    int dataSize = 0;

                    foreach (byte[] dataByte in bytesToWrite)
                    {
                        dataSize += dataByte.Length;
                    }

                    byte[] dataBytes = new byte[dataSize];
                    int dataOffset = 0;

                    foreach (byte[] dataByte in bytesToWrite)
                    {
                        dataByte.CopyTo(dataBytes, dataOffset);
                        dataOffset += dataByte.Length;
                    }

                    string data = Convert.ToBase64String(dataBytes);
                    dataBytes = null;


                    AppAbsoluteSegmentViewModel defaultSegment = new AppAbsoluteSegmentViewModel();
                    defaultSegment.Id = $"{app.Id}_AS-default";
                    defaultSegment.ApplicationId = app.Id;
                    defaultSegment.Size = dataSize;
                    defaultSegment.Data = data;

                    _context.AppAbsoluteSegments.Add(defaultSegment);
                }


                foreach (AppComObjectRef comref in ComObjectRefs.Values)
                {
                    if (!_context.AppComObjects.Any(r => r.Id == comref.Id))
                    {
                        AppComObject obj = new AppComObject();
                        obj.LoadComp(ComObjects[comref.RefId]);
                        obj.Id = comref.Id;
                        obj.ApplicationId = app.Id;
                        if (comref.FunctionText_DE != null) obj.FunctionText_DE = comref.FunctionText_DE;
                        if (comref.FunctionText_EN != null) obj.FunctionText_EN = comref.FunctionText_EN;
                        if (comref.Text_DE != null) obj.Text_DE = comref.Text_DE;
                        if (comref.Text_EN != null) obj.Text_EN = comref.Text_EN;
                        if (comref.Datapoint != -1) obj.Datapoint = comref.Datapoint;
                        if (comref.DatapointSub != -1) obj.DatapointSub = comref.DatapointSub;
                        if (comref.Size != -1) obj.Size = comref.Size;

                        if (comref.Flag_Communicate != null) obj.Flag_Communicate = (bool)comref.Flag_Communicate;
                        if (comref.Flag_Read != null) obj.Flag_Read = (bool)comref.Flag_Read;
                        if (comref.Flag_ReadOnInit != null) obj.Flag_ReadOnInit = (bool)comref.Flag_ReadOnInit;
                        if (comref.Flag_Transmit != null) obj.Flag_Transmit = (bool)comref.Flag_Transmit;
                        if (comref.Flag_Update != null) obj.Flag_Update = (bool)comref.Flag_Update;
                        if (comref.Flag_Write != null) obj.Flag_Write = (bool)comref.Flag_Write;

                        _context.AppComObjects.Add(obj);
                    }
                    c2++;
                    //if (Debug) await SendSocket("{ \"type\": \"test\", \"value\": \"c2: " + c2.ToString() + "\" }");
                }
            }
            catch (Exception e)
            {
                string exmsg = e.Message;
                //await SendSocket("{ \"type\": \"test\", \"value\": \"" + c.ToString() + "|" + c1.ToString() + "|" + c2.ToString() + " " + e.Message + "\" }");
            }


            await TranslateDynamic(app, transParamBock);

            try
            {
                ProgressAppChanged(maxcount);
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                string exmsg = e.Message;
                //await SendSocket("{ \"type\": \"test\", \"value\": \"message2: " + e.Message + "\" }");
            }

            await SaveHelper.GenerateDefaultComs(app.Id);
        }

        private async Task TranslateDynamic(ApplicationViewModel app, Dictionary<string, string> channels)
        {
            StorageFile file = null;
            XDocument dyn = null;
            try
            {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("Dynamic");
                file = await folder.GetFileAsync(app.Id + ".xml");
                dyn = XDocument.Parse(await FileIO.ReadTextAsync(file));
            } catch
            {

            }

            IEnumerable<XElement> elements = dyn.Descendants();

            foreach(string key in channels.Keys)
            {
                string translation = channels[key];

                XElement temp = elements.SingleOrDefault(e => e.Attribute("Id")?.Value == key);
                if (temp != null)
                    temp.Attribute("Text").Value = translation;
            }

            await FileIO.WriteTextAsync(file, dyn.ToString());
        }

        private bool GetAttributeAsBool(XElement ele, string attr)
        {
            string val = ele.Attribute(attr)?.Value;
            return (val == "1" || val == "true") ? true : false;
        }

        private int ConvertBusCurrent(string input)
        {
            if (input == null) return 10;

            if (input.ToLower().Contains("e+"))
            {
                float numb = float.Parse(input.Substring(0, 5).Replace('.', ','));
                int expo = int.Parse(input.Substring(input.IndexOf('+') + 1));
                if (expo == 0)
                    return int.Parse(numb.ToString());
                float res = numb * (10 * expo);
                return int.Parse(res.ToString());
            }

            try
            {
                return int.Parse(input);
            }
            catch
            {
                return 10;
            }
        }

        private List<string> CheckApplication(XmlReader reader)
        {
            List<string> errs = new List<string>();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                    continue;

                switch (reader.Name)
                {
                    case "Extension":
                        string plugdown = reader.GetAttribute("EtsDownloadPlugin");
                        string plugrequ = reader.GetAttribute("RequiresExternalSoftware");
                        string plugui = reader.GetAttribute("EtsUiPlugin");
                        string plughand = reader.GetAttribute("EtsDataHandler");
                        if (plugdown != null && !errs.Contains("EtsDownloadPlugin")) errs.Add("EtsDownloadPlugin");
                        if (plugui != null && !errs.Contains("EtsUiPlugin")) errs.Add("EtsUiPlugin");
                        if (plughand != null && !errs.Contains("EtsDataHandler")) errs.Add("EtsDataHandler");
                        if (plugrequ != null && (plugrequ == "1" || plugrequ == "true") && !errs.Contains("RequiresExternalSoftware")) errs.Add("RequiresExternalSoftware");
                        break;
                    case "ParameterCalculations":
                        if (!errs.Contains("ParameterCalculations")) errs.Add("ParameterCalculations");
                        reader.ReadOuterXml();
                        break;
                    case "Property":
                        //if (!errs.Contains("Property")) errs.Add("Property");
                        // ToDo: Check what it means
                        break;
                }
            }

            return errs;
        }

        private XName GetXName(string name)
        {
            return XName.Get(name, currentNamespace);
        }
    }
}
