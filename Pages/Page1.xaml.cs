using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI;
using Microsoft.UI.Text;
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


    public sealed partial class Page1 : Page
    {

        // TODO

        // group headers in list view

        // setting cards:
        //      indicate possible values to which we can set the setting
        //      data units + format
        //      possible range
        //      range checking?
        //      indicate if setting was applied or failed (green flash / red error icon)
        //      ac + dc menus

        // make a tree view rather than navigationview?



        public Page1()
        {
            this.InitializeComponent();
        }



        // Add power setting cards to main ListView
        private void ListView_Loaded(object sender, RoutedEventArgs e)
        {            
            var setting_dict = App.pub_setting_store_dict;

            ObservableCollection<FrameworkElement> setting_items = new ObservableCollection<FrameworkElement>();

            foreach (var kvp in setting_dict)
            {
                string setting_guid = kvp.Key;
                SettingStore setting = kvp.Value;

                Control box_elem;
                if (setting.is_range)
                {
                    DataTemplate box_template = (DataTemplate)this.Resources["NumberBoxTemplate"];
                    NumberBox nb_elem = (NumberBox)box_template.LoadContent();

                    nb_elem.ValueChanged += NumberBoxValueChanged;
                    nb_elem.Tag = setting_guid;

                    box_elem = nb_elem;
                }
                else
                {
                    DataTemplate box_template = (DataTemplate)this.Resources["ComboBoxTemplate"];
                    ComboBox cb_elem = (ComboBox)box_template.LoadContent();

                    cb_elem.SelectionChanged += ComboBoxSelectionChanged;
                    cb_elem.Tag = setting_guid;

                    box_elem = cb_elem;
                }

                DataTemplate setting_template = (DataTemplate)this.Resources["SettingTemplate"];
                Grid setting_elem = (Grid)setting_template.LoadContent();

                setting_elem.Children.Add(box_elem);
                setting_elem.DataContext = setting;

                setting_items.Add(setting_elem);
            }

            this.ListView_main.ItemsSource = setting_items;
        }



        // todo: propagate changed values into setting's value_by_scheme for current scheme
        private void NumberBoxValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs e)
        {
            if (sender.IsLoaded) 
            {
                SettingStore setting = App.pub_setting_store_dict[ (string)sender.Tag ];
                                                                
                string current_scheme = App.pub_curr_scheme_guid;

                bool result = (App.Current as App).set_powersetting(current_scheme, setting._parent_groupguid, sender.Tag.ToString(), (int)sender.Value);
            }
        }

        private void ComboBoxSelectionChanged(object _sender, SelectionChangedEventArgs e)
        {
            ComboBox sender = _sender as ComboBox;

            if (sender.IsLoaded)
            {
                SettingStore setting = App.pub_setting_store_dict[ (string)sender.Tag ];

                string current_scheme = App.pub_curr_scheme_guid;

                bool result = (App.Current as App).set_powersetting(current_scheme, setting._parent_groupguid, sender.Tag.ToString(), (int)sender.SelectedIndex);
            }
        }





        private void Page1_GridLoaded(object sender, RoutedEventArgs e)
        {
            App.Window.SetTitleBar(AppTitleBar);
        }


        // Add navigationviewitems to navigationview
        private void SchemeNavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            var group_dict = App.pub_subgroup_store_dict;
            var scheme_dict = App.pub_scheme_guids;


            this.SchemeNavigationView.MenuItems.Add(new NavigationViewItemHeader() { Content = "Installed Schemes", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.SlateBlue) });

            foreach (var scheme in scheme_dict)
            {
                var scheme_menuitem = new NavigationViewItem();
                scheme_menuitem.Tag = scheme.Key;
                scheme_menuitem.ContentTemplate = (DataTemplate)this.Resources["NavSchemeItem"];
                scheme_menuitem.DataContext = scheme.Value;                

                scheme_menuitem.MenuItems.Add(new NavigationViewItemHeader() { Content = "Setting Groups", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.SlateBlue) });

                foreach (var group in group_dict)
                {
                    scheme_menuitem.MenuItems.Add(new NavigationViewItem() { Content = group.Value._group_name, Tag = group.Key });
                }
                this.SchemeNavigationView.MenuItems.Add(scheme_menuitem);
            }

            NavSetSchemeItemActive(App.pub_curr_scheme_guid, true);
        }


        private void NavSetSchemeItemActive(string guid, bool nav_to_active)
        {
            var scheme_dict = App.pub_scheme_guids;

            foreach (object _schemeitem in this.SchemeNavigationView.MenuItems)
            {
                if (_schemeitem is NavigationViewItem)
                {
                    NavigationViewItem schemeitem = _schemeitem as NavigationViewItem;
                    string scheme_guid = (string)schemeitem.Tag;
                    if (scheme_guid == guid)
                    {
                        scheme_dict[scheme_guid].active_indicator = "(Active)";
                        if (nav_to_active) this.SchemeNavigationView.SelectedItem = schemeitem;
                        break;
                    }
                }                
            }
        }






        private void SchemeNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {

            if (args.IsSettingsSelected)
            {
                //RootFrame.Navigate( typeof(SettingsPage) );                
            }
            else if (args.SelectedItemContainer != null)
            {
                // update the settings in the main listview to reflect the selected theme's current values

                var sel_menuitem = args.SelectedItemContainer;
                string scheme_guid = (string)sel_menuitem.Tag;

                var setting_dict = App.pub_setting_store_dict;

                foreach (var kvp in setting_dict)
                {
                    SettingStore setting_data = kvp.Value;
                    setting_data.curr_ac_val = setting_data.curr_setting_vals_by_scheme[scheme_guid].ac_val;
                    setting_data.curr_dc_val = setting_data.curr_setting_vals_by_scheme[scheme_guid].dc_val;
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

        private void SchemeNavigationView_SearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) { }




        private void OnMenuFlyoutItemClick(object sender, RoutedEventArgs e) { }

        // todo: check for compact and morph Active indicator to circle
        private void SchemeNavigationView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args) { }

        private void CtrlF_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            SchemeNavigationView_SearchBox.Focus(FocusState.Programmatic);
        }
    }


}
