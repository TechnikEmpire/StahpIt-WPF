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

using System;
using System.Windows;
using Te.StahpIt.ViewModels;

namespace Te.StahpIt.Views
{
    /// <summary>
    /// Interaction logic for Waste.xaml
    /// </summary>
    public partial class Waste : BaseView
    {
        private WasteViewModel m_viewModel;

        /// <summary>
        /// Constructs a new Waste view with the corresponding view model.
        /// </summary>
        /// <param name="viewModel">
        /// The view model for this view.
        /// </param>
        /// <exception cref="ArgumentException">
        /// In the event that the supplied WasteViewModel is null, will throw ArgumentException.
        /// </exception>
        public Waste(WasteViewModel viewModel)
        {
            InitializeComponent();

            m_viewModel = viewModel;

            if (m_viewModel == null)
            {
                throw new ArgumentException("Expected valid WasteViewModel instance.");
            }

            DataContext = m_viewModel;
        }

        /// <summary>
        /// Handler for when the hyperlink pointing to the settings view is clicked.
        /// </summary>
        /// <param name="sender">
        /// Object raising the event.
        /// </param>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        private void OnSettingsLinkClicked(object sender, RoutedEventArgs e)
        {
            RequestViewChange(View.Settings);
        }
    }
}