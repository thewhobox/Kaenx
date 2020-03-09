﻿using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.Storage;
using Kaenx.DataContext.Catalog;
using Serilog;
using Kaenx.MVVM;
using System.Collections.ObjectModel;
using Kaenx.View.Controls;
using Kaenx.Classes.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System.Diagnostics;
using Windows.ApplicationModel.Resources;
using System.IO;

namespace Kaenx.Classes.Helper
{
    public class ImportHelper
    {
        public delegate void ProgressChangedHandler(int count);
        public event ProgressChangedHandler ProgressChanged;
        public event ProgressChangedHandler ProgressMaxChanged;
        public event ProgressChangedHandler ProgressAppChanged;
        public event ProgressChangedHandler ProgressAppMaxChanged;

        public delegate void ValueHandler(string value);
        public event ValueHandler OnError;
        public event ValueHandler OnDeviceChanged;
        public event ValueHandler OnStateChanged;

        public ImportDevices Imports;

        private string currentNamespace;
        private List<ManufacturerViewModel> tempManus;
        private CatalogContext _context = new CatalogContext();
        private ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Import");

        private string currentAppName;


        public static async Task TranslateXml(XElement xml, string selectedLang, ProgressChangedHandler maxH = null, ProgressChangedHandler currH = null)
        {
            if (selectedLang == null) return;



            if (!xml.Descendants(XName.Get("Language", xml.Name.NamespaceName)).Any(l => l.Attribute("Identifier").Value == selectedLang))
            {
                return;
            }

            XElement lang = xml.Descendants(XName.Get("Language", xml.Name.NamespaceName)).Single(l => l.Attribute("Identifier").Value == selectedLang);
            List<XElement> trans = lang.Descendants(XName.Get("TranslationElement", xml.Name.NamespaceName)).ToList();

            maxH?.Invoke(trans.Count);

            await Task.Delay(100);

            int c = 0;
            foreach(XElement translate in trans)
            {
                string id = translate.Attribute("RefId").Value;
                XElement ele = xml.Descendants().Single(e => e.Attribute("Id")?.Value == id && e.Name.LocalName != "TranslationElement");

                foreach(XElement transele in translate.Elements())
                {
                    if (ele.Attribute(transele.Attribute("AttributeName").Value) == null)
                    {
                        ele.Add(new XAttribute(transele.Attribute("AttributeName").Value, transele.Attribute("Text").Value));
                    }
                    else
                    {
                        ele.Attribute(transele.Attribute("AttributeName").Value).Value = transele.Attribute("Text").Value;
                    }
                }

                c++;
                if(c % 100 == 0)
                {
                    currH?.Invoke(c);
                    await Task.Delay(1);
                }
            }
        }




        public async Task StartImport(ObservableCollection<DeviceImportInfo> deviceList)
        {
            await UpdateKnxMaster();

            await ImportCatalog(deviceList);

            List<string> loadedIds = new List<string>();
            Dictionary<string, XElement> hards = new Dictionary<string, XElement>();

            foreach (DeviceImportInfo device in deviceList)
            {
                //ViewDevicesList.SelectedItem = device;

                ProgressChanged?.Invoke(0);
                await Task.Delay(1000);

                OnDeviceChanged?.Invoke(resourceLoader.GetString("StateHard"));
                Log.Information("---- Hardware wird importiert");


                string manu = device.Id.Substring(0, 6);
                XElement xml;

                if (hards.ContainsKey(manu))
                {
                    xml = hards[manu];
                } else
                {
                    try
                    {
                        ZipArchiveEntry entry = Imports.Archive.GetEntry(manu + "/Hardware.xml");
                        xml = XDocument.Load(entry.Open()).Root;
                        await ImportHelper.TranslateXml(xml, Imports.SelectedLanguage);
                        hards.Add(manu, xml);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Hardware Fehler!");
                        OnError?.Invoke(device.ApplicationId + ": " + e.Message);
                        device.Icon = Symbol.ReportHacked;
                        continue;
                    }
                }

                ImportHardware(xml, device);

                await Task.Delay(2000);
                ProgressChanged?.Invoke(1);

                if (!loadedIds.Contains(device.ApplicationId))
                {
                    OnDeviceChanged?.Invoke(resourceLoader.GetString("StateApp"));
                    Log.Information("---- Applikation wird importiert");
                    Log.Information(device.ApplicationId);

                    string manuId = device.ApplicationId.Substring(0, device.ApplicationId.IndexOf('_'));
                    ZipArchiveEntry appEntry = Imports.Archive.GetEntry(manuId + "/" + device.ApplicationId + ".xml");
                    List<string> errs = CheckApplication(XmlReader.Create(appEntry.Open()));

                    if (errs.Count > 0)
                    {
                        Log.Error("Check nicht bestanden! " + string.Join(",", errs));
                        OnError?.Invoke(device.ApplicationId + ": Die Applikation hat die Überprüfung nicht bestanden. (evtl. Plugin benötigt) " + string.Join(",", errs));
                        device.Icon = Symbol.ReportHacked;
                        continue;
                    }

                    await Task.Delay(1000);

                    try
                    {
                        XElement doc = XDocument.Load(appEntry.Open()).Root;
                        OnDeviceChanged?.Invoke("Übersetzen"); //TODO localisation
                        await TranslateXml(doc, Imports.SelectedLanguage, ProgressAppMaxChanged, ProgressAppChanged);
                        ProgressChanged?.Invoke(2);
                        XElement appXml = doc.Descendants(XName.Get("ApplicationProgram", doc.Name.Namespace.NamespaceName)).First();
                        OnDeviceChanged?.Invoke("Applikation importieren"); //TODO localisation
                        await ImportApplications(appXml, device);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Applikation Fehler!");
                        OnError?.Invoke(device.ApplicationId + ": " + e.Message);
                        device.Icon = Symbol.ReportHacked;
                        continue;
                    }
                    Log.Information("Import Applikationen abgeschlossen");
                    loadedIds.Add(device.ApplicationId);
                }
                ProgressChanged?.Invoke(5);


                OnDeviceChanged?.Invoke(resourceLoader.GetString("StateCheck"));
                //TODO check wirklich implementieren
                await Task.Delay(2000);
                ProgressChanged?.Invoke(6);

                device.Icon = Symbol.Like;
                await Task.Delay(2000);

            }


            Imports.Archive.Dispose();
            StorageFile file = await ApplicationData.Current.TemporaryFolder.GetFileAsync("temp.knxprod");
            await file.DeleteAsync();


            await Task.Delay(1000);
            OnStateChanged?.Invoke(resourceLoader.GetString("StateFin"));

            Log.Information("Import abgeschlossen");
        }


