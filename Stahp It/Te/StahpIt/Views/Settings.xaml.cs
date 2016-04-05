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
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Te.StahpIt.Controls;
using Te.StahpIt.ViewModels;

namespace Te.StahpIt.Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : BaseView
    {
        private AddCategoryControl m_addCategoryControl;

        private SettingsViewModel m_viewModel;

        private readonly Regex m_regexDigitOnlyValidation;

        private readonly Regex m_regexDollarValueOnlyValidation;

        /// <summary>
        /// Constructs a new Settings view with the given view model, and with the provided
        /// AddCategoryControl control. The AddCategoryControl instance is not actually displayed
        /// within the Settings view at all, but rather the Settings view internally finds its parent
        /// window and shows this instance of AddCategoryControl within a flyout. This is part of the
        /// reason why the control is supplied.
        ///
        /// The real reason is that presently, there is not a better solution to the problem where
        /// the AddCategoryControl needs to hold a reference to the Engine instance that it is
        /// creating categories for. Handing off this instance to this view or view model, then
        /// passing it up to an internally created instance of AddCategoryControl seems like a worse
        /// solution than this. Doing it this way, the AddCategoryControl instance is kept neatly
        /// together with associated objects at the App code behind level. However, XXX TODO, find a
        /// more elegant solution to this.
        /// </summary>
        /// <param name="viewModel">
        /// The settings view model.
        /// </param>
        /// <param name="addCategoryControl">
        /// The user control that generates, by user input, new FilteringCategory instances.
        /// </param>
        /// <exception cref="ArgumentException">
        /// In the event that either the supplied SettingsViewModel or AddCategoryControl instances
        /// are null, will throw ArgumentException.
        /// </exception>
        public Settings(SettingsViewModel viewModel, AddCategoryControl addCategoryControl)
        {
            InitializeComponent();

            m_viewModel = viewModel;

            if (m_viewModel == null)
            {
                throw new ArgumentException("Expected valid instance of SettingsViewModel");
            }

            m_addCategoryControl = addCategoryControl;

            if (m_addCategoryControl == null)
            {
                throw new ArgumentException("Expected valid instance of AddCategoryControl");
            }

            DataContext = m_viewModel;

            // The control handles input, validation and creation of objects independently, so we
            // simply display the control to the user and listen for any events where a category was
            // successfully created.
            m_addCategoryControl.CategoryCreated += OnFilteringCategoryCreated;

            m_btnDeleteCategory.Click += OnDeleteCategoryClicked;
            m_btnShowAddCategory.Click += OnShowAddCategoryClicked;

            m_regexDigitOnlyValidation = new Regex("[^0-9]");
            m_regexDollarValueOnlyValidation = new Regex("[^0-9\\.]");
        }

        /// <summary>
        /// Handler for when the create category control has received and validated user input for a
        /// new filtering category. The purpose of this handler is to decide what to do with the data
        /// generated by the control.
        /// </summary>
        /// <param name="sender">
        /// The origin of the raised event.
        /// </param>
        /// <param name="args">
        /// The event arguments. In this case, the arguments contain already constructed
        /// FilteringCategory object.
        /// </param>
        private async void OnFilteringCategoryCreated(object sender, FilteringCategoryCreatedArgs args)
        {
            // First thing is to hide the flyout now that it's no longer required.
            var mainWindow = Window.GetWindow(this) as MetroWindow;
            if (mainWindow != null)
            {
                Flyout rightFlyout = mainWindow.FindName("rightFlyout") as Flyout;

                if (rightFlyout != null)
                {
                    rightFlyout.IsOpen = false;
                }
            }

            // Request a view change to the ProgressWait view, because downloading and parsing large
            // lists can take time.
            RequestViewChange(View.ProgressWait, "Loading list, please wait.");

            bool error = false;

            try
            {
                // Try to download the filtering rules from the HTTP ur HTTPS URI supplied by the
                // user. This will also attempt to load and parse the rules into the underlying
                // Engine.
                //
                // If absolutely anything goes wrong, show a relevant message to the user and return.
                await Task.Run(() => args.Category.UpdateAndLoad());
            }
            catch(WebException we)
            {
                error = true;
                ShowUserMessage("Error", "Error downloading list file.");
            }
            catch (NotSupportedException ne)
            {
                error = true;
                ShowUserMessage("Error", "Error downloading list file.");
            }
            catch (ArgumentNullException ane)
            {
                error = true;
                ShowUserMessage("Error", "Error checking list expiry. Is the source a valid filtering list?");
            }
            catch (FormatException fe)
            {
                error = true;
                ShowUserMessage("Error", "Error checking list expiry. Is the source a valid filtering list?");
            }

            // Either way, return to the original view. This view.
            RequestViewChange(View.Settings);

            if(error)
            {
                return;
            }

            await Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)delegate ()
                {
                    try
                    {
                        // Push the new filtering category to the view model's observable list of categories.
                        m_viewModel.FilterCategories.Add(new CategorizedFilteredRequestsViewModel(args.Category));
                    }
                    catch(ArgumentException ae)
                    {
                        ShowUserMessage("Error", "Error while adding new filtering category to list.");
                        m_logger.Error(string.Format("Error while adding new filtering category to list: {0}", ae.Message));
                    }                    
                }
            );
        }

        /// <summary>
        /// Handler for when the add category button is clicked. In this handler, we search for a
        /// specific flyout on the main Window, then set the content of that flyout to our new
        /// category control and slide the flyout into user view.
        /// </summary>
        /// <param name="sender">
        /// The control that raised the event. Should always be the add category button.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        private void OnShowAddCategoryClicked(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MetroWindow;
            if (mainWindow != null)
            {
                Flyout rightFlyout = mainWindow.FindName("rightFlyout") as Flyout;

                if (rightFlyout != null)
                {
                    rightFlyout.Header = "Add New Category";
                    rightFlyout.Content = m_addCategoryControl;
                    rightFlyout.IsOpen = true;
                }
            }
        }

        /// <summary>
        /// Called when the user clicks the remove category button. The enabled state of this button
        /// is data bound to whether or not the list of categories currently has an entry selected.
        /// So, in this handler, we get the selected list item and remove it from the list.
        /// </summary>
        /// <param name="sender">
        /// The object raising the event. Should always be the remove category button.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        private async void OnDeleteCategoryClicked(object sender, RoutedEventArgs e)
        {
            if (m_userFilteringCategories.SelectedItem == null)
            {
                return;
            }

            // Verify with the user whether the delete should really happen or not.
            MetroDialogSettings mds = new MetroDialogSettings();
            mds.AffirmativeButtonText = "Yes";
            mds.NegativeButtonText = "No";
            MetroWindow parentWindow = this.TryFindParent<MetroWindow>();

            if (parentWindow != null)
            {
                var result = await DialogManager.ShowMessageAsync(parentWindow, "Delete Category?", "Are you sure you would like to delete the selected Filtering Category?", MessageDialogStyle.AffirmativeAndNegative, mds);

                if (result == MessageDialogResult.Affirmative)
                {
                    // Get the current selected item in the categories list.
                    var castItem = m_userFilteringCategories.SelectedItem as CategorizedFilteredRequestsViewModel;

                    if (castItem != null)
                    {
                        // Disable the category, then remove it from the list and dispose.
                        castItem.Enabled = false;
                        await Application.Current.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Normal,
                            (Action)delegate ()
                            {
                                m_viewModel.FilterCategories.Remove(castItem);

                                // Important! This must be called to ensure that the
                                // FilteringCategory class continues to work correctly! Disposing of
                                // the category will cause the limited bag of category unique IDs to
                                // be repopulated with the IDs taken by disposed categories.
                                castItem.Dispose();
                            }
                        );
                    }
                }
            }
        }

        /// <summary>
        /// For enforcing digit-only input into the blocked payload byte estimation TextBox.
        /// </summary>
        /// <param name="sender">
        /// Who dun' it.
        /// </param>
        /// <param name="e">
        /// Event parameters.
        /// </param>
        private void OnDigitOnlyTextPreview(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = m_regexDigitOnlyValidation.IsMatch(e.Text);
        }

        /// <summary>
        /// For enforcing digit-only input into the blocked payload byte estimation TextBox when the
        /// user pastes clipboard content into the box.
        /// </summary>
        /// <param name="sender">
        /// Who dun' it.
        /// </param>
        /// <param name="e">
        /// Event parameters.
        /// </param>
        private void OnDigitOnlyTextPasting(object sender, DataObjectPastingEventArgs e)
        {
            // Simply use our existing regex to blow away all non-digit text, then replace the
            // payload of the paste with the result.
            
            string pastedText = (string)e.DataObject.GetData(typeof(string));
            DataObject newObj = new DataObject();
            if(pastedText != null)
            {
                newObj.SetData(DataFormats.Text, m_regexDigitOnlyValidation.Replace(pastedText, ""));
            }
            
            e.DataObject = newObj;
        }
        
        /// <summary>
        /// Previews text changes to an input that is meant to only accept a dollar amount as input.
        /// That is, digits and a single decimal, followed by a maximum of two digits.
        /// </summary>
        /// <param name="sender">
        /// The input raising the event.
        /// </param>
        /// <param name="e">
        /// The change parameters.
        /// </param>
        private void OnDollarValueInputPreview(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if(sender is TextBox)
            {               
                TextBox originBox = (TextBox)sender;

                // Don't allow two decimals.
                if (e.Text.IndexOf('.') != -1 && originBox.Text.IndexOf('.') != -1)
                {
                    e.Handled = true;
                    return;
                }

                // Deny entering any characters past two decimal places.
                var decimalIndex = originBox.Text.Split('.');
                if(decimalIndex != null)
                {
                    if (decimalIndex.Length >= 2 && decimalIndex[1].Length >= 2)
                    {
                        e.Handled = true;
                        return;
                    }                    
                }
            }

            e.Handled = m_regexDollarValueOnlyValidation.IsMatch(e.Text);
        }

        /// <summary>
        /// Controls pasted values on an input that is meant to only accept a dollar amount as input.
        /// That is, digits and a single decimal, followed by a maximum of two digits.
        /// </summary>
        /// <param name="sender">
        /// The input raising the event.
        /// </param>
        /// <param name="e">
        /// The paste data arguments.
        /// </param>
        private void OnDollarValueTextPasting(object sender, DataObjectPastingEventArgs e)
        {

            // Automatically try to parse the pasted data as a decimal and convert it to the
            // appropriate string format. On failure, deny the paste.

            string pastedText = (string)e.DataObject.GetData(typeof(string));
            DataObject newObj = new DataObject();
            if (pastedText != null)
            {
                pastedText = m_regexDollarValueOnlyValidation.Replace(pastedText, "");

                decimal asDecimal;
                if(decimal.TryParse(pastedText, out asDecimal))
                {
                    pastedText = Math.Round(asDecimal, 2).ToString();
                }
                else
                {
                    // On conversion failure, just refuse paste.
                    pastedText = string.Empty;
                }

                newObj.SetData(DataFormats.Text, pastedText);
            }

            e.DataObject = newObj;
        }
    }
}