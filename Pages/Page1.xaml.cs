using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Truncon.Collections;
using Windows.Foundation;
using Windows.Foundation.Collections;

using Windows.UI;


namespace better_power
{

    public sealed partial class Page1 : Page
    {
        // required ordereddict to maintain the ordering of headers and setting elements in the listView
        ObservableCollection<FrameworkElement> setting_elements;
        OrderedDictionary<string, FrameworkElement> setting_element_dict = new OrderedDictionary<string, FrameworkElement>();

        // ordering doesn't matter for these
        Dictionary<string, NavigationViewItem> scheme_element_dict = new Dictionary<string, NavigationViewItem>();
        Dictionary<string, List<FrameworkElement>> setting_elements_by_group_dict = new Dictionary<string, List<FrameworkElement>>();

        // current scheme guid being displayed in the main ListView UI
        string current_display_scheme_guid;

        string systemactive_schemeguid;

        // inidicate that setting values changing in listview should not fire system settings changes
        bool settings_locked_for_navigation = false;



        // generate listview settings elements; build instance lists and dicts
        public Page1()
        {
            this.InitializeComponent();
            App.Window.SetTitleBar(this.AppTitleBar);

            this.systemactive_schemeguid = App.Current.power_manager.get_systemactive_schemeguid();

            string curr_groupid = "";
            ListViewHeaderItem curr_groupheader = null;

            // ordering of setting_data_dict elements matters; ordering of setting_element_dict must match
            foreach (var kvp in App.setting_data_dict)
            {
                string setting_guid = kvp.Key;
                SettingStore setting = kvp.Value;

                if (setting._parent_groupguid != curr_groupid)
                {
                    curr_groupid = setting._parent_groupguid;
                    string curr_groupname = App.group_data_dict[curr_groupid]._group_name;

                    curr_groupheader = new ListViewHeaderItem() { Content = curr_groupname, Tag = curr_groupid };

                    this.setting_element_dict[curr_groupid] = curr_groupheader;

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
                this.setting_element_dict[setting_guid] = setting_elem;
                this.setting_elements_by_group_dict[curr_groupid].Add(setting_elem);
            }

            this.setting_elements = new ObservableCollection<FrameworkElement>(this.setting_element_dict.Values);
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

                bool success = App.Current.power_manager.set_powersetting(selected_scheme_guid, setting._parent_groupguid, sender.Tag.ToString(), (int)sender.Value);

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

                bool success = App.Current.power_manager.set_powersetting(selected_scheme_guid, setting._parent_groupguid, sender.Tag.ToString(), (int)sender.SelectedIndex);

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
            this.SchemeNavigationView.MenuItems.Add(new NavigationViewItemHeader() { Content = "Installed Power Schemes", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.SlateBlue) });

            foreach (var scheme_kvp in App.scheme_data_dict)
            {
                // generate menuitem from bound data-backing scheme_data object
                NavigationViewItem scheme_menuitem = Generate_SchemeMenuItem(scheme_kvp.Value);

                // add the menuitem to the navigationview's menuitems
                this.SchemeNavigationView.MenuItems.Add(scheme_menuitem);

                // store references to elements for later use
                this.scheme_element_dict[scheme_kvp.Key] = scheme_menuitem;
            }

            var importitem = new NavigationViewItem() { Content = "Import Power Scheme", Tag = "Import_NavItem", Icon=new SymbolIcon() {Symbol=Symbol.Import} };
            var installitem = new NavigationViewItem() { Content = "Install Classic Schemes", Tag = "Install_NavItem", Icon=new SymbolIcon() {Symbol=Symbol.ImportAll} };
            this.SchemeNavigationView.FooterMenuItems.Add(importitem);
            this.SchemeNavigationView.FooterMenuItems.Add(installitem);

            ShowSchemeSystemActive(this.systemactive_schemeguid);
            this.SchemeNavigationView.SelectedItem = this.scheme_element_dict[this.systemactive_schemeguid];
        }

