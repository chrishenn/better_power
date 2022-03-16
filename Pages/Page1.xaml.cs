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
using System.Threading.Tasks;




namespace better_power
{

    public sealed partial class Page1 : Page
    {

        ObservableCollection<FrameworkElement> setting_items = new ObservableCollection<FrameworkElement>();
        ObservableCollection<SettingStore> setting_data = new ObservableCollection<SettingStore>();

        bool settings_locked_for_navigation = false;



        public Page1()
        {
            this.InitializeComponent();

            foreach (var kvp in App.pub_setting_store_dict)
            {
                this.setting_data.Add( kvp.Value );
            }
        }



        // Add power setting cards to main ListView
        private void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            var setting_dict = App.pub_setting_store_dict;

            foreach (var setting in this.setting_data)
            {
                string setting_guid = setting._setting_guid;

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

                this.setting_items.Add(setting_elem);
            }

            this.ListView_main.ItemsSource = this.setting_items;
        }



        private void NumberBoxValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs e)
        {
            if (sender.IsLoaded && !this.settings_locked_for_navigation)
            {
                SettingStore setting = App.pub_setting_store_dict[sender.Tag.ToString()];

                string selected_scheme_guid = (SchemeNavigationView.SelectedItem as NavigationViewItemBase).Tag.ToString();

                // propogate changed values into setting_vals_by_scheme dict for this setting.
                // todo: will not be needed if the application watches system settings changes
                // todo: update either AC or DC setting
                var curr_vals = setting.curr_setting_vals_by_scheme[selected_scheme_guid];
                setting.curr_setting_vals_by_scheme[selected_scheme_guid] = ((int)sender.Value, curr_vals.dc_val);

                bool result = (App.Current as App).set_powersetting(selected_scheme_guid, setting._parent_groupguid, sender.Tag.ToString(), (int)sender.Value);
            }
        }

        private void ComboBoxSelectionChanged(object _sender, SelectionChangedEventArgs e)
        {
            ComboBox sender = _sender as ComboBox;

            if (sender.IsLoaded && !this.settings_locked_for_navigation)
            {
                SettingStore setting = App.pub_setting_store_dict[sender.Tag.ToString()];

                string selected_scheme_guid = (SchemeNavigationView.SelectedItem as NavigationViewItemBase).Tag.ToString();

                // propogate changed values into setting_vals_by_scheme dict for this setting.
                // todo: will not be needed if the application watches system settings changes
                // todo: update either AC or DC setting
                var curr_vals = setting.curr_setting_vals_by_scheme[selected_scheme_guid];
                setting.curr_setting_vals_by_scheme[selected_scheme_guid] = ((int)sender.SelectedIndex, curr_vals.dc_val);

                bool result = (App.Current as App).set_powersetting(selected_scheme_guid, setting._parent_groupguid, sender.Tag.ToString(), (int)sender.SelectedIndex);
            }
        }





        private void Page1_GridLoaded(object sender, RoutedEventArgs e)
        {
            App.Window.SetTitleBar(AppTitleBar);
        }


        // Add navigationviewitems to navigationview
        private void SchemeNavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            this.SchemeNavigationView.MenuItems.Add(new NavigationViewItemHeader() { Content = "Installed Schemes", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.SlateBlue) });

            foreach (var scheme in App.pub_scheme_store_dict)
            {
                var scheme_menuitem = new NavigationViewItem();
                scheme_menuitem.Tag = scheme.Key;
                scheme_menuitem.ContentTemplate = (DataTemplate)this.Resources["NavSchemeItem"];
                scheme_menuitem.DataContext = scheme.Value;

                var flyout_setactive = new MenuFlyoutItem() { Text = "Set Active", Tag = scheme.Key };
                flyout_setactive.Click += SchemeSetActiveFlyout_Clicked;
                scheme_menuitem.ContextFlyout = new MenuFlyout() { Items = { flyout_setactive } };

                scheme_menuitem.MenuItems.Add(new NavigationViewItemHeader() { Content = "Setting Groups", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.SlateBlue) });

                foreach (var group in App.pub_subgroup_store_dict)
                {
                    scheme_menuitem.MenuItems.Add(new NavigationViewItem() { Content = group.Value._group_name, Tag = group.Key });
                }
                this.SchemeNavigationView.MenuItems.Add(scheme_menuitem);
            }

            NavSetSchemeItemActive(App.pub_curr_scheme_guid, true);
        }

        private void SchemeSetActiveFlyout_Clicked(object sender, RoutedEventArgs e)
        {
            string scheme_guid = (sender as MenuFlyoutItem).Tag.ToString();

            bool result = (App.Current as App).set_powerscheme(scheme_guid);

            NavSetSchemeItemActive(scheme_guid, false);
        }


        // Add items to navigationTreeView
        //private void TreeViewNavigation_Loaded(object sender, RoutedEventArgs e)
        //{
        //    this.navigation_items.Add(new TreeViewItem() { Content = "Installed Schemes", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.SlateBlue), Tag = "Header" });

        //    foreach (var scheme in App.pub_scheme_guids)
        //    {
        //        var scheme_menuitem = new TreeViewItem();
        //        scheme_menuitem.Tag = scheme.Key;
        //        scheme_menuitem.ContentTemplate = (DataTemplate)this.Resources["NavSchemeItem"];
        //        scheme_menuitem.DataContext = scheme.Value;
        //        scheme_menuitem.AddHandler(TappedEvent, new TappedEventHandler(TreeView_SchemeItem_Clicked), true);

        //        var scheme_items = new List<FrameworkElement>();

        //        scheme_items.Add(new TreeViewItem() {Content = "Setting Groups", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.SlateBlue), Tag="Header" });

        //        foreach (var group in App.pub_subgroup_store_dict)
        //        {
        //            var groupitem = new TreeViewItem() { Content = group.Value._group_name, Tag = group.Key };
        //            groupitem.AddHandler(TappedEvent, new TappedEventHandler(TreeView_GroupItem_Clicked), true);

        //            scheme_items.Add(groupitem);
        //        }

        //        scheme_menuitem.ItemsSource = scheme_items;
        //        this.navigation_items.Add(scheme_menuitem);
        //    }

        //    this.TreeViewNavigation.ItemsSource = this.navigation_items;

        //    NavSetSchemeItemActive(App.pub_curr_scheme_guid, true);
        //}

        //private void TreeView_SchemeItem_Clicked(object sender, TappedRoutedEventArgs args)
        //{

        //}

        //private void TreeView_GroupItem_Clicked(object sender, TappedRoutedEventArgs args)
        //{

        //}


        private void NavSetSchemeItemActive(string target_guid, bool nav_to_active)
        {
            foreach (object _schemeitem in this.SchemeNavigationView.MenuItems)
            {
                if (_schemeitem is NavigationViewItem)
                {
                    NavigationViewItem schemeitem = _schemeitem as NavigationViewItem;
                    string nav_scheme_guid = schemeitem.Tag.ToString();
                    if (nav_scheme_guid == target_guid)
                    {
                        App.pub_scheme_store_dict[nav_scheme_guid].is_active_scheme = "Visible";
                        if (nav_to_active) this.SchemeNavigationView.SelectedItem = schemeitem;                        
                    }
                    else
                    {
                        App.pub_scheme_store_dict[nav_scheme_guid].is_active_scheme = "Collapsed";
                    }
                }
            }
        }

        ////navigationtreeview set item active
        //private void NavSetSchemeItemActive(string guid, bool nav_to_active)
        //{
        //    foreach (object _schemeitem in this.TreeViewNavigation.RootNodes)
        //    {
        //        string scheme_guid = ((_schemeitem as TreeViewNode).Content as TreeViewItem).Tag.ToString();
        //        if (scheme_guid == guid)
        //        {
        //            App.pub_scheme_guids[scheme_guid].active_indicator = "(Active)";
        //            if (nav_to_active) this.TreeViewNavigation.SelectedItem = _schemeitem;
        //            break;
        //        }                
        //    }
        //}




        // "navigate" to a new page
        
        private void SchemeNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                //RootFrame.Navigate( typeof(SettingsPage) );                
            }
            else if (args.SelectedItemContainer != null)
            {
                this.settings_locked_for_navigation = true;

                string scheme_guid = args.SelectedItemContainer.Tag.ToString();

                foreach (var setting_data in this.setting_data)
                {
                    var vals = setting_data.curr_setting_vals_by_scheme[scheme_guid];

                    setting_data.curr_ac_val = vals.ac_val;
                    setting_data.curr_dc_val = vals.dc_val;
                }

                this.settings_locked_for_navigation = false;
            }
        }




        private void SchemeNavigationView_DisplayModeChanged(object sender, NavigationViewDisplayModeChangedEventArgs e)
        {

        }
        private void SchemeNavigationView_SelectionChanged(object sender, NavigationViewSelectionChangedEventArgs e)
        {

        }




        private void SearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
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

        private void SearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) { }

        private void OnMenuFlyoutItemClick(object sender, RoutedEventArgs e) { }

        private void CtrlF_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            SearchBox.Focus(FocusState.Programmatic);
        }
    }


}
