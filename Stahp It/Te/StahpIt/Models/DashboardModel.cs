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
using System.Threading;
using Te.HttpFilteringEngine;

namespace Te.StahpIt.Models
{
    public class DashboardModel : IDisposable
    {
        private readonly Engine m_engine;

        /// <summary>
        /// Internal storage for total requests blocked. Public access controlled by Interlocked.
        /// </summary>
        private long m_totalRequestsBlocked;

        /// <summary>
        /// Internal storage for total HTML elements removed. Public access controlled by
        /// Interlocked.
        /// </summary>
        private long m_totalHtmlElementsRemoved;

        /// <summary>
        /// Internal storage for total data blocked. Public access controlled by
        /// ReaderWriterLockSlim.
        /// </summary>
        private ByteSize m_totalDataBlocked;

        /// <summary>
        /// Read/write access control for total data blocked;
        /// </summary>
        private ReaderWriterLockSlim m_totalDataBlockedLock = new ReaderWriterLockSlim();

        public DashboardModel(Engine engine)
        {
            m_engine = engine;
        }

        /// <summary>
        /// Gets or sets whether or not filtering is enabled. Thread safety is enforced internally
        /// within the Engine. All methods accessed here are safe.
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
        public long TotalRequestsBlocked
        {
            get
            {
                return Interlocked.Read(ref m_totalRequestsBlocked);
            }

            set
            {
                Interlocked.Exchange(ref m_totalRequestsBlocked, value);
            }
        }

        /// <summary>
        /// The total number of HTML objects removed by the filtering process.
        /// </summary>
        public long TotalHtmlElementsRemoved
        {
            get
            {
                return Interlocked.Read(ref m_totalHtmlElementsRemoved);
            }

            set
            {
                Interlocked.Exchange(ref m_totalHtmlElementsRemoved, value);
            }
        }

        /// <summary>
        /// Total bytes blocked. This object helps convert easily between B/KB/MB etc and
        /// automatically builds out string representations easily.
        /// </summary>
        public ByteSize TotalDataBlocked
        {
            get
            {
                m_totalDataBlockedLock.EnterReadLock();

                try
                {
                    return m_totalDataBlocked;
                }
                finally
                {
                    m_totalDataBlockedLock.ExitReadLock();
                }
            }

            set
            {
                m_totalDataBlockedLock.EnterWriteLock();

                try
                {
                    m_totalDataBlocked = value;
                }
                finally
                {
                    m_totalDataBlockedLock.ExitWriteLock();
                }
            }
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

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if(m_totalDataBlockedLock != null)
                    {
                        m_totalDataBlockedLock.Dispose();
                    }
                }              

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}