        public async Task UpdateKnxMaster()
        {
            OnStateChanged?.Invoke("KNX-Master Datei aktualisieren");
            ZipArchiveEntry entry = Imports.Archive.GetEntry("knx_master.xml");
            Log.Information("---- Integrierte KNX_Master wird überprüft");
            //await Task.Delay(2000);
            XElement manXML = XDocument.Load(entry.Open()).Root;
            StorageFile masterFile;

            try
            {
                masterFile = await ApplicationData.Current.LocalFolder.GetFileAsync("knx_master.xml");
            }
            catch (Exception e)
            {
                StorageFile defaultFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Data/knx_master.xml"));
                masterFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("knx_master.xml");
                await FileIO.WriteTextAsync(masterFile, await FileIO.ReadTextAsync(defaultFile));
                Log.Warning(e, "Es konnte keine KNX_Master Datei gefunden werden.");
            }


            XDocument masterXml;
            try
            {
                masterXml = XDocument.Load(await masterFile.OpenStreamForReadAsync());
            }
            catch (Exception e)
            {
                Imports.Archive.Dispose();
                OnStateChanged?.Invoke(resourceLoader.GetString("StateFin"));
                Log.Error(e, "KNX_Master konnte geöffnet werden.");
                OnError?.Invoke("Die KNX_Master Datei konnte nicht geöffnet werden.");
                return;
            }

            string versionO = masterXml.Root.Element(XName.Get("MasterData", masterXml.Root.Name.NamespaceName)).Attribute("Version").Value;
            string versionN = manXML.Element(XName.Get("MasterData", manXML.Name.NamespaceName)).Attribute("Version").Value;

            int versionNew, versionOld;

            try
            {
                versionNew = int.Parse(versionN);
                versionOld = int.Parse(versionO);

                bool newer = versionNew > versionOld;

                if (newer)
                {
                    await FileIO.WriteTextAsync(masterFile, manXML.ToString());
                    Log.Information("KNX_Master wurde aktualisiert");
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "KNX_Master konnte nicht aktualisiert werden.");
                OnError?.Invoke("KNX_Master konnte nicht aktualisiert werden.");
                return;
            }

            OnStateChanged?.Invoke(resourceLoader.GetString("StateManus"));
            UpdateManufacturers(manXML);
        }



        public async Task<bool> GetDeviceList(ImportDevices Import, bool changeLang = false)
        {
            string manName = "";

            foreach(ZipArchiveEntry entryTemp in Import.Archive.Entries)
            {
                if (entryTemp.FullName.StartsWith("M-"))
                    manName = entryTemp.FullName.Substring(0, 6);
            }

            if (manName == "")
                return false;

                
            ZipArchiveEntry entry = Import.Archive.GetEntry(manName + "/Catalog.xml");

            XDocument catXML = XDocument.Load(entry.Open());
            string ns = catXML.Root.Name.NamespaceName;
            List<XElement> langs = catXML.Descendants(XName.Get("Language", ns)).ToList();

            ObservableCollection<string> tempLangs = new ObservableCollection<string>();
            foreach (XElement lang in langs)
            {
                tempLangs.Add(lang.Attribute("Identifier").Value);
            }

            if (tempLangs.Count > 1)
            {
                ApplicationDataContainer container = ApplicationData.Current.LocalSettings;
                string defaultLang = container.Values["defaultLang"]?.ToString();

                if (!tempLangs.Contains(defaultLang) || changeLang)
                {
                    if (!changeLang && !string.IsNullOrEmpty(defaultLang) && tempLangs.Any(l => l.StartsWith(defaultLang.Split("-")[0])))
                    {

                    }
                    else
                    {
                        DiagLanguage diaglang = new DiagLanguage(tempLangs);
                        await diaglang.ShowAsync();
                        Import.SelectedLanguage = diaglang.SelectedLanguage;
                        await ImportHelper.TranslateXml(catXML.Root, diaglang.SelectedLanguage);
                    }
                }
                else
                {
                    Import.SelectedLanguage = defaultLang;
                    await ImportHelper.TranslateXml(catXML.Root, defaultLang);
                }
            }
            else if (tempLangs.Count == 1)
            {
                Import.SelectedLanguage = tempLangs[0];
                await ImportHelper.TranslateXml(catXML.Root, tempLangs[0]);
            }

            XElement catalogXML = catXML.Descendants(XName.Get("Catalog", ns)).ElementAt<XElement>(0);
            Import.DeviceList = CatalogHelper.GetDevicesFromCatalog(catalogXML);

            if (Import.DeviceList.Count == 0) return false ;

            if (Import.DeviceList.Count > 0)
            {
                foreach (Device device in Import.DeviceList)
                {
                    SlideListItemBase swipe = new SlideListItemBase();
                    swipe.LeftSymbol = Symbol.Accept;
                    swipe.LeftBackground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 128, 34));
                    device.SlideSettings = swipe;
                }

