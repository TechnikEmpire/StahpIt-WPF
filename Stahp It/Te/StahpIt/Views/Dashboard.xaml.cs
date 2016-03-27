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

using Te.StahpIt.ViewModels;
using System.Windows.Controls;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System;
using System.Windows;

namespace Te.StahpIt.Views
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : BaseView
    {

        /// <summary>
        /// The view model.
        /// </summary>
        private DashboardViewModel m_viewModel;

        /// <summary>
        /// Constructs a new Dashboard view.
        /// </summary>
        /// <param name="viewModel">
        /// The view model to supply to the view's data context for binding's sake.
        /// </param>
        /// <exception cref="ArgumentException">
        /// In the event that the supplied view model is null, will throw ArgumentException.
        /// </exception>
        public Dashboard(DashboardViewModel viewModel)
        {
            InitializeComponent();

            m_viewModel = viewModel;

            if(m_viewModel == null)
            {
                throw new ArgumentException("Expected valid DashboardViewModel instance.");
            }
            
            DataContext = m_viewModel;

            //THIS IS IMPORTANT, so that the list does not add an extra blank line.
            dataGridFilterApps.CanUserAddRows = false;

            btnFilterToggle.Click += OnBtnFilterToggleClicked;

            exitButton.Click += OnExitClicked;
        }

        private async void OnExitClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            MetroDialogSettings mds = new MetroDialogSettings();
            mds.AffirmativeButtonText = "Yes";
            mds.NegativeButtonText = "No";
            MetroWindow parentWindow = this.TryFindParent<MetroWindow>();

            if(parentWindow != null)
            {
                var result = await DialogManager.ShowMessageAsync(parentWindow, "Exit Stahp It", "Are you sure you would like to fully exit? You will no longer have filtering.", MessageDialogStyle.AffirmativeAndNegative, mds);

                if (result == MessageDialogResult.Affirmative)
                {
                    RequestViewChange(View.ProgressWait, "Shutting Down. Please Wait.");
                                        
                    Application.Current.Shutdown();
                }
            }            
        }

        private async void OnBtnFilterToggleClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            string message = m_viewModel.FilteringEnabled ? "Stopping filter. Please Wait." : "Starting Filer. Please Wait.";

            RequestViewChange(View.ProgressWait, message);

            await Task.Run(() => m_viewModel.FilteringEnabled = !m_viewModel.FilteringEnabled);

            RequestViewChange(View.Dashboard);
        }
    }
}