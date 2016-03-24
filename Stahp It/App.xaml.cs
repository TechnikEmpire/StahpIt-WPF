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

using NLog;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows;
using Te.HttpFilteringEngine;
using Te.StahpIt.Models;
using Te.StahpIt.Update;
using Te.StahpIt.ViewModels;
using Te.StahpIt.Views;

namespace Te.StahpIt
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly Logger m_logger = LogManager.GetLogger("StahpIt");

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
        private ConcurrentDictionary<byte, CategorizedFilteredRequestsViewModel> m_filteringCategories;

        #region APP_UI_MEMBER_VARS

        /// <summary>
        /// The primary window. That's pretty much at all it's responsible for, just sitting there
        /// looking beautiful. But seriously, its simply a window that we push and pop various views
        /// to at the request of the user.
        /// </summary>
        private MainWindow m_primaryWindow;

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

        private Settings m_viewSettings;

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

        #endregion APP_UI_MEMBER_VARS

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
        /// Entry point for the application.
        /// </summary>
        /// <param name="e">
        /// Arguments passed to the executable at launch.
        /// </param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DoInit();

            bool startMinimized = false;
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i].Equals("/StartMinimized", StringComparison.OrdinalIgnoreCase))
                {
                    startMinimized = true;
                    break;
                }
            }

            if (startMinimized)
            {
                MinimizeToTray(false);
            }
        }

        /// <summary>
        /// Calls all other init methods, which are split up into logical groupings.
        /// </summary>
        private void DoInit()
        {
            //InitWinsparkle();
            InitEngine();
            InitTrayIcon();
            InitViews();

            Exit += OnApplicationShutdown;
        }

        private void OnApplicationShutdown(object sender, ExitEventArgs e)
        {
            if (m_filteringEngine != null && m_filteringEngine.IsRunning)
            {
                m_filteringEngine.Stop();
            }
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

            // This collection is initialized here because it has a direct connection to the UI.
            m_filteredApplicationsTable = new ConcurrentDictionary<string, FilteredAppModel>();

            m_primaryWindow = new MainWindow();
            m_primaryWindow.Show();

            m_viewProgressWait = new ProgressWait();

            m_modelDashboard = new DashboardModel(m_filteringEngine);
            m_viewModelDashboard = new DashboardViewModel(m_modelDashboard);
            m_viewDashboard = new Dashboard(m_viewModelDashboard);

            m_modelStatistics = new StatisticsModel();
            m_viewModelStatistics = new StatisticsViewModel(m_modelStatistics);
            m_viewStatistics = new Statistics(m_viewModelStatistics);

            m_viewSettings = new Settings();

            m_primaryWindow.ViewChangeRequest += OnViewRequestChange;
            m_viewDashboard.ViewChangeRequest += OnViewRequestChange;
            m_viewStatistics.ViewChangeRequest += OnViewRequestChange;
            //m_viewSettings.ViewChangeRequest += OnViewRequestChange;

            OnViewRequestChange(this, new ViewChangeRequestArgs(View.Dashboard));
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
        private void OnViewRequestChange(object sender, ViewChangeRequestArgs e)
        {
            BaseView viewToLoad = null;
            string windowTitle = string.Empty;

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
            }

            if (viewToLoad != null)
            {
                Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    (Action)delegate ()
                    {
                        Debug.WriteLine("Setting view.");
                        m_primaryWindow.CurrentView.Content = viewToLoad;
                        m_primaryWindow.Title = "Stahp It" + windowTitle;
                    }
                );
            }
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

            // Hardcoded app update URL strings, because that's how we roll.
            if (Environment.Is64BitProcess)
            {
                AppcastUrl = "https://raw.githubusercontent.com/TechnikEmpire/StahpIt-WPF/master/update/winx64/update.xml";
            }
            else
            {
                AppcastUrl = "https://raw.githubusercontent.com/TechnikEmpire/StahpIt-WPF/master/update/winx86/update.xml";
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
            }
            catch (System.Exception e)
            {
                m_logger.Error(e.Message);
            }

            m_filteringCategories = new ConcurrentDictionary<byte, CategorizedFilteredRequestsViewModel>();
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
            CategorizedFilteredRequestsViewModel cat = null;

            if (m_filteringCategories != null)
            {
                m_filteringCategories.TryGetValue(category, out cat);
            }

            if (m_viewModelDashboard != null)
            {
                Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    (Action)delegate ()
                    {
                        m_viewModelDashboard.TotalRequestsBlocked += 1;

                        if (cat != null)
                        {
                            cat.TotalRequestsBlocked += 1;
                        }
                    }
                );
            }
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
                    Current.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Normal,
                        (Action)delegate ()
                        {
                            if (m_primaryWindow != null)
                            {
                                m_primaryWindow.Show();
                                m_primaryWindow.WindowState = WindowState.Normal;
                                m_trayIcon.Visible = false;
                            }
                        }
                    );
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
                        m_primaryWindow.Visibility = System.Windows.Visibility.Hidden;

                        if (showTip)
                        {
                            m_trayIcon.ShowBalloonTip(1500, "Still Running", "Stahp It will continue running in the background. If you want exit completely, do so inside the dashboard.", System.Windows.Forms.ToolTipIcon.Info);
                        }
                    }
                }
            );
        }

        private int WinSparkleCheckIfShutdownOkay()
        {
            return 0;
        }

        private void WinSparkleRequestsShutdown()
        {
        }

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
                    Current.Dispatcher.Invoke(
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

            //m_logger.Info(string.Format("Denying binary {0} internet access.", binaryFullPath));
            return false;
        }
    }
}