        // generate menuitem from data-backing scheme_data object
        private NavigationViewItem Generate_SchemeMenuItem(SchemeStore scheme_data)
        {
            string scheme_guid = scheme_data.scheme_guid;

            // create elements to compose the scheme menuitem
            var namebox = (this.Resources["NameBoxTemplate"] as DataTemplate).LoadContent() as TextBlock;
            var activebox = (this.Resources["SchemeActiveBoxTemplate"] as DataTemplate).LoadContent() as Grid;
            namebox.Tag = scheme_guid;
            activebox.Tag = scheme_guid;

            var stackpanel = new Grid()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { namebox, activebox }
            };

            var scheme_menuitem = new NavigationViewItem() {Icon=new SymbolIcon() {Symbol=Symbol.List} };
            scheme_menuitem.Content = stackpanel;
            scheme_menuitem.Tag = scheme_data.scheme_guid;
            scheme_menuitem.DataContext = scheme_data;

            // register animators
            Color background_gray = (Application.Current.Resources["AppTitleBar_Grey"] as SolidColorBrush).Color;
            SolidColorBrush background_brush = new SolidColorBrush(background_gray);
            scheme_menuitem.Background = background_brush;

            register_animation(scheme_menuitem, background_brush, Colors.MediumSpringGreen, "success_animation");
            Storyboard.SetTargetName(scheme_menuitem.Resources["success_animation"] as Storyboard, scheme_guid);
            register_animation(scheme_menuitem, background_brush, Colors.MediumVioletRed, "fail_animation");
            Storyboard.SetTargetName(scheme_menuitem.Resources["fail_animation"] as Storyboard, scheme_guid);

            // create flyouts for scheme menuitem's contextmenu
            var setactive = new MenuFlyoutItem() { Text = "Set Active", Icon=new SymbolIcon(){Symbol=Symbol.Accept}, Tag=scheme_guid };
            setactive.Click += SchemeSetActiveFlyout_Clicked;

            var separate1 = new MenuFlyoutSeparator();

            var export = new MenuFlyoutItem() { Text = "Export", Icon = new SymbolIcon(){Symbol=Symbol.Save}, Tag=scheme_guid };
            export.Click += SchemeExportFlyout_Clicked;

            var separate2 = new MenuFlyoutSeparator();

            var rename = new MenuFlyoutItem() { Text = "Rename", Icon = new SymbolIcon(){Symbol=Symbol.Edit}, Tag =scheme_guid };
            rename.Click += SchemeRenameFlyout_Clicked;

            var copy = new MenuFlyoutItem() { Text = "Copy", Icon = new SymbolIcon() {Symbol=Symbol.Copy}, Tag = scheme_guid };
            copy.Click += SchemeCopyFlyout_Clicked;

            var delete = new MenuFlyoutItem() { Text = "Delete", Icon = new SymbolIcon() {Symbol = Symbol.Delete }, Tag = scheme_guid };
            delete.Click += SchemeDeleteFlyout_Clicked;

            scheme_menuitem.ContextFlyout = new MenuFlyout() { Items = { setactive, separate1, export, separate2, rename, copy, delete } };

            // each scheme menuitem gets a complete list of all groups as submenu items
            scheme_menuitem.MenuItems.Add(new NavigationViewItemHeader() { Content = "Power Setting Groups", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.SlateBlue) });

            foreach (var group_data in App.group_data_dict)
            {
                scheme_menuitem.MenuItems.Add(new NavigationViewItem() { Content = group_data.Value._group_name, Tag = group_data.Key });
            }

