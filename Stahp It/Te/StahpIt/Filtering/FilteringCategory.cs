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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Te.HttpFilteringEngine;

namespace Te.StahpIt.Filtering
{
    /// <summary>
    /// The FilteringCategory class represents a category that includes a source of filtering rules,
    /// which is supplied to the filtering Engine. FilteringCategory objects are, once loaded, given
    /// over to the user to enable and disable.
    /// </summary>
    public class FilteringCategory : IDisposable
    {
        /// <summary>
        /// The maximum number of categories that can be set in the filtering Engine is the numeric
        /// limits of an unsigned 8 bit integer. As such, every time we construct a new category, we
        /// need to "borrow" a unique ID from the total number of possible categories. Then we a
        /// category is destroyed, it needs to be put back. We use a ConcurrentBag for this.
        ///
        /// This ID is not really relevant for anything except to serve as a very simple identifier
        /// that the underlying Engine can use for enabling and disabling rules at the request of the
        /// user. What the category means and such, the Engine is absolutely oblivious to. The only
        /// constraint on this mechanism is that the value cannot ever be zero, as zero is reserved
        /// for "do no block."
        /// </summary>
        private static ConcurrentBag<byte> AvailableFilteringCategories;

        static FilteringCategory()
        {
            AvailableFilteringCategories = new ConcurrentBag<byte>();

            var possibleValues = Enumerable.Range(1, (byte.MaxValue - 1));
            foreach(var entry in possibleValues)
            {
                AvailableFilteringCategories.Add((byte)entry);
            }            
        }

        /// <summary>
        /// Requires reference to the Engine, so that Enable/Disable can be used.
        /// </summary>
        private readonly Engine m_engine;

        /// <summary>
        /// The unique category ID for this instance.
        /// </summary>
        private byte m_filteringCategory;

        /// <summary>
        /// The name given for the filtering category.
        /// </summary>
        public string CategoryName
        {
            get;
            set;
        }

        /// <summary>
        /// The unique ID for the category.
        /// </summary>
        public byte CategoryId
        {
            get
            {
                return m_filteringCategory;
            }
        }

        /// <summary>
        /// Indicates whether or not this category is enabled in the filtering Engine.
        /// </summary>
        public bool Enabled
        {
            get
            {
                if(m_engine != null)
                {
                    return m_engine.IsCategoryEnabled(CategoryId);
                }

                return false;
            }

            set
            {
                if(m_engine != null)
                {
                    m_engine.SetCategoryEnabled(CategoryId, value);
                }
            }
        }

        /// <summary>
        /// The full URI of the source rule list for this category.
        /// </summary>
        public Uri RuleSource
        {
            get;
            set;
        }

        /// <summary>
        /// The total bytes blocked for this category.
        /// </summary>
        public ByteSize TotalBytesBlocked
        {
            get;
            set;
        }

        /// <summary>
        /// The total number of requests blocked for this category.
        /// </summary>
        public UInt64 TotalRequestsBlocked
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates the total number of rules that were successfully loaded from this category's
        /// source URL.
        /// </summary>
        public UInt32 TotalRulesLoaded
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates the total number of rules that failed to be loaded from this category's source
        /// URL. These are typically rules that were not correctly formatted as far as the Engine is
        /// concerned.
        /// </summary>
        public UInt32 TotalFailedRules
        {
            get;
            set;
        }

        /// <summary>
        /// Constructs a new filtering category. All members must be manually set, barring the ID.
        /// This is automatically generated within the constructor.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The maximum number of categories is equal to the numeric limits of an 8 bit integer.
        /// Category ID are automatically generated from a static, Interlocked controlled count, then
        /// the result of the interlocked incremement is tested against this numeric limit. In other
        /// words, if more categories than this limit permits are constructed, this constructor will
        /// throw.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// In the event that the supplied Engine reference is null, will throw ArgumentException.
        /// </exception>
        public FilteringCategory(Engine engine)
        {
            m_engine = engine;

            if(m_engine == null)
            {
                throw new ArgumentException("Expected valid Engine instance.");
            }

            if(!AvailableFilteringCategories.TryTake(out m_filteringCategory))
            {
                throw new ArgumentOutOfRangeException(string.Format("Number of possible categories exceeded. Maximum number of categories is {0}.", byte.MaxValue.ToString()));
            }           
        }

        /// <summary>
        /// We need a destructor aka finalizer in order to decrement the static count, and also to
        /// force the resources for a loaded list to be unloaded.
        /// </summary>
        ~FilteringCategory()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);            
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Put the category ID that we took back into the collection.
                    AvailableFilteringCategories.Add(m_filteringCategory);
                }

                if (m_engine != null)
                {
                    m_engine.UnloadAllRulesForCategory(m_filteringCategory);
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {            
            Dispose(true);
            
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}