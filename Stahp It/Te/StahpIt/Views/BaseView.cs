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
using System.Windows.Controls;

namespace Te.StahpIt.Views
{   

    /// <summary>
    /// Enum listing all possible/avaiable views that can be requested.
    /// </summary>
    public enum View
    {
        Dashboard,
        Statistics,
        ProgressWait,
        Settings,
        EnvironmentalImpact
    }

    /// <summary>
    /// Parameters for ViewChangeRequest event.
    /// </summary>
    public class ViewChangeRequestArgs : EventArgs
    {
        /// <summary>
        /// The view being requested.
        /// </summary>
        public View View
        {
            get;
            private set;
        }

        /// <summary>
        /// The additional data for the requested view. This is null by default. Presently, there is
        /// only view that can potentially benefit from additional data: the ProgressWait view. This
        /// event being raised may be another view/operation temporarily requested a progress/wait
        /// and may wish to attach a special message for the user.
        ///
        /// This isn't ideal, but this is a the easiest solution at the time without writing an
        /// entire, separate and specialized system like this just for one view. XXX TODO - Improve
        /// this if this system needs to be extended later.
        /// </summary>
        public object Data
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructs a new ViewChangeRequestArgs class with the View being requested, as well as
        /// some optional data for to be supplied to the requested view. See notes on the Data
        /// member.
        /// </summary>
        /// <param name="view">
        /// The view requested.
        /// </param>
        /// <param name="data">
        /// Optional data for the requested view.
        /// </param>
        public ViewChangeRequestArgs(View view, object data = null)
        {
            View = view;
            Data = data;
        }
    }

    /// <summary>
    /// Base class that all views in this application should extend from. This is so that each of
    /// those views has the basic mechanism for data or user driven events to raise requests to
    /// modify the current view to respond to or notify the user.
    /// </summary>
    public class BaseView : UserControl, IViewController
    {
        /// <summary>
        /// Event for when a view requests another view.
        /// </summary>
        public event ViewChangeRequestCallback ViewChangeRequest;

        /// <summary>
        /// Default ctor.
        /// </summary>
        public BaseView()
        {

        }

        /// <summary>
        /// Requests a change of view.
        /// </summary>
        /// <param name="view">
        /// The requested view.
        /// </param>
        /// <param name="data">
        /// Optional data for the requested view.
        /// </param>
        protected void RequestViewChange(View view, object data = null)
        {
            if (ViewChangeRequest != null)
            {
                var args = new ViewChangeRequestArgs(view, data);
                ViewChangeRequest(this, args);
            }
        }
    }
}