            return scheme_menuitem;
        }



        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // Set a scheme active via its context flyout
        private void SchemeSetActiveFlyout_Clicked(object sender, RoutedEventArgs e)
        {
            string scheme_guid = (sender as MenuFlyoutItem).Tag.ToString();
            NavigationViewItem scheme_elem = (NavigationViewItem)this.scheme_element_dict[scheme_guid];

            bool success = App.Current.power_manager.set_systemactive_powerscheme(scheme_guid);
            if (success)
            {
                ShowSchemeSystemActive(scheme_guid);
                this.systemactive_schemeguid = scheme_guid;
            }

            var storyboard_success = (scheme_elem.Resources["success_animation"] as Storyboard);
            var storyboard_fail = (scheme_elem.Resources["fail_animation"] as Storyboard);

            if (scheme_guid != this.current_display_scheme_guid)
            {
                storyboard_success.Completed -= ReSelectItem;
                storyboard_fail.Completed -= ReSelectItem;

                FireSchemeSuccessFlash(scheme_elem, success);
            }
            else
            {
                // this scheme is selected in navigationview. must de-select for flash to be visible                
                this.SchemeNavigationView.SelectedItem = null;

                // ReSelectItem will run when the success/fail animation is done running
                storyboard_success.Completed += ReSelectItem;
                storyboard_fail.Completed += ReSelectItem;

                FireSchemeSuccessFlash(scheme_elem, success);
            }
        }

        private void ReSelectItem(object sender, object e) 
        {
            // we have to store the scheme_guid of this storyboard's owning menuitem because it doesn't have a tag
            string scheme_guid = Storyboard.GetTargetName(sender as Storyboard);
            this.SchemeNavigationView.SelectedItem = this.scheme_element_dict[scheme_guid];                        
        }


        // Rename a scheme via its context flyout
        private async void SchemeRenameFlyout_Clicked(object sender, RoutedEventArgs e)
        {
            var senderitem = sender as MenuFlyoutItem;
            SchemeStore scheme_data = senderitem.DataContext as SchemeStore;

            RenameDialog rename_dialog = new RenameDialog(scheme_data.scheme_name);
            rename_dialog.XamlRoot = this.XamlRoot;
            await rename_dialog.ShowAsync();

            if (rename_dialog.result == RenameResult.RenameSuccess)
            {
                string new_name = rename_dialog.new_name;
                string curr_name = scheme_data.scheme_name;
                if (new_name != curr_name)
                {
                    // change scheme name in application state
                    scheme_data.scheme_name = new_name;

                    // do system rename of this scheme
                    App.Current.power_manager.set_powerscheme_name(scheme_data.scheme_guid, new_name);
                }
            }
        }
             
        // Copy a scheme
        private async void SchemeCopyFlyout_Clicked(object sender, RoutedEventArgs e)
        {
            // ask for a new scheme name with default 
            var senderitem = sender as MenuFlyoutItem;
            string scheme_guid = senderitem.Tag.ToString();
            SchemeStore scheme_data = senderitem.DataContext as SchemeStore;

            CopyDialog copy_dialog = new CopyDialog(scheme_data.scheme_name);
            copy_dialog.XamlRoot = this.XamlRoot;
            await copy_dialog.ShowAsync();

            if (copy_dialog.result == CopyResult.CopySuccess)
            {
                string new_scheme_guid = Guid.NewGuid().ToString();
                string new_scheme_name = copy_dialog.new_name;

                // copy scheme in system. rename new scheme to new name.
                bool success1 = App.Current.power_manager.powercfg_copy_powerscheme(scheme_guid, new_scheme_guid);
                bool success2 = App.Current.power_manager.set_powerscheme_name(new_scheme_guid, new_scheme_name);

                if (success1 && success2)                 
                    NewScheme_UpdateAppData_UpdateUIElems(new_scheme_name, new_scheme_guid);                
            }

        }

        private void NewScheme_UpdateAppData_UpdateUIElems(string new_scheme_name, string new_scheme_guid)
        {
            // update Application datastructures for new scheme
            SchemeStore new_scheme_data = new SchemeStore(new_scheme_name, new_scheme_guid);
            App.scheme_data_dict[new_scheme_guid] = new_scheme_data;
            App.Current.store_setting_values_one_scheme(new_scheme_guid);

            // update view elements for the new scheme
            var new_scheme_elem = Generate_SchemeMenuItem(new_scheme_data);
            this.SchemeNavigationView.MenuItems.Add(new_scheme_elem);
            this.scheme_element_dict[new_scheme_guid] = new_scheme_elem;

            FireSchemeSuccessFlash(new_scheme_elem, true);
        }

        // delete a scheme
        private async void SchemeDeleteFlyout_Clicked(object sender, RoutedEventArgs e)
        {
            var senderitem = sender as MenuFlyoutItem;
            string scheme_guid = senderitem.Tag.ToString();
            var scheme_elem = this.scheme_element_dict[scheme_guid];

            if (scheme_guid == this.systemactive_schemeguid)
            {
                // flyout a dialog - active scheme cannot be deleted
                ContentDialog no_delete_dialog = new ContentDialog()
                {
                    Title = "Cannot Delete",
                    Content = "You cannot delete the system active power scheme",
                    CloseButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Close
                };

                no_delete_dialog.XamlRoot = this.XamlRoot;
                await no_delete_dialog.ShowAsync();
                return;
            }
            
            // dialog - confirm delete
            ContentDialog confirm_delete_dialog = new ContentDialog()
            {
                Title = "Confirm Delete",
                Content = "Are you sure you want to delete?",
                PrimaryButtonText = "Confirm",
                CloseButtonText = "Cancel",

                DefaultButton = ContentDialogButton.Primary
            };

            confirm_delete_dialog.XamlRoot = this.XamlRoot;
            ContentDialogResult result = await confirm_delete_dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // delete scheme in system
                bool success = App.Current.power_manager.powercfg_del_powerscheme(scheme_guid);

                if (success)
                {
                    // if deleted scheme is selected by navigationview, select the systemactive scheme 
                    if (scheme_elem.IsSelected)
                        this.SchemeNavigationView.SelectedItem = this.scheme_element_dict[systemactive_schemeguid];
                    // todo: this highlights the header in the listview - why 

                    // delete scheme from navigationview
                    this.SchemeNavigationView.MenuItems.Remove(scheme_elem);
                    this.scheme_element_dict.Remove(scheme_guid);

                    // delete scheme from application data
                    App.scheme_data_dict.Remove(scheme_guid);
                    App.Current.remove_setting_values_one_scheme(scheme_guid);
                }
            }
        }

        // export a scheme
        private async void SchemeExportFlyout_Clicked(object sender, RoutedEventArgs e)
        {
            var senderitem = sender as MenuFlyoutItem;
            string scheme_guid = senderitem.Tag.ToString();
            var scheme_data = App.scheme_data_dict[scheme_guid];

            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("All Files", new List<string>() { "." });
            savePicker.FileTypeChoices.Add(".pow File", new List<string>() { ".pow" });
            savePicker.SuggestedFileName = scheme_data.scheme_name + ".pow";

            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, App.Current._hwnd);
            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();

            if (file != null)            
                App.Current.power_manager.powercfg_export_scheme(scheme_guid, file.Path);            
        }





        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        private void FireSchemeSuccessFlash(NavigationViewItem applied_scheme_elem, bool success)
        {
            if (success)
                (applied_scheme_elem.Resources["success_animation"] as Storyboard).Begin();
            else
                (applied_scheme_elem.Resources["fail_animation"] as Storyboard).Begin();
        }

        private void ShowSchemeSystemActive(string active_scheme_guid)
        {
            // ordering doesn't matter here. All elements must be visited
            foreach (var kvp in this.scheme_element_dict)
            {
                if (kvp.Key == active_scheme_guid)
                {
                    (App.scheme_data_dict[kvp.Key] as SchemeStore).activebox_visible = "Visible";
                }
                else
                {
                    (App.scheme_data_dict[kvp.Key] as SchemeStore).activebox_visible = "Collapsed";
                }
            }
        }

        private async void SchemeNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer != null)
            {
                this.settings_locked_for_navigation = true;

                string selected_tag = args.SelectedItemContainer.Tag.ToString();
                string selected_scheme_guid;

                // selected is a scheme. update settings cards current values in listview
                if (App.scheme_data_dict.ContainsKey(selected_tag))
                {
                    selected_scheme_guid = selected_tag;

                    // if the application is already showing these scheme values, no need to change to them
                    if (selected_scheme_guid != this.current_display_scheme_guid)
                    {
                        update_settings_displaydata_to_scheme(selected_scheme_guid);
                        this.current_display_scheme_guid = selected_scheme_guid;
                    }

                    // but we always add all elements back into listview when scheme clicked
                    this.setting_elements.Clear();
                    foreach (FrameworkElement elem in this.setting_element_dict.Values)
                        this.setting_elements.Add(elem);
                }
                // selected_guid is a groupid. get selected scheme. check for scheme change. filter to group's settings in view.
                else if (App.group_data_dict.ContainsKey(selected_tag))
                {
                    string selected_group_guid = selected_tag;
                    selected_scheme_guid = (args.SelectedItemContainer.DataContext as SchemeStore).scheme_guid;

                    if (selected_scheme_guid != this.current_display_scheme_guid)
                    {
                        update_settings_displaydata_to_scheme(selected_scheme_guid);
                        this.current_display_scheme_guid = selected_scheme_guid;
                    }

                    this.setting_elements.Clear();
                    foreach (var setting_element in this.setting_elements_by_group_dict[selected_group_guid])
                        this.setting_elements.Add(setting_element);
                }
                else if (selected_tag == "Import_NavItem") 
                {
                    var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
                    openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                    openPicker.FileTypeFilter.Add( "." );
                    openPicker.FileTypeFilter.Add( ".pow" );

                    WinRT.Interop.InitializeWithWindow.Initialize(openPicker, App.Current._hwnd);
                    Windows.Storage.StorageFile file = await openPicker.PickSingleFileAsync();

                    if (file != null)
                    {
                        string new_scheme_guid = Guid.NewGuid().ToString();
                        App.Current.power_manager.powercfg_import_scheme(new_scheme_guid, file.Path);
                        string new_scheme_name = App.Current.power_manager.powercfg_get_schemename(new_scheme_guid);
                        NewScheme_UpdateAppData_UpdateUIElems(new_scheme_guid, new_scheme_name);
                    }
                }
                else if (selected_tag == "Install_NavItem") 
                { }
                                
                this.settings_locked_for_navigation = false;
            }
        }

        private void update_settings_displaydata_to_scheme(string selected_scheme_guid)
        {
            foreach (SettingStore setting_data in App.setting_data_dict.Values)
            {
                var vals = setting_data.curr_setting_vals_by_scheme[selected_scheme_guid];

                setting_data.curr_ac_val = vals.ac_val;
                setting_data.curr_dc_val = vals.dc_val;
            }
        }




        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private void SearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                this.setting_elements.Clear();

                var query = sender.Text.ToLower().Trim();
                bool header_added = false;
                FrameworkElement curr_groupheader_elem = null;

                foreach (var setting_elem in this.setting_element_dict.Values)
                {
                    string elem_guid = setting_elem.Tag.ToString();

                    if (App.group_data_dict.ContainsKey(elem_guid))
                    {
                        curr_groupheader_elem = this.setting_element_dict[elem_guid];
                        header_added = false;
                    }
                    else
                    {
                        var setting_data = App.setting_data_dict[elem_guid];
                        string setting_name = setting_data._setting_name.ToLower();

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
            }
        }

        private void CtrlF_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            SearchBox.Focus(FocusState.Programmatic);
        }
    }

}
