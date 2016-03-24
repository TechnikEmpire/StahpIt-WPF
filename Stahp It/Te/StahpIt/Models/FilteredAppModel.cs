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
using System.IO;

namespace Te.StahpIt.Models
{
    /// <summary>
    /// The FilteredAppModel class represents a system-local application that has performed
    /// socket IO over standard HTTP ports, including port 80 and port 443. Such applications are
    /// represented by this class, and presented to the user via a special control where the user can
    /// specify that the application should or should not have its port 80 and port 443 forced
    /// through the filtering process.
    /// </summary>
    public class FilteredAppModel
    {
        /// <summary>
        /// Application name without path or file extension.
        /// </summary>
        public string ApplicationName = string.Empty;

        /// <summary>
        /// Full path to the application binary.
        /// </summary>
        public string ApplicationPath = string.Empty;

        /// <summary>
        /// Whether the application should be filtered.
        /// </summary>
        public bool Filter = false;

        /// <summary>
        /// Constructs a new FilteredAppModel for the given application.
        /// </summary>
        /// <param name="appPath">
        /// The full path to the application that is to be a filtering candidate.
        /// </param>
        /// <param name="filter">
        /// </param>
        /// <exception cref="ArgumentException">
        /// In the event that the supplied appPath parameter does not point to a valid binary file,
        /// or the parameter is a null, empty or whitespace string, this constructor will throw.
        /// </exception>
        public FilteredAppModel(string appPath, bool filter)
        {
            if (string.IsNullOrEmpty(appPath) || string.IsNullOrWhiteSpace(appPath))
            {
                throw new ArgumentException("Expected full path to binary, got null/empty/whitespace string.");
            }

            if (!File.Exists(appPath))
            {
                throw new ArgumentException("Supplied path points to non-existant binary file.");
            }

            ApplicationPath = appPath;

            ApplicationName = Path.GetFileNameWithoutExtension(ApplicationPath);
        }
    }
}