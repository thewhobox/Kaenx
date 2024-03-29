﻿using Kaenx.DataContext;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.EntityFrameworkCore;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using Windows.UI.Core;
using Windows.Storage;
using System.Runtime.InteropServices;
using Kaenx.Classes;
using Serilog;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Push;
using Windows.UI.StartScreen;
using Kaenx.DataContext.Local;
using Windows.UI.Xaml.Media.Animation;
using System.Diagnostics;
using Kaenx.Classes.Bus;

namespace Kaenx
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public static CoreDispatcher _dispatcher;
        public static Dictionary<string, Page> _pages;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            using (var db = new LocalContext())
            {
                db.Database.Migrate();
                if (db.ConnsProject.Count() == 0)
                {
                    LocalConnectionProject connP = new LocalConnectionProject()
                    {
                        Type = LocalConnectionProject.DbConnectionType.SqlLite,
                        Name = "Lokal",
                        DbHostname = "Projects.db"
                    };
                    db.ConnsProject.Add(connP);
                    db.SaveChanges();
                    ProjectContext conP = new ProjectContext(connP);
                    conP.Database.Migrate();
                }
                if (db.ConnsCatalog.Count() == 0)
                {
                    LocalConnectionCatalog connC = new LocalConnectionCatalog()
                    {
                        Type = LocalConnectionCatalog.DbConnectionType.SqlLite,
                        Name = "Lokal",
                        DbHostname = "Catalog.db"
                    };
                    db.ConnsCatalog.Add(connC);
                    db.SaveChanges();
                    CatalogContext conC = new CatalogContext(connC);
                    conC.Database.Migrate();
                }
            }

            CreateLogger();


#if DEBUG
            Log.Information("Mode = Debug");
#else
            AppCenter.Start("15f6e92f-3928-4b75-a73d-6e95dc2a0ee4",
                   typeof(Analytics), typeof(Crashes), typeof(Push));
#endif

            Log.Information($"" +
                $"{System.Environment.OSVersion.Version.Major}." +
                $"{System.Environment.OSVersion.Version.Minor}." +
                $"{System.Environment.OSVersion.Version.Build}" +
                $"");


            this.UnhandledException += App_UnhandledException;




        }


        private void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "UnhandledException!");
            if (e.Exception.InnerException != null)
                Log.Error(e.Exception.InnerException, "InnerException!");

            if (e.Exception.HResult == -2147024809) return;

            throw e.Exception;
        }

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            //----------------< OnFileActivated() >---------------- 
            //* when opened by file-extension 
            base.OnFileActivated(args);

            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            StorageFile file = args.Files[0] as StorageFile;
            switch (file.FileType)
            {
                case ".knxprod":
                    //Window.Current.Content as Frame: Content je nach 
                    Log.Information("Window Content: " + Window.Current.Content.GetType().FullName);
                    
                    if (rootFrame.Content == null || rootFrame.Content is View.MainPage)
                    {
                        Navigate(typeof(View.Import), file);
                    }
                    else if (rootFrame.Content is View.Import)
                    {
                        //TODO fix this
                        //((View.Catalog)rootFrame.Content).PrepareImport(file);
                    }
                    Log.Information("Frame Content: " + rootFrame.Content.GetType().FullName);
                    break;

                default:
                    string errMsg = "Nicht unterstützter Dateityp: " + file.FileType;
                    if (rootFrame.Content == null)
                    {
                        Navigate(typeof(View.MainPage), errMsg);
                    } else
                    {
                        Classes.Helper.ViewHelper.Instance.ShowNotification("main", errMsg, 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                    }
                    Log.Warning(errMsg);
                    break;
            }

            Window.Current.Activate();


            App._dispatcher = Window.Current.Dispatcher;
        }

        private async void CreateLogger()
        {
            StorageFolder localState = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Logs", CreationCollisionOption.OpenIfExists);

            //TODO make setting to change level
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(localState.Path, "log-.txt"), rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        public static Frame AppFrame
        {
            get
            {
                return (Frame)Window.Current.Content;
            }
        }

        public static void Navigate(Type target, object param = null, NavigationTransitionInfo info = null)
        {
            AppFrame.Navigate(target, param, info);
        }

        public static void Goback()
        {
            AppFrame.GoBack();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                //this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(View.MainPage), e.Arguments);
                } else if (e.Kind == ActivationKind.Launch)
                {
                    //TODO impelement jumplist
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }

            //TODO implement change theme
            //(Application.Current.Resources["BrushAccentColor2"] as SolidColorBrush).Color = Windows.UI.Colors.Red;

        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            Log.Error(e.Exception, "Navigation failed!");
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            
            if(BusRemoteConnection.Instance.Remote.IsActive)
                await BusRemoteConnection.Instance.Remote.Disconnect();
            deferral.Complete();
        }
    }
}
