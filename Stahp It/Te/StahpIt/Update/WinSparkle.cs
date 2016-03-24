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

using System.Runtime.InteropServices;

namespace Te.StahpIt.Update
{
    internal class WinSparkle
    {
        /// <summary>
        /// Callback where WinSparkle will request if the application can shutdown to allow an
        /// update.
        /// </summary>
        /// <returns>
        /// Zero if a shutdown is not possible, one if a shutdown is possible.
        /// </returns>
        public delegate int WinSparkleCanShutdownCheckCallback();

        /// <summary>
        /// Callback where WinSparkle is requesting a shutdown of the application to allow for an
        /// update. This will immediately follow a call to the WinSparkleCanShutdownCheckCallback
        /// callback, where the return value was one.
        /// </summary>
        public delegate void WinSparkleRequestShutdownCallback();

        [DllImport("WinSparkle.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "win_sparkle_set_can_shutdown_callback", ExactSpelling = true)]
        public static extern void SetCanShutdownCallback(WinSparkleCanShutdownCheckCallback cb);

        [DllImport("WinSparkle.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "win_sparkle_set_shutdown_request_callback", ExactSpelling = true)]
        public static extern void SetShutdownRequestCallback(WinSparkleRequestShutdownCallback cb);

        [DllImport("WinSparkle.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "win_sparkle_init", ExactSpelling = true)]
        public static extern void Init();

        [DllImport("WinSparkle.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "win_sparkle_cleanup", ExactSpelling = true)]
        public static extern void Cleanup();

        [DllImport("WinSparkle.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "win_sparkle_set_appcast_url", ExactSpelling = true)]
        public static extern void SetAppcastUrl(string url);

        [DllImport("WinSparkle.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "win_sparkle_set_app_details", ExactSpelling = true)]
        public static extern void SetAppDetails(string companyName, string appName, string appVersion);

        [DllImport("WinSparkle.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "win_sparkle_set_registry_path", ExactSpelling = true)]
        public static extern void SetRegistryPath(string path);

        [DllImport("WinSparkle.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "win_sparkle_check_update_with_ui", ExactSpelling = true)]
        public static extern void CheckUpdateWithUI();

        [DllImport("WinSparkle.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "win_sparkle_check_update_without_ui", ExactSpelling = true)]
        public static extern void CheckUpdateWithoutUI();
    }
}