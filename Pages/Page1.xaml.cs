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
        // required list to enforce the ordering of headers and setting elements in the listView
        List<FrameworkElement> all_setting_elements;
        ObservableCollection<FrameworkElement> setting_elements = new ObservableCollection<FrameworkElement>();                

        Dictionary<string, FrameworkElement> setting_element_dict = new Dictionary<string, FrameworkElement>();
        Dictionary<string, FrameworkElement> scheme_element_dict = new Dictionary<string, FrameworkElement>();
        Dictionary<string, List<FrameworkElement>> scheme_subelements_dict = new Dictionary<string, List<FrameworkElement>>();
        Dictionary<string, List<FrameworkElement>> setting_elements_by_group_dict = new Dictionary<string, List<FrameworkElement>>();
        Dictionary<string, FrameworkElement> group_headerelements_dict = new Dictionary<string, FrameworkElement>();

        // current scheme guid being displayed in the main ListView UI
        string current_display_scheme_guid;
        
        bool settings_locked_for_navigation = false;



        // generate listview settings elements; build instance lists and dicts
        public Page1()
        {
            this.InitializeComponent();

            string curr_groupid = "";
            ListViewHeaderItem curr_groupheader = null;

            // todo: relies on stable ordering in the setting_data_dict. use ordereddict
            foreach (var kvp in App.setting_data_dict)
            {
                string setting_guid = kvp.Key;
                SettingStore setting = kvp.Value;

                if (setting._parent_groupguid != curr_groupid)
                {
                    curr_groupid = setting._parent_groupguid;
                    string curr_groupname = App.group_data_dict[curr_groupid]._group_name;

                    curr_groupheader = new ListViewHeaderItem() { Content = curr_groupname, Tag = curr_groupid };
                    this.setting_elements.Add(curr_groupheader);

                    this.setting_elements_by_group_dict[curr_groupid] = new List<FrameworkElement>();
                    this.setting_elements_by_group_dict[curr_groupid].Add(curr_groupheader); 
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

                // register animators into element's Resources
                Color background_gray = (Application.Current.Resources["AppTitleBar_Grey"] as SolidColorBrush).Color;
                SolidColorBrush background_brush = new SolidColorBrush(background_gray);
                setting_elem.Background = background_brush;

                register_animation(setting_elem, background_brush, Colors.MediumSpringGreen, "success_animation");
                register_animation(setting_elem, background_brush, Colors.MediumVioletRed, "fail_animation");

                // add setting element to instance collections to find later
                this.setting_elements.Add(setting_elem);
                this.setting_element_dict[setting_guid] = setting_elem;
                this.setting_elements_by_group_dict[curr_groupid].Add(setting_elem);
                this.group_headerelements_dict[curr_groupid] = curr_groupheader;
            }

            // copy all setting items; this.setting_items is observed by the listview
            this.all_setting_elements = new List<FrameworkElement>(this.setting_elements);
        }

        private void Page1_GridLoaded(object sender, RoutedEventArgs e)
        {
            App.Window.SetTitleBar(AppTitleBar);
        }

        // register animation to a settings element or a scheme menuitem in the navigationview
        private void register_animation(FrameworkElement element, SolidColorBrush animated_brush, Color color, string animation_name)
        {
            Color background_gray = (Application.Current.Resources["AppTitleBar_Grey"] as SolidColorBrush).Color;

            var color_tocolor = new LinearColorKeyFrame() { Value = color, KeyTime = TimeSpan.FromSeconds(0.025) };
            var color_hold = new LinearColorKeyFrame() { Value = color, KeyTime = TimeSpan.FromSeconds(0.1) };
            var color_togray = new LinearColorKeyFrame() { Value = background_gray, KeyTime = TimeSpan.FromSeconds(0.25) };

            var animation = new ColorAnimationUsingKeyFrames();
            animation.KeyFrames.Add(color_tocolor);
            animation.KeyFrames.Add(color_hold);
            animation.KeyFrames.Add(color_togray);

            Storyboard.SetTarget(animation, animated_brush);
            Storyboard.SetTargetProperty(animation, "Color");

            Storyboard story_board = new Storyboard() { Children = { animation } };

            element.Resources.Add(animation_name, story_board);
        }




        // Add power setting cards collection to main ListView
        private void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            this.ListView_main.ItemsSource = this.setting_elements;            
        }

        private void NumberBoxValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs e)
        {
            if (sender.IsLoaded && !this.settings_locked_for_navigation)
            {
                SettingStore setting = App.setting_data_dict[sender.Tag.ToString()];

                string selected_scheme_guid = this.current_display_scheme_guid;

                var curr_vals = setting.curr_setting_vals_by_scheme[selected_scheme_guid];
                setting.curr_setting_vals_by_scheme[selected_scheme_guid] = ((int)sender.Value, curr_vals.dc_val);

                bool success = (App.Current as App).set_powersetting(selected_scheme_guid, setting._parent_groupguid, sender.Tag.ToString(), (int)sender.Value);

                FireSettingSuccessFlash(setting, success);
            }
        }

        private void ComboBoxSelectionChanged(object _sender, SelectionChangedEventArgs e)
        {
            ComboBox sender = _sender as ComboBox;

            if (sender.IsLoaded && !this.settings_locked_for_navigation)
            {
                SettingStore setting = App.setting_data_dict[sender.Tag.ToString()];

                string selected_scheme_guid = this.current_display_scheme_guid;
                                
                var curr_vals = setting.curr_setting_vals_by_scheme[selected_scheme_guid];
                setting.curr_setting_vals_by_scheme[selected_scheme_guid] = ((int)sender.SelectedIndex, curr_vals.dc_val);

                bool success = (App.Current as App).set_powersetting(selected_scheme_guid, setting._parent_groupguid, sender.Tag.ToString(), (int)sender.SelectedIndex);

                FireSettingSuccessFlash(setting, success);
            }
        }

        private void FireSettingSuccessFlash(SettingStore setting, bool success)
        {
            if (success)
                (this.setting_element_dict[setting._setting_guid].Resources["success_animation"] as Storyboard).Begin();
            else
                (this.setting_element_dict[setting._setting_guid].Resources["fail_animation"] as Storyboard).Begin();
        }






        // Generate and Add navigationviewitems to navigationview
        private void SchemeNavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            this.SchemeNavigationView.MenuItems.Add(new NavigationViewItemHeader() { Content = "Installed Schemes", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.SlateBlue) });

            foreach (var scheme in App.scheme_data_dict)
            {
                // create elements to compose the scheme menuitem
                var displaybox = (this.Resources["DisplayBox"] as DataTemplate).LoadContent() as TextBlock;
                var editbox = (this.Resources["EditBox"] as DataTemplate).LoadContent() as TextBox;
                var activebox = (this.Resources["SchemeActiveBox"] as DataTemplate).LoadContent() as Grid;
                displaybox.Tag = scheme.Key;
                editbox.Tag = scheme.Key;
                activebox.Tag = scheme.Key;

                var stackpanel = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Stretch, Children = {displaybox, editbox, activebox} };

                var scheme_menuitem = new NavigationViewItem();
                scheme_menuitem.Content = stackpanel;
                scheme_menuitem.Tag = scheme.Key;
                scheme_menuitem.DataContext = scheme.Value;

                // register animators
                Color background_gray = (Application.Current.Resources["AppTitleBar_Grey"] as SolidColorBrush).Color;
                SolidColorBrush background_brush = new SolidColorBrush(background_gray);
                scheme_menuitem.Background = background_brush;

                register_animation(scheme_menuitem, background_brush, Colors.MediumSpringGreen, "success_animation");
                register_animation(scheme_menuitem, background_brush, Colors.MediumVioletRed, "fail_animation");

                // create flyouts for scheme menuitem's contextmenu
                var flyout_setactive = new MenuFlyoutItem() { Text = "Set Active", Tag = scheme.Key };
                flyout_setactive.Click += SchemeSetActiveFlyout_Clicked;

                var flyout_separator = new MenuFlyoutSeparator();

                var flyout_rename = new MenuFlyoutItem() { Text = "Rename Scheme", Tag = scheme.Key };
                flyout_rename.Click += SchemeRenameFlyout_Clicked;

                var flyout_copy = new MenuFlyoutItem() { Text = "Copy Scheme", Tag = scheme.Key };
                flyout_copy.Click += SchemeCopyFlyout_Clicked;

                var flyout_del = new MenuFlyoutItem() { Text = "Delete Scheme", Tag = scheme.Key };
                flyout_del.Click += SchemeDeleteFlyout_Clicked;

                scheme_menuitem.ContextFlyout = new MenuFlyout() { Items = { flyout_setactive, flyout_separator, flyout_rename, flyout_copy, flyout_del } };

                // each scheme menuitem gets a complete list of all groups as submenu items
                scheme_menuitem.MenuItems.Add(new NavigationViewItemHeader() { Content = "Setting Groups", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.SlateBlue) });

                foreach (var group in App.group_data_dict)
                {
                    scheme_menuitem.MenuItems.Add(new NavigationViewItem() { Content = group.Value._group_name, Tag = group.Key });
                }

                // add the menuitem to the navigationview's menuitems
                this.SchemeNavigationView.MenuItems.Add(scheme_menuitem);

                // store references to FrameworkElements for later use
                this.scheme_element_dict[scheme.Key] = scheme_menuitem;
                this.scheme_subelements_dict[scheme.Key] = new List<FrameworkElement> { displaybox, editbox, activebox };

            }

            NavSetSchemeItemActive(App.curr_system_applied_scheme_guid);
            this.SchemeNavigationView.SelectedItem = this.scheme_element_dict[App.curr_system_applied_scheme_guid];
        }

        // Set a scheme active via its context flyout
        private void SchemeSetActiveFlyout_Clicked(object sender, RoutedEventArgs e)
        {
            string scheme_guid = (sender as MenuFlyoutItem).Tag.ToString();

            bool success = (App.Current as App).set_powerscheme(scheme_guid);

            NavSetSchemeItemActive(scheme_guid);
            FireSchemeSuccessFlash(scheme_guid, success);
        }


        // Rename a scheme via its context flyout
        private async void SchemeRenameFlyout_Clicked(object sender, RoutedEventArgs e)
        {
            var senderitem = sender as MenuFlyoutItem;
            SchemeStore scheme_data = senderitem.DataContext as SchemeStore;

            scheme_data.displaybox_visible = "Collapsed";
            scheme_data.editbox_visible = "Visible";

            string scheme_guid = senderitem.Tag.ToString();
            FrameworkElement selected_schemeitem = this.scheme_element_dict[scheme_guid];

            var subelements_list = this.scheme_subelements_dict[scheme_guid];


            ContentDialog dialog = new ContentDialog();
            dialog.Title = "Save your work?";
            dialog.PrimaryButtonText = "Save";
            dialog.SecondaryButtonText = "Don't Save";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = ContentDialogButton.Primary;

            var result = await dialog.ShowAsync();


        }

        private void SchemeRenameTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                SchemeRenameFlyout_RenameConfirm(sender, e);
            }
            else if (e.Key == Windows.System.VirtualKey.Escape)
            {
                SchemeRenameFlyout_RenameCancel(sender, e);
            }
        }        

        private void SchemeRenameFlyout_RenameConfirm(object sender, RoutedEventArgs e)
        {
            SchemeStore scheme_data;
            string scheme_name;

            var senderitem = sender as TextBox;
            scheme_data = senderitem.DataContext as SchemeStore;
            scheme_name = senderitem.Text;
                 
            scheme_data.scheme_name = scheme_name;
            string scheme_guid = scheme_data.scheme_guid;
            
            scheme_data.editbox_visible = "Collapsed";
            scheme_data.displaybox_visible = "Visible";

            // do system rename of this scheme
            //(App.Current as App).set_powerscheme_name(scheme_guid, scheme_name);

            FrameworkElement selected_schemeitem = this.scheme_element_dict[scheme_guid];
            
        }

        private void SchemeRenameFlyout_RenameCancel(object sender, RoutedEventArgs e)
        {
            var senderitem = sender as TextBox;
            SchemeStore scheme_data = senderitem.DataContext as SchemeStore;
            string scheme_guid = scheme_data.scheme_guid;

            scheme_data.editbox_visible = "Collapsed";
            scheme_data.displaybox_visible = "Visible";

            FrameworkElement selected_schemeitem = this.scheme_element_dict[scheme_guid];
            
        }



        // Copy a scheme
        private void SchemeCopyFlyout_Clicked(object sender, RoutedEventArgs e)
        {
            // ask for a new scheme name with default in menu like "copy of Ultimate Performance"
            // copy the scheme with given scheme_guid through windows ps
            // create a scheme menuitem for the new scheme
        }

        // delete a scheme
        private void SchemeDeleteFlyout_Clicked(object sender, RoutedEventArgs e)
        {

        }



        // todo: bug. doesn't flash when flashing on currently-selected scheme
        private void FireSchemeSuccessFlash(string scheme_guid, bool success)
        {
            var applied_scheme_elem = (NavigationViewItem)this.scheme_element_dict[scheme_guid];

            if (success)
                (applied_scheme_elem.Resources["success_animation"] as Storyboard).Begin();
            else
                (applied_scheme_elem.Resources["fail_animation"] as Storyboard).Begin();
        }

        private void NavSetSchemeItemActive(string active_scheme_guid)
        {
            foreach (var kvp in this.scheme_element_dict)
            {
                if (kvp.Key == active_scheme_guid)
                {
                    App.scheme_data_dict[kvp.Key].activebox_visible = "Visible";
                }
                else
                {
                    App.scheme_data_dict[kvp.Key].activebox_visible = "Collapsed";
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
                if (App.scheme_data_dict.ContainsKey(selected_guid))
                {
                    selected_scheme_guid = selected_guid;                    
                    update_settings_to_scheme(selected_scheme_guid);

                    this.setting_elements.Clear();
                    foreach (var item in this.all_setting_elements) 
                        this.setting_elements.Add(item);
                }
                // else selected_guid is a groupid. get selected scheme. check for scheme change. filter settings in view.
                else
                {
                    string selected_group_guid = selected_guid;
                    selected_scheme_guid = (args.SelectedItemContainer.DataContext as SchemeStore).scheme_guid;

                    if (selected_scheme_guid != this.current_display_scheme_guid)
                        update_settings_to_scheme(selected_scheme_guid);

                    this.setting_elements.Clear();

                    foreach (var setting_element in this.setting_elements_by_group_dict[selected_group_guid])                    
                        this.setting_elements.Add(setting_element);                    
                }

                this.current_display_scheme_guid = selected_scheme_guid;
                this.settings_locked_for_navigation = false;
            }
        }

        private void update_settings_to_scheme(string selected_scheme_guid)
        {
            foreach (SettingStore setting_data in App.setting_data_dict.Values)
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
                this.setting_elements.Clear();

                var query = sender.Text.ToLower().Trim();
                bool header_added = false;
                FrameworkElement curr_groupheader_elem = null;

                foreach (var setting_elem in this.all_setting_elements)
                {
                    string elem_guid = setting_elem.Tag.ToString();

                    if (this.group_headerelements_dict.ContainsKey(elem_guid))
                    {
                        curr_groupheader_elem = this.group_headerelements_dict[elem_guid];
                        header_added = false;
                    }
                    else
                    {
                        var setting_data = App.setting_data_dict[elem_guid];
                        string setting_name = setting_data._setting_name.ToLower();
                        string setting_groupguid = setting_data._parent_groupguid;

                        if (setting_name.Contains(query))
                        {
                            if (!header_added)
                            {
                                this.setting_elements.Add(curr_groupheader_elem);
                                header_added = true;
                            }

                            this.setting_elements.Add(this.setting_element_dict[elem_guid]);
                        }
                    }
                }

                // todo: show "none found" element
                if (this.setting_elements.Count == 0) { } 
            }
        }

        private void CtrlF_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            SearchBox.Focus(FocusState.Programmatic);
        }
    }

}
