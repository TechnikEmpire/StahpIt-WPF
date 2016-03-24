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
using System.Threading;

namespace Te.StahpIt.Filtering
{
    /// <summary>
    /// The FilteringCategory class represents a category that includes a source of filtering rules,
    /// which is supplied to the filtering Engine. FilteringCategory objects are, once loaded, given
    /// over to the user to enable and disable.
    /// </summary>
    public class FilteringCategory
    {
        /// <summary>
        /// The maximum number of categories that can be set in the filtering Engine is the numeric
        /// limits of an 8 bit integer. As such, every time we construct a new category, we need to,
        /// in a thread safe manner, incremement this static counter and test if we've hit this limit
        /// or not. If not, we'll store the result of the interlocked increment and use that to
        /// represent this category's ID.
        ///
        /// This ID is not really relevant for anything except to serve as a very simple identifier
        /// that the underlying Engine can use for enabling and disabling rules at the request of the
        /// user. What the category means and such, the Engine is absolutely oblivious to. The only
        /// constraint on this mechanism is that the value cannot ever be zero, as zero is reserved
        /// for "do no block."
        /// </summary>
        private static int FilteringCategoryCount = 0;

        /// <summary>
        /// The unique category ID for this instance.
        /// </summary>
        private int m_filteringCategory;

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
                return (byte)m_filteringCategory;
            }
        }

        /// <summary>
        /// Indicates whether or not this category is enabled in the filtering Engine.
        /// </summary>
        public bool Enabled
        {
            get;
            set;
        }

        /// <summary>
        /// The full URL of the source rule list for this category.
        /// </summary>
        public string RuleSourceUrl
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
        public FilteringCategory()
        {
            m_filteringCategory = Interlocked.Increment(ref FilteringCategoryCount);

            if (m_filteringCategory > byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException(string.Format("Number of possible categories exceeded. Maximum number of categories is {0}.", byte.MaxValue.ToString()));
            }
        }
    }
}