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
using MahApps.Metro.Controls.Dialogs;
using System;
using Te.StahpIt.ViewModels;

namespace Te.StahpIt.Views
{
    /// <summary>
    /// Interaction logic for Statistics.xaml
    /// </summary>
    public partial class Statistics : BaseView
    {
        /// <summary>
        /// Delegate for receiving requests to clear statistics. 
        /// </summary>
        /// <param name="sender">
        /// Event Source.
        /// </param>
        public delegate void ClearStatisticsRequest(object sender);

        /// <summary>
        /// Event for requesting that program wide statistics be reset. This is a quick and dirty
        /// solution for needing to be able to reset stats program-wide. XXX TODO ViewModels could be
        /// improved to eliminate the need for such an event.
        /// </summary>
        public event ClearStatisticsRequest ClearStatisticsRequested;

        /// <summary>
        /// The view model.
        /// </summary>
        private StatisticsViewModel m_viewModel;

        /// <summary>
        /// Constructs a new Dashboard view.
        /// </summary>
        /// <param name="viewModel">
        /// The view model to supply to the view's data context for binding's sake.
        /// </param>
        /// <exception cref="ArgumentException">
        /// In the event that the supplied view model is null, will throw ArgumentException.
        /// </exception>
        public Statistics(StatisticsViewModel viewModel)
        {
            InitializeComponent();

            m_viewModel = viewModel;

            if (m_viewModel == null)
            {
                throw new ArgumentException("Expected valid StatisticsViewModel instance.");
            }

            DataContext = m_viewModel;

            m_btnClearStats.Click += OnClearStatsClicked;
        }

        /// <summary>
        /// Handler for when the user clicks the clear stats button.
        /// </summary>
        /// <param name="sender">
        /// Event origin.
        /// </param>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        private async void OnClearStatsClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            // Verify with the user whether the delete should really happen or not.
            MetroDialogSettings mds = new MetroDialogSettings();
            mds.AffirmativeButtonText = "Yes";
            mds.NegativeButtonText = "No";
            MetroWindow parentWindow = this.TryFindParent<MetroWindow>();

            if (parentWindow != null)
            {
                var result = await DialogManager.ShowMessageAsync(parentWindow, "Clear All Stats?", "Are you sure you would like to clear all stats? This cannot be undone.", MessageDialogStyle.AffirmativeAndNegative, mds);

                if (result == MessageDialogResult.Affirmative)
                {
                    if (ClearStatisticsRequested != null)
                    {
                        ClearStatisticsRequested(this);
                    }
                }
            }            
        }
    }
}