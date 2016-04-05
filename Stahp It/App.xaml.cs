/*
* Copyright (c) 2016 Jesse Nicholson.
*
* This file is part of Stahp It.
*
* Stahp It is free software: you can redistribute it and/or
* modify it under the terms of the GNU General Public License as published
* by the Free Software Foundation, either version 3 of the License, or (at
* your option) any later version.
*
* In addition, as a special exception, the copyright holders give
* permission to link the code of portions of this program with the OpenSSL
* library.
*
* You must obey the GNU General Public License in all respects for all of
* the code used other than OpenSSL. If you modify file(s) with this
* exception, you may extend this exception to your version of the file(s),
* but you are not obligated to do so. If you do not wish to do so, delete
* this exception statement from your version. If you delete this exception
* statement from all source files in the program, then also delete it
* here.
*
* Stahp It is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
* Public License for more details.
*
* You should have received a copy of the GNU General Public License along
* with Stahp It. If not, see <http://www.gnu.org/licenses/>.
*/

using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Te.HttpFilteringEngine;
using Te.StahpIt.Controls;
using Te.StahpIt.Filtering;
using Te.StahpIt.Models;
using Te.StahpIt.Serialization.Json.Converters;
using Te.StahpIt.Update;
using Te.StahpIt.ViewModels;
using Te.StahpIt.Views;
using Te.StahpIt.Windows;

