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

using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using Te.StahpIt.ViewModels;

namespace Te.StahpIt.Models
{

    /// <summary>
    /// The SettingsModel class holds all data related to user configurable application settings.
    /// </summary>
    public class SettingsModel : IDisposable
    {
        /// <summary>
        /// Holds the current value that the user estimates, in bytes, for the size of blocked
        /// requests that generate a chunked response.
        /// </summary>
        private long m_blockedChunkedByteEstimate;

        /// <summary>
        /// Holds the current value that the user estimates, in bytes, for the size of blocked
        /// requests that generate a chunked response. Represented as a string.
        /// </summary>
        private string m_blockedChunkedByteEstimateString;

        /// <summary>
        /// Read/write access control for user estimated chunked content size string.
        /// </summary>
        private ReaderWriterLockSlim m_userEstBlockedChunkedStringLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Represents the user's cost of bandwidth per GB.
        /// </summary>
        private string m_userEstimatedFinancialCostPerGbString;

        /// <summary>
        /// Read/write access control for the user's cost of bandwidth per GB string.
        /// </summary>
        private ReaderWriterLockSlim m_userEstFinancialStringLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Represents the user's estimated energy cost in kWh for bandwidth per GB.
        /// </summary>
        private string m_userEstimatedKwhCostPerGbString;

        /// <summary>
        /// Read/write access control for the user's estimated energy cost in kWh for bandwidth per
        /// GB string.
        /// </summary>
        private ReaderWriterLockSlim m_userEstPowerStringLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Decimal representation of user specified cost of bandwidth per GB.
        /// </summary>
        private decimal m_userEstimatedFinancialCostPerGb;

        /// <summary>
        /// Read/Write access control for the decimal representation of user specified cost of
        /// bandwidth per GB.
        /// </summary>
        private ReaderWriterLockSlim m_userEstFinancialDecLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Integer representation of user specified estimated energy cost in kWh for bandwidth per
        /// GB.
        /// </summary>
        private long m_userEstimatedKwhCostPerGb;

        /// <summary>
        /// To estimate, or not to estimate. That is the question.
        /// </summary>
        private volatile bool m_shouldEstimate;

        /// <summary>
        /// Read/write access control for the run at startup option.
        /// </summary>
        private ReaderWriterLockSlim m_runAtStartupLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Shared instance of all filtering categories in an ObservableCollection. This collection
        /// is bound to subview in many different views in the overall application.
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<CategorizedFilteredRequestsViewModel> FilterCategories
        {
            get;
            set;
        }

