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

using MahApps.Metro.Controls;
using Te.StahpIt.Views;

namespace Te.StahpIt.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, IViewController
    {
        public MainWindow()
        {
            InitializeComponent();

            Closing += OnWindowClosing;

            m_btnDashboard.Click += ((s, a) => RequestViewChange(View.Dashboard));
            m_btnSettings.Click += ((s, a) => RequestViewChange(View.Settings));
            m_btnStatistics.Click += ((s, a) => RequestViewChange(View.Statistics));
            m_btnEnvImpact.Click += ((s, a) => RequestViewChange(View.Waste));
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Closing window does not exit the application. Rather, a explicit shutdown does this.
            // So, whenever the user closes the window, we need to request that the application be
            // hidden to the tray.
            e.Cancel = true;

            RequestViewChange(View.Tray, true);
        }

        /// <summary>
        /// Event for when a this view requests another view.
        /// </summary>
        public event ViewChangeRequestCallback ViewChangeRequest;

        /// <summary>
        /// Requests a change of view.
        /// </summary>
        /// <param name="view">
        /// The requested view.
        /// </param>
        /// <param name="data">
        /// Optional data for the requested view.
        /// </param>
        protected void RequestViewChange(View view, object data = null)
        {
            if (ViewChangeRequest != null)
            {
                var args = new ViewChangeRequestArgs(view, data);
                ViewChangeRequest(this, args);
            }
        }

        public void EnableMainMenu()
        {
            m_btnDashboard.IsEnabled = true;
            m_btnSettings.IsEnabled = true;
            m_btnStatistics.IsEnabled = true;
            m_btnEnvImpact.IsEnabled = true;
        }

        public void DisableMainMenu()
        {
            m_btnDashboard.IsEnabled = false;
            m_btnSettings.IsEnabled = false;
            m_btnStatistics.IsEnabled = false;
            m_btnEnvImpact.IsEnabled = false;
        }

        /// <summary>
        /// Hides any/all open flyouts.
        /// </summary>
        public void HideAllFlyouts()
        {
            foreach(var flyout in m_flyoutControl.FindChildren<Flyout>())
            {
                flyout.IsOpen = false;
            }
        }
    }
}