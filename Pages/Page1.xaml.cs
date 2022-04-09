using better_power.Common;
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
using System.Reflection;
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
        ObservableCollection<FrameworkElement> setting_elements = new ObservableCollection<FrameworkElement>();
        OrderedDictionary<string, FrameworkElement> setting_elements_dict = new OrderedDictionary<string, FrameworkElement>();
        Dictionary<string, List<FrameworkElement>> setting_elements_by_group_dict = new Dictionary<string, List<FrameworkElement>>();

        // ordering needed to support drag-n-drop reordering in navigationview
        OrderedDictionary<string, NavigationViewItem> scheme_elements_dict = new OrderedDictionary<string, NavigationViewItem>();
        StackPanel scheme_footerelements;
              
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

            Refresh_App_Elements();
        }

        // generate setting elements from App setting data objects
        private void generate_setting_elements()
        {
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

                    this.setting_elements_dict[curr_groupid] = curr_groupheader;

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

                register_animation(setting_elem, Colors.MediumSpringGreen, "success_animation");
                register_animation(setting_elem, Colors.MediumVioletRed, "fail_animation");

                // add setting element to instance collections to find later
                this.setting_elements_dict[setting_guid] = setting_elem;
                this.setting_elements_by_group_dict[curr_groupid].Add(setting_elem);
            }

            foreach (var elem in this.setting_elements_dict.Values) 
                this.setting_elements.Add(elem);
        }

        // settings elements: settings changed handler; numberbox
        private void NumberBoxValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs e)
        {
            if (sender.IsLoaded && !this.settings_locked_for_navigation)
            {
                SettingStore setting = App.setting_data_dict[sender.Tag.ToString()];

                string selected_scheme_guid = this.current_display_scheme_guid;

                var curr_vals = setting.curr_setting_vals_by_scheme[selected_scheme_guid];
                setting.curr_setting_vals_by_scheme[selected_scheme_guid] = ((int)sender.Value, curr_vals.dc_val);

                bool success = App.Current.power_manager.set_powersetting(selected_scheme_guid, setting._parent_groupguid, sender.Tag.ToString(), (int)sender.Value);

                var setting_elem = this.setting_elements_dict[setting._setting_guid];
                fire_success_animation_panel(setting_elem as Panel, success);
            }
        }
        
        // settings elements: settings changed handler; combobox
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

                var setting_elem = this.setting_elements_dict[setting._setting_guid];
                fire_success_animation_panel(setting_elem as Panel, success);
            }
        }




        private void Refresh_App_Elements()
        {
            this.settings_locked_for_navigation = true;

            this.setting_elements.Clear();
            this.setting_elements_dict.Clear();
            this.setting_elements_by_group_dict.Clear();
            this.scheme_elements_dict.Clear();

            this.SchemeNavigationView.MenuItems.Clear();

            this.generate_setting_elements();
            this.generate_scheme_elements();

            this.systemactive_schemeguid = App.Current.power_manager.get_systemactive_schemeguid();
            this.current_display_scheme_guid = this.systemactive_schemeguid;

            this.settings_locked_for_navigation = false;
        }

        // register animation to (a Control) or (a Panel); dispatcher
        private static void register_animation(FrameworkElement element, Color color, string animation_name, string storyboard_tag = null)
        {
            if (element is Control)
                register_animation_control(element as Control, color, animation_name, storyboard_tag);
            else if (element is Panel)
                register_animation_panel(element as Panel, color, animation_name, storyboard_tag);
            else
                throw new ArgumentException();
        }
        private static void register_animation_control(Control element, Color color, string animation_name, string storyboard_tag = null)
        {
            Color background_gray = (App.Current.Resources["AppTitleBar_Grey"] as SolidColorBrush).Color;

            if (!element.Resources.ContainsKey("background_brush"))
            {
                SolidColorBrush background_brush = new SolidColorBrush(background_gray);
                element.Background = background_brush;
                element.Resources["background_brush"] = background_brush;
            }

            var color_tocolor = new LinearColorKeyFrame() { Value = color, KeyTime = TimeSpan.FromSeconds(0.025) };
            var color_hold = new LinearColorKeyFrame() { Value = color, KeyTime = TimeSpan.FromSeconds(0.1) };
            var color_togray = new LinearColorKeyFrame() { Value = background_gray, KeyTime = TimeSpan.FromSeconds(0.25) };

            var animation = new ColorAnimationUsingKeyFrames();
            animation.KeyFrames.Add(color_tocolor);
            animation.KeyFrames.Add(color_hold);
            animation.KeyFrames.Add(color_togray);

            Storyboard.SetTarget(animation, element.Resources["background_brush"] as SolidColorBrush);
            Storyboard.SetTargetProperty(animation, "Color");

            Storyboard story_board = new Storyboard() { Children = { animation } };
            if (storyboard_tag != null) 
                Storyboard.SetTargetName(story_board, storyboard_tag);

            element.Resources[animation_name] = story_board;
        }
        private static void register_animation_panel(Panel element, Color color, string animation_name, string storyboard_tag = null)
        {
            Color background_gray = (App.Current.Resources["AppTitleBar_Grey"] as SolidColorBrush).Color;

            if (!element.Resources.ContainsKey("background_brush"))
            {
                SolidColorBrush background_brush = new SolidColorBrush(background_gray);
                element.Background = background_brush;
                element.Resources["background_brush"] = background_brush;
            }

            var color_tocolor = new LinearColorKeyFrame() { Value = color, KeyTime = TimeSpan.FromSeconds(0.025) };
            var color_hold = new LinearColorKeyFrame() { Value = color, KeyTime = TimeSpan.FromSeconds(0.1) };
            var color_togray = new LinearColorKeyFrame() { Value = background_gray, KeyTime = TimeSpan.FromSeconds(0.25) };

            var animation = new ColorAnimationUsingKeyFrames();
            animation.KeyFrames.Add(color_tocolor);
            animation.KeyFrames.Add(color_hold);
            animation.KeyFrames.Add(color_togray);

            Storyboard.SetTarget(animation, element.Resources["background_brush"] as SolidColorBrush);
            Storyboard.SetTargetProperty(animation, "Color");

            Storyboard story_board = new Storyboard() { Children = { animation } };
            if (storyboard_tag != null)
                Storyboard.SetTargetName(story_board, storyboard_tag);

            element.Resources[animation_name] = story_board;
        }

        // success animation runner; control
        private void fire_success_animation_control(Control element, bool success)
        {
            if (success)
                (element.Resources["success_animation"] as Storyboard).Begin();
            else
                (element.Resources["fail_animation"] as Storyboard).Begin();
        }
        // success animation runner; panel
        private void fire_success_animation_panel(Panel element, bool success)
        {
            if (success)
                (element.Resources["success_animation"] as Storyboard).Begin();
            else
                (element.Resources["fail_animation"] as Storyboard).Begin();
        }





        // add stored power setting cards collection to main ListView
        private void ListView_Load_Elements(object sender, RoutedEventArgs e)
        {
            this.ListView_main.ItemsSource = this.setting_elements;
        }


        // add stored navigation elements to SchemeNavigationView
        private void Navigationview_Load_Elements(object _object, RoutedEventArgs e)
        {
            this.SchemeNavigationView.MenuItems.Add(new NavigationViewItemHeader() { Content = "Installed Power Schemes", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.SlateBlue) });

            foreach (var elem in this.scheme_elements_dict.Values)
                this.SchemeNavigationView.MenuItems.Add(elem);

            this.SchemeNavigationView.PaneFooter = this.scheme_footerelements;

            UpdateUI_ShowSystemActiveScheme();
            this.SchemeNavigationView.SelectedItem = this.scheme_elements_dict[this.current_display_scheme_guid];
        }





        // Generate navigationview elements 
        private void generate_scheme_elements()
        {
            foreach (var scheme_kvp in App.scheme_data_dict)
            {
                // generate menuitem and bind to scheme_data object
                NavigationViewItem scheme_menuitem = generate_schememenuitem(scheme_kvp.Value);

                // store references to elements for later use
                this.scheme_elements_dict[scheme_kvp.Key] = scheme_menuitem;
            }

            var importitem = new NavigationViewItem() { Content = "Import Power Scheme", Icon = new SymbolIcon() { Symbol = Symbol.Import } };
            var installitem = new NavigationViewItem() { Content = "Install Classic Schemes", Icon = new SymbolIcon() { Symbol = Symbol.ImportAll } };
            var resetitem = new NavigationViewItem() { Content = "Reset Default Schemes", Icon = new SymbolIcon() { Symbol = Symbol.Undo } };
            var refreshitem = new NavigationViewItem() { Content = "Refresh App Data", Icon = new SymbolIcon() { Symbol = Symbol.Refresh } };
            importitem.Tapped += Scheme_ImportButton_Tapped;
            installitem.Tapped += Scheme_InstallButton_Tapped;
            resetitem.Tapped += Scheme_ResetButton_Tapped;
            refreshitem.Tapped += Scheme_RefreshButton_Tapped;

            // store to use when the navigationview loads
            this.scheme_footerelements = new StackPanel() { Children = {importitem, installitem, resetitem, refreshitem}, Orientation= Orientation.Vertical, HorizontalAlignment=HorizontalAlignment.Stretch };       
        }

        // generate a menuitem from scheme_data object
        private NavigationViewItem generate_schememenuitem(SchemeStore scheme_data)
        {
            string scheme_guid = scheme_data.scheme_guid;

            // create elements to compose the scheme menuitem
            var namebox = (this.Resources["NameBoxTemplate"] as DataTemplate).LoadContent() as TextBlock;
            var activebox = (this.Resources["SchemeActiveBoxTemplate"] as DataTemplate).LoadContent() as Grid;

            var stackpanel = new Grid()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { namebox, activebox }
            };

            var scheme_menuitem = new NavigationViewItem() { Icon = new SymbolIcon() { Symbol = Symbol.List } };
            scheme_menuitem.Content = stackpanel;
            scheme_menuitem.Tag = scheme_data.scheme_guid;
            scheme_menuitem.DataContext = scheme_data;

            // register animators
            register_animation(scheme_menuitem, Colors.MediumSpringGreen, "success_animation", storyboard_tag: scheme_guid);
            register_animation(scheme_menuitem, Colors.MediumVioletRed, "fail_animation", storyboard_tag: scheme_guid);

            // create flyouts for scheme menuitem's contextmenu
            var setactive = new MenuFlyoutItem() { Text = "Set Active", Icon = new SymbolIcon() { Symbol = Symbol.Accept }, Tag = scheme_guid };
            setactive.Click += SchemeSetActiveFlyout_Clicked;

            var separate1 = new MenuFlyoutSeparator();

            var export = new MenuFlyoutItem() { Text = "Export", Icon = new SymbolIcon() { Symbol = Symbol.Save }, Tag = scheme_guid };
            export.Click += SchemeExportFlyout_Clicked;

            var separate2 = new MenuFlyoutSeparator();

            var rename = new MenuFlyoutItem() { Text = "Rename", Icon = new SymbolIcon() { Symbol = Symbol.Edit }, Tag = scheme_guid };
            rename.Click += SchemeRenameFlyout_Clicked;

            var copy = new MenuFlyoutItem() { Text = "Copy", Icon = new SymbolIcon() { Symbol = Symbol.Copy }, Tag = scheme_guid };
            copy.Click += SchemeCopyFlyout_Clicked;

            var delete = new MenuFlyoutItem() { Text = "Delete", Icon = new SymbolIcon() { Symbol = Symbol.Delete }, Tag = scheme_guid };
            delete.Click += SchemeDeleteFlyout_Clicked;

            scheme_menuitem.ContextFlyout = new MenuFlyout() { Items = { setactive, separate1, export, separate2, rename, copy, delete } };

            // each scheme menuitem gets a complete list of all groups as submenu items
            scheme_menuitem.MenuItems.Add(new NavigationViewItemHeader() { Content = "Power Setting Groups", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.SlateBlue) });

            foreach (var group_data in App.group_data_dict)
                scheme_menuitem.MenuItems.Add(new NavigationViewItem() { Content = group_data.Value._group_name, Tag = group_data.Key });

            return scheme_menuitem;
        }





        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // Scheme Elements in Navigationview: Right-click context flyout handlers
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        // Set a scheme active via its context flyout
        private void SchemeSetActiveFlyout_Clicked(object sender, RoutedEventArgs e)
        {
            string scheme_guid = (sender as MenuFlyoutItem).Tag.ToString();
            NavigationViewItem scheme_elem = (NavigationViewItem)this.scheme_elements_dict[scheme_guid];

            bool success = App.Current.power_manager.set_systemactive_scheme(scheme_guid);
            if (success)
            {
                this.systemactive_schemeguid = scheme_guid;
                UpdateUI_ShowSystemActiveScheme();                
            }

            var storyboard_success = (scheme_elem.Resources["success_animation"] as Storyboard);
            var storyboard_fail = (scheme_elem.Resources["fail_animation"] as Storyboard);

            if (scheme_guid != this.current_display_scheme_guid)
            {
                storyboard_success.Completed -= ReSelectItem;
                storyboard_fail.Completed -= ReSelectItem;

                fire_success_animation_control(scheme_elem, success);
            }
            else
            {
                // this scheme is selected in navigationview. must de-select for flash to be visible                
                this.SchemeNavigationView.SelectedItem = null;

                // ReSelectItem will run when the success/fail animation is done running
                storyboard_success.Completed += ReSelectItem;
                storyboard_fail.Completed += ReSelectItem;

                fire_success_animation_control(scheme_elem, success);
            }
        }

        private void ReSelectItem(object sender, object e)
        {
            // we have to store the scheme_guid of this storyboard's owning menuitem because it doesn't have a tag
            string scheme_guid = Storyboard.GetTargetName(sender as Storyboard);
            this.SchemeNavigationView.SelectedItem = this.scheme_elements_dict[scheme_guid];
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
                    App.Current.power_manager.powercfg_rename_scheme(scheme_data.scheme_guid, new_name);
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
                bool success1 = App.Current.power_manager.powercfg_copy_scheme(scheme_guid, new_scheme_guid);
                bool success2 = App.Current.power_manager.powercfg_rename_scheme(new_scheme_guid, new_scheme_name);

                if (success1 && success2)
                    NewScheme_UpdateAppData_UpdateUIElems(new_scheme_name, new_scheme_guid);
            }
        }

        // todo: roll more import scheme code into this method
        private void NewScheme_UpdateAppData_UpdateUIElems(string new_scheme_name, string new_scheme_guid)
        {
            // update Application datastructures for new scheme
            SchemeStore new_scheme_data = new SchemeStore(new_scheme_name, new_scheme_guid);
            App.scheme_data_dict[new_scheme_guid] = new_scheme_data;
            App.Current.store_setting_values_one_scheme(new_scheme_guid);

            // update view elements for the new scheme
            var new_scheme_elem = generate_schememenuitem(new_scheme_data);
            this.SchemeNavigationView.MenuItems.Add(new_scheme_elem);
            this.scheme_elements_dict[new_scheme_guid] = new_scheme_elem;

            fire_success_animation_control(new_scheme_elem, true);
        }

        // delete a scheme
        private async void SchemeDeleteFlyout_Clicked(object sender, RoutedEventArgs e)
        {
            var senderitem = sender as MenuFlyoutItem;
            string scheme_guid = senderitem.Tag.ToString();
            var scheme_elem = this.scheme_elements_dict[scheme_guid];

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
                bool success = App.Current.power_manager.powercfg_del_scheme(scheme_guid);

                if (success)
                {
                    // if deleted scheme is selected by navigationview, select the systemactive scheme 
                    if (scheme_elem.IsSelected)
                        this.SchemeNavigationView.SelectedItem = this.scheme_elements_dict[systemactive_schemeguid];
                    // todo: this highlights the header in the listview - why 

                    // delete scheme from navigationview
                    this.SchemeNavigationView.MenuItems.Remove(scheme_elem);
                    this.scheme_elements_dict.Remove(scheme_guid);

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
                PowercfgManager.powercfg_export_scheme(scheme_guid, file.Path);
        }





        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // Navgation: view settings in each scheme; open scheme import dialog; open classic scheme install dialog
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        private void UpdateUI_ShowSystemActiveScheme()
        {
            string active_scheme_guid = this.systemactive_schemeguid;

            // ordering doesn't matter here. All elements must be visited
            foreach (var kvp in this.scheme_elements_dict)
            {
                if (kvp.Key == active_scheme_guid)                
                    App.scheme_data_dict[kvp.Key].activebox_visible = "Visible";                
                else                
                    App.scheme_data_dict[kvp.Key].activebox_visible = "Collapsed";                
            }
        }

        private void SchemeNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer == null) return;
            
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
                foreach (FrameworkElement elem in this.setting_elements_dict.Values)
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
                
            this.settings_locked_for_navigation = false;            
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



        private async void Scheme_ImportButton_Tapped(object _sender, TappedRoutedEventArgs e)
        {
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
            openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".");
            openPicker.FileTypeFilter.Add(".pow");

            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, App.Current._hwnd);
            Windows.Storage.StorageFile file = await openPicker.PickSingleFileAsync();

            if (file != null)
            {
                string new_scheme_guid = Guid.NewGuid().ToString();
                bool success = PowercfgManager.powercfg_import_scheme(new_scheme_guid, file.Path);
                if (success)
                {
                    string new_scheme_name = App.Current.power_manager.powercfg_get_schemename(new_scheme_guid);
                    NewScheme_UpdateAppData_UpdateUIElems(new_scheme_name, new_scheme_guid);
                }
            }
        }

        private async void Scheme_InstallButton_Tapped(object _sender, TappedRoutedEventArgs e)
        {
            var file_panel = new StackPanel() { Orientation = Orientation.Vertical };
            file_panel.Children.Add(new TextBlock()
            {
                Text = "These buttons will install a new copy of the given default scheme. Existing schemes will not be modified.",
                TextWrapping = TextWrapping.WrapWholeWords,
                Margin = new Thickness(20)
            });

            foreach (int i in App.Current.classic_order)
            {
                string path = App.Current.classic_filepaths[i];
                string filename = System.IO.Path.GetFileName(path);
                string schemename = filename.Substring(0, filename.IndexOf("."));

                var file_button = new Button() { Content = schemename, Margin = new Thickness(2), Tag = path };

                // register animators into buttons's Resources
                register_animation(file_button, Colors.MediumSpringGreen, "success_animation");
                register_animation(file_button, Colors.MediumVioletRed, "fail_animation");

                // button click handler
                file_button.Click += Scheme_InstallDialog_InstallButtonTapped;

                file_panel.Children.Add(file_button);
            }

            ContentDialog install_dialog = new ContentDialog()
            {
                Title = "Install Classic Powerschemes",
                Content = file_panel,

                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Close
            };

            install_dialog.XamlRoot = this.XamlRoot;
            await install_dialog.ShowAsync();
        }

        // makes a fresh scheme using default values from classic scheme name
        private void Scheme_InstallDialog_InstallButtonTapped(object sender, RoutedEventArgs e)
        {
            var senderitem = sender as Button;
            string schemepath = senderitem.Tag.ToString();

            var new_scheme_guid = Guid.NewGuid().ToString();
            bool success = PowercfgManager.powercfg_import_scheme(new_scheme_guid, schemepath);
            if (success)
            {
                string new_scheme_name = App.Current.power_manager.powercfg_get_schemename(new_scheme_guid);
                NewScheme_UpdateAppData_UpdateUIElems(new_scheme_name, new_scheme_guid);
            }

            fire_success_animation_control(senderitem, success);
        }


        private async void Scheme_ResetButton_Tapped(object _sender, TappedRoutedEventArgs e)
        {
            ContentDialog reset_dialog = new ContentDialog()
            {
                Title = "Reset Default Powerschemes",
                Content = "Resetting your system schemes to default will uninstall any custom or copied schemes, and revert each scheme to all its default setting values." +
                "The system active scheme will be set to a default scheme.",

                PrimaryButtonText = "Reset Schemes to Default",
                CloseButtonText = "Cancel",

                DefaultButton = ContentDialogButton.Primary
            };

            reset_dialog.XamlRoot = this.XamlRoot;
            ContentDialogResult result = await reset_dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // todo: show loading screen and lock app

                App.Current.power_manager.powercfg_resetdefaultschemes();

                App.Current.Refresh_App_Data();
                this.Refresh_App_Elements();

                this.ListView_Load_Elements(null, null);
                this.Navigationview_Load_Elements(null, null);
            }
        }

        private void Scheme_RefreshButton_Tapped(object _sender, TappedRoutedEventArgs e)
        {
            App.Current.Refresh_App_Data();
            this.Refresh_App_Elements();

            this.ListView_Load_Elements(null, null);
            this.Navigationview_Load_Elements(null, null);
        }

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // SearchBox 
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private void SearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                this.setting_elements.Clear();

                var query = sender.Text.ToLower().Trim();
                bool header_added = false;
                FrameworkElement curr_groupheader_elem = null;

                foreach (var setting_elem in this.setting_elements_dict.Values)
                {
                    string elem_guid = setting_elem.Tag.ToString();

                    if (App.group_data_dict.ContainsKey(elem_guid))
                    {
                        curr_groupheader_elem = this.setting_elements_dict[elem_guid];
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

                            this.setting_elements.Add(this.setting_elements_dict[elem_guid]);
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