        /// <summary>
        /// Whether or not the application should run at user logon.
        /// </summary>
        [JsonIgnore]
        public bool RunAtStartup
        {
            get
            {
                m_runAtStartupLock.EnterReadLock();

                try
                {
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    p.StartInfo.FileName = "schtasks";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.Arguments += "/nh /fo TABLE /tn \"Stahp It\"";
                    p.StartInfo.RedirectStandardError = true;
                    p.Start();

                    string output = p.StandardOutput.ReadToEnd();
                    string errorOutput = p.StandardError.ReadToEnd();

                    p.WaitForExit();

                    var runAtStartup = false;
                    if (p.ExitCode == 0 && output.IndexOf("Stahp It") != -1)
                    {
                        runAtStartup = true;
                    }

                    return runAtStartup;
                }
                finally
                {
                    m_runAtStartupLock.ExitReadLock();
                }
            }

            set
            {
                m_runAtStartupLock.EnterWriteLock();

                try
                {
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    p.StartInfo.FileName = "schtasks";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardError = true;

                    switch (value)
                    {
                        case true:
                            {
                                string createTaskCommand = "/create /F /sc onlogon /tn \"Stahp It\" /rl highest /tr \"'" + System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + "'/StartMinimized\"";
                                p.StartInfo.Arguments += createTaskCommand;

                                // Only create an entry if there isn't already one.
                                if (RunAtStartup == false)
                                {
                                    p.Start();
                                    p.WaitForExit();
                                }
                            }
                            break;
                        case false:
                            {
                                string deleteTaskCommand = "/delete /F /tn \"Stahp It\"";
                                p.StartInfo.Arguments += deleteTaskCommand;
                                p.Start();
                                p.WaitForExit();
                            }
                            break;
                    }
                }
                finally
                {
                    m_runAtStartupLock.ExitWriteLock();
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
                return m_shouldEstimate;
            }

            set
            {
                m_shouldEstimate = value;
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
                m_userEstBlockedChunkedStringLock.EnterReadLock();

                try
                {
                    return m_blockedChunkedByteEstimateString;
                }
                finally
                {
                    m_userEstBlockedChunkedStringLock.ExitReadLock();
                }                
            }

            set
            {
                m_userEstBlockedChunkedStringLock.EnterWriteLock();

                try
                {
                    m_blockedChunkedByteEstimateString = value;

                    long asLong = 0;
                    if (long.TryParse(value, out asLong))
                    {
                        ChunkedPayloadByteEstimate = asLong;
                    }
                }
                finally
                {
                    m_userEstBlockedChunkedStringLock.ExitWriteLock();
                }                           
            }
        }

        /// <summary>
        /// The user's estimate of the cost of bandwidth per GB.
        /// </summary>
        public string UserEstimatedFinancialCostPerGbString
        {
            get
            {
                m_userEstBlockedChunkedStringLock.EnterReadLock();

                try
                {
                    return m_userEstimatedFinancialCostPerGbString;
                }
                finally
                {
                    m_userEstBlockedChunkedStringLock.ExitReadLock();
                }                
            }

            set
            {
                m_userEstFinancialStringLock.EnterWriteLock();

                try
                {
                    m_userEstimatedFinancialCostPerGbString = value;

                    // Update the decimal version.
                    decimal asDecimal;
                    if(decimal.TryParse(value, out asDecimal))
                    {
                        UserEstimatedFinancialCostPerGb = asDecimal;
                    }
                }
                finally
                {
                    m_userEstFinancialStringLock.ExitWriteLock();
                }                         
            }
        }

        /// <summary>
        /// Integer representation of the user specified cost per kWh.
        /// </summary>
        public decimal UserEstimatedFinancialCostPerGb
        {
            get
            {
                m_userEstFinancialDecLock.EnterReadLock();

                try
                {
                    return m_userEstimatedFinancialCostPerGb;
                }
                finally
                {
                    m_userEstFinancialDecLock.ExitReadLock();
                }
            }

            set
            {
                m_userEstFinancialDecLock.EnterWriteLock();

                try
                {
                    m_userEstimatedFinancialCostPerGb = value;
                }
                finally
                {
                    m_userEstFinancialDecLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// The user's estimate of the energy cost of bandwidth per GB.
        /// </summary>
        public string UserEstimatedKwhCostPerGbString
        {
            get
            {
                m_userEstPowerStringLock.EnterReadLock();

                try
                {
                    return m_userEstimatedKwhCostPerGbString;
                }
                finally
                {
                    m_userEstPowerStringLock.ExitReadLock();
                }                
            }

            set
            {
                m_userEstPowerStringLock.EnterWriteLock();

                try
                {
                    m_userEstimatedKwhCostPerGbString = value;

                    // Update the long version.
                    long asLong;
                    if(long.TryParse(value, out asLong))
                    {
                        UserEstimatedKwhCostPerGb = asLong;
                    }                    
                }
                finally
                {
                    m_userEstPowerStringLock.ExitWriteLock();
                }                
            }
        }

        /// <summary>
        /// Integer representation of the user specified cost per kWh.
        /// </summary>
        public long UserEstimatedKwhCostPerGb
        {
            get
            {
                return Interlocked.Read(ref m_userEstimatedKwhCostPerGb);
            }
            set
            {
                Interlocked.Exchange(ref m_userEstimatedKwhCostPerGb, value);
            }
        }        

        /// <summary>
        /// The total bytes estimated for requests blocked that generated a chunked response.
        /// </summary>
        public long ChunkedPayloadByteEstimate
        {
            get
            {
                return Interlocked.Read(ref m_blockedChunkedByteEstimate);
            }

            set
            {
                Interlocked.Exchange(ref m_blockedChunkedByteEstimate, value);
            }
        }

        /// <summary>
        /// A string representation of the total amount of data that the user wants to estimate a
        /// blocked request to be.
        /// </summary>
        [JsonIgnore]
        public string EstimateFriendlyString
        {
            get
            {
                var byteSize = new ByteSizeLib.ByteSize(ChunkedPayloadByteEstimate);
                return string.Format("{0} {1}", Math.Round(byteSize.LargestWholeNumberValue, 2).ToString(), byteSize.LargestWholeNumberSymbol);
            }
        }

        /// <summary>
        /// Constructs a new SettingsModel instance.
        /// </summary>
        public SettingsModel()
        {

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if(m_userEstBlockedChunkedStringLock != null)
                    {
                        m_userEstBlockedChunkedStringLock.Dispose();
                    }

                    if (m_userEstFinancialStringLock != null)
                    {
                        m_userEstFinancialStringLock.Dispose();
                    }

                    if (m_userEstPowerStringLock != null)
                    {
                        m_userEstPowerStringLock.Dispose();
                    }

                    if (m_userEstFinancialDecLock != null)
                    {
                        m_userEstFinancialDecLock.Dispose();
                    }

                    if (m_runAtStartupLock != null)
                    {
                        m_runAtStartupLock.Dispose();
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
