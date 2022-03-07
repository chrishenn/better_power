using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;

namespace better_power
{

    public class MyDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Range_Setting { get; set; }
        public DataTemplate Index_Setting { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            var setting = (App.setting_store)item;

            if (setting._setting_possible_vals.is_range) {
                return Range_Setting;
            }
            else {
                return Index_Setting;
            }
        }
    }


    // TODO
    //  Create collection of settings-data objects
    //  bind observable collection of settings-data objects to listview (allows filtering and search later)
    // 
    //  Settings-data objects must implement INotifyPropertyChanged to handle arbitrary property changes

    public sealed partial class Page1 : Page
    {
        public Page1()
        {
            this.InitializeComponent();



            // Add power setting cards to main ListView
            var setting_dict = App.pub_setting_store_dict;

            foreach( var setting in setting_dict.Values )
            {               
                if (setting._setting_possible_vals.is_range)
                {
                    ListViewItem lvitem = new ListViewItem();
                    DataTemplate r_setting_template = (DataTemplate)this.Resources["RangeSettingTemplate"];

                    lvitem.ContentTemplate = r_setting_template;
                    lvitem.DataContext = setting;

                    this.ListView_main.Items.Add(lvitem);

                } else {
                    DataTemplate i_setting_template = (DataTemplate)this.Resources["IndexSettingTemplate"];

                    Grid i_elem = (Grid)i_setting_template.LoadContent();

                    TextBlock i_textblock = (TextBlock)i_elem.Children[0];
                    i_textblock.Text = setting._setting_name;

                    ComboBox cbox = (ComboBox)i_elem.Children[1];
                    cbox.ItemsSource = setting._setting_possible_vals.index_dict.Values;
                                                        
                    this.ListView_main.Items.Add(i_elem);
                }
            }


        }

        private void Page1_GridLoaded(object sender, RoutedEventArgs e)
        {
            App.Window.SetTitleBar(AppTitleBar);
        }

        private void SchemeNavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            // Delay necessary to ensure NavigationView visual state can match navigation
            //Task.Delay(500).ContinueWith(_ => this.NavigationViewLoaded?.Invoke(), TaskScheduler.FromCurrentSynchronizationContext());

            var setting_dict = App.pub_setting_store_dict;
            var group_dict = App.pub_subgroup_store_dict;
            var scheme_list = App.pub_scheme_guids;

            foreach (var scheme_guid in scheme_list)
            {
                var scheme_menuitem = new NavigationViewItem() { Content = scheme_guid };

                foreach (KeyValuePair<string, App.group_store> kvp in group_dict)
                {
                    string group_guid = kvp.Key;

                    scheme_menuitem.MenuItems.Add(new NavigationViewItem() { Content = group_guid });
                }

                this.SchemeNavigationView.MenuItems.Add(scheme_menuitem);
            }
        }





        private void OnMenuFlyoutItemClick(object sender, RoutedEventArgs e)
        {

        }

        private void SchemeNavigationView_PaneOpen(NavigationView sender, object args) { }
        private void SchemeNavigationView_PaneClose(NavigationView sender, object args) { }





        private void SchemeNavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {

            if (args.InvokedItemContainer.IsSelected)
            {
                // Clicked on an item that is already selected,
                // Avoid navigating to the same page again causing movement.
                return;
            }

            if (args.IsSettingsInvoked)
            {
                //if (rootFrame.CurrentSourcePageType != typeof(SettingsPage))
                //{
                //    rootFrame.Navigate(typeof(SettingsPage));
                //}
            }
            else
            {
                var invokedItem = args.InvokedItemContainer;

                if (invokedItem == null)
                {
                    //this.Frame.Navigate(typeof(Page1));
                }

            }
        }



        private void SchemeNavigationView_SearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                //    var suggestions = new List<ControlInfoDataItem>();

                //    var querySplit = sender.Text.Split(" ");
                //    foreach (var group in ControlInfoDataSource.Instance.Groups)
                //    {
                //        var matchingItems = group.Items.Where(
                //            item =>
                //            {
                //                // Idea: check for every word entered (separated by space) if it is in the name, 
                //                // e.g. for query "split button" the only result should "SplitButton" since its the only query to contain "split" and "button"
                //                // If any of the sub tokens is not in the string, we ignore the item. So the search gets more precise with more words
                //                bool flag = true;
                //                foreach (string queryToken in querySplit)
                //                {
                //                    // Check if token is not in string
                //                    if (item.Title.IndexOf(queryToken, StringComparison.CurrentCultureIgnoreCase) < 0)
                //                    {
                //                        // Token is not in string, so we ignore this item.
                //                        flag = false;
                //                    }
                //                }
                //                return flag;
                //            });
                //        foreach (var item in matchingItems)
                //        {
                //            suggestions.Add(item);
                //        }
                //    }
                //    if (suggestions.Count > 0)
                //    {
                //        controlsSearchBox.ItemsSource = suggestions.OrderByDescending(i => i.Title.StartsWith(sender.Text, StringComparison.CurrentCultureIgnoreCase)).ThenBy(i => i.Title);
                //    }
                //    else
                //    {
                //        controlsSearchBox.ItemsSource = new string[] { "No results found" };
                //    }
            }
        }

        private void SchemeNavigationView_SearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {

        }        
        private void SchemeNavigationView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {

        }
        private void CtrlF_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            SchemeNavigationView_SearchBox.Focus(FocusState.Programmatic);
        }
    }


}
