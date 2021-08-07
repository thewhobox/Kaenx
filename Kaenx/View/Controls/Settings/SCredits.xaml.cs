using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace Kaenx.View.Controls.Settings
{
    public sealed partial class SCredits : UserControl
    {
        public List<CreditEntry> Credits { get; set; } = new List<CreditEntry>()
        {
            new CreditEntry() {
                Name = "NewtonSoft.json",
                Author = "James Newton-King",
                License = "MIT",
                LicenseUrl = new Uri("https://licenses.nuget.org/MIT"),
                ProjectUrl = new Uri("https://www.newtonsoft.com/json"),
                ImageUrl = "https://api.nuget.org/v3-flatcontainer/newtonsoft.json/13.0.1/icon"
            },
            new CreditEntry() {
                Name = "NewtonSoft.Json.Bson",
                Author = "James Newton-King",
                License = "MIT",
                LicenseUrl = new Uri("https://licenses.nuget.org/MIT"),
                ProjectUrl = new Uri("https://www.newtonsoft.com/json"),
                ImageUrl = "https://api.nuget.org/v3-flatcontainer/newtonsoft.json/13.0.1/icon"
            },
            new CreditEntry()
            {
                Name = "Device.Net",
                License = "MIT",
                Author = "Christian Findlay",
                LicenseUrl = new Uri("https://licenses.nuget.org/MIT"),
                ProjectUrl = new Uri("https://github.com/MelbourneDeveloper/Device.Net"),
                ImageUrl = "https://raw.githubusercontent.com/MelbourneDeveloper/Device.Net/main/Diagram.png"
            },
            new CreditEntry()
            {
                Name = "Serilog",
                Author = "Serilog Contributors",
                License = "Apache-2.0",
                LicenseUrl = new Uri("https://licenses.nuget.org/Apache-2.0"),
                ProjectUrl = new Uri("https://serilog.net/"),
                ImageUrl = "https://serilog.net/img/serilog.png"
            },
            new CreditEntry()
            {
                Name = "Serilog.Sinks.File",
                Author = "Serilog Contributors",
                License = "Apache-2.0",
                LicenseUrl = new Uri("https://licenses.nuget.org/Apache-2.0"),
                ProjectUrl = new Uri("https://serilog.net/"),
                ImageUrl = "https://api.nuget.org/v3-flatcontainer/serilog.sinks.file/4.1.0/icon"
            },
            new CreditEntry()
            {
                Name = "Microsoft.UI.XAML",
                Author = "Microsoft",
                License = "MICROSOFT SOFTWARE LICENSE TERMS",
                LicenseUrl = new Uri("https://www.nuget.org/packages/Microsoft.UI.Xaml/2.5.0/license"),
                ProjectUrl = new Uri("https://github.com/microsoft/microsoft-ui-xaml"),
                ImageUrl = "https://docs.microsoft.com/en-us/windows/apps/images/logo-winui.png"
            },
            new CreditEntry()
            {
                Name = "Microsoft.AppCenter.Analytics",
                Author = "Microsoft",
                License = "MIT",
                LicenseUrl = new Uri("https://licenses.nuget.org/MIT"),
                ProjectUrl = new Uri("https://azure.microsoft.com/en-us/services/app-center/"),
                ImageUrl = "https://mobilecentersdkdev.blob.core.windows.net/sdk/mobilecenter-logo.png"
            },
            new CreditEntry()
            {
                Name = "Microsoft.AppCenter.Crashes",
                Author = "Microsoft",
                License = "MIT",
                LicenseUrl = new Uri("https://licenses.nuget.org/MIT"),
                ProjectUrl = new Uri("https://azure.microsoft.com/en-us/services/app-center/"),
                ImageUrl = "https://mobilecentersdkdev.blob.core.windows.net/sdk/mobilecenter-logo.png"
            },
            new CreditEntry()
            {
                Name = "Microsoft.AppCenter.Push",
                Author = "Microsoft",
                License = "MIT",
                LicenseUrl = new Uri("https://licenses.nuget.org/MIT"),
                ProjectUrl = new Uri("https://azure.microsoft.com/en-us/services/app-center/"),
                ImageUrl = "https://mobilecentersdkdev.blob.core.windows.net/sdk/mobilecenter-logo.png"
            },
            new CreditEntry()
            {
                Name = "Microsoft.EntityFrameworkCore.Sqlite",
                Author = "Microsoft",
                License = "Apache-2.0",
                LicenseUrl = new Uri("https://licenses.nuget.org/Apache-2.0"),
                ProjectUrl = new Uri("https://docs.microsoft.com/de-de/ef/core/"),
                ImageUrl = "https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore.sqlite/5.0.8/icon"
            },
            new CreditEntry()
            {
                Name = "Microsoft.EntityFrameworkCore.Tools",
                Author = "Microsoft",
                License = "Apache-2.0",
                LicenseUrl = new Uri("https://licenses.nuget.org/Apache-2.0"),
                ProjectUrl = new Uri("https://docs.microsoft.com/de-de/ef/core/"),
                ImageUrl = "https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore.sqlite/5.0.8/icon"
            },
            new CreditEntry()
            {
                Name = "Microsoft.Extensions.Configuration.Abstractions",
                Author = "Microsoft",
                License = "Apache-2.0",
                LicenseUrl = new Uri("https://licenses.nuget.org/Apache-2.0"),
                ProjectUrl = new Uri("https://asp.net/"),
                ImageUrl = "https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore.sqlite/5.0.8/icon"
            },
            new CreditEntry()
            {
                Name = "Microsoft.Extensions.Loggin.Debug",
                Author = "Microsoft",
                License = "Apache-2.0",
                LicenseUrl = new Uri("https://licenses.nuget.org/Apache-2.0"),
                ProjectUrl = new Uri("https://asp.net/"),
                ImageUrl = "https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore.sqlite/5.0.8/icon"
            },
            new CreditEntry()
            {
                Name = "Microsoft.NETCore.UniversalWindowsPlatform",
                Author = "Microsoft",
                License = "MICROSOFT SOFTWARE LICENSE TERMS",
                LicenseUrl = new Uri("https://github.com/Microsoft/dotnet/blob/master/releases/UWP/LICENSE.TXT"),
                ProjectUrl = new Uri("https://github.com/Microsoft/dotnet/blob/master/releases/UWP/README.md"),
                ImageUrl = "https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore.sqlite/5.0.8/icon"
            },
            new CreditEntry()
            {
                Name = "Microsoft.Toolkit.Uwp.UI",
                Author = "Microsoft.Toolkit",
                License = "MIT",
                LicenseUrl = new Uri("https://licenses.nuget.org/MIT"),
                ProjectUrl = new Uri("https://github.com/CommunityToolkit/WindowsCommunityToolkit"),
                ImageUrl = "https://api.nuget.org/v3-flatcontainer/microsoft.toolkit.uwp.ui/7.0.2/icon"
            },
            new CreditEntry()
            {
                Name = "Microsoft.Toolkit.Uwp.UI.DataGrid",
                Author = "Microsoft.Toolkit",
                License = "MIT",
                LicenseUrl = new Uri("https://licenses.nuget.org/MIT"),
                ProjectUrl = new Uri("https://github.com/CommunityToolkit/WindowsCommunityToolkit"),
                ImageUrl = "https://api.nuget.org/v3-flatcontainer/microsoft.toolkit.uwp.ui/7.0.2/icon"
            }
        };




        public SCredits()
        {
            this.InitializeComponent();
            CreditsList.DataContext = Credits.OrderBy(x => x.Name);
        }
    }

    public class CreditEntry
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public string License { get; set; }
        public Uri LicenseUrl { get; set; }
        public Uri ProjectUrl { get; set; }
        public string ImageUrl { get; set; }
    }
}
