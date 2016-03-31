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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
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
        [JsonIgnore]
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
        public ByteSize TotalDataBlocked
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

        private string ListFilePath
        {
            get
            {
                using (SHA256Managed sha = new SHA256Managed())
                {
                    byte[] hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(CategoryName + RuleSource.ToString()));
                    string listHash = BitConverter.ToString(hash).Replace("-", string.Empty);
                    return AppDomain.CurrentDomain.BaseDirectory + @"rules\" + listHash + ".rules";
                }                    
            }
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
        /// Updates, if necessary, the rule list and loads it into the Engine for use.
        /// </summary>
        /// <exception cref="WebException">
        /// If updating, while attempting to download the list source, this exception may be thrown
        /// by the WebClient. This exception is not handled internally in order to allow users of the
        /// model to handle them. This exception will also be manually thrown if no file is present
        /// after the download.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If updating, while attempting to download the list source, this exception may be thrown
        /// by the WebClient. This exception is not handled internally in order to allow users of the
        /// model to handle them. This exception may also be thrown if the file downloaded is larger
        /// than a hard-coded accepted memory limit.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If updating, while attempting to parse the date, this exception may be thrown. This
        /// exception is not handled internally in order to allow users of the model to handle them.
        /// </exception>
        /// <exception cref="FormatException">
        /// If updating, while attempting to parse the date, this exception may be thrown. This
        /// exception is not handled internally in order to allow users of the model to handle them.
        /// </exception>
        public void UpdateAndLoad()
        {
            var listPath = ListFilePath;

            // Ensure that the correct directories exist. Always.
            Directory.CreateDirectory(Path.GetDirectoryName(listPath));

            if (IsListExpired())
            {               
                using (WebClient webClient = new WebClient())
                {
                    webClient.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT x.y; rv:10.0) Gecko/20100101 Firefox/10.0");

                    webClient.DownloadFile(RuleSource, listPath);                    
                }
            }

            if (File.Exists(listPath))
            {
                var fileNfo = new FileInfo(listPath);

                if (fileNfo.Length > ((1000 * 1000) * 10))
                {
                    throw new NotSupportedException("List file size exceeds 10MB");
                }
            }
            else
            {
                throw new WebException("Failed to download list.");
            }

            if (m_engine != null)
            {
                Debug.WriteLine("Loading list to Engine.");
                var listContents = File.ReadAllText(listPath);

                uint loaded = 0;
                uint failed = 0;
                m_engine.LoadAbpFormattedString(listContents, m_filteringCategory, true, out loaded, out failed);

                Debug.WriteLine(string.Format("{0} loaded and {1} failed.", loaded, failed));

                TotalRulesLoaded = loaded;
                TotalFailedRules = failed;
            }
            else
            {
                Debug.WriteLine("Engine is null!");
            }
        }

        /// <summary>
        /// Checks if the current list is expired.
        /// </summary>
        /// <returns>
        /// True if the list is expired or non-existant on the filesystem, false if the list is
        /// present and up to date.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// While attempting to parse the date, this exception may be thrown. This exception is not
        /// handled internally in order to allow users of the model to handle them.
        /// </exception>
        /// <exception cref="FormatException">
        /// While attempting to parse the date, this exception may be thrown. This exception is not
        /// handled internally in order to allow users of the model to handle them.
        /// </exception>
        private bool IsListExpired()
        {
            var filePath = ListFilePath;

            if (!File.Exists(filePath))
            {
                return true;
            }

            string fileContents = File.ReadAllText(filePath);

            DateTime? expiryDate = null;

            int expiryDays = -1;

            using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bufferedStream = new BufferedStream(fileStream))
            using (StreamReader streamReader = new StreamReader(bufferedStream))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    var lastModified = line.IndexOf("Last modified:");

                    if (lastModified != -1)
                    {
                        var dateTimeStr = line.Substring(lastModified + "Last modified:".Length).Trim();

                        var cultureInfo = new CultureInfo("en-US");

                        expiryDate = DateTime.ParseExact(dateTimeStr, "dd MMM yyyy HH:mm \\U\\T\\C", cultureInfo);
                        continue;
                    }

                    var expires = line.IndexOf("Expires:");

                    if (expires != -1)
                    {
                        var expiresString = line.Substring(expires + "Expires:".Length).Trim();

                        var regexMatch = Regex.Match(expiresString, "([0-9]+) days(.*)");

                        if (regexMatch.Success && regexMatch.Groups.Count > 0)
                        {
                            int.TryParse(regexMatch.Groups[0].Value, out expiryDays);

                            // Quit early if we can.
                            if (expiryDate.HasValue)
                            {
                                break;
                            }
                        }

                        continue;
                    }
                }
            }

            if(expiryDate.HasValue)
            {
                // Default to 4 day expiry time.
                if(expiryDays == -1)
                {
                    expiryDays = 4;
                }

                if(expiryDate.Value.AddDays(expiryDays) < DateTime.Now)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// We need a destructor aka finalizer in order to force the resources for a loaded list to
        /// be unloaded.
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
                    // Delete the list so we don't cause conflicts when the ID is recycled.
                    var listFilePath = ListFilePath;

                    if(File.Exists(listFilePath))
                    {
                        File.Delete(listFilePath);
                    }

                    // Put the category ID that we took back into the collection.
                    AvailableFilteringCategories.Add(m_filteringCategory);                    
                }

                // XXX TODO should we do this before putting back the ID?
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