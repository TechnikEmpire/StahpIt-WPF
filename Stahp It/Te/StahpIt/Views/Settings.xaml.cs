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
using System.Threading.Tasks;
using System.Windows;
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

            btnDeleteCategory.Click += OnDeleteCategoryClicked;
            btnShowAddCategory.Click += OnShowAddCategoryClicked;
        }

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

            RequestViewChange(View.ProgressWait, "Loading list, please wait.");

            bool error = false;

            try
            {
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
                    rightFlyout.IsOpen = !rightFlyout.IsOpen;
                }
            }
        }

        private async void OnDeleteCategoryClicked(object sender, RoutedEventArgs e)
        {
            if (userFilteringCategories.SelectedItem == null)
            {
                return;
            }

            MetroDialogSettings mds = new MetroDialogSettings();
            mds.AffirmativeButtonText = "Yes";
            mds.NegativeButtonText = "No";
            MetroWindow parentWindow = this.TryFindParent<MetroWindow>();

            if (parentWindow != null)
            {
                var result = await DialogManager.ShowMessageAsync(parentWindow, "Delete Category?", "Are you sure you would like to delete the selected Filtering Category?", MessageDialogStyle.AffirmativeAndNegative, mds);

                if (result == MessageDialogResult.Affirmative)
                {
                    var castItem = userFilteringCategories.SelectedItem as CategorizedFilteredRequestsViewModel;

                    if (castItem != null)
                    {
                        castItem.Enabled = false;
                        await Application.Current.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Normal,
                            (Action)delegate ()
                            {
                                m_viewModel.FilterCategories.Remove(castItem);

                                // Important! This must be called to ensure that the FilteringCategory class continues to
                                // work correctly!
                                castItem.Dispose();
                            }
                        );
                    }
                }
            }
        }
    }
}