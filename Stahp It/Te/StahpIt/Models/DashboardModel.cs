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
using Newtonsoft.Json;
using System;
using Te.HttpFilteringEngine;

namespace Te.StahpIt.Models
{
    public class DashboardModel
    {
        private readonly Engine m_engine;

        public DashboardModel(Engine engine)
        {
            m_engine = engine;
        }

        /// <summary>
        /// Gets or sets whether or not filtering is enabled.
        /// </summary>
        [JsonIgnore]
        public bool FilteringEnabled
        {
            get
            {
                if (m_engine != null)
                {
                    return m_engine.IsRunning;
                }

                return false;
            }

            set
            {
                if (m_engine != null)
                {
                    switch (value)
                    {
                        case true:
                            {
                                if (!m_engine.IsRunning)
                                {
                                    m_engine.Start();
                                }
                            }
                            break;

                        case false:
                            {
                                if (m_engine.IsRunning)
                                {
                                    m_engine.Stop();
                                }
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// The total number of requests blocked according to logged statistics.
        /// </summary>
        public UInt32 TotalRequestsBlocked
        {
            get;
            set;
        }

        /// <summary>
        /// The total number of HTML objects removed by the filtering process.
        /// </summary>
        public UInt32 TotalHtmlElementsRemoved
        {
            get;
            set;
        }

        /// <summary>
        /// Total bytes blocked. This object helps convert easily between B/KB/MB etc and
        /// automatically builds out string representations easily.
        /// </summary>
        public ByteSize TotalDataBlocked
        {
            get;
            set;
        }

        /// <summary>
        /// A string representation of the total amount of data prevented from download due to
        /// request blocking.
        /// </summary>
        [JsonIgnore]
        public string TotalDataBlockedString
        {
            get
            {
                return string.Format("{0} {1}", Math.Round(TotalDataBlocked.LargestWholeNumberValue, 2).ToString(), TotalDataBlocked.LargestWholeNumberSymbol);
            }
        }
    }
}