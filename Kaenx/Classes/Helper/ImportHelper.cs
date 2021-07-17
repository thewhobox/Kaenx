using System;
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
using System.Text;
using System.Text.RegularExpressions;
using Kaenx.View;
using Kaenx.DataContext.Import;

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
        public event ValueHandler OnWarning;
        public event ValueHandler OnDeviceChanged;
        public event ValueHandler OnStateChanged;

        public ImportDevices Imports = null;

        private string currentNamespace;
        private CatalogContext _context = new CatalogContext();
        private ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Import");
        private Dictionary<int, int> app2hard = new Dictionary<int, int>();

        private string currentAppName;

        public static void TranslateXml(XElement xml, string selectedLang)
        {
            if (selectedLang == null) return;



            if (!xml.Descendants(XName.Get("Language", xml.Name.NamespaceName)).Any(l => l.Attribute("Identifier").Value == selectedLang))
            {
                return;
            }


            Dictionary<string, Dictionary<string, string>> transl = new Dictionary<string, Dictionary<string, string>>();


            XElement lang = xml.Descendants(XName.Get("Language", xml.Name.NamespaceName)).Single(l => l.Attribute("Identifier").Value == selectedLang);
            List<XElement> trans = lang.Descendants(XName.Get("TranslationElement", xml.Name.NamespaceName)).ToList();

            foreach(XElement translate in trans)
            {
                string id = translate.Attribute("RefId").Value;

                Dictionary<string, string> translations = new Dictionary<string, string>();

                foreach(XElement transele in translate.Elements())
                {
                    translations.Add(transele.Attribute("AttributeName").Value, transele.Attribute("Text").Value);
                }

                transl.Add(id, translations);
            }


            foreach(XElement ele in xml.Descendants())
            {
                if (ele.Attribute("Id") == null || !transl.ContainsKey(ele.Attribute("Id").Value)) continue;
                string eleId = ele.Attribute("Id").Value;


                foreach (string attr in transl[eleId].Keys)
                {
                    if(ele.Attribute(attr) != null)
                    {
                        ele.Attribute(attr).Value = transl[eleId][attr];
                    } else
                    {
                        ele.Add(new XAttribute(XName.Get(attr), transl[eleId][attr]));
                    }
                }

            }
        }




        public async Task StartImport(ObservableCollection<DeviceImportInfo> deviceList)
        {
            await UpdateKnxMaster();

            await ImportCatalog(deviceList);

            List<Reference> loadedIds = new List<Reference>();
            Dictionary<string, XElement> hards = new Dictionary<string, XElement>();

            foreach (DeviceImportInfo device in deviceList)
            {
                //ViewDevicesList.SelectedItem = device;
                ProgressChanged?.Invoke(0);

                _= App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => device.Icon = Symbol.Sync);
                

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
                        ImportHelper.TranslateXml(xml, Imports.SelectedLanguage);
                        hards.Add(manu, xml);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Hardware Fehler!");
                        OnError?.Invoke(device.ApplicationRef.Additional + ": " + e.Message + Environment.NewLine + e.StackTrace);
                        device.Icon = Symbol.ReportHacked;
                        continue;
                    }
                }

                ImportHardware(xml, device);

                await Task.Delay(10);
                ProgressChanged?.Invoke(1);

                if (device.ApplicationRef != null)
                {
                    if (!loadedIds.Any(l => l.Equals(device.ApplicationRef)))
                    {
                        OnDeviceChanged?.Invoke(resourceLoader.GetString("StateApp"));
                        Log.Information("---- Applikation wird importiert");
                        Log.Information($"Manu:{device.ApplicationRef.Manufacturer:X2} - AppId:{device.ApplicationRef.Id:X4} - Version:{device.ApplicationRef.Version:X2}");

                        ZipArchiveEntry appEntry = Imports.Archive.GetEntry($"{device.ApplicationRef.Additional.Substring(0,6)}/" + device.ApplicationRef.Additional + ".xml");
                        List<string> errs = new List<string>();
                        bool isOk = CheckApplication(XmlReader.Create(appEntry.Open()), errs);

                        if (!isOk)
                        {
                            Log.Warning("Check mit Warnungen bestanden! " + string.Join(",", errs));
                            OnWarning?.Invoke(device.ApplicationRef.Additional + ": Die Applikation hat die Überprüfung nicht bestanden. (evtl. Plugin benötigt) " + string.Join(",", errs));
                            //_ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => device.Icon = Symbol.ReportHacked);
                            //continue;
                        }

                        if (errs.Count > 0)
                        {
                            Log.Error("Check mit Warnungen bestanden! " + string.Join(",", errs));
                            OnWarning?.Invoke(device.ApplicationRef.Additional + ": Die Applikation hat die Überprüfung nicht bestanden. (evtl. Plugin benötigt) " + string.Join(",", errs));
                        }

                        await Task.Delay(10);

                        try
                        {
                            XElement doc = XDocument.Load(appEntry.Open()).Root;
                            OnDeviceChanged?.Invoke(resourceLoader.GetString("StateTranslate"));
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            TranslateXml(doc, Imports.SelectedLanguage);
                            sw.Stop();
                            Debug.WriteLine("Translate: " + sw.Elapsed.TotalSeconds);
                            ProgressChanged?.Invoke(2);
                            XElement appXml = doc.Descendants(XName.Get("ApplicationProgram", doc.Name.Namespace.NamespaceName)).First();
                            OnDeviceChanged?.Invoke(resourceLoader.GetString("StateAppImport"));
                            await ImportApplications(appXml, device);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, "Applikation Fehler!");
                            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                OnError?.Invoke(device.ApplicationRef.Additional + ": " + e.Message);
                                device.Icon = Symbol.ReportHacked;
                            });
                            continue;
                        }
                        Log.Information("Import Applikationen abgeschlossen");
                        loadedIds.Add(device.ApplicationRef);
                    }
                    else
                    {
                        //TODO check what to do here
                        /*Hardware2AppModel hard2App = _context.Hardware2App.Single(h => h.ApplicationId == device.ApplicationRef.Additional && h.Name != null);

                        foreach (Hardware2AppModel model in _context.Hardware2App.Where(h => h.ApplicationId == device.ApplicationId && h.Name == null))
                        {
                            model.Name = hard2App.Name;
                            model.Version = hard2App.Version;
                            model.Number = hard2App.Number;
                            _context.Hardware2App.Update(model);
                        }
                        _context.SaveChanges();*/
                    }
                }
                ProgressChanged?.Invoke(5);


                OnDeviceChanged?.Invoke(resourceLoader.GetString("StateCheck"));
                //TODO check wirklich implementieren
                await Task.Delay(10);
                ProgressChanged?.Invoke(6);

                _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => device.Icon = Symbol.Like);
                await Task.Delay(10);

            }


            Imports.Archive.Dispose();
            StorageFile file = await ApplicationData.Current.TemporaryFolder.GetFileAsync("temp.knxprod");
            await file.DeleteAsync();


            await Task.Delay(10);
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
            Import.DeviceList.Clear();

            List<string> manus = new List<string>();

            foreach(ZipArchiveEntry entryTemp in Import.Archive.Entries)
            {
                if (entryTemp.FullName.StartsWith("M-"))
                {
                    string manName = "";
                    manName = entryTemp.FullName.Substring(0, 6);
                    if (!manus.Contains(manName))
                        manus.Add(manName);
                }
            }


            foreach(string manName in manus)
            {
                ZipArchiveEntry entry = Import.Archive.GetEntry(manName + "/Catalog.xml");
                XDocument catXML = XDocument.Load(entry.Open());


                string ns = catXML.Root.Name.NamespaceName;
                List<XElement> langs = catXML.Descendants(XName.Get("Language", ns)).ToList();

                if (string.IsNullOrEmpty(Import.SelectedLanguage))
                {
                    ObservableCollection<string> tempLangs = new ObservableCollection<string>();
                    foreach (XElement lang in langs)
                    {
                        tempLangs.Add(lang.Attribute("Identifier").Value);
                    }

                    XElement xapp = catXML.Descendants(XName.Get("CatalogSection", ns)).First();
                    string appDefaultLang = xapp.Attribute("DefaultLanguage")?.Value;
                    if (!string.IsNullOrEmpty(appDefaultLang))
                    {
                        if (!tempLangs.Contains(appDefaultLang))
                            tempLangs.Add(appDefaultLang);
                    }

                    ApplicationDataContainer container = ApplicationData.Current.LocalSettings;
                    string defaultLang = container.Values["defaultLang"]?.ToString();

                    if (tempLangs.Count > 1)
                    {
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
                                ImportHelper.TranslateXml(catXML.Root, diaglang.SelectedLanguage);
                            }
                        }
                        else
                        {
                            Import.SelectedLanguage = defaultLang;
                            ImportHelper.TranslateXml(catXML.Root, defaultLang);
                        }
                    }
                    else if (tempLangs.Count == 1)
                    {
                        Import.SelectedLanguage = defaultLang;
                        ImportHelper.TranslateXml(catXML.Root, tempLangs[0]);
                    }
                } else
                {
                    ImportHelper.TranslateXml(catXML.Root, Import.SelectedLanguage);
                }

                XElement catalogXML = catXML.Descendants(XName.Get("Catalog", ns)).ElementAt<XElement>(0);
                foreach(Device dev in GetDevicesFromCatalog(catalogXML, Import))
                {
                    Import.DeviceList.Add(dev);
                }

            }


            if (Import.DeviceList.Count == 0) return false;

            if (Import.DeviceList.Count > 0)
            {
                foreach (Device device in Import.DeviceList)
                {
                    SlideListItemBase swipe = new SlideListItemBase
                    {
                        LeftSymbol = Symbol.Accept,
                        LeftBackground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 128, 34))
                    };
                    device.SlideSettings = swipe;
                }
            }


            return true;
        }

        public ObservableCollection<Device> GetDevicesFromCatalog(XElement catalogXML, ImportDevices Import)
        {
            ObservableCollection<Device> deviceList = new ObservableCollection<Device>();
            IEnumerable<XElement> catalogItems = catalogXML.Descendants(XName.Get("CatalogItem", catalogXML.Name.NamespaceName));
            catalogItems = catalogItems.OrderBy(c => c.Attribute("Name").Value);

            ZipArchiveEntry entry = Import.Archive.GetEntry(catalogItems.ElementAt(0).Attribute("Id").Value.Substring(0,6) + "/Hardware.xml");
            XElement hardXML = XDocument.Load(entry.Open()).Root;

            foreach (XElement catalogItem in catalogItems)
            {
                Device device = new Device
                {
                    Id = catalogItem.Attribute("Id").Value,
                    Name = catalogItem.Attribute("Name").Value,
                    VisibleDescription = catalogItem.Attribute("VisibleDescription")?.Value.Replace(Environment.NewLine, " "),
                    ProductRefId = catalogItem.Attribute("ProductRefId").Value,
                    Hardware2ProgramRefId = catalogItem.Attribute("Hardware2ProgramRefId").Value
                };

                XElement hard = hardXML.Descendants(XName.Get("Product", hardXML.Name.NamespaceName)).Single(p => p.Attribute("Id").Value == device.ProductRefId);
                if(hard.Parent.Parent.Attribute("NoDownloadWithoutPlugin")?.Value == "1")
                {
                    device.Info = "Gerät benötigt ein Plugin!";
                } else
                {
                    XElement app = null;
                    try
                    {
                        app = hardXML.Descendants(XName.Get("ApplicationProgramRef", hardXML.Name.NamespaceName)).Single(ap => ap.Parent.Attribute("Id").Value == device.Hardware2ProgramRefId);
                        entry = Import.Archive.GetEntry(device.Id.Substring(0, 6) + "/" + app.Attribute("RefId").Value + ".xml");
                        XElement appXML = XDocument.Load(entry.Open()).Root;

                        List<string> errors = new List<string>();
                        device.IsEnabled = CheckApplication(appXML.CreateReader(), errors);
                        device.Info = string.Join(", ", errors);
                    }
                    catch
                    {
                        device.IsEnabled = true;
                    }
                }


                deviceList.Add(device);
            }

            return deviceList;
        }


        public async Task ImportCatalog(ObservableCollection<DeviceImportInfo> devicesList)
        {
            foreach(ZipArchiveEntry entry in Imports.Archive.Entries)
            {
                if (entry.Name != "Catalog.xml") continue;

                string manu = entry.FullName.Substring(2, 4);
                int manuId = int.Parse(manu, System.Globalization.NumberStyles.HexNumber);

                CatalogViewModel parent = null;

                if (!_context.Sections.Any(s => s.ImportType == DataContext.Import.ImportTypes.ETS && s.Key == "M-" + manuId))
                {
                    ManufacturerViewModel man = _context.Manufacturers.Single(m => m.ImportType == DataContext.Import.ImportTypes.ETS && m.ManuId == manuId);
                    parent = new CatalogViewModel
                    {
                        ImportType = DataContext.Import.ImportTypes.ETS,
                        Name = man.Name,
                        ParentId = -1,
                        Key = "M-" + man.ManuId
                    };
                    _context.Sections.Add(parent);
                    _context.SaveChanges();
                } else
                {
                    parent = _context.Sections.Single(s => s.ImportType == DataContext.Import.ImportTypes.ETS && s.Key == "M-" + manuId);
                }

                XElement catalog = XDocument.Load(entry.Open()).Root;
                TranslateXml(catalog, Imports.SelectedLanguage);
                currentNamespace = catalog.Name.NamespaceName;
                catalog = catalog.Element(GetXName("ManufacturerData")).Element(GetXName("Manufacturer")).Element(GetXName("Catalog"));

                await GetSubItems(catalog, parent, devicesList);

            }

            _context.SaveChanges();
        }


        private async Task<bool> GetSubItems(XElement xparent, CatalogViewModel parent, ObservableCollection<DeviceImportInfo> devicesList)
        {
            bool flagHasItem = false;

            foreach (XElement xele in xparent.Elements())
            {
                if(xele.Name.LocalName == "CatalogItem")
                {
                    if(devicesList.Any(d => d.Id == xele.Attribute("Id").Value))
                    {
                        DeviceImportInfo device = devicesList.Single(d => d.Id == xele.Attribute("Id").Value);
                        device.CatalogId = parent.Id;

                        int manuId = int.Parse(xele.Attribute("Id").Value.Substring(2, 4), System.Globalization.NumberStyles.HexNumber);
                        ManufacturerViewModel manu = _context.Manufacturers.Single(m => m.ImportType == ImportTypes.ETS && m.ManuId == manuId);
                        device.ManuId = manu.Id;

                        string strId = xele.Attribute("Hardware2ProgramRefId").Value.Split("_")[1];
                        strId = strId.Substring(strId.LastIndexOf("H-") + 2);

                        string strVer = strId.Substring(strId.LastIndexOf("-") + 1);

                        strId = strId.Substring(0, strId.LastIndexOf("-"));


                        device.HardwareRef = new Reference()
                            {
                                Additional = xele.Attribute("Hardware2ProgramRefId").Value,
                                Id = int.Parse(strId),
                                Version = int.Parse(strVer)
                            };

                        device.ProductRef = xele.Attribute("ProductRefId").Value;
                        flagHasItem = true;
                    }
                } else
                {
                    string key = GetAttributeAsString(xele, "Id");
                    key = key.Substring(key.LastIndexOf("CS-") + 3);
                    CatalogViewModel section = null;

                    if (!_context.Sections.Any(s => s.Key == key))
                    {
                        section = new CatalogViewModel
                        {
                            ImportType = ImportTypes.ETS,
                            Key = key,
                            Name = xele.Attribute("Name")?.Value,
                            ParentId = parent.Id
                        };
                        _context.Sections.Add(section);
                        _context.SaveChanges();
                    }
                    else
                    {
                        section = _context.Sections.Single(s => s.Key == key);
                    }
                     
                    bool hasItem = await GetSubItems(xele, section, devicesList);
                    
                    if (hasItem)
                    {
                        flagHasItem = true;
                    } else
                    {
                        _context.Sections.Remove(section);
                    }
                }
            }
            return flagHasItem;
        }



        public void ImportHardware(XElement hardXML, DeviceImportInfo deviceInfo)
        {
            currentNamespace = hardXML.Name.NamespaceName;

            XElement productXml = hardXML.Descendants(GetXName("Product")).Single(p => p.Attribute("Id").Value == deviceInfo.ProductRef);
            XElement hardwareXml = productXml.Parent.Parent;
            XElement hardware2ProgXml = hardXML.Descendants(GetXName("Hardware2Program")).Single(p => p.Attribute("Id").Value == deviceInfo.HardwareRef.Additional);

            int snum = GetAttributeAsInt(hardwareXml, "SerialNumber");
            int vnum = GetAttributeAsInt(hardwareXml, "VersionNumber");
            Hardware2AppModel hard = null;
            if (_context.Hardware2App.Any(h => h.ManuId == deviceInfo.ManuId && h.Number == snum && h.Version == vnum))
            {
                hard = _context.Hardware2App.Single(h => h.ManuId == deviceInfo.ManuId && h.Number == snum && h.Version == vnum);
            } else
            {
                hard = new Hardware2AppModel()
                {
                    ManuId = deviceInfo.ManuId,
                    Number = snum,
                    Version = vnum
                };
                _context.Hardware2App.Add(hard);
                _context.SaveChanges();
            }

            foreach(XElement xapp in hardwareXml.Descendants(GetXName("ApplicationProgramRef")))
            {
                string appidstr = GetItemIdAsString(xapp.Attribute("RefId").Value);
                int appid = int.Parse(appidstr, System.Globalization.NumberStyles.HexNumber);
                app2hard.Add(appid, hard.Id);
            }


            DeviceViewModel device;
            string prodId = GetAttributeAsString(productXml, "Id");
            prodId = prodId.Substring(prodId.LastIndexOf("-") + 1);

            bool existed = _context.Devices.Any(d => d.ImportType == ImportTypes.ETS && d.Key == prodId);

            if (existed)
                device = _context.Devices.Single(d => d.ImportType == ImportTypes.ETS && d.Key == prodId);
            else
                device = new DeviceViewModel() { ManufacturerId = deviceInfo.ManuId, Key = prodId, ImportType = ImportTypes.ETS };


            device.Name = deviceInfo.Name;
            device.VisibleDescription = deviceInfo.Description;
            device.OrderNumber = productXml.Attribute("OrderNumber").Value;
            device.BusCurrent = SaveHelper.StringToInt(hardwareXml.Attribute("BusCurrent")?.Value);
            device.IsRailMounted = GetAttributeAsBool(productXml, "IsRailMounted");
            device.IsPowerSupply = GetAttributeAsBool(hardwareXml, "IsPowerSupply");
            device.IsCoupler = GetAttributeAsBool(hardwareXml, "IsCoupler");
            device.HasApplicationProgram = GetAttributeAsBool(hardwareXml, "HasApplicationProgram");
            device.HasIndividualAddress = GetAttributeAsBool(hardwareXml, "HasIndividualAddress");
            device.CatalogId = deviceInfo.CatalogId;
            device.HardwareId = hard.Id;

            if (device.BusCurrent == 0 && device.HasApplicationProgram)
                device.BusCurrent = 10;

            if (device.HasApplicationProgram)
            {
                string strId = hardware2ProgXml.Element(GetXName("ApplicationProgramRef")).Attribute("RefId").Value;
                strId = strId.Substring(strId.IndexOf("_A-") + 3);
                string strVer = strId.Split("-")[1];
                strId = strId.Substring(0, strId.IndexOf("-"));

                deviceInfo.ApplicationRef = new Reference()
                {
                    Additional = hardware2ProgXml.Element(GetXName("ApplicationProgramRef")).Attribute("RefId").Value,
                    Id = int.Parse(strId, System.Globalization.NumberStyles.HexNumber),
                    Version = int.Parse(strVer, System.Globalization.NumberStyles.HexNumber)
                };
            }

            if (existed)
                _context.Devices.Update(device);
            else
                _context.Devices.Add(device);

            _context.SaveChanges();
        }

        public async Task ImportApplications(XElement appXml, DeviceImportInfo device)
        {
            currentNamespace = appXml.Name.NamespaceName;
            Log.Information("---- Applikation wird importiert");
            Log.Information(device.ApplicationRef.Additional);

            ApplicationViewModel app;
            //Todo ändern
            bool appExisted = _context.Applications.Any(a => a.Hash == device.ApplicationRef.Additional);
            if (appExisted)
                app = _context.Applications.Single(a => a.Hash == device.ApplicationRef.Additional);
            else
                app = new ApplicationViewModel() { Hash = device.ApplicationRef.Additional };


            string appidstr = GetItemIdAsString(appXml.Attribute("Id").Value);
            int appid = int.Parse(appidstr, System.Globalization.NumberStyles.HexNumber);

            app.Number = int.Parse(appXml.Attribute("ApplicationNumber").Value);
            app.Version = int.Parse(appXml.Attribute("ApplicationVersion").Value);
            app.Mask = appXml.Attribute("MaskVersion").Value;
            app.Name = appXml.Attribute("Name").Value;
            app.HardwareId = app2hard[appid];

            int manuid = int.Parse(appXml.Attribute("Id").Value.Substring(2, 4), System.Globalization.NumberStyles.HexNumber);
            ManufacturerViewModel manu = _context.Manufacturers.Single(m => m.ImportType == ImportTypes.ETS && m.ManuId == manuid);
            app.Manufacturer = manu.Id;

            switch (appXml.Attribute("LoadProcedureStyle").Value)
            {
                case "ProductProcedure":
                    app.LoadProcedure = LoadProcedureTypes.Product;
                    break;

                case "MergedProcedure":
                    app.LoadProcedure = LoadProcedureTypes.Merge;
                    break;

                case "DefaultProcedure":
                    app.LoadProcedure = LoadProcedureTypes.Default;
                    break;

                default:
                    app.LoadProcedure = LoadProcedureTypes.Unknown;
                    break;
            }


            if (appExisted)
                _context.Applications.Update(app);
            else
                _context.Applications.Add(app);
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
            Debug.WriteLine("Read Appl.: " + sw.Elapsed.TotalSeconds);

            Log.Information("Applikation wurde eingelesen...");
        }



        public void UpdateManufacturers(XElement manXML)
        {
            currentNamespace = manXML.Attribute("xmlns").Value;
            XElement mans = manXML.Element(GetXName("MasterData")).Element(GetXName("Manufacturers"));

            foreach (XElement manEle in mans.Elements())
            {
                int manuId = int.Parse(manEle.Attribute("Id").Value.Substring(2), System.Globalization.NumberStyles.HexNumber);
                if (!_context.Manufacturers.Any(m => m.ImportType == DataContext.Import.ImportTypes.ETS && m.ManuId == manuId))
                {
                    ManufacturerViewModel man = new ManufacturerViewModel
                    {
                        ManuId = manuId,
                        ImportType = DataContext.Import.ImportTypes.ETS,
                        Name = manEle.Attribute("Name").Value,
                    };
                    _context.Manufacturers.Add(man);
                }
            }
            _context.SaveChanges();
        }

        public async Task<List<DeviceViewModel>> CheckDevices()
        {

            List<DeviceViewModel> AddedDevices = new List<DeviceViewModel>();
            
            //Todo wieder einfügen
            /*List<string> DeviceIds = new List<string>();
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

            */
            return AddedDevices;
        }

        private int GetItemId(string id)
        {
            return int.Parse(id.Substring(id.LastIndexOf("-") + 1));
        }
        private string GetItemIdAsString(string id)
        {
            return id.Substring(id.LastIndexOf("-") + 1);
        }

        private async Task ReadApplication(XElement doc, ApplicationViewModel app, int maxcount)
        {
            List<string> Errors = new List<string>();
            Dictionary<int, AppParameter> Params = new Dictionary<int, AppParameter>();
            Dictionary<int, AppComObject> ComObjects = new Dictionary<int, AppComObject>();
            currentNamespace = doc.Name.NamespaceName;
            int position = 0;
            List<XElement> tempList;

            Dictionary<string, int> paramTypeIds = new Dictionary<string, int>();
            Dictionary<int, int> SegmentIds = new Dictionary<int, int>();



            tempList = doc.Descendants(GetXName("Baggage")).ToList();
            if(tempList.Count != 0)
            {
                Log.Information("Baggages werden gespeichert");
                ZipArchiveEntry entryBags = Imports.Archive.GetEntry($"M-{app.Manufacturer:X4}" + "/Baggages.xml");
                List<XElement> baggs = XDocument.Load(entryBags.Open()).Root.Descendants(GetXName("Baggage")).ToList();
                StorageFolder appData = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Baggages", CreationCollisionOption.OpenIfExists);
                List<string> acceptedFiles = new List<string>() { ".png", ".jpeg", ".jpg", ".bmp" };
                bool alreadySentWarning = false;

                foreach(XElement xbag in tempList)
                {
                    XElement ebag = baggs.Single(b => b.Attribute("Id").Value == xbag.Attribute("RefId").Value);

                    string fileName = ebag.Attribute("Name").Value;
                    string fileExtension = fileName.Substring(fileName.LastIndexOf('.'));
                    if (!acceptedFiles.Contains(fileExtension))
                    {
                        Log.Warning("Nicht unterstützter Baggage Filetyp: " + fileName);
                        if (!alreadySentWarning)
                        {
                            OnWarning?.Invoke("Applikation enthält nicht unterstützte Baggages");
                            alreadySentWarning = true;
                        }
                        continue;
                    }

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
                    } else
                    {
                        file = await appData.CreateFileAsync(bagId, CreationCollisionOption.ReplaceExisting);
                        File.SetCreationTime(file.Path, time);
                    }

                    if (flagSaveBag)
                    {
                        try
                        {
                            foreach(ZipArchiveEntry entryBag in Imports.Archive.Entries)
                            {
                                if (!entryBag.FullName.EndsWith("/Baggages/" + ebag.Attribute("Name").Value)) continue;
                                Stream sread = entryBag.Open();
                                Stream swrite = await file.OpenStreamForWriteAsync();
                                sread.CopyTo(swrite);
                                try { sread.Flush(); } catch { }
                                sread.Close();
                                swrite.Close();
                            }
                        } catch(Exception e)
                        {

                        }
                    }
                }
            }
            else
                Log.Information("Keine Baggages vorhanden");

            CatalogContext context = new CatalogContext();


            //TODO hardwareid einlesen

            List<string> contextIds = new List<string>();
            foreach (AppSegmentViewModel x in context.AppSegments.Where(s => s.ApplicationId == app.Id))
                contextIds.Add("seg" + x.SegmentId);

            foreach (AppComObject x in context.AppComObjects.Where(c => c.ApplicationId == app.Id))
                contextIds.Add("com" + x.Id);

            foreach (AppParameter x in context.AppParameters.Where(p => p.ApplicationId == app.Id))
                contextIds.Add("par" + x.ParameterId);


            foreach (AppParameterTypeViewModel x in context.AppParameterTypes.Where(pt => pt.ApplicationId == app.Id))
            {
                contextIds.Add("typ" + x.Name);
                foreach (AppParameterTypeEnumViewModel y in context.AppParameterTypeEnums.Where(pe => pe.TypeId == x.Id))
                    contextIds.Add("ten" + y.ParameterId);
            }

            string msg = "";
            OnDeviceChanged?.Invoke(currentAppName + " ParameterTypes");
            Dictionary<int, int> paraType2UId = new Dictionary<int, int>();
            Log.Information("Parameter Typen werden eingelesen");
            //TODO check what happens if the application has no parametertypes
            tempList = doc.Element(GetXName("Static")).Element(GetXName("ParameterTypes")).Elements(GetXName("ParameterType")).ToList();
            //tempList = doc.Descendants(GetXName("ParameterType")).ToList();
            foreach(XElement type in tempList)
            {
                string typeName = GetItemIdAsString(type.Attribute("Id").Value);
                bool existed = contextIds.Contains("typ" + typeName);
                XElement child = type.Elements().ElementAt(0);
                AppParameterTypeViewModel paramt;

                if (existed)
                    paramt = context.AppParameterTypes.Single(p => p.ApplicationId == app.Id && p.Name == typeName);
                else
                    paramt = new AppParameterTypeViewModel() { 
                        Name = typeName, 
                        ApplicationId = app.Id
                    };


                switch (child.Name.LocalName)
                {
                    case "TypeNumber":
                        if (child.Attribute("UIHint")?.Value == "CheckBox")
                        {
                            paramt.Type = ParamTypes.CheckBox;
                        } else 
                        { 
                            switch (child.Attribute("Type").Value)
                            {
                                case "signedInt":
                                    paramt.Type = ParamTypes.NumberInt;
                                    break;
                                case "unsignedInt":
                                    paramt.Type = ParamTypes.NumberUInt;
                                    break;
                                default:
                                    msg = "Unbekannter Nummerntype: " + child.Attribute("Type").Value;
                                    if (!Errors.Contains(msg))
                                        Errors.Add(msg);
                                    Log.Error("Unbekannter Nummerntyp: " + child.Name.LocalName + " - " + child.Attribute("Type").Value);
                                    break;
                            }
                        }
                        paramt.Size = int.Parse(child.Attribute("SizeInBit").Value);
                        paramt.Tag1 = child.Attribute("minInclusive").Value;
                        paramt.Tag2 = child.Attribute("maxInclusive").Value;
                        break;
                    case "TypeTime":
                        paramt.Type = ParamTypes.Time;
                        paramt.Size = int.Parse(child.Attribute("SizeInBit").Value);
                        paramt.Tag1 = child.Attribute("minInclusive").Value + ";" + child.Attribute("Unit").Value;
                        paramt.Tag2 = child.Attribute("maxInclusive").Value;
                        break;

                    case "TypeRestriction":
                        if (existed)
                            context.AppParameterTypes.Update(paramt);
                        else
                            context.AppParameterTypes.Add(paramt);
                        existed = true;
                        context.SaveChanges(); // Damit die paramt schon eine ID haben
                        paramt.Type = ParamTypes.Enum;
                        paramt.Size = int.Parse(child.Attribute("SizeInBit").Value);
                        string _base = child.Attribute("Base").Value;
                        int cenu = 0;
                        foreach (XElement en in child.Elements().ToList())
                        {
                            AppParameterTypeEnumViewModel enu = null;

                            int id = GetItemId(en.Attribute("Id").Value);
                            bool enExisted = contextIds.Contains("ten" + id);


                            if (enExisted)
                                enu = context.AppParameterTypeEnums.Single(e => e.TypeId == paramt.Id && e.ParameterId == id);
                            else
                                enu = new AppParameterTypeEnumViewModel
                            {
                                TypeId = paramt.Id,
                                ParameterId = id
                            };
                            //TODO prüfen ob langdauernde abfrage notwendig ist
                            enu.Value = en.Attribute(_base).Value;
                            enu.Text = en.Attribute("Text").Value;
                            enu.Order = (en.Attribute("DisplayOrder") == null) ? cenu : int.Parse(en.Attribute("DisplayOrder").Value);
                            cenu++;

                            if (enExisted)
                                context.AppParameterTypeEnums.Update(enu);
                            else
                                context.AppParameterTypeEnums.Add(enu);


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
                                break;
                        }
                        paramt.Tag1 = child.Attribute("minInclusive").Value;
                        paramt.Tag2 = child.Attribute("maxInclusive").Value;
                        break;
                    case "TypePicture":
                        paramt.Type = ParamTypes.Picture;
                        paramt.Tag1 = GetItemId(child.Attribute("RefId").Value).ToString();
                        break;
                    case "TypeIPAddress":
                        paramt.Type = ParamTypes.IpAdress;
                        paramt.Tag1 = child.Attribute("AddressType").Value;
                        paramt.Size = 4 * 8;
                        break;
                    case "TypeNone":
                        paramt.Type = ParamTypes.None;
                        break;
                    case "TypeColor":
                        paramt.Type = ParamTypes.Color;
                        paramt.Tag1 = child.Attribute("Space").Value;
                        break;

                    default:
                        msg = "Unbekannter Parametertype: " + child.Name.LocalName;
                        if (!Errors.Contains(msg))
                            Errors.Add(msg);
                        Log.Error("Unbekannter Parametertyp: " + child.Name.LocalName);
                        break;
                }

                if (existed)
                    context.AppParameterTypes.Update(paramt);
                else
                {
                    context.AppParameterTypes.Add(paramt);
                }
                position++;
                ProgressAppChanged(position);
                //del if (position % iterationToWait == 0) await Task.Delay(1);
            }



            XElement table = null;
            tempList = null;
            if (doc.Descendants(GetXName("Code")).Count() != 0)
            {
                Log.Information("Code Segmente werden eingelesen");
                table = doc.Element(GetXName("Static")).Element(GetXName("Code"));

                foreach (XElement seg in table.Elements())
                {
                    AppSegmentViewModel aas;
                    bool existed = false;

                    switch (seg.Name.LocalName)
                    {
                        case "AbsoluteSegment":
                            app.IsRelativeSegment = false;
                            string codesegment = seg.Attribute("Id").Value.Substring(seg.Attribute("Id").Value.LastIndexOf("-") + 1);
                            int segId = int.Parse(codesegment, System.Globalization.NumberStyles.HexNumber);
                            existed = contextIds.Contains("seg" + segId);

                            if (existed)
                                aas = context.AppSegments.Single(a => a.ApplicationId == app.Id && a.SegmentId == segId);
                            else
                                aas = new AppSegmentViewModel() { SegmentId = segId };

                            aas.ApplicationId = app.Id;
                            aas.Address = int.Parse(seg.Attribute("Address").Value);
                            aas.Size = int.Parse(seg.Attribute("Size").Value);
                            aas.Data = seg.Element(GetXName("Data"))?.Value;
                            aas.Mask = seg.Element(GetXName("Mask"))?.Value;

                            if (existed)
                                context.AppSegments.Update(aas);
                            else
                                context.AppSegments.Add(aas);
                            break;

                        case "RelativeSegment":
                            app.IsRelativeSegment = true;
                            int relId = GetItemId(seg.Attribute("Id").Value);
                            existed = contextIds.Contains("seg" + relId);

                            if (existed) aas = context.AppSegments.Single(a => a.ApplicationId == app.Id && a.SegmentId == relId);
                            else aas = new AppSegmentViewModel() { Id = relId };

                            aas.ApplicationId = app.Id;
                            aas.Offset = int.Parse(seg.Attribute("Offset").Value);
                            aas.Size = int.Parse(seg.Attribute("Size").Value);
                            aas.LsmId = int.Parse(seg.Attribute("LoadStateMachine").Value);

                            if (existed)
                                context.AppSegments.Update(aas);
                            else
                                context.AppSegments.Add(aas);
                            break;

                        default:
                            msg = "Unbekanntes Segment: " + seg.Name.LocalName;
                            if (!Errors.Contains(msg))
                                Errors.Add(msg);
                            break;
                    }
                }

                context.SaveChanges();
                foreach(AppSegmentViewModel seg in context.AppSegments.Where(s => s.ApplicationId == app.Id))
                {
                    SegmentIds.Add(seg.SegmentId, seg.Id);
                }
            }
            else
                Log.Information("Keine Code Segmente vorhanden");



            context.SaveChanges();
            foreach (AppParameterTypeViewModel para in context.AppParameterTypes.Where(p => p.ApplicationId == app.Id))
                paramTypeIds.Add(para.Name, para.Id);

            OnDeviceChanged?.Invoke(currentAppName + " Parameter");
            Log.Information("Parameter werden eingelesen");
            tempList = doc.Element(GetXName("Static")).Element(GetXName("Parameters")).Descendants(GetXName("Parameter")).ToList();
            //tempList = doc.Descendants(GetXName("Parameter")).ToList();
            foreach(XElement para in tempList)
            {
                AppParameter param = new AppParameter
                {
                    ParameterId = GetItemId(para.Attribute("Id").Value),
                    Text = para.Attribute("Text").Value,
                    Value = para.Attribute("Value")?.Value,
                };
                param.ParameterTypeId = paramTypeIds[GetItemIdAsString(para.Attribute("ParameterType").Value)];
                string suffix = para.Attribute("SuffixText")?.Value;
                if (!string.IsNullOrEmpty(suffix))
                    param.SuffixText = suffix;
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
                    string codesegment = mem.Attribute("CodeSegment").Value.Substring(mem.Attribute("CodeSegment").Value.LastIndexOf("-") + 1);
                    int segId = int.Parse(codesegment, System.Globalization.NumberStyles.HexNumber);
                    param.SegmentId = SegmentIds[segId];
                    param.Offset = int.Parse(mem.Attribute("Offset").Value);
                    param.OffsetBit = int.Parse(mem.Attribute("BitOffset").Value);
                    param.SegmentType = SegmentTypes.Memory;
                }
                //TODO implement typeid
                Params.Add(param.ParameterId, param);
                position++;
                ProgressAppChanged(position);
                //del if (position % iterationToWait == 0) await Task.Delay(1);
            }





            OnDeviceChanged?.Invoke(currentAppName + " ParameterUnions");
            Log.Information("Unions werden eingelesen");
            tempList = doc.Element(GetXName("Static")).Element(GetXName("Parameters")).Elements(GetXName("Union")).ToList();
            //tempList = doc.Descendants(GetXName("Union")).ToList();
            int unionId = 0;
            foreach (XElement union in tempList)
            {
                unionId++;

                //TODO also check for property for parameter
                int t1 = -1;
                int t2 = 0;
                int t3 = 0;
                SegmentTypes segType = SegmentTypes.None;
                XElement mem = union.Element(GetXName("Memory"));
                if (mem != null)
                {
                    string codesegment = mem.Attribute("CodeSegment").Value.Substring(mem.Attribute("CodeSegment").Value.LastIndexOf("-") + 1);
                    int segId = int.Parse(codesegment, System.Globalization.NumberStyles.HexNumber);
                    t1 = SegmentIds[segId];
                    t2 = int.Parse(mem.Attribute("Offset").Value);
                    t3 = int.Parse(mem.Attribute("BitOffset").Value);
                    segType = SegmentTypes.Memory;
                } else
                {
                    mem = union.Element(GetXName("Property"));
                    if (mem != null)
                    {
                        //ObjectIndex="6" PropertyId="57" Offset="17" BitOffset="0"

                        //TODO check changed get property to work
                        t1 = -2; // "Property:" + mem.Attribute("ObjectIndex").Value + ":" + mem.Attribute("PropertyId").Value;
                        t2 = int.Parse(mem.Attribute("Offset").Value);
                        t3 = int.Parse(mem.Attribute("BitOffset").Value);
                        segType = SegmentTypes.Property;
                        Log.Error("Änderung nicht implementiert! Importhelper->1295 - Parameter in Property " + union.ToString());
                        throw new Exception("Änderung nicht implementiert! Importhelper->1295 - Parameter in Property");
                    } else
                    {
                        msg = "Union hat keinen bekannten Speicher! " + union.ToString();
                        if (!Errors.Contains(msg))
                            Errors.Add(msg);
                        Log.Error("Union hat keinen bekannten Speicher! " + union.ToString());
                    }
                }
                
                int t4 = int.Parse(union.Attribute("SizeInBit").Value);
                mem = null;
                foreach (XElement para in union.Elements(GetXName("Parameter")))
                {
                    AppParameter param = Params[GetItemId(para.Attribute("Id").Value)];
                    param.SegmentId = t1;
                    int off = int.Parse(para.Attribute("Offset").Value);
                    int offb = int.Parse(para.Attribute("BitOffset").Value);
                    param.Offset = t2 + off;
                    param.OffsetBit = t3 + offb;
                    param.SegmentType = segType;
                    param.UnionId = unionId;
                    string def = para.Attribute("DefaultUnionParameter")?.Value.ToLower();
                    param.UnionDefault = def == "true" || def == "1";
                    param.ParameterTypeId = paramTypeIds[GetItemIdAsString(para.Attribute("ParameterType").Value)];
                    //TODO implement parameterTypeId
                }
                position++;
                ProgressAppChanged(position);
            }



            OnDeviceChanged?.Invoke(currentAppName + " ParameterRefs");
            Log.Information("ParameterRefs werden eingelesen");
            tempList = doc.Element(GetXName("Static")).Element(GetXName("ParameterRefs")).Elements(GetXName("ParameterRef")).ToList();
            //tempList = doc.Descendants(GetXName("ParameterRef")).ToList();
            foreach (XElement pref in tempList)
            {
                position++;
                ProgressAppChanged(position);
                //del if (position % iterationToWait == 0) await Task.Delay(1);

                int pId = GetItemId(pref.Attribute("Id").Value);
                AppParameter old = Params[GetItemId(pref.Attribute("RefId").Value)];
                bool existed = contextIds.Contains("par" + pId);
                AppParameter final;
                if (existed)
                {
                    final = context.AppParameters.Single(p => p.ParameterId == pId && p.ApplicationId == app.Id);
                } else
                {
                    final = new AppParameter();
                    final.LoadPara(old);
                }

                final.ParameterId = pId;
                final.ApplicationId = app.Id;

                string text = pref.Attribute("Text")?.Value;
                final.Text = text ?? old.Text;

                string value = pref.Attribute("Value")?.Value;
                if (final.UnionDefault && value != null && value != old.Value) final.UnionDefault = false;
                final.Value = value ?? old.Value;

                AccessType access = AccessType.Null;
                switch (pref.Attribute("Access")?.Value)
                {
                    case "None":
                        access = AccessType.None;
                        break;
                    case "Read":
                        access = AccessType.Read;
                        break;
                    case "ReadWrite":
                        access = AccessType.Full;
                        break;
                }
                final.Access = access == AccessType.Null ? old.Access : access;

                if (existed)
                    context.AppParameters.Update(final);
                else
                    context.AppParameters.Add(final);

            }



            context.SaveChanges();
            Log.Information("ComObjectTable wird eingelesen");
            if(doc.Descendants(GetXName("ComObjectTable")).Count() != 0)
            {
                table = doc.Element(GetXName("Static")).Element(GetXName("ComObjectTable"));
                //table = doc.Descendants(GetXName("ComObjectTable")).ElementAt(0);

                if (table.Attribute("CodeSegment") != null)
                {
                    table = doc.Descendants(GetXName("ComObjectTable")).ElementAt(0);
                    string tableIdStr = GetItemIdAsString(table.Attribute("CodeSegment").Value);
                    int tableId = int.Parse(tableIdStr, System.Globalization.NumberStyles.HexNumber);
                    app.Table_Object = SegmentIds[tableId];
                    int.TryParse(table.Attribute("Offset").Value, out int offsetObject);
                    app.Table_Object_Offset = offsetObject;
                }
                else
                {
                    Log.Information("Für ComObjectTable kein CodeSegment gefunden");
                }

                OnDeviceChanged?.Invoke(currentAppName + " ComObjects");
                Log.Information("ComObjects werden eingelesen");
                foreach (XElement com in table.Elements())
                {
                    AppComObject cobj = new AppComObject
                    {
                        Id = GetItemId(com.Attribute("Id").Value),
                        Text = com.Attribute("Text")?.Value,
                        FunctionText = com.Attribute("FunctionText")?.Value,
                        Number = int.Parse(com.Attribute("Number").Value),

                        Flag_Communicate = com.Attribute("CommunicationFlag")?.Value == "Enabled",
                        Flag_Read = com.Attribute("ReadFlag")?.Value == "Enabled",
                        Flag_ReadOnInit = com.Attribute("ReadOnInitFlag")?.Value == "Enabled",
                        Flag_Transmit = com.Attribute("TransmitFlag")?.Value == "Enabled",
                        Flag_Update = com.Attribute("UpdateFlag")?.Value == "Enabled",
                        Flag_Write = com.Attribute("WriteFlag")?.Value == "Enabled"
                    };

                    cobj.SetSize(com.Attribute("ObjectSize")?.Value);
                    cobj.SetDatapoint(com.Attribute("DatapointType")?.Value);

                    ComObjects.Add(cobj.Id, cobj);
                    position++;
                    ProgressAppChanged(position);
                    //del if (position % iterationToWait == 0) await Task.Delay(1);
                }
            }

            //TODO zusammenbringen mit ComObject auslesen

            OnDeviceChanged?.Invoke(currentAppName + " ComObjectRefs");
            Log.Information("ComObjectRefs werden eingelesen");
            tempList = doc.Element(GetXName("Static")).Element(GetXName("ComObjectRefs")).Elements(GetXName("ComObjectRef")).ToList();
            //tempList = doc.Descendants(GetXName("ComObjectRef")).ToList();
            foreach (XElement cref in tempList)
            {
                position++;
                ProgressAppChanged(position);
                //del if (position % iterationToWait == 0) await Task.Delay(1);

                AppComObjectRef cobjr = new AppComObjectRef
                {
                    Id = GetItemId(cref.Attribute("Id").Value),
                    RefId = GetItemId(cref.Attribute("RefId").Value),
                    Number = cref.Attribute("Number") == null ? -1 : int.Parse(cref.Attribute("Number").Value),
                    Text = cref.Attribute("Text")?.Value,
                    FunctionText = cref.Attribute("FunctionText")?.Value
                };
                cobjr.SetSize(cref.Attribute("ObjectSize")?.Value);
                cobjr.SetDatapoint(cref.Attribute("DatapointType")?.Value);

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

                AppComObject obj = null;
                bool existed = contextIds.Contains("com" + cobjr.Id);
                try
                {
                    if (existed)
                        obj = context.AppComObjects.Single(c => c.ApplicationId == app.Id && c.Id == cobjr.Id);
                    else
                    {
                        obj = new AppComObject();
                        obj.LoadComp(ComObjects[cobjr.RefId]);
                        obj.Id = cobjr.Id;
                    }
                } catch(Exception ex)
                {
                    Log.Error("Something went wrong!", ex);
                }



                obj.ApplicationId = app.Id;
                if (cobjr.FunctionText != null) obj.FunctionText = cobjr.FunctionText;
                if (cobjr.Text != null) obj.Text = cobjr.Text;
                if (cobjr.Datapoint != -1) obj.Datapoint = cobjr.Datapoint;
                if (cobjr.DatapointSub != -1) obj.DatapointSub = cobjr.DatapointSub;
                if (cobjr.Size != -1) obj.Size = cobjr.Size;


                if(obj.Datapoint == -1)
                {
                    Dictionary<string, Dictionary<string, DataPointSubType>> DPSTs = await SaveHelper.GenerateDatapoints();
                    foreach(Dictionary<string, DataPointSubType> numb in DPSTs.Values)
                    {
                        if (numb["xxx"].SizeInBit != obj.Size) continue;

                        bool x = numb.Values.Any(d => d.Default);
                        if (x)
                        {
                            DataPointSubType type = numb.Values.Single(d => d.SizeInBit == obj.Size && d.Default);
                            obj.Datapoint = int.Parse(type.MainNumber);
                            break;
                        }
                    }
                }


                if (cobjr.Flag_Communicate != null) obj.Flag_Communicate = (bool)cobjr.Flag_Communicate;
                if (cobjr.Flag_Read != null) obj.Flag_Read = (bool)cobjr.Flag_Read;
                if (cobjr.Flag_ReadOnInit != null) obj.Flag_ReadOnInit = (bool)cobjr.Flag_ReadOnInit;
                if (cobjr.Flag_Transmit != null) obj.Flag_Transmit = (bool)cobjr.Flag_Transmit;
                if (cobjr.Flag_Update != null) obj.Flag_Update = (bool)cobjr.Flag_Update;
                if (cobjr.Flag_Write != null) obj.Flag_Write = (bool)cobjr.Flag_Write;

                if (obj.Text.Contains("{{"))
                {
                    Regex reg = new Regex("{{((.+):(.+))}}");
                    Match m = reg.Match(obj.Text);
                    if (m.Success)
                    {
                        if(m.Groups[2].Value == "0")
                        {
                            //TODO check changed what to to when Binded is parent
                            obj.BindedId = -1;
                        } else
                        {
                            try
                            {
                                obj.BindedId = int.Parse(m.Groups[2].Value);
                            } catch(Exception e)
                            {
                                throw new Exception("Kein ParameterRef zum Binden gefunden", e);
                            }
                        }
                        obj.BindedDefaultText = m.Groups[3].Value;
                    } else
                    {
                        reg = new Regex("{{(.+)}}");
                        m = reg.Match(obj.Text);
                        if (m.Success)
                        {
                            if (m.Groups[2].Value == "0")
                            {
                                obj.BindedId = -1;
                            }
                            else
                            {
                                try
                                {
                                    obj.BindedId = int.Parse(m.Groups[2].Value);
                                }
                                catch (Exception e)
                                {
                                    throw new Exception("Kein ParameterRef zum Binden gefunden", e);
                                }
                            }
                            obj.BindedDefaultText = "";
                        }
                    }
                }

                if (existed)
                    context.AppComObjects.Update(obj);
                else
                    context.AppComObjects.Add(obj);
            }

            if (doc.Descendants(GetXName("AddressTable")).Count() != 0)
            {
                Log.Information("AddressTable wird eingelesen");
                table = doc.Descendants(GetXName("AddressTable")).ElementAt(0);
                if(table.Attribute("CodeSegment") != null)
                {
                    string tableIdStr = GetItemIdAsString(table.Attribute("CodeSegment").Value);
                    int tableId = int.Parse(tableIdStr, System.Globalization.NumberStyles.HexNumber);
                    app.Table_Group = SegmentIds[tableId];
                    int.TryParse(table.Attribute("Offset")?.Value, out int offsetGroup);
                    app.Table_Group_Offset = offsetGroup;
                }
                else
                    Log.Information("Für AddressTable wurde kein CodeSegment gefunden.");

                int.TryParse(table.Attribute("MaxEntries")?.Value, out int maxEntries);
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
                    string tableIdStr = GetItemIdAsString(table.Attribute("CodeSegment").Value);
                    int tableId = int.Parse(tableIdStr, System.Globalization.NumberStyles.HexNumber);
                    app.Table_Assosiations = SegmentIds[tableId];
                    int.TryParse(table.Attribute("Offset")?.Value, out int offsetAssoc);
                    app.Table_Assosiations_Offset = offsetAssoc;
                }
                else
                    Log.Information("Für AssociationTable wurde kein CodeSegment gefunden.");

                int.TryParse(table.Attribute("MaxEntries")?.Value, out int maxEntriesA);
                app.Table_Assosiations_Max = maxEntriesA;
            }
            else
                Log.Information("Kein AssociationTable vorhanden");


            




            if (Errors.Count != 0)
            {
                string err = "";
                foreach (string ae in Errors)
                    err += Environment.NewLine + "     " + ae;
                Log.Error("Es traten " + Errors.Count.ToString() + " Fehler auf...");
                throw new Exception("Es traten " + Errors.Count.ToString() + " Fehler auf!" + err);
            }



            Log.Information("Applikation in Datenbank speichern");
            context.Applications.Update(app);

            try
            {
                context.SaveChanges();
                ProgressAppChanged(maxcount);
            }
            catch (Exception e)
            {
                Log.Error(e, "Applikation speichern Fehler!");
            }


            AppAdditional adds;
            bool existedAdds = context.AppAdditionals.Any(a => a.Id == app.Id);
            if (existedAdds)
                adds = context.AppAdditionals.Single(a => a.Id == app.Id);
            else
            {
                adds = new AppAdditional() { ApplicationId = app.Id };
            }

            Log.Information("LoadProcedures werden gespeichert");
            IEnumerable<XElement> loads = doc.Descendants(GetXName("LoadProcedures"));
            if(loads.Count() != 0)
            {
                XElement procedures = doc.Descendants(GetXName("LoadProcedures"))?.ElementAt(0);
                adds.LoadProcedures = System.Text.Encoding.UTF8.GetBytes(MinimizeXML(procedures.ToString()));
            }

            if (doc.Descendants(GetXName("Dynamic")).Count() != 0)
            {
                Log.Information("Dynamic wird gespeichert");
                table = doc.Descendants(GetXName("Dynamic")).ElementAt(0);
                adds.Dynamic = System.Text.Encoding.UTF8.GetBytes(MinimizeXML(table.ToString()));
            }
            else
                Log.Information("Kein Dynamic vorhanden");

            Log.Information("Dynamics werden generiert");
            ProgressChanged?.Invoke(3);
            OnDeviceChanged(currentAppName + " - Generiere Dynamics");
            await Task.Delay(10);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Dictionary<int, Dynamic.ViewParamModel> Id2Param = SaveHelper.GenerateDynamic(adds);
            sw.Stop();
            Debug.WriteLine("Generate Dyn: " + sw.Elapsed.TotalSeconds);

            Log.Information("Standard ComObjects werden generiert");
            ProgressChanged?.Invoke(4);
            OnDeviceChanged(currentAppName + " - Generiere ComObjects");
            await Task.Delay(10);
            sw = new Stopwatch();
            sw.Start();
            await SaveHelper.GenerateDefaultComs(adds, Id2Param);
            sw.Stop();
            Debug.WriteLine("Generate Coms: " + sw.Elapsed.TotalSeconds);

            if (existedAdds)
                context.AppAdditionals.Update(adds);
            else
                context.AppAdditionals.Add(adds);

            context.SaveChanges();
        }


        private string MinimizeXML(string xml)
        {
            xml = xml.Replace("  ", "");
            xml = xml.Replace("> <", "><");
            xml = xml.Replace(Environment.NewLine, "");
            return xml;
        }

        private bool GetAttributeAsBool(XElement ele, string attr)
        {
            string val = ele.Attribute(attr)?.Value;
            return (val == "1" || val == "true");
        }

        private int GetAttributeAsInt(XElement ele, string attr)
        {
            string val = ele.Attribute(attr)?.Value;
            return int.Parse(val);
        }

        private string GetAttributeAsString(XElement ele, string attr)
        {
            return (ele.Attribute(attr) == null) ? "" : ele.Attribute(attr).Value;
        }

        public bool CheckApplication(XmlReader reader, List<string> errs)
        {
            bool flag = true;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                    continue;

                switch (reader.Name)
                {
                    case "ApplicationProgram":
                        if(reader.GetAttribute("IsSecureEnabled") == "true")
                        {
                            errs.Add("SecureEnabled");
                            flag = false;
                            Log.Warning("Applikation benötigt KNX Secure!");
                        }

                        break;
                    case "Extension":
                        string plugdown = reader.GetAttribute("EtsDownloadPlugin");
                        string plugrequ = reader.GetAttribute("RequiresExternalSoftware");
                        string plugui = reader.GetAttribute("EtsUiPlugin");
                        string plughand = reader.GetAttribute("EtsDataHandler");
                        if (plugdown != null && !errs.Contains("EtsDownloadPlugin")) errs.Add("EtsDownloadPlugin");
                        if (plugui != null && !errs.Contains("EtsUiPlugin")) errs.Add("EtsUiPlugin");
                        if (plughand != null && !errs.Contains("EtsDataHandler")) errs.Add("EtsDataHandler");
                        if (plugrequ != null && (plugrequ == "1" || plugrequ == "true") && !errs.Contains("RequiresExternalSoftware")) errs.Add("RequiresExternalSoftware");
                        Log.Warning("Applikation enthält Extension: " + reader.Name, reader.ReadOuterXml());
                        break;
                    case "RelativeSegment":
                        //if(!errs.Contains("RelativeSegment")) 
                        //    errs.Add("RelativeSegment");
                        break;
                    case "ParameterCalculations":
                        if (!errs.Contains("ParameterCalculations"))
                        {
                            Log.Warning("Applikation enthält Berechnungen: " + reader.Name);
                            errs.Add("ParameterCalculations");
                            flag = false;
                        }
                        reader.ReadOuterXml();
                        break;
                        //case "Property":
                        //    Log.Warning("Unbekannte Property! ", reader.ReadOuterXml());
                        //    //if (!errs.Contains("Property")) errs.Add("Property");
                        //    // ToDo: Check what it means
                        //    break;
                }
            }

            return flag;
        }

        private XName GetXName(string name)
        {
            return XName.Get(name, currentNamespace);
        }
    }
}