namespace Te.StahpIt
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class StahpIt : Application
    {
        private readonly Logger m_logger;

        /// <summary>
        /// Holds a table of existing FilteredAppModel indexed by binary absolute path. We need to
        /// keep a collection of such applications separately from wherever the view/model/viewmodel
        /// for the filtered applications actually lives (presently inside the dashboard view model
        /// as a sub-view). We need to do this to control creation, loading/saving of unique entries
        /// before we give these things to the user.
        /// </summary>
        private ConcurrentDictionary<string, FilteredAppModel> m_filteredApplicationsTable;

        /// <summary>
        /// Holds a table of existing CategorizedFilteredRequestsViewModel objects that are indexed
        /// by the auto-generated category ID. These view models are required in multiple view, but
        /// they all wrap the same underlying model reference.
        ///
        /// These objects are held here, and references are given over to various view models, such
        /// as the statistics page view model, so that this information can be presented to the user.
        /// </summary>
        private ObservableCollection<CategorizedFilteredRequestsViewModel> m_filteringCategoriesObservable;

        #region WINDOWS_VIEWS_MODELS_DATA

        /// <summary>
        /// The primary window. That's pretty much at all it's responsible for, just sitting there
        /// looking beautiful. But seriously, its simply a window that we push and pop various views
        /// to at the request of the user.
        /// </summary>
        private MainWindow m_primaryWindow;

        /// <summary>
        /// Loading screen shown during application startup.
        /// </summary>
        private Splash m_splashScreen;

        /// <summary>
        /// Wait view to provide feedback to the user while awaiting the completion of asynchronous
        /// background tasks.
        /// </summary>
        private ProgressWait m_viewProgressWait;

        private StatisticsModel m_modelStatistics;
        private StatisticsViewModel m_viewModelStatistics;
        private Statistics m_viewStatistics;

        private DashboardModel m_modelDashboard;
        private DashboardViewModel m_viewModelDashboard;
        private Dashboard m_viewDashboard;

        private SettingsModel m_modelSettings;
        private SettingsViewModel m_viewModelSettings;
        private Settings m_viewSettings;

        private WasteViewModel m_viewModelWaste;
        private Waste m_viewWaste;

        #endregion VIEWS_MODELS_DATA

        /// <summary>
        /// The Engine that actually does all the work begind the scenes.
        /// </summary>
        private Engine m_filteringEngine;

        private Engine.OnFirewallCheckCallback m_firewallCheckDelegateCb;

        /// <summary>
        /// We override the minimize window functionality and go straight to minimizing to the tray.
        /// This makes sense, given that this is an application the user doesn't really need to
        /// constantly interact with. So, we just automatically send it to the background.
        /// </summary>
        private System.Windows.Forms.NotifyIcon m_trayIcon;        

        #region APP_UPDATE_MEMBER_VARS

        /// <summary>
        /// Delegate we supply to the WinSparkle DLL, which it will use to check with us if a
        /// shutdown is okay. We can reply yes or no to this request. If we reply yes, then
        /// WinSparkle will make an official shutdown request, allowing us to cleanly shutdown
        /// before getting updated.
        /// </summary>
        private WinSparkle.WinSparkleCanShutdownCheckCallback m_winsparkleShutdownCheckCb;

        /// <summary>
        /// Delegate we supply to the WinSparkle DLL, which if we've given permission, means that
        /// when WinSparkle invokes this method, we are to cleanly shutdown so that WinSparkle can
        /// complete an application update.
        /// </summary>
        private WinSparkle.WinSparkleRequestShutdownCallback m_winsparkleShutdownRequestCb;

        /// <summary>
        /// The url where WinSparkle will search for updates.
        /// </summary>
        internal string AppcastUrl
        {
            get;
            private set;
        }

        #endregion APP_UPDATE_MEMBER_VARS

        /// <summary>
        /// This BackgroundWorker object handles initializing the application off the UI thread.
        /// Allows the splash screen to function.
        /// </summary>
        private BackgroundWorker m_backgroundInitWorker;        

        public StahpIt()
        {
            // Apparently NLOG has some issues auto creating directories even when its told to.
            string logDir = AppDomain.CurrentDomain.BaseDirectory + "logs";
            Directory.CreateDirectory(logDir);

            m_logger = LogManager.GetLogger("StahpIt");
        }

        /// <summary>
        /// Entry point for the application.
        /// </summary>
        /// <param name="e">
        /// Arguments passed to the executable at launch.
        /// </param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            m_filteringCategoriesObservable = new ObservableCollection<CategorizedFilteredRequestsViewModel>();

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            m_splashScreen = new Splash();
            m_splashScreen.Show();

            m_backgroundInitWorker = new BackgroundWorker();
            m_backgroundInitWorker.DoWork += DoBackgroundInit;
            m_backgroundInitWorker.RunWorkerCompleted += OnBackgroundInitComplete;

            m_backgroundInitWorker.RunWorkerAsync(e);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (m_logger != null)
            {
                var err = e.ExceptionObject as Exception;
                m_logger.Error(err.Message);
            }
        }

        private void DoBackgroundInit(object sender, DoWorkEventArgs e)
        {
            try
            {
                DoInit();
            }
            catch (Exception err)
            {
                m_logger.Error("During startup, encountered error: {0}.", err.Message);
                m_logger.Error(err.StackTrace);

                if (err.InnerException != null)
                {
                    m_logger.Error("Inner Exception: {0}.", err.InnerException.Message);
                }
                
                m_logger.Error("Critical error. Exiting.");
                Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    (Action)delegate ()
                    {
                        Current.Shutdown();
                    }
                );
                return;
            }

            var startupArgs = e.Argument as StartupEventArgs;

            bool startMinimized = false;
            for (int i = 0; i != startupArgs.Args.Length; ++i)
            {
                if (startupArgs.Args[i].Equals("/StartMinimized", StringComparison.OrdinalIgnoreCase))
                {
                    startMinimized = true;
                    break;
                }
            }

            if (startMinimized)
            {
                // Start the filter.
                m_viewModelDashboard.FilteringEnabled = true;
                MinimizeToTray(false);
            }
        }

        private void OnBackgroundInitComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                m_logger.Error("Error during initialization.");
                if (e.Error != null && m_logger != null)
                {
                    m_logger.Error(e.Error.Message);
                    m_logger.Error(e.Error.StackTrace);
                }

                Current.Shutdown(-1);
                return;
            }

            Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)delegate ()
                {
                    Exit += OnApplicationShutdown;

                    // Must be done or our single instance enforcement is going to bug out.
                    // https://github.com/TechnikEmpire/StahpIt-WPF/issues/2
                    m_splashScreen.Close();
                    m_splashScreen = null;
                    //

                    m_primaryWindow.Show();
                    OnViewChangeRequest(this, new ViewChangeRequestArgs(View.Dashboard));
                }
            );

            // Check for updates, always.
            WinSparkle.CheckUpdateWithoutUI();
        }

        /// <summary>
        /// Calls all other init methods, which are split up into ordered, logical groupings.
        /// </summary>
        private void DoInit()
        {
            InitEngine();

            try
            {
                // Attempt to load program state, if exists.
                LoadProgramState();
            }
            catch (Exception err)
            {
                m_logger.Error("Error while loading program state: {0}.", err.Message);
            }

            InitTrayIcon();
            InitViews();

            // Last, we'll initialize WinSparkle and let it do an update check.
            InitWinsparkle();
        }

        private void OnApplicationShutdown(object sender, ExitEventArgs e)
        {
            if (m_filteringEngine != null && m_filteringEngine.IsRunning)
            {
                m_filteringEngine.Stop();
            }

            try
            {
                SaveProgramState();
            }
            catch (Exception err)
            {
                m_logger.Error("Error while saving program state: {0}.", err.Message);
            }

            // Dispose all models that implement IDisposable.
            if(m_modelDashboard != null)
            {
                m_modelDashboard.Dispose();
            }

            if (m_modelSettings != null)
            {
                m_modelSettings.Dispose();
            }

            WinSparkle.Cleanup();
        }

        /// <summary>
        /// Inits all the various views for the application, which will be pushed and popped on the
        /// primary window as requested or required.
        /// </summary>
        private void InitViews()
        {
            if (m_filteringEngine == null)
            {
                throw new Exception("Engine must be initialized prior to initializing views, as views require references to allow user control.");
            }

            // We null check, because if the save state load worked, these conditions will be
            // false and we won't overwrite our restored state.

            // This collection is initialized here because it has a direct connection to the UI.
            if (m_filteredApplicationsTable == null)
            {
                m_filteredApplicationsTable = new ConcurrentDictionary<string, FilteredAppModel>();
            }

            if (m_modelDashboard == null)
            {
                m_modelDashboard = new DashboardModel(m_filteringEngine);
            }

            if (m_viewModelDashboard == null)
            {
                m_viewModelDashboard = new DashboardViewModel(m_modelDashboard);
            }

            if (m_modelStatistics == null)
            {
                m_modelStatistics = new StatisticsModel();
            }

            if (m_viewModelStatistics == null)
            {
                m_viewModelStatistics = new StatisticsViewModel(m_modelStatistics);
            }

            if (m_modelSettings == null)
            {
                m_modelSettings = new SettingsModel();
            }

            if (m_viewModelSettings == null)
            {
                m_viewModelSettings = new SettingsViewModel(m_modelSettings);
            }

            if(m_viewModelWaste == null)
            {
                m_viewModelWaste = new WasteViewModel(m_viewModelSettings, m_viewModelDashboard);
            }

            // Necessary because we use a background worker. This thread != UI thread.
            Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)delegate ()
                {
                    m_modelStatistics.FilterCategories = m_filteringCategoriesObservable;
                    m_modelSettings.FilterCategories = m_filteringCategoriesObservable;

                    m_primaryWindow = new MainWindow();
                    m_viewProgressWait = new ProgressWait();
                    m_viewStatistics = new Statistics(m_viewModelStatistics);
                    m_viewDashboard = new Dashboard(m_viewModelDashboard);
                    m_viewSettings = new Settings(m_viewModelSettings, new AddCategoryControl(m_filteringEngine));
                    m_viewWaste = new Waste(m_viewModelWaste);

                    m_primaryWindow.ViewChangeRequest += OnViewChangeRequest;
                    m_viewDashboard.ViewChangeRequest += OnViewChangeRequest;
                    m_viewStatistics.ViewChangeRequest += OnViewChangeRequest;
                    m_viewSettings.ViewChangeRequest += OnViewChangeRequest;
                    m_viewWaste.ViewChangeRequest += OnViewChangeRequest;

                    // Listen for the statistics view requests for app-wide stats deletion.
                    m_viewStatistics.ClearStatisticsRequested += OnClearAllStatsRequest;

                    MainWindow = m_primaryWindow;
                }
            );
        }        

        /// <summary>
        /// Callback for when a change in the current view has been requested.
        /// </summary>
        /// <param name="sender">
        /// Who requested the change.
        /// </param>
        /// <param name="e">
        /// Arguments for the view change.
        /// </param>
        private void OnViewChangeRequest(object sender, ViewChangeRequestArgs e)
        {
            BaseView viewToLoad = null;
            string windowTitle = string.Empty;

            bool mainMenuEnabled = true;

            switch (e.View)
            {
                case View.Dashboard:
                    {
                        windowTitle = " - Dashboard";
                        viewToLoad = m_viewDashboard;
                    }
                    break;

                case View.ProgressWait:
                    {
                        windowTitle = " - Working";
                        viewToLoad = m_viewProgressWait;
                        if (e.Data != null)
                        {
                            if (e.Data is string)
                            {
                                m_viewProgressWait.SetMessage(e.Data as string);
                            }
                        }

                        mainMenuEnabled = false;
                    }
                    break;

                case View.Statistics:
                    {
                        windowTitle = " - Statistics";
                        viewToLoad = m_viewStatistics;
                    }
                    break;

                case View.Settings:
                    {
                        windowTitle = " - Settings";
                        viewToLoad = m_viewSettings;
                    }
                    break;

                case View.Waste:
                    {
                        windowTitle = " - Waste Cost";
                        viewToLoad = m_viewWaste;
                    }
                    break;

                case View.Tray:
                    {
                        bool showTooltip = false;

                        if (e.Data != null)
                        {
                            if (e.Data is bool)
                            {
                                showTooltip = (bool)e.Data;
                            }
                        }

                        MinimizeToTray(showTooltip);
                    }
                    break;
            }

            if (viewToLoad != null)
            {
                Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    (Action)delegate ()
                    {
                        try
                        {
                            // Hide any active flyouts, as the way that we use them, they are always related to
                            // the current view.
                            m_primaryWindow.HideAllFlyouts();

                            // Progress view requires main menu to be disabled
                            if (mainMenuEnabled)
                            {
                                m_primaryWindow.EnableMainMenu();
                            }
                            else
                            {
                                m_primaryWindow.DisableMainMenu();
                            }

                            m_primaryWindow.CurrentView.Content = viewToLoad;
                            m_primaryWindow.Title = "Stahp It" + windowTitle;
                        }
                        catch (Exception err)
                        {
                            Debug.WriteLine(err.Message);
                            Debug.WriteLine(err.InnerException.Message);
                        }
                    }
                );
            }
        }

        /// <summary>
        /// Handler for when the user requests, via the Statistics View, program-wide deletion of
        /// stats.
        /// </summary>
        /// <param name="sender">
        /// Event origin.
        /// </param>
        private void OnClearAllStatsRequest(object sender)
        {
            Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)delegate ()
                {
                    foreach (var category in m_filteringCategoriesObservable)
                    {
                        category.TotalBytesBlocked = 0;
                        category.TotalRequestsBlocked = 0;                        
                    }

                    m_viewModelDashboard.TotalRequestsBlocked = 0;
                    m_viewModelDashboard.TotalBytesBlocked = 0;
                    m_viewModelDashboard.TotalHtmlElementsRemoved = 0;
                }
            );
        }

        /// <summary>
        /// Inits all the callbacks for WinSparkle, so that when we call for update checks and such,
        /// it has all appropriate callbacks to request app shutdown, restart, etc, to allow for
        /// updating.
        /// </summary>
        private void InitWinsparkle()
        {
            m_winsparkleShutdownCheckCb = new WinSparkle.WinSparkleCanShutdownCheckCallback(WinSparkleCheckIfShutdownOkay);
            m_winsparkleShutdownRequestCb = new WinSparkle.WinSparkleRequestShutdownCallback(WinSparkleRequestsShutdown);
            
            if (Environment.Is64BitProcess)
            {
                AppcastUrl = System.Configuration.ConfigurationManager.AppSettings["Updatex64AppcastURL"];
            }
            else
            {
                AppcastUrl = System.Configuration.ConfigurationManager.AppSettings["Updatex86AppcastURL"];
            }

            WinSparkle.SetCanShutdownCallback(m_winsparkleShutdownCheckCb);
            WinSparkle.SetShutdownRequestCallback(m_winsparkleShutdownRequestCb);
            WinSparkle.SetAppcastUrl(AppcastUrl);
        }

        private void InitEngine()
        {
            m_firewallCheckDelegateCb = new Engine.OnFirewallCheckCallback(FirewallCheck);

            try
            {
                string certPath = AppDomain.CurrentDomain.BaseDirectory + "ca-bundle.crt";
                m_filteringEngine = new Engine(m_firewallCheckDelegateCb, certPath, 0, 0, 8);

                m_filteringEngine.OnElementsBlocked += OnElementsBlocked;
                m_filteringEngine.OnRequestBlocked += OnRequestBlocked;
                m_filteringEngine.OnInfo += OnInfo;
                m_filteringEngine.OnWarning += OnWarning;
                m_filteringEngine.OnError += OnError;

                // Attempt to establish trust with Firefox, if we can.
                EstablishTrustWithFirefox();
            }
            catch (System.Exception e)
            {
                m_logger.Error(e.Message);
            }
        }

        /// <summary>
        /// Brings the main application window into focus for the user and removes it from the tray
        /// if the application icon is in the tray.
        /// </summary>
        public void BringAppToFocus()
        {
            Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)delegate ()
                {
                    if (m_primaryWindow != null)
                    {
                        m_primaryWindow.Show();
                        m_primaryWindow.WindowState = WindowState.Normal;
                    }

                    if (m_trayIcon != null)
                    {
                        m_trayIcon.Visible = false;
                    }
                }
            );
        }

        /// <summary>
        /// Callback for when the Engine generates general information. This can be very verbose,
        /// non-critical information. Usually best to simply ignore.
        /// </summary>
        /// <param name="message">
        /// The informational message.
        /// </param>
        private void OnInfo(string message)
        {
            m_logger.Info(message);
        }

        /// <summary>
        /// Callback for when the Engine issues a warning. A warning is a handled situation that is
        /// non-fatal but is determined to potentially warrant closer inspection.
        /// </summary>
        /// <param name="message">
        /// The warning message.
        /// </param>
        private void OnWarning(string message)
        {
            m_logger.Warn(message);
        }

        /// <summary>
        /// Callback for when the Engine reports a handled error.
        /// </summary>
        /// <param name="message">
        /// The error message.
        /// </param>
        private void OnError(string message)
        {
            m_logger.Error(message);
        }

        /// <summary>
        /// Callback for when the Engine notifies that a HTTP transaction was blocked from
        /// completing.
        /// </summary>
        /// <param name="category">
        /// The category of the filtering rule which caused the block to happen.
        /// </param>
        /// <param name="payloadSizeBlocked">
        /// The total number of bytes blocked from downloading. This is determined in one of two
        /// ways. If the response being blocked is not a chunked response, then the value of the
        /// Content-Length header is supplied for this argument. If the response is a chunked
        /// response, then an estimation is made internally by the Engine. This estimation is a
        /// flat-rate, and is based on the current average total size of a web page.
        /// </param>
        /// <param name="fullRequest">
        /// The full request that was blocked from completing.
        /// </param>
        private void OnRequestBlocked(byte category, uint payloadSizeBlocked, string fullRequest)
        {
            Debug.WriteLine("Total Bytes Blocked: {0}", payloadSizeBlocked);

            Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)delegate ()
                {
                    CategorizedFilteredRequestsViewModel cat = m_filteringCategoriesObservable.Single(item => item.CategoryId == category);

                    // If the blocked size is zero, and the user wants to set this value to "estimate"
                    // the size of the blocked requests's response, then do it.
                    if(payloadSizeBlocked == 0 && m_viewModelSettings.EstimateBlockedChunkedPayloadSize)
                    {
                        payloadSizeBlocked = (uint)m_viewModelSettings.ChunkedPayloadByteEstimate;
                    }

                    m_viewModelDashboard.TotalRequestsBlocked += 1;
                    m_viewModelDashboard.TotalBytesBlocked += payloadSizeBlocked;

                    if (cat != null)
                    {
                        cat.TotalRequestsBlocked += 1;
                        cat.TotalBytesBlocked += payloadSizeBlocked;
                    }
                }
            );
        }

        /// <summary>
        /// Callback for when the Engine notifies that HTML elements have been removed from a HTML
        /// payload based on CSS selectors.
        /// </summary>
        /// <param name="numElementsRemoved">
        /// The total number of HTML elements removed from the payload.
        /// </param>
        /// <param name="fullRequest">
        /// The request that generated the HTML response payload that had the elements removed.
        /// </param>
        private void OnElementsBlocked(uint numElementsRemoved, string fullRequest)
        {
            if (m_viewModelDashboard != null)
            {
                Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    (Action)delegate ()
                    {
                        m_viewModelDashboard.TotalHtmlElementsRemoved += numElementsRemoved;
                    }
                );
            }
        }

        /// <summary>
        /// Initializes the m_trayIcon member, loading the icon graphic and hooking appropriate
        /// handlers to respond to user iteraction requesting to bring the application back out of
        /// the tray.
        /// </summary>
        private void InitTrayIcon()
        {
            m_trayIcon = new System.Windows.Forms.NotifyIcon();

            m_trayIcon.Icon = new System.Drawing.Icon(AppDomain.CurrentDomain.BaseDirectory + "stahpit.ico");

            m_trayIcon.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    BringAppToFocus();
                };
        }

        /// <summary>
        /// Sends the application to the task tray, optionally showing a tooltip explaining that the
        /// application is now hiding away, and how to correctly exit the application, if that's
        /// what the user desires.
        /// </summary>
        /// <param name="showTip">
        /// Bool that determines if a short tooltip explaining that the application is now in the
        /// background, and how to exit if so desired.
        /// </param>
        private void MinimizeToTray(bool showTip = false)
        {
            Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)delegate ()
                {
                    if (m_primaryWindow != null && m_trayIcon != null)
                    {
                        m_trayIcon.Visible = true;
                        m_primaryWindow.Visibility = Visibility.Hidden;

                        if (showTip)
                        {
                            m_trayIcon.ShowBalloonTip(1500, "Still Running", "Stahp It will continue running in the background. If you want exit completely, do so inside the dashboard.", System.Windows.Forms.ToolTipIcon.Info);
                        }
                    }
                }
            );
        }

        /// <summary>
        /// Called by WinSparkle when it wants to check if it is alright to shut down this
        /// application in order to install an update.
        /// </summary>
        /// <returns>
        /// Return zero if a shutdown is not okay at this time, return one if it is okay to shut
        /// down the application immediately after this function returns.
        /// </returns>
        private int WinSparkleCheckIfShutdownOkay()
        {
            // Winsparkle can always shut down. There isn't a reason why, once the user has 
            // requested the update, the application can't be shut down.
            return 1;
        }

        /// <summary>
        /// Called by WinSparkle when it has confirmed that a shutdown is okay and WinSparkle is
        /// ready to shut this application down so it can install a downloaded update.
        /// </summary>
        private void WinSparkleRequestsShutdown()
        {
            Shutdown();
        }

        /// <summary>
        /// Callback for the Engine to determine if the given binary should be filtered or not. The
        /// reason why this callback is named FirewallCheck is because the transparent proxy that the
        /// Engine employs could theoretically allow an application access to the internet even when
        /// the installed firewall has not necessarily given permission for this particular
        /// application to have internet access. This also why users are told this in plain text in
        /// the UI component for enabling/disabling filtering on applications.
        /// </summary>
        /// <param name="binaryFullPath">
        /// </param>
        /// <returns>
        /// True if the supplied binary should have its traffic filtered, false otherwise.
        /// </returns>
        private bool FirewallCheck(string binaryFullPath)
        {
            // Note that returning "false" doesn't mean the app doesn't get to access the internet, it
            // means it won't be diverted through our proxy. So we can also choose which applications we
            // want to filter as well.

            FilteredAppModel famdl;
            if (m_filteredApplicationsTable.TryGetValue(binaryFullPath, out famdl))
            {
                if (famdl != null)
                {
                    return famdl.Filter;
                }
            }

            // No entry for this binary. Create it and push it to the dashboard collection for the user
            // to have.
            try
            {
                famdl = new FilteredAppModel(binaryFullPath, false);

                if (m_filteredApplicationsTable.TryAdd(binaryFullPath, famdl))
                {
                    Current.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Normal,
                        (Action)delegate ()
                        {
                            m_viewModelDashboard.FilteredApplications.Add(new FilteredAppViewModel(famdl));
                        }
                    );
                }
                else
                {
                    m_logger.Error("Error pushing new binary {0} to collection.");
                }
            }
            catch (ArgumentException ae)
            {
                m_logger.Error("Got erreor while constructing new FilteredAppModel: {0}.", ae.Message);
            }

            return false;
        }

        /// <summary>
        /// Serializes the models simply using JSON.
        /// </summary>
        private void SaveProgramState()
        {
            var stateOutputDir = AppDomain.CurrentDomain.BaseDirectory + @"User\";

            if (!Directory.Exists(stateOutputDir))
            {
                Directory.CreateDirectory(stateOutputDir);
            }

            if (m_modelDashboard != null)
            {
                var dashboardStats = JsonConvert.SerializeObject(m_modelDashboard);

                File.WriteAllText(stateOutputDir + "Dashboard.json", dashboardStats);
            }

            if(m_modelSettings != null)
            {
                var settingsData = JsonConvert.SerializeObject(m_modelSettings);

                File.WriteAllText(stateOutputDir + "Settings.json", settingsData);
            }

            if (m_filteredApplicationsTable != null)
            {
                var filteredApplicationsSerialized = JsonConvert.SerializeObject(m_filteredApplicationsTable);

                File.WriteAllText(stateOutputDir + "FilteredApps.json", filteredApplicationsSerialized);
            }

            if (m_filteringCategoriesObservable != null)
            {
                // XXX TODO - This is a bit of a filthy hack. See
                // https://github.com/TechnikEmpire/StahpIt-WPF/issues/1
                //
                // We need to get a list, then push the CategorizedFilteredRequestsViewModel.Category
                // member, which is a FilteringCategory object to a new list and serialize it.
                var asList = m_filteringCategoriesObservable.ToList();

                var filteringCatList = new List<FilteringCategory>();

                foreach (var entry in asList)
                {
                    filteringCatList.Add(entry.Category);
                }

                var filterCategoriesSerialized = JsonConvert.SerializeObject(filteringCatList);

                File.WriteAllText(stateOutputDir + "FilterCategories.json", filterCategoriesSerialized);
            }
        }

        /// <summary>
        /// Loads the models, if present, from their serialized JSON files.
        /// </summary>
        private void LoadProgramState()
        {
            var stateOutputDir = AppDomain.CurrentDomain.BaseDirectory + @"User\";

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(new FilteringCategoryConverter(m_filteringEngine));
            settings.Converters.Add(new DashboardConverter(m_filteringEngine));
            settings.Converters.Add(new FilteredAppConverter());

            // This thread != UI thread.

            Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)delegate ()
                {
                    if (m_filteringCategoriesObservable == null)
                    {
                        m_filteringCategoriesObservable = new ObservableCollection<CategorizedFilteredRequestsViewModel>();
                    }
                }
            );

            // Restore filtering categories.
            if (File.Exists(stateOutputDir + "FilterCategories.json"))
            {
                var filterCatsSerialized = File.ReadAllText(stateOutputDir + "FilterCategories.json");

                var filteringCatsList = JsonConvert.DeserializeObject<List<FilteringCategory>>(filterCatsSerialized, settings);

                foreach (var filteringList in filteringCatsList)
                {
                    // Ensure lists are up to date.
                    try
                    {
                        filteringList.UpdateAndLoad();

                        Current.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Normal,
                            (Action)delegate ()
                            {
                                m_filteringCategoriesObservable.Add(new CategorizedFilteredRequestsViewModel(filteringList));
                            }
                        );
                    }
                    catch (Exception err)
                    {
                        m_logger.Error("Error while updating filtering list: {0}.", err.Message);
                    }
                }
            }

            // Restore dashboard stats.
            if (File.Exists(stateOutputDir + "Dashboard.json"))
            {
                var dashboardSerialized = File.ReadAllText(stateOutputDir + "Dashboard.json");

                m_modelDashboard = JsonConvert.DeserializeObject<DashboardModel>(dashboardSerialized, settings);
                m_viewModelDashboard = new DashboardViewModel(m_modelDashboard);
            }

            // Restore settings.
            if (File.Exists(stateOutputDir + "Settings.json"))
            {
                var settingsSerialized = File.ReadAllText(stateOutputDir + "Settings.json");

                m_modelSettings = JsonConvert.DeserializeObject<SettingsModel>(settingsSerialized, settings);
                m_viewModelSettings = new SettingsViewModel(m_modelSettings);
            }

            // Restore filtered apps.
            if (File.Exists(stateOutputDir + "FilteredApps.json"))
            {
                var filteredAppsSerialized = File.ReadAllText(stateOutputDir + "FilteredApps.json");

                m_filteredApplicationsTable = JsonConvert.DeserializeObject<ConcurrentDictionary<string, FilteredAppModel>>(filteredAppsSerialized, settings);

                foreach (var entry in m_filteredApplicationsTable)
                {
                    m_logger.Info(entry.Key);
                    Current.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Normal,
                        (Action)delegate ()
                        {
                            m_viewModelDashboard.FilteredApplications.Add(new FilteredAppViewModel(entry.Value));
                        }
                    );
                }
            }
        }

        /// <summary>
        /// Detects if FireFox is installed and attempts to install the current CA as a trusted CA
        /// into all discovered FireFox profiles.
        /// </summary>
        private void EstablishTrustWithFirefox()
        {
            string caFilePath = AppDomain.CurrentDomain.BaseDirectory + "stahpitca.pem";
            string defaultFirefoxProfilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            defaultFirefoxProfilesPath += "\\Mozilla\\Firefox\\Profiles";

            var ourCaBytes = m_filteringEngine.GetRootCaPEM();

            File.WriteAllBytes(caFilePath, ourCaBytes);

            if (Directory.Exists(defaultFirefoxProfilesPath))
            {
                string[] firefoxProfileDirs = Directory.GetDirectories(defaultFirefoxProfilesPath);

                foreach (string profileDir in firefoxProfileDirs)
                {
                    // First, delete any old, dead versions of our CA
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    p.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "certutil.exe";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.Arguments += string.Format("-D -n \"Stahp It CA\" -d \"{0}\"", profileDir);
                    p.StartInfo.RedirectStandardError = true;
                    p.Start();

                    string output = p.StandardOutput.ReadToEnd();
                    string errorOutput = p.StandardError.ReadToEnd();

                    m_logger.Info("FF Cleanup Out: {0}", output);

                    if (!string.IsNullOrEmpty(errorOutput) && !string.IsNullOrWhiteSpace(errorOutput))
                    {
                        m_logger.Error("FF Cleanup Error: {0}", errorOutput);
                    }

                    p.WaitForExit();

                    output = string.Empty;
                    errorOutput = string.Empty;

                    // Install the new version.
                    p = new Process();
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    p.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "certutil.exe";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.Arguments += string.Format("-A -n \"Stahp It CA\" -t \"TCu,Cuw,Tuw\" -i \"{0}\" -d \"{1}\"", caFilePath, profileDir);
                    p.StartInfo.RedirectStandardError = true;
                    p.Start();

                    output = p.StandardOutput.ReadToEnd();

                    errorOutput = p.StandardError.ReadToEnd();

                    m_logger.Info("FF Setup Out: {0}", output);

                    if (!string.IsNullOrEmpty(errorOutput) && !string.IsNullOrWhiteSpace(errorOutput))
                    {
                        m_logger.Error("FF Cleanup Error: {0}", errorOutput);
                    }

                    p.WaitForExit();
                }
            }
        }
    }
}