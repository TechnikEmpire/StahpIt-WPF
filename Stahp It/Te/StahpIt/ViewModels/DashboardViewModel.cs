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

using ByteSizeLib;
using System;
using System.ComponentModel;
using System.Windows.Media;
using Te.StahpIt.Models;

namespace Te.StahpIt.ViewModels
{
    /// <summary>
    /// Serves as the ViewModel for the Dashboard view.
    /// </summary>
    public class DashboardViewModel : BaseViewModel
    {
        /// <summary>
        /// The underlying DashboardModel instance to and from which values are served or set.
        /// </summary>
        private DashboardModel m_model;

        /// <summary>
        /// This collection provides the ViewModels for the DataGrid control on the Dashboard, which
        /// dynamically serves up a sub-view of applications that were detected as potential
        /// filtering candidates. The user can then choose whether or not to filter these
        /// applications inline.
        /// </summary>
        public BindingList<FilteredAppViewModel> FilteredApplications
        {
            get;
            set;
        }

        /// <summary>
        /// The total number of requests blocked globally. That is to say, the total number spanning
        /// all categories.
        /// </summary>
        public UInt32 TotalRequestsBlocked
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.TotalRequestsBlocked;
                }

                return 0;
            }

            set
            {
                if (m_model != null && value != m_model.TotalRequestsBlocked)
                {
                    m_model.TotalRequestsBlocked = value;

                    PropertyHasChanged("TotalRequestsBlocked");
                }
            }
        }

        /// <summary>
        /// The total number of HTML removes globally, spanning all categories.
        /// </summary>
        public UInt32 TotalHtmlElementsRemoved
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.TotalHtmlElementsRemoved;
                }

                return 0;
            }

            set
            {
                if (m_model != null && value != m_model.TotalHtmlElementsRemoved)
                {
                    m_model.TotalHtmlElementsRemoved = value;

                    PropertyHasChanged("TotalHtmlElementsRemoved");
                }
            }
        }

        /// <summary>
        /// A string which represents the total amount of data that has been blocked, to be
        /// presented. This string is dynamically adjusted to show the values, with the value sign,
        /// according to the largest whole number value. Examples of this value are "10.1KB, 11b,
        /// 14MB" etc.
        /// </summary>
        public string TotalDataBlockedString
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.TotalDataBlockedString;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Total bytes blocked all-time, all categories.
        /// </summary>
        public double TotalBytesBlocked
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.TotalDataBlocked.Bytes;
                }

                return 0;
            }

            set
            {
                if (m_model != null)
                {
                    m_model.TotalDataBlocked = ByteSize.FromBytes(value);

                    PropertyHasChanged("TotalBytesBlocked");
                    PropertyHasChanged("TotalDataBlockedString");
                }
            }
        }

        /// <summary>
        /// Gets the color of the filter control toggle button based on the current data state.
        /// </summary>
        public Color FilteringButtonColor
        {
            get
            {
                if (m_model != null)
                {
                    if (m_model.FilteringEnabled)
                    {
                        return Colors.LimeGreen;
                    }
                }

                return Colors.Salmon;
            }
        }

        /// <summary>
        /// Gets a text status of the filtering system based on current model data.
        /// </summary>
        public string FilteringStatus
        {
            get
            {
                if (m_model != null)
                {
                    if (m_model.FilteringEnabled)
                    {
                        return "ON";
                    }
                    else
                    {
                        return "OFF";
                    }
                }

                return "OFF";
            }
        }

        /// <summary>
        /// Gets or sets whether filtering is enabled.
        /// </summary>
        public bool FilteringEnabled
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.FilteringEnabled;
                }

                return false;
            }

            set
            {
                if (m_model != null && value != m_model.FilteringEnabled)
                {
                    m_model.FilteringEnabled = value;

                    // Push that this property has changed.
                    PropertyHasChanged("FilteringEnabled");

                    // Also push that the filtering button color has changed as well so this will update.
                    PropertyHasChanged("FilteringButtonColor");

                    // Also push FilteringStatus as changed so that the text will update.
                    PropertyHasChanged("FilteringStatus");
                }
            }
        }

        /// <summary>
        /// Constructs a new DashboardViewModel instance.
        /// </summary>
        /// <param name="model">
        /// The underlying DashboardModel from which to derive and modify state.
        /// </param>
        /// <exception cref="ArgumentException">
        /// In the event that the model parameter is null, will throw ArgumentException.
        /// </exception>
        public DashboardViewModel(DashboardModel model)
        {
            m_model = model;

            if (m_model == null)
            {
                throw new ArgumentException("Expected valid DashboardModel instance.");
            }

            FilteredApplications = new BindingList<FilteredAppViewModel>();
        }
    }
}