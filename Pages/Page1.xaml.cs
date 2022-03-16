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
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;

namespace better_power
{

    public sealed partial class Page1 : Page
    {

        ObservableCollection<FrameworkElement> setting_items = new ObservableCollection<FrameworkElement>();
        ObservableCollection<FrameworkElement> all_setting_items;

        Dictionary<string, FrameworkElement> setting_item_dict = new Dictionary<string, FrameworkElement>();

        ObservableCollection<SettingStore> setting_data = new ObservableCollection<SettingStore>();

        string current_display_scheme_guid;
        
        bool settings_locked_for_navigation = false;



        public Page1()
        {
            this.InitializeComponent();

            foreach (var kvp in App.pub_setting_store_dict)
            {
                this.setting_data.Add( kvp.Value );
            }


            // generate collections for listview settings elements
            string curr_groupid = "";

            foreach (var setting in this.setting_data)
            {
                string setting_guid = setting._setting_guid;

                if (setting._parent_groupguid != curr_groupid)
                {
                    curr_groupid = setting._parent_groupguid;
                    string curr_groupname = App.pub_subgroup_store_dict[curr_groupid]._group_name;

                    this.setting_items.Add(new ListViewHeaderItem() { Content = curr_groupname, Tag = curr_groupid });
                }

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

                // compose the setting element from constituents
                DataTemplate setting_template = (DataTemplate)this.Resources["SettingTemplate"];
                Grid setting_elem = (Grid)setting_template.LoadContent();

                setting_elem.Children.Add(box_elem);
                setting_elem.DataContext = setting;
                setting_elem.Tag = setting_guid;

                // Success animation
                Color background_gray = (Application.Current.Resources["AppTitleBar_Grey"] as SolidColorBrush).Color;
                Duration duration = new Duration(TimeSpan.FromSeconds(1));

                SolidColorBrush background_brush = new SolidColorBrush(background_gray);
                setting_elem.Background = background_brush;

                var color_togreen = new LinearColorKeyFrame() { Value = Colors.MediumSpringGreen, KeyTime=TimeSpan.FromSeconds(0.025) };
                var color_green = new LinearColorKeyFrame() { Value = Colors.MediumSpringGreen, KeyTime=TimeSpan.FromSeconds(0.1) };
                var color_togray = new LinearColorKeyFrame() { Value = background_gray, KeyTime = TimeSpan.FromSeconds(0.25) };

                var animation = new ColorAnimationUsingKeyFrames();
                animation.KeyFrames.Add(color_togreen);
                animation.KeyFrames.Add(color_green);
                animation.KeyFrames.Add(color_togray);

                Storyboard.SetTarget(animation, background_brush);
                Storyboard.SetTargetProperty(animation, "Color");

                Storyboard story_board = new Storyboard() { Children={ animation } };
                
                setting_elem.Resources.Add("storyboard", story_board);

                // add setting element to instance collections to find later
                this.setting_items.Add(setting_elem);
                this.setting_item_dict[setting_guid] = setting_elem;
            }

            // copy all setting items; this.setting_items is observed by the listview
            this.all_setting_items = new ObservableCollection<FrameworkElement>(this.setting_items);
        }



        // Add power setting cards collection to main ListView
        private void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            this.ListView_main.ItemsSource = this.setting_items;            
        }



        private void NumberBoxValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs e)
        {
            if (sender.IsLoaded && !this.settings_locked_for_navigation)
            {
                SettingStore setting = App.pub_setting_store_dict[sender.Tag.ToString()];

                string selected_scheme_guid = this.current_display_scheme_guid;

                var curr_vals = setting.curr_setting_vals_by_scheme[selected_scheme_guid];
                setting.curr_setting_vals_by_scheme[selected_scheme_guid] = ((int)sender.Value, curr_vals.dc_val);

                bool result = (App.Current as App).set_powersetting(selected_scheme_guid, setting._parent_groupguid, sender.Tag.ToString(), (int)sender.Value);

                if (result) FireSuccessFlash(setting);
            }
        }

        private void ComboBoxSelectionChanged(object _sender, SelectionChangedEventArgs e)
        {
            ComboBox sender = _sender as ComboBox;

            if (sender.IsLoaded && !this.settings_locked_for_navigation)
            {
                SettingStore setting = App.pub_setting_store_dict[sender.Tag.ToString()];

                string selected_scheme_guid = this.current_display_scheme_guid;
                                
                var curr_vals = setting.curr_setting_vals_by_scheme[selected_scheme_guid];
                setting.curr_setting_vals_by_scheme[selected_scheme_guid] = ((int)sender.SelectedIndex, curr_vals.dc_val);

                bool result = (App.Current as App).set_powersetting(selected_scheme_guid, setting._parent_groupguid, sender.Tag.ToString(), (int)sender.SelectedIndex);

                if (result) FireSuccessFlash(setting);
            }
        }


        private void FireSuccessFlash(SettingStore setting)
        {
            (this.setting_item_dict[setting._setting_guid].Resources["storyboard"] as Storyboard).Begin();
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
                    // note: subgroup menuitems have key of parent's scheme guid! needed for navigation selectionchanged
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
                        if (nav_to_active) 
                            this.SchemeNavigationView.SelectedItem = schemeitem;                        
                    }
                    else
                    {
                        App.pub_scheme_store_dict[nav_scheme_guid].is_active_scheme = "Collapsed";
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
                this.settings_locked_for_navigation = true;

                string selected_guid = args.SelectedItemContainer.Tag.ToString();
                string selected_scheme_guid;

                // selected is a scheme. update settings cards current values in listview
                if (App.pub_scheme_store_dict.ContainsKey(selected_guid))
                {
                    selected_scheme_guid = selected_guid;                    
                    update_settings_to_scheme(selected_scheme_guid);

                    this.setting_items.Clear();
                    foreach (var item in this.all_setting_items) 
                        this.setting_items.Add(item);
                }
                // else selected_guid is a groupid. get selected scheme. check for scheme change. filter settings in view.
                else
                {
                    selected_scheme_guid = (args.SelectedItemContainer.DataContext as SchemeStore).scheme_guid;

                    if (selected_scheme_guid != this.current_display_scheme_guid)
                        update_settings_to_scheme(selected_scheme_guid);

                    this.setting_items.Clear();

                    foreach (var setting_item in this.all_setting_items)
                    {
                        string setting_guid = setting_item.Tag.ToString();

                        if (App.pub_setting_store_dict.ContainsKey(setting_guid))
                        {
                            if (App.pub_setting_store_dict[setting_guid]._parent_groupguid == selected_guid)
                                this.setting_items.Add(setting_item);
                        }
                        else if (setting_guid == selected_guid)
                            this.setting_items.Add(setting_item);                                             
                    }
                }

                this.current_display_scheme_guid = selected_scheme_guid;
                this.settings_locked_for_navigation = false;
            }
        }

        private void update_settings_to_scheme(string selected_scheme_guid)
        {
            foreach (var setting_data in this.setting_data)
            {
                var vals = setting_data.curr_setting_vals_by_scheme[selected_scheme_guid];

                setting_data.curr_ac_val = vals.ac_val;
                setting_data.curr_dc_val = vals.dc_val;
            }
        }







        private void SchemeNavigationView_DisplayModeChanged(object sender, NavigationViewDisplayModeChangedEventArgs e) { }

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

        private void CtrlF_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            SearchBox.Focus(FocusState.Programmatic);
        }
    }


}
