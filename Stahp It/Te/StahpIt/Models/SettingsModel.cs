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
using Te.StahpIt.ViewModels;

namespace Te.StahpIt.Models
{
    public class SettingsModel
    {
        [JsonIgnore]
        public ObservableCollection<CategorizedFilteredRequestsViewModel> FilterCategories
        {
            get;
            set;
        }

        [JsonIgnore]
        public bool RunAtStartup
        {
            get
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

                if(p.ExitCode == 0 && output.IndexOf("Stahp It") != -1)
                {
                    return true;
                }

                return false;
            }

            set
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
                            if(RunAtStartup == false)
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
        }

        public SettingsModel()
        {

        }
    }
}
