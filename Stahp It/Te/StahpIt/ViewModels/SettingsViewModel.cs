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
using System.Collections.ObjectModel;
using Te.StahpIt.Models;

namespace Te.StahpIt.ViewModels
{

    /// <summary>
    /// View model for the Settings view.
    /// </summary>
    public class SettingsViewModel : BaseViewModel
    {

        /// <summary>
        /// The model.
        /// </summary>
        private SettingsModel m_model;

        /// <summary>
        /// Shared instance of all filtering categories in an ObservableCollection. This collection
        /// is bound to subview in many different views in the overall application.
        /// </summary>
        public ObservableCollection<CategorizedFilteredRequestsViewModel> FilterCategories
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.FilterCategories;
                }

                return null;
            }
        }

        /// <summary>
        /// Whether or not the application should run at user logon.
        /// </summary>
        public bool RunAtStartup
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.RunAtStartup;
                }

                return false;
            }

            set
            {
                if (m_model != null && value != m_model.RunAtStartup)
                {
                    m_model.RunAtStartup = value;

                    PropertyHasChanged("RunAtStartup");
                }
            }
        }

        /// <summary>
        /// Whether or not to estimate the size of blocked requests that had a chunked payload as a
        /// response.
        /// </summary>
        public bool EstimateBlockedChunkedPayloadSize
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.EstimateBlockedChunkedPayloadSize;
                }

                return false;
            }

            set
            {
                if (m_model != null && value != m_model.EstimateBlockedChunkedPayloadSize)
                {
                    m_model.EstimateBlockedChunkedPayloadSize = value;

                    PropertyHasChanged("EstimateBlockedChunkedPayloadSize");
                }
            }
        }

        /// <summary>
        /// A string representation of the total amount of data that the user wants to estimate a
        /// blocked request to be.
        /// </summary>
        public string EstimateFriendlyString
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.EstimateFriendlyString;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// The total bytes estimated for requests blocked that generated a chunked response, in
        /// string format.
        /// </summary>
        public string ChunkedPayloadEstimateString
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.ChunkedPayloadEstimateString;
                }

                return string.Empty;
            }

            set
            {
                if (m_model != null)
                {
                    m_model.ChunkedPayloadEstimateString = value;

                    PropertyHasChanged("ChunkedPayloadEstimateString");                    

                    // Also notify that EstimateFriendlyString has changed, because this will give
                    // the user a nice string representation of just how "big" the size 
                    // they've entered for blocked requests is.
                    PropertyHasChanged("EstimateFriendlyString");
                }
            }
        }

        /// <summary>
        /// The user's estimate of the cost of bandwidth per GB, in string format.
        /// </summary>
        public string UserFinancialCostPerGbString
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.UserEstimatedFinancialCostPerGbString;
                }

                return string.Empty;
            }

            set
            {
                if (m_model != null)
                {
                    m_model.UserEstimatedFinancialCostPerGbString = value;

                    PropertyHasChanged("UserFinancialCostPerGbString");

                    // Also trigger update for decimal version.
                    PropertyHasChanged("UserFinancialCostPerGb");
                }
            }
        }

        /// <summary>
        /// The user's estimate of the cost of bandwidth per GB, in decimal.
        /// </summary>
        public decimal UserFinancialCostPerGb
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.UserEstimatedFinancialCostPerGb;
                }

                return 0;
            }
        }

        /// <summary>
        /// The user's estimate of the energy cost of bandwidth per GB, in string format.
        /// </summary>
        public string UserKwhCostPerGbString
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.UserEstimatedKwhCostPerGbString;
                }

                return string.Empty;
            }

            set
            {
                if (m_model != null)
                {
                    m_model.UserEstimatedKwhCostPerGbString = value;

                    PropertyHasChanged("UserKwhCostPerGbString");

                    // Also trigger update for integer version.
                    PropertyHasChanged("UserKwhCostPerGb");
                }
            }
        }


        /// <summary>
        /// The user's estimate of the energy cost of bandwidth per GB, in integer format.
        /// </summary>
        public long UserKwhCostPerGb
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.UserEstimatedKwhCostPerGb;
                }

                return 0;
            }
        }

        /// <summary>
        /// The total bytes estimated for requests blocked that generated a chunked response.
        /// </summary>
        public long ChunkedPayloadByteEstimate
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.ChunkedPayloadByteEstimate;
                }

                return 0;
            }
        }

        /// <summary>
        /// Constructs a new Settings View Model object.
        /// </summary>
        /// <param name="model">
        /// The settings model.
        /// </param>
        /// <exception cref="ArgumentException">
        /// In the event that the supplied SettingsModel is null, will throw ArgumentException.
        /// </exception>
        public SettingsViewModel(SettingsModel model)
        {
            m_model = model;

            if (m_model == null)
            {
                throw new ArgumentException("Expected valid instance of SettingsModel");
            }
        }
    }
}