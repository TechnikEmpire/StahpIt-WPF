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

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Windows;
using System.Windows.Controls;
using Te.HttpFilteringEngine;
using Te.StahpIt.Filtering;

namespace Te.StahpIt.Controls
{

    /// <summary>
    /// Arguments for the FilteringCategoryCreated event.
    /// </summary>
    public class FilteringCategoryCreatedArgs: EventArgs
    {  

        /// <summary>
        /// Constructs a new FilteringCategoryCreatedArgs instance.
        /// </summary>
        /// <param name="category">
        /// The newly created FilteringCategory.
        /// </param>
        /// <exception cref="ArgumentException">
        /// In the event that the supplied category parameter is null, will throw ArgumentException.
        /// </exception>
        public FilteringCategoryCreatedArgs(FilteringCategory category)
        {
            Category = category;

            if(Category == null)
            {
                throw new ArgumentException("Expected valid instance of FilteringCategory");
            }
        }

        public FilteringCategory Category
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Delegate for CategoryCreated event.
    /// </summary>
    /// <param name="sender">
    /// Object raising the event.
    /// </param>
    /// <param name="e">
    /// The event arguments.
    /// </param>
    public delegate void FilteringCategoryCreated(object sender, FilteringCategoryCreatedArgs args);

    /// <summary>
    /// Interaction logic for AddCategoryControl.xaml
    /// </summary>
    public partial class AddCategoryControl : UserControl
    {

        /// <summary>
        /// This is a bit messy, but we need an Engine instance when constructing categories and this
        /// is the Control that handles that.
        /// </summary>
        private readonly Engine m_engine;

        /// <summary>
        /// Event raised whenever the control generates a new category based on user supplied control
        /// inputs.
        /// </summary>
        public event FilteringCategoryCreated CategoryCreated;

        /// <summary>
        /// Constructs a new AddCategoryControl instance. Initializes control inputs.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// In the event that the supplied Engine instance is null, will throw ArgumentException.
        /// </exception>
        public AddCategoryControl(Engine engine)
        {
            InitializeComponent();

            m_engine = engine;

            if(m_engine == null)
            {
                throw new ArgumentException("Expected valid Engine instance.");
            }

            AddButtonEnabled = false;

            btnAddCategory.Click += OnAddCategoryClicked;

            textboxCategoryName.TextChanged += OnInputChanged;

            textboxCategoryUrl.TextChanged += OnInputChanged;

            IsVisibleChanged += OnVisibilityChanged;
        }

        private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Reset();
        }

        /// <summary>
        /// Gets or sets whether the button to generate a new category is enabled.
        /// </summary>
        private bool AddButtonEnabled
        {
            set
            {
                btnAddCategory.IsEnabled = value;
            }

            get
            {
                return btnAddCategory.IsEnabled;
            }
        }

        /// <summary>
        /// Attempts to parse and return a valid HTTP or HTTPS URI from the supplied string.
        /// </summary>
        /// <param name="uriString">
        /// The string to attempt to parse a valid HTTP or HTTPS URI from.
        /// </param>
        /// <returns>
        /// In the event that the supplied string was successfully parsed into a valid HTTP or HTTPS
        /// URI, the constructed URI is returned. In the event that a valid HTTP or HTTPS URI could
        /// not be parsed from the supplied string, null is returned.
        /// </returns>
        private Uri TryGetSourceUri(string uriString)
        {
            if(string.IsNullOrEmpty(uriString) || string.IsNullOrWhiteSpace(uriString))
            {
                return null;
            }

            Uri parsedUri;
            bool tryResult = Uri.TryCreate(uriString, UriKind.Absolute, out parsedUri);

            if (tryResult && (parsedUri.Scheme == Uri.UriSchemeHttp || parsedUri.Scheme == Uri.UriSchemeHttps))
            {
                return parsedUri;
            }

            return null;
        }

        /// <summary>
        /// Gets whether or not the current input of the control is valid.
        /// </summary>
        private bool IsInputValid
        {
            get
            {
                if (textboxCategoryName.Text.Length > 0 && textboxCategoryUrl.Text.Length > 0)
                {
                    var parsedUri = TryGetSourceUri(textboxCategoryUrl.Text);

                    if (parsedUri != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private void OnInputChanged(object sender, TextChangedEventArgs e)
        {
            // Whenever the text changes, we want to validate all of the required inputs.
            AddButtonEnabled = IsInputValid;
        }

        private void OnAddCategoryClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            // Lets validate again, just because.
            if(!IsInputValid)
            {
                return;
            }

            if(CategoryCreated != null)
            {
                try
                {
                    var source = TryGetSourceUri(textboxCategoryUrl.Text);
                    var filteringCategory = new FilteringCategory(m_engine);
                    filteringCategory.RuleSource = source;
                    filteringCategory.CategoryName = textboxCategoryName.Text;
                    CategoryCreated(this, new FilteringCategoryCreatedArgs(filteringCategory));
                }
                catch(ArgumentException ae)
                {
                    // Attempt to notify user of error
                    MetroDialogSettings mds = new MetroDialogSettings();
                    mds.AffirmativeButtonText = "Ok";
                    MetroWindow parentWindow = this.TryFindParent<MetroWindow>();

                    if(parentWindow != null)
                    {
                        DialogManager.ShowMessageAsync(parentWindow, "Error", "Error creating new filtering category.", MessageDialogStyle.Affirmative, mds);
                    }
                }
                
                Reset();
            }
        }

        /// <summary>
        /// Clears the input/state of the control.
        /// </summary>
        public void Reset()
        {
            textboxCategoryName.Clear();
            textboxCategoryUrl.Clear();
        }
    }
}