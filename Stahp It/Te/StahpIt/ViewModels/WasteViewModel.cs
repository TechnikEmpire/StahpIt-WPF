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

namespace Te.StahpIt.ViewModels
{
    /// <summary>
    /// The WasteViewModel simply holds references to existing ViewModels that provide existing data.
    /// This data is recycled into the WasteViewModel View, because its sole purpose is to bring this
    /// other data together and perform some simple math.
    /// </summary>
    public class WasteViewModel : BaseViewModel
    {
        /// <summary>
        /// A reference to the settings view model.
        /// </summary>
        private SettingsViewModel m_settingsViewModel;

        /// <summary>
        /// A reference to the statistics view model.
        /// </summary>
        private DashboardViewModel m_dashboardViewModel;

        /// <summary>
        /// Friendly string representation of the largest whole value of blocked data.
        /// </summary>
        public string TotalDataBlockedString
        {
            get
            {
                if (m_dashboardViewModel != null)
                {
                    return m_dashboardViewModel.TotalDataBlockedString;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Total number of requests blocked.
        /// </summary>
        public long TotalRequestsBlocked
        {
            get
            {
                if (m_dashboardViewModel != null)
                {
                    return m_dashboardViewModel.TotalRequestsBlocked;
                }

                return 0;
            }
        }

        /// <summary>
        /// String representation of the total, user estimated financial cost of content that was
        /// blocked.
        /// </summary>
        public string TotalFinancialCost
        {
            get
            {
                decimal costPerGb = 0;
                double totalGbUsed = 0;

                if (m_settingsViewModel != null)
                {
                    costPerGb = m_settingsViewModel.UserFinancialCostPerGb;
                }

                if (m_dashboardViewModel != null)
                {
                    totalGbUsed = ByteSizeLib.ByteSize.FromBytes(m_dashboardViewModel.TotalBytesBlocked).GigaBytes;
                }

                return string.Format("${0}", Math.Round(((double)costPerGb * totalGbUsed), 2));
            }
        }

        /// <summary>
        /// String representation of the total, user estimated energy cost of content that was
        /// blocked.
        /// </summary>
        public string TotalEnergyCost
        {
            get
            {
                long asLong = 0;
                double totalGbUsed = 0;

                if (m_settingsViewModel != null)
                {
                    asLong = m_settingsViewModel.UserKwhCostPerGb;
                }

                if (m_dashboardViewModel != null)
                {
                    totalGbUsed = ByteSizeLib.ByteSize.FromBytes(m_dashboardViewModel.TotalBytesBlocked).GigaBytes;
                }

                return string.Format("{0} kWh", Math.Round(totalGbUsed * asLong, 2));
            }
        }

        /// <summary>
        /// Constructs a new WasteViewModel instance.
        /// </summary>
        /// <param name="settingsViewModel">
        /// The SettingsViewModel from which to receive updated user input about waste calculations.
        /// </param>
        /// <param name="dashboardViewModel">
        /// The m_dashboardViewModelViewModel from which to receive updated blocking statistics.
        /// </param>
        /// <exception cref="ArgumentException">
        /// In the event that the arguments provided are null, will throw ArgumentException.
        /// </exception>
        public WasteViewModel(SettingsViewModel settingsViewModel, DashboardViewModel dashboardViewModel)
        {
            m_settingsViewModel = settingsViewModel;
            m_dashboardViewModel = dashboardViewModel;

            if (m_settingsViewModel == null)
            {
                throw new ArgumentException("Expected valid instance of SettingsViewModel");
            }

            if (m_dashboardViewModel == null)
            {
                throw new ArgumentException("Expected valid instance of m_dashboardViewModelViewModel");
            }

            // Simply subscribe to the property changed events of these view models, and replicate
            // the same change event for mirrored data members.
            m_dashboardViewModel.PropertyChanged += OnLinkedPropertiesChanged;
            m_settingsViewModel.PropertyChanged += OnLinkedPropertiesChanged;
        }

        /// <summary>
        /// Handler for whenever properties in other linked view models change. All we're really
        /// interested in doing here is re-raising the same events for arguments that we mirror
        /// within this view model. This makes it so that we don't need a model behind this view
        /// model, we simply repeat data that exists elsewhere.
        /// </summary>
        /// <param name="sender">
        /// Object raising the event.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        private void OnLinkedPropertiesChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(m_dashboardViewModel.TotalRequestsBlocked):
                case nameof(m_dashboardViewModel.TotalDataBlockedString):
                case nameof(m_settingsViewModel.UserFinancialCostPerGbString):
                case nameof(m_settingsViewModel.UserKwhCostPerGbString):
                    {
                        PropertyHasChanged("TotalRequestsBlocked");
                        PropertyHasChanged("TotalDataBlockedString");
                        PropertyHasChanged("TotalFinancialCost");
                        PropertyHasChanged("TotalEnergyCost");
                    }
                    break;
            }
        }
    }
}