                if(Import.DeviceList.Count == 1)
                {
                    Import.DeviceList[0].SlideSettings.IsSelected = true;
                }
            }

            return true;
        }


        public async Task ImportCatalog(ObservableCollection<DeviceImportInfo> devicesList)
        {
            foreach(ZipArchiveEntry entry in Imports.Archive.Entries)
            {
                if (entry.Name != "Catalog.xml") continue;

                string manu = entry.FullName.Substring(0, 6);

                if (!_context.Sections.Any(s => s.Id == manu))
                {
                    CatalogViewModel section = null;
                    ManufacturerViewModel man = tempManus.Find(e => e.Id == manu);
                    section = new CatalogViewModel();
                    section.Id = manu;
                    section.Name = man.Name;
                    section.ParentId = "main";
                    _context.Sections.Add(section);
                    _context.SaveChanges();
                }

                XElement catalog = XDocument.Load(entry.Open()).Root;
                currentNamespace = catalog.Name.NamespaceName;
                catalog = catalog.Element(GetXName("ManufacturerData")).Element(GetXName("Manufacturer")).Element(GetXName("Catalog"));

                await GetSubItems(catalog, manu, devicesList);

            }

            _context.SaveChanges();
        }


        private async Task<bool> GetSubItems(XElement xparent, string parentSub, ObservableCollection<DeviceImportInfo> devicesList)
        {
            bool flagHasItem = false;

            foreach (XElement xele in xparent.Elements())
            {
                if(xele.Name.LocalName == "CatalogItem")
                {
                    if(devicesList.Any(d => d.Id == xele.Attribute("Id").Value))
                    {
                        DeviceImportInfo device = devicesList.Single(d => d.Id == xele.Attribute("Id").Value);
                        device.CatalogId = parentSub;
                        device.HardwareRefId = xele.Attribute("Hardware2ProgramRefId").Value;
                        device.ProductRefId = xele.Attribute("ProductRefId").Value;
                        flagHasItem = true;
                    }
                } else
                {
                    bool hasItem = await GetSubItems(xele, xele.Attribute("Id").Value, devicesList);
                    if (!_context.Sections.Any(s => s.Id == xele.Attribute("Id").Value))
                    {
                        CatalogViewModel section = new CatalogViewModel();
                        section.Id = xele.Attribute("Id").Value;
                        section.Name = xele.Attribute("Name")?.Value;
                        section.ParentId = parentSub;
                        _context.Sections.Add(section);
                    }
                    if (hasItem) flagHasItem = true;
                }
            }
            return flagHasItem;
        }



        public void ImportHardware(XElement hardXML, DeviceImportInfo deviceInfo)
        {
            currentNamespace = hardXML.Name.NamespaceName;
            DeviceViewModel device;
            bool existed = _context.Devices.Any(d => d.Id == deviceInfo.Id);

            if (existed)
                device = _context.Devices.Single(d => d.Id == deviceInfo.Id);
            else
                device = new DeviceViewModel() { Id = deviceInfo.Id };


            XElement productXml = hardXML.Descendants(GetXName("Product")).Single(p => p.Attribute("Id").Value == deviceInfo.ProductRefId);
            XElement hardwareXml = productXml.Parent.Parent;
            XElement hardware2ProgXml = hardXML.Descendants(GetXName("Hardware2Program")).Single(p => p.Attribute("Id").Value == deviceInfo.HardwareRefId);

            device.ManufacturerId = hardwareXml.Attribute("Id").Value.Substring(0,6);
            device.Name = deviceInfo.Name;
            device.VisibleDescription = deviceInfo.Description;
            device.OrderNumber = productXml.Attribute("OrderNumber").Value;
            device.BusCurrent = ConvertBusCurrent(hardwareXml.Attribute("BusCurrent")?.Value);
            device.IsRailMounted = GetAttributeAsBool(productXml, "IsRailMounted");
            device.IsPowerSupply = GetAttributeAsBool(hardwareXml, "IsPowerSupply");
            device.IsCoupler = GetAttributeAsBool(hardwareXml, "IsCoupler");
            device.HasApplicationProgram = GetAttributeAsBool(hardwareXml, "HasApplicationProgram");
            device.HasIndividualAddress = GetAttributeAsBool(hardwareXml, "HasIndividualAddress");
            device.HardwareId = hardwareXml.Attribute("Id").Value;
            device.CatalogId = deviceInfo.CatalogId;

            if (existed)
                _context.Devices.Update(device);
            else
                _context.Devices.Add(device);


            deviceInfo.ApplicationId = hardware2ProgXml.Element(GetXName("ApplicationProgramRef")).Attribute("RefId").Value;
            deviceInfo.HardwareId = device.HardwareId;

            if (!_context.Hardware2App.Any(h => h.HardwareId == device.HardwareId && h.ApplicationId == deviceInfo.ApplicationId))
                _context.Hardware2App.Add(new Hardware2AppModel { HardwareId = device.HardwareId, ApplicationId = deviceInfo.ApplicationId });

            _context.SaveChanges();
        }

        public async Task ImportApplications(XElement appXml, DeviceImportInfo device)
        {
            currentNamespace = appXml.Name.NamespaceName;
            Log.Information("---- Applikation wird importiert");
            Log.Information(device.ApplicationId);

            ApplicationViewModel app;
            if (_context.Applications.Any(a => a.Id == device.ApplicationId))
                app = _context.Applications.Single(a => a.Id == device.ApplicationId);
            else
                app = new ApplicationViewModel() { Id = device.ApplicationId };


            app.Number = int.Parse(appXml.Attribute("ApplicationNumber").Value);
            app.Version = int.Parse(appXml.Attribute("ApplicationVersion").Value);
            app.Mask = appXml.Attribute("MaskVersion").Value;
            app.Name = appXml.Attribute("Name").Value;

            Hardware2AppModel hard2App = _context.Hardware2App.Single(h => h.ApplicationId == app.Id && h.HardwareId == device.HardwareId);
            hard2App.Name = app.Name;
            hard2App.Version = app.Version;
            hard2App.Number = app.Number;
            _context.Hardware2App.Update(hard2App);
            _context.SaveChanges();


            int rest = app.Version % 16;
            int full = (app.Version - rest) / 16;

            currentAppName = app.Name + " " + "V" + full.ToString() + "." + rest.ToString();
            OnDeviceChanged(currentAppName + " - Check Application");
            await Task.Delay(10);

            Log.Information(app.Name + " V" + full.ToString() + "." + rest.ToString());
                
            int cmax = 0;
            cmax += appXml.Descendants(GetXName("ParameterType")).Count();
            cmax += appXml.Descendants(GetXName("Parameter")).Count();
            cmax += appXml.Descendants(GetXName("Union")).Count();
            cmax += appXml.Descendants(GetXName("ParameterRef")).Count();
            cmax += appXml.Descendants(GetXName("ComObject")).Count();
            cmax += appXml.Descendants(GetXName("ComObjectRef")).Count();

            Log.Information("Berechnete Anzahl an Elementen: " + cmax);
            ProgressAppMaxChanged(cmax);


            Log.Information("Applikation wird nun eingelesen...");
            OnDeviceChanged(currentAppName + " - Einlesen");
            await Task.Delay(10);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            await ReadApplication(appXml, app, cmax);
            sw.Stop();

            Log.Information("Applikation wurde eingelesen...");
        }



        public void UpdateManufacturers(XElement manXML)
        {
            tempManus = new List<ManufacturerViewModel>();
            currentNamespace = manXML.Attribute("xmlns").Value;
            XElement mans = manXML.Element(GetXName("MasterData")).Element(GetXName("Manufacturers"));

            foreach (XElement manEle in mans.Elements())
            {
                ManufacturerViewModel man = new ManufacturerViewModel();
                man.Id = manEle.Attribute("Id").Value;
                man.Name = manEle.Attribute("Name").Value;
                man.KnxManufacturerId = int.Parse(manEle.Attribute("KnxManufacturerId").Value);

                tempManus.Add(man);
            }
        }

        //TODO really implement a check. dont forget to clean catalog sections
        public async Task CheckParams()
        {
            Dictionary<string, string> App2Hardware = new Dictionary<string, string>();
            ProgressMaxChanged(App2Hardware.Count);
            int count = 0;

            foreach (string appId in App2Hardware.Keys)
            {
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
            List<string> DeviceIds = new List<string>();
            ProgressMaxChanged(DeviceIds.Count);
            int count = 0;

            foreach (string deviceId in DeviceIds)
            {
                DeviceViewModel device = _context.Devices.FirstOrDefault(d => d.Id == deviceId);

                if (device == null)
                {
                    Log.Warning("DeviceId wurde nicht gefunden: " + deviceId);
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
                    {
                        Log.Warning("Gerät wurde erfolgreich hinzugefügt: " + device.Name);
                        AddedDevices.Add(device);
                    }
                    else
                    {
                        Log.Warning("Das Gerät hat keine Applikation: " + device.Name);
                        _context.Devices.Remove(device);
                    }
                }
                else
                {
                    Log.Warning("Zu dem Gerät wurde keine Hardware gefunden: " + device.Name);
                    _context.Devices.Remove(device);
                }

                count++;
                ProgressChanged(count);
                await Task.Delay(500);
            }

            try { 
                _context.SaveChanges();
            }
            catch(Exception e)
            {
                Log.Error(e, "CheckDevices Fehler!");
            }


            return AddedDevices;
        }


        private async Task ReadApplication(XElement doc, ApplicationViewModel app, int maxcount)
        {
            try
            {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Dynamic", CreationCollisionOption.OpenIfExists);
            } catch(Exception e) {
                Log.Error(e, "Applikation Dynamic Ordner Fehler!");
            }


            //_context.ChangeTracker.AutoDetectChangesEnabled = false;

            List<AppError> Errors = new List<AppError>();
            Dictionary<string, AppParameter> Params = new Dictionary<string, AppParameter>();
            Dictionary<string, AppComObject> ComObjects = new Dictionary<string, AppComObject>();
            currentNamespace = doc.Name.NamespaceName;
            int position = 0;
            int iterationToWait = 50;

            List<XElement> tempList;


            tempList = doc.Descendants(GetXName("Baggage")).ToList();
            if(tempList.Count != 0)
            {
                Log.Information("Baggages werden gespeichert");
                ZipArchiveEntry entryBags = Imports.Archive.GetEntry(app.Id.Substring(0, 6) + "/Baggages.xml");
                List<XElement> baggs = XDocument.Load(entryBags.Open()).Root.Descendants(GetXName("Baggage")).ToList();
                StorageFolder appData = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Baggages", CreationCollisionOption.OpenIfExists);

                foreach(XElement xbag in tempList)
                {
                    XElement ebag = baggs.Single(b => b.Attribute("Id").Value == xbag.Attribute("RefId").Value);
                    bool flagSaveBag= true;
                    string bagId = ebag.Attribute("Id").Value;
                    DateTime time = DateTime.Parse(ebag.Element(GetXName("FileInfo")).Attribute("TimeInfo").Value);
                    StorageFile file = null;

                    try
                    {
                        file = await appData.GetFileAsync(bagId);
                    } catch { }

                    if(file != null && time <= file.DateCreated)
                    {
                        flagSaveBag = false;
                        //TODO check if file is newer
                    } else
                    {
                        file = await appData.CreateFileAsync(bagId);
                        File.SetCreationTime(file.Path, time);
                    }

                    if (flagSaveBag)
                    {
                        ZipArchiveEntry entryBag = Imports.Archive.GetEntry(app.Id.Substring(0, 6) + "/Baggages/" + ebag.Attribute("Name").Value);
                        Stream sread = entryBag.Open();
                        Stream swrite = await file.OpenStreamForWriteAsync();
                        sread.CopyTo(swrite);
                        sread.Flush();
                        sread.Close();
                        swrite.Close();
                    }
                }
            }
            else
                Log.Information("Keine Baggages vorhanden");


            Log.Information("Parameter Typen werden eingelesen");
            tempList = doc.Descendants(GetXName("ParameterType")).ToList();
            foreach(XElement type in tempList)
            {
                bool existed = _context.AppParameterTypes.Any(p => p.Id == type.Attribute("Id").Value);
                XElement child = type.Elements().ElementAt(0);
                AppParameterTypeViewModel paramt;

                if (existed)
                    paramt = _context.AppParameterTypes.Single(p => p.Id == type.Attribute("Id").Value);
                else
                    paramt = new AppParameterTypeViewModel() { Id = type.Attribute("Id").Value };


                switch (child.Name.LocalName)
                {
                    case "TypeNumber":
                        switch (child.Attribute("Type").Value)
                        {
                            case "signedInt":
                                paramt.Type = ParamTypes.NumberInt;
                                break;
                            case "unsignedInt":
                                paramt.Type = ParamTypes.NumberUInt;
                                break;
                            default:
                                Errors.Add(new AppError(app.Id, "ParameterType", child.Name.LocalName, child.Attribute("Type").Value, "Unbekannter Nummerntype"));
                                Log.Error("Unbekannter Nummerntyp: " + child.Name.LocalName + " - " + child.Attribute("Type").Value);
                                break;
                        }
                        paramt.Size = int.Parse(child.Attribute("SizeInBit").Value);
                        paramt.Tag1 = child.Attribute("minInclusive").Value;
                        paramt.Tag2 = child.Attribute("maxInclusive").Value;
                        break;
                    case "TypeRestriction":
                        paramt.Type = ParamTypes.Enum;
                        paramt.Size = int.Parse(child.Attribute("SizeInBit").Value);
                        string _base = child.Attribute("Base").Value;
                        int cenu = 0;
                        foreach (XElement en in child.Elements())
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
                        paramt.Size = int.Parse(child.Attribute("SizeInBit").Value);
                        break;
                    case "TypeFloat":
                        switch (child.Attribute("Encoding").Value)
                        {
                            case "DPT 9":
                                paramt.Type = ParamTypes.Float9;
                                break;
                            default:
                                Errors.Add(new AppError(app.Id, "ParameterType", "TypeFloat", child.Attribute("Encoding").Value, "Unbekannter Floattype"));
                                break;
                        }
                        paramt.Tag1 = child.Attribute("minInclusive").Value;
                        paramt.Tag2 = child.Attribute("maxInclusive").Value;
                        break;
                    case "TypePicture":
                        paramt.Type = ParamTypes.Picture;
                        paramt.Tag1 = child.Attribute("RefId").Value;
                        break;
                    case "TypeIPAddress":
                        paramt.Type = ParamTypes.IpAdress;
                        paramt.Tag1 = child.Attribute("AddressType").Value;
                        paramt.Size = 4 * 8;
                        break;
                    case "TypeNone":
                        paramt.Type = ParamTypes.None;
                        break;
                    default:
                        Errors.Add(new AppError(app.Id, "ParameterType", child.Name.LocalName, "", "Unbekannter Parametertype"));
                        Log.Error("Unbekannter Parametertyp: " + child.Name.LocalName);
                        break;
                }

                if (existed)
                    _context.AppParameterTypes.Update(paramt);
                else
                    _context.AppParameterTypes.Add(paramt);
                position++;
                ProgressAppChanged(position);
                if (position % iterationToWait == 0) await Task.Delay(1);
            }


            Log.Information("Parameter werden eingelesen");
            tempList = doc.Descendants(GetXName("Parameter")).ToList();
            foreach(XElement para in tempList)
            {
                AppParameter param = new AppParameter();
                param.Id = para.Attribute("Id").Value;
                param.Text = para.Attribute("Text").Value;
                param.ParameterTypeId = para.Attribute("ParameterType").Value;
                param.Value = para.Attribute("Value")?.Value;
                switch (para.Attribute("Access")?.Value)
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

                if (para.Elements(GetXName("Memory")).Count() > 0)
                {
                    XElement mem = para.Elements(GetXName("Memory")).ElementAt(0);
                    param.SegmentId = mem.Attribute("CodeSegment").Value;
                    param.Offset = int.Parse(mem.Attribute("Offset").Value);
                    param.OffsetBit = int.Parse(mem.Attribute("BitOffset").Value);
                }
                Params.Add(param.Id, param);
                position++;
                ProgressAppChanged(position);
                if (position % iterationToWait == 0) await Task.Delay(1);
            }


            Log.Information("Unions werden eingelesen");
            tempList = doc.Descendants(GetXName("Union")).ToList();
            foreach (XElement union in tempList)
            {
                //TODO also check for property for parameter
                string t1 = null;
                int t2 = 0;
                int t3 = 0;
                SegmentTypes segType = SegmentTypes.None;
                XElement mem = union.Element(GetXName("Memory"));
                if (mem != null)
                {
                    t1 = mem.Attribute("CodeSegment").Value;
                    t2 = int.Parse(mem.Attribute("Offset").Value);
                    t3 = int.Parse(mem.Attribute("BitOffset").Value);
                    segType = SegmentTypes.Memory;
                } else
                {
                    mem = union.Element(GetXName("Property"));
                    if (mem != null)
                    {
                        //ObjectIndex="6" PropertyId="57" Offset="17" BitOffset="0"

                        t1 = "Property:" + mem.Attribute("ObjectIndex").Value + ":" + mem.Attribute("PropertyId").Value;
                        t2 = int.Parse(mem.Attribute("Offset").Value);
                        t3 = int.Parse(mem.Attribute("BitOffset").Value);
                        segType = SegmentTypes.Property;
                    } else
                    {
                        Log.Error("Union hat keinen bekannten Speicher! " + union.ToString());
                    }
                }
                
                int t4 = int.Parse(union.Attribute("SizeInBit").Value);
                mem = null;
                foreach (XElement para in union.Elements(GetXName("Parameter")))
                {
                    AppParameter param = Params[para.Attribute("Id").Value];
                    param.SegmentId = t1;
                    int off = int.Parse(para.Attribute("Offset").Value);
                    int offb = int.Parse(para.Attribute("BitOffset").Value);
                    param.Offset = t2 + off;
                    param.OffsetBit = t3 + offb;
                    param.SegmentType = segType;
                }
                position++;
                ProgressAppChanged(position);
                if (position % iterationToWait == 0) await Task.Delay(1);
            }


            Log.Information("ParameterRefs werden eingelesen");
            tempList = doc.Descendants(GetXName("ParameterRef")).ToList();
            foreach (XElement pref in tempList)
            {
                AppParameter old = Params[pref.Attribute("RefId").Value];
                bool existed = _context.AppParameters.Any(p => p.Id == pref.Attribute("Id").Value);
                AppParameter final;

                if (existed)
                    final = _context.AppParameters.Single(p => p.Id == pref.Attribute("Id").Value);
                else
                {
                    final = new AppParameter();
                    final.LoadPara(old);
                    final.Id = pref.Attribute("Id").Value;
                }
                final.ApplicationId = app.Id;

                string text = pref.Attribute("Text")?.Value;
                final.Text = text == null ? old.Text : text;

                string value = pref.Attribute("Value")?.Value;
                final.Value = value == null ? old.Value : value;

                AccessType access = (pref.Attribute("Access")) == null ? AccessType.Null : ((pref.Attribute("Access").Value == "None") ? AccessType.None : AccessType.Full);
                final.Access = access == AccessType.Null ? old.Access : access;

                if (existed)
                    _context.AppParameters.Update(final);
                else
                    _context.AppParameters.Add(final);

                position++;
                ProgressAppChanged(position);
                if (position % iterationToWait == 0) await Task.Delay(1);
            }

            XElement table = null;
            Log.Information("ComObjectTable wird eingelesen");
            if(doc.Descendants(GetXName("ComObjectTable")).Count() != 0)
            {
                table = doc.Descendants(GetXName("ComObjectTable")).ElementAt(0);

                if (table.Attribute("CodeSegment") != null)
                {
                    table = doc.Descendants(GetXName("ComObjectTable")).ElementAt(0);
                    app.Table_Object = table.Attribute("CodeSegment").Value;
                    int offsetObject;
                    int.TryParse(table.Attribute("Offset").Value, out offsetObject);
                    app.Table_Object_Offset = offsetObject;
                }
                else
                {
                    Log.Information("Für ComObjectTable kein CodeSegment gefunden");
                }

                Log.Information("ComObjects werden eingelesen");
                foreach (XElement com in table.Elements())
                {
                    AppComObject cobj = new AppComObject();
                    cobj.Id = com.Attribute("Id").Value;
                    cobj.SetText(com.Attribute("Text")?.Value);
                    cobj.SetFunction(com.Attribute("FunctionText")?.Value);
                    cobj.SetSize(com.Attribute("ObjectSize")?.Value);
                    cobj.SetDatapoint(com.Attribute("DatapointType")?.Value);
                    cobj.Number = int.Parse(com.Attribute("Number").Value);

                    cobj.Flag_Communicate = com.Attribute("CommunicationFlag")?.Value == "Enabled";
                    cobj.Flag_Read = com.Attribute("ReadFlag")?.Value == "Enabled";
                    cobj.Flag_ReadOnInit = com.Attribute("ReadOnInitFlag")?.Value == "Enabled";
                    cobj.Flag_Transmit = com.Attribute("TransmitFlag")?.Value == "Enabled";
                    cobj.Flag_Update = com.Attribute("UpdateFlag")?.Value == "Enabled";
                    cobj.Flag_Write = com.Attribute("WriteFlag")?.Value == "Enabled";

                    ComObjects.Add(cobj.Id, cobj);
                    position++;
                    ProgressAppChanged(position);
                    if (position % iterationToWait == 0) await Task.Delay(1);
                }
            }
            
            //TODO zusammenbringen mit ComObject auslesen
            Log.Information("ComObjectRefs werden eingelesen");
            tempList = doc.Descendants(GetXName("ComObjectRef")).ToList();
            foreach (XElement cref in tempList)
            {
                AppComObjectRef cobjr = new AppComObjectRef();
                cobjr.Id = cref.Attribute("Id").Value;
                cobjr.RefId = cref.Attribute("RefId").Value;

                //TODO implement auto translate
                cobjr.SetText(cref.Attribute("Text")?.Value);
                cobjr.SetFunction(cref.Attribute("FunctionText")?.Value);
                cobjr.SetSize(cref.Attribute("ObjectSize")?.Value);
                cobjr.SetDatapoint(cref.Attribute("DatapointType")?.Value);
                cobjr.Number = cref.Attribute("Number") == null ? -1 : int.Parse(cref.Attribute("Number").Value);

                if (cref.Attribute("CommunicationFlag")?.Value == "Enabled")
                    cobjr.Flag_Communicate = true;
                if (cref.Attribute("CommunicationFlag")?.Value == "Disabled")
                    cobjr.Flag_Communicate = false;
                if (cref.Attribute("ReadFlag")?.Value == "Enabled")
                    cobjr.Flag_Read = true;
                if (cref.Attribute("ReadFlag")?.Value == "Disabled")
                    cobjr.Flag_Read = false;
                if (cref.Attribute("ReadOnInitFlag")?.Value == "Enabled")
                    cobjr.Flag_ReadOnInit = true;
                if (cref.Attribute("ReadOnInitFlag")?.Value == "Disabled")
                    cobjr.Flag_ReadOnInit = false;
                if (cref.Attribute("TransmitFlag")?.Value == "Enabled")
                    cobjr.Flag_Transmit = true;
                if (cref.Attribute("TransmitFlag")?.Value == "Disabled")
                    cobjr.Flag_Transmit = false;
                if (cref.Attribute("UpdateFlag")?.Value == "Enabled")
                    cobjr.Flag_Update = true;
                if (cref.Attribute("UpdateFlag")?.Value == "Disabled")
                    cobjr.Flag_Update = false;
                if (cref.Attribute("WriteFlag")?.Value == "Enabled")
                    cobjr.Flag_Write = true;
                if (cref.Attribute("WriteFlag")?.Value == "Disabled")
                    cobjr.Flag_Write = false;


                AppComObject old = ComObjects[cobjr.RefId];
                bool existed = _context.AppComObjects.Any(c => c.Id == cref.Attribute("Id").Value);
                AppComObject obj;

                if (existed)
                    obj = _context.AppComObjects.Single(c => c.Id == cref.Attribute("Id").Value);
                else
                {
                    obj = new AppComObject();
                    obj.LoadComp(old);
                    obj.Id = cobjr.Id;
                }

                obj.ApplicationId = app.Id;
                if (cobjr.FunctionText_DE != null) obj.FunctionText_DE = cobjr.FunctionText_DE;
                if (cobjr.FunctionText_EN != null) obj.FunctionText_EN = cobjr.FunctionText_EN;
                if (cobjr.Text_DE != null) obj.Text_DE = cobjr.Text_DE;
                if (cobjr.Text_EN != null) obj.Text_EN = cobjr.Text_EN;
                if (cobjr.Datapoint != -1) obj.Datapoint = cobjr.Datapoint;
                if (cobjr.DatapointSub != -1) obj.DatapointSub = cobjr.DatapointSub;
                if (cobjr.Size != -1) obj.Size = cobjr.Size;

                if (cobjr.Flag_Communicate != null) obj.Flag_Communicate = (bool)cobjr.Flag_Communicate;
                if (cobjr.Flag_Read != null) obj.Flag_Read = (bool)cobjr.Flag_Read;
                if (cobjr.Flag_ReadOnInit != null) obj.Flag_ReadOnInit = (bool)cobjr.Flag_ReadOnInit;
                if (cobjr.Flag_Transmit != null) obj.Flag_Transmit = (bool)cobjr.Flag_Transmit;
                if (cobjr.Flag_Update != null) obj.Flag_Update = (bool)cobjr.Flag_Update;
                if (cobjr.Flag_Write != null) obj.Flag_Write = (bool)cobjr.Flag_Write;

                if (existed)
                    _context.AppComObjects.Update(obj);
                else
                    _context.AppComObjects.Add(obj);

                position++;
                ProgressAppChanged(position);
                if (position % iterationToWait == 0) await Task.Delay(1);
            }


            if (doc.Descendants(GetXName("AddressTable")).Count() != 0)
            {
                Log.Information("AddressTable wird eingelesen");
                table = doc.Descendants(GetXName("AddressTable")).ElementAt(0);
                if(table.Attribute("CodeSegment") != null)
                {
                    app.Table_Group = table.Attribute("CodeSegment").Value;
                    int offsetGroup;
                    int.TryParse(table.Attribute("Offset")?.Value, out offsetGroup);
                    app.Table_Group_Offset = offsetGroup;
                }
                else
                    Log.Information("Für AddressTable wurde kein CodeSegment gefunden.");

                int maxEntries;
                int.TryParse(table.Attribute("MaxEntries")?.Value, out maxEntries);
                app.Table_Group_Max = maxEntries;
            }
            else
                Log.Information("Kein AddressTable vorhanden");


            if (doc.Descendants(GetXName("AssociationTable")).Count() != 0)
            {
                Log.Information("AssociationTable wird eingelesen");
                table = doc.Descendants(GetXName("AssociationTable")).ElementAt(0);
                if (table.Attribute("CodeSegment") != null)
                {
                    app.Table_Assosiations = table.Attribute("CodeSegment").Value;
                    int offsetAssoc;
                    int.TryParse(table.Attribute("Offset")?.Value, out offsetAssoc);
                    app.Table_Assosiations_Offset = offsetAssoc;
                }
                else
                    Log.Information("Für AssociationTable wurde kein CodeSegment gefunden.");

                int maxEntriesA;
                int.TryParse(table.Attribute("MaxEntries")?.Value, out maxEntriesA);
                app.Table_Assosiations_Max = maxEntriesA;
            }
            else
                Log.Information("Kein AssociationTable vorhanden");


            if (doc.Descendants(GetXName("Dynamic")).Count() != 0)
            {
                Log.Information("Dynamic wird gespeichert");
                table = doc.Descendants(GetXName("Dynamic")).ElementAt(0);

                try
                {
                    StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("Dynamic");
                    StorageFile file = await folder.CreateFileAsync(app.Id + ".xml", CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(file, table.ToString());
                }
                catch (Exception e)
                {
                    Log.Error(e, "Dynamic konnte nicht gespeichert werden!");
                    Errors.Add(new AppError("dynamic1", "create", "file", "none", e.Message));
                }
            }
            else
                Log.Information("Kein Dynamic vorhanden");


            if (doc.Descendants(GetXName("Code")).Count() != 0)
            {
                Log.Information("Code Segmente werden eingelesen");
                table = doc.Descendants(GetXName("Code")).ElementAt(0);

                foreach(XElement seg in table.Elements())
                {
                    switch (seg.Name.LocalName)
                    {
                        case "AbsoluteSegment":
                            AppAbsoluteSegmentViewModel aas;
                            bool existed = _context.AppAbsoluteSegments.Any(a => a.Id == seg.Attribute("Id").Value);

                            if (existed)
                                aas = _context.AppAbsoluteSegments.Single(a => a.Id == seg.Attribute("Id").Value);
                            else
                                aas = new AppAbsoluteSegmentViewModel() { Id = seg.Attribute("Id").Value };
                            
                            aas.ApplicationId = app.Id;
                            aas.Address = int.Parse(seg.Attribute("Address").Value);
                            aas.Size = int.Parse(seg.Attribute("Size").Value);
                            aas.Data = seg.Element(GetXName("Data"))?.Value;
                            aas.Mask = seg.Element(GetXName("Mask"))?.Value;

                            if (existed)
                                _context.AppAbsoluteSegments.Update(aas);
                            else
                                _context.AppAbsoluteSegments.Add(aas);
                            break;
                        default:
                            Log.Error("Unbekanntes Segment!", seg.ToString());
                            break;
                    }
                }
            }
            else
                Log.Information("Keine Code Segmente vorhanden");

            Log.Information("Applikation in Datenbank speichern");
            if (!_context.Applications.Any(a => a.Id == app.Id))
                _context.Applications.Add(app);
            else
                _context.Applications.Update(app);

            if (Errors.Count != 0)
            {
                string err = "";
                foreach (AppError ae in Errors)
                    err += ae.Message + "\r\n";
                Log.Error("Es traten " + Errors.Count.ToString() + " Fehler auf...");
                OnError("Es traten " + Errors.Count.ToString() + " Fehler auf!");
            }

            try
            {
                //_context.ChangeTracker.AutoDetectChangesEnabled = true;
                //_context.ChangeTracker.DetectChanges();
                _context.SaveChanges();
                ProgressAppChanged(maxcount);
            }
            catch (Exception e)
            {
                Log.Error(e, "Applikation speichern Fehler!");
            }

            Log.Information("LoadProcedures werden gespeichert");
            XElement prods = doc.Descendants(GetXName("LoadProcedures"))?.ElementAt(0);
            if(prods != null)
            {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("Dynamic");
                StorageFile file = await folder.CreateFileAsync(app.Id + "-LP.xml", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, prods.ToString());
            }


            Log.Information("Standard ComObjects werden generiert");
            ProgressChanged?.Invoke(3);
            OnDeviceChanged(currentAppName + " - Generiere ComObjects");
            await SaveHelper.GenerateDefaultComs(app.Id);
            Log.Information("Standard Props werden generiert");
            ProgressChanged?.Invoke(4);
            OnDeviceChanged(currentAppName + " - Generiere Props");
            await SaveHelper.GenerateVisibleProps(app.Id);
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

        public List<string> CheckApplication(XmlReader reader)
        {
            List<string> errs = new List<string>();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                    continue;

                switch (reader.Name)
                {
                    case "Extension":
                        Log.Warning("Applikation enthält Extension: " + reader.Name, reader.ReadOuterXml());
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
                        Log.Warning("Applikation enthält Berechnungen: " + reader.Name, reader.ReadOuterXml());
                        if (!errs.Contains("ParameterCalculations")) errs.Add("ParameterCalculations");
                        reader.ReadOuterXml();
                        break;
                    //case "Property":
                    //    Log.Warning("Unbekannte Property! ", reader.ReadOuterXml());
                    //    //if (!errs.Contains("Property")) errs.Add("Property");
                    //    // ToDo: Check what it means
                    //    break;
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
