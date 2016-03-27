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
using Te.StahpIt.Filtering;

namespace Te.StahpIt.ViewModels
{
    /// <summary>
    /// Stores statistics about blocked/filtered requests for a specified category.
    /// </summary>
    public class CategorizedFilteredRequestsViewModel : BaseViewModel, IDisposable
    {

        /// <summary>
        /// The underlying FilteringCategory instance to and from which values are served or set.
        /// </summary>
        private FilteringCategory m_category;

        /// <summary>
        /// The unique ID of the category.
        /// </summary>
        public byte CategoryId
        {
            get
            {
                if (m_category != null)
                {
                    return m_category.CategoryId;
                }

                return 0;
            }
        }

        /// <summary>
        /// The category that the requests were blocked/filtered by.
        /// </summary>
        public string CategoryName
        {
            get
            {
                if(m_category != null)
                {
                    return m_category.CategoryName;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// The total bytes blocked for this category.
        /// </summary>
        public double TotalKilobytesBlocked
        {
            get
            {
                if (m_category != null)
                {
                    return m_category.TotalBytesBlocked.KiloBytes;
                }

                return 0d;
            }

            set
            {
                if (m_category != null)
                {
                    m_category.TotalBytesBlocked = new ByteSize().AddKiloBytes(value);
                    PropertyHasChanged("TotalKilobytesBlocked");
                }
            }
        }

        /// <summary>
        /// The total number of requests blocked for this category.
        /// </summary>
        public UInt64 TotalRequestsBlocked
        {
            get
            {
                if (m_category != null)
                {
                    return m_category.TotalRequestsBlocked;
                }

                return 0;
            }

            set
            {
                if (m_category != null && m_category.TotalRequestsBlocked != value)
                {
                    m_category.TotalRequestsBlocked = value;
                    PropertyHasChanged("TotalRequestsBlocked");
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the category is enabled or not.
        /// </summary>
        public bool Enabled
        {
            get
            {
                if (m_category != null)
                {
                    return m_category.Enabled;
                }

                return false;
            }

            set
            {
                if (m_category != null && m_category.Enabled != value)
                {
                    m_category.Enabled = value;
                    PropertyHasChanged("Enabled");
                }
            }
        }

        public UInt32 TotalRulesLoaded
        {
            get
            {
                if (m_category != null)
                {
                    return m_category.TotalRulesLoaded;
                }

                return 0;
            }
        }

        public UInt32 TotalRulesFailed
        {
            get
            {
                if (m_category != null)
                {
                    return m_category.TotalFailedRules;
                }

                return 0;
            }
        }

        public string RuleListURL
        {
            get
            {
                if (m_category != null)
                {
                    return m_category.RuleSource.OriginalString;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <exception cref="ArgumentException">
        /// </exception>
        public CategorizedFilteredRequestsViewModel(FilteringCategory category)
        {
            m_category = category;

            if(m_category == null)
            {
                throw new ArgumentException("Expected valid FilteringCategory instance.");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_category.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {           
            Dispose(true);            
        }
        #endregion
    }
}