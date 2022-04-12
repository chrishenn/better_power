﻿using Microsoft.UI;
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
using better_power.Common;
using Windows.UI.Core;
using System.Diagnostics;
using System.Threading.Tasks;


namespace better_power
{    
    public sealed partial class MainPage : Page
    {
        // required ordereddict to maintain the ordering of headers and setting elements in the listView
        ObservableCollection<FrameworkElement> setting_elements = new ObservableCollection<FrameworkElement>();
        OrderedDictionary<string, FrameworkElement> setting_elements_dict = new OrderedDictionary<string, FrameworkElement>();
        Dictionary<string, List<FrameworkElement>> setting_elements_by_group_dict = new Dictionary<string, List<FrameworkElement>>();

        // ordering needed to support drag-n-drop reordering in navigationview
        OrderedDictionary<string, NavigationViewItem> scheme_elements_dict = new OrderedDictionary<string, NavigationViewItem>();
        ObservableCollection<NavigationViewItemBase> scheme_elements = new ObservableCollection<NavigationViewItemBase>();

        // TODO: these class-global state strings are all now suspect after the debacle with setting element's locking for navigation
        // indicate the guid of the scheme element in nav view that was most recently selected
        string selected_scheme_element_guid;

        // the parent schemeguid of the selected group or scheme in nav view
        string selected_parent_schemeguid;

        // the active power scheme guid as reported by system
        string systemactive_schemeguid;

        // indicate that settings elements shown in listview should not change
        bool settings_elements_locked_in_view = false;
       
        const string ANIMATION_SUCCESS_KEY = "animation_success";
        const string ANIMATION_FAIL_KEY = "animation_fail";
        const string BACKGROUND_BRUSH_KEY = "background_brush";

        public MainPage()
        {
            this.InitializeComponent();

            this.Generate_App_Elements();
        }

        private void Generate_App_Elements()
        {
            this.generate_setting_elements();
            this.generate_scheme_elements();

            register_animation(this.globalinfo, Colors.MediumSpringGreen, ANIMATION_SUCCESS_KEY);
            register_animation(this.globalinfo, Colors.MediumVioletRed, ANIMATION_FAIL_KEY);

            // todo: is this faster or slower because of thread-creation ovehead?
            //string systemactive_schemeguid = PowercfgManager.get_systemactive_schemeguid();
            string systemactive_schemeguid = Task.Run(() => PowercfgManager.get_systemactive_schemeguid()).Result;
            this.systemactive_schemeguid = systemactive_schemeguid;
            this.navigationview.SelectedItem = this.scheme_elements_dict[systemactive_schemeguid];

            UpdateUI_ShowSystemActiveScheme();
        }

        private async Task Application_Full_Refresh()
        {
            this.Frame.Navigate(typeof(WaitPage));

            await Task.Run(() => App.Current.Refresh_App_Data());

            // will construct a new MainPage object, and navigate the App.AppFrame to it
            this.Frame.Navigate(typeof(MainPage));
        }


        // -----------------------------------------------------------------------------------------------------------------------------------------------------


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
                register_animation(setting_elem, Colors.MediumSpringGreen, ANIMATION_SUCCESS_KEY);
                register_animation(setting_elem, Colors.MediumVioletRed, ANIMATION_FAIL_KEY);

                // add setting element to instance collections to find later
                this.setting_elements_dict[setting_guid] = setting_elem;
                this.setting_elements_by_group_dict[curr_groupid].Add(setting_elem);
            }

            foreach (var elem in this.setting_elements_dict.Values)
                this.setting_elements.Add(elem);
        }

        // settings elements: settings changed handler; numberbox
        private void NumberBoxValueChanged(NumberBox _sender, NumberBoxValueChangedEventArgs e)
        {
            NumberBox sender = _sender as NumberBox;
            if (!sender.IsEnabled || !sender.IsLoaded) return;

            string setting_guid = sender.Tag.ToString();
            SettingStore setting_data = App.setting_data_dict[setting_guid];
            var setting_elem = this.setting_elements_dict[setting_guid] as Panel;

            string selected_parent_schemeguid = this.selected_parent_schemeguid;

            var curr_vals = setting_data.curr_setting_vals_by_scheme[selected_parent_schemeguid];
            setting_data.curr_setting_vals_by_scheme[selected_parent_schemeguid] = ((int)sender.Value, curr_vals.dc_val);

            bool success = App.Current.power_manager.set_powersetting(selected_parent_schemeguid, setting_data._parent_groupguid, setting_guid, (int)sender.Value);
                        
            fire_success_animation(setting_elem, success);
        }

        // settings elements: settings changed handler; combobox
        private void ComboBoxSelectionChanged(object _sender, SelectionChangedEventArgs e)
        {
            ComboBox sender = _sender as ComboBox;
            if (!sender.IsEnabled || !sender.IsLoaded) return;

            string setting_guid = sender.Tag.ToString();
            SettingStore setting_data = App.setting_data_dict[setting_guid];
            var setting_elem = this.setting_elements_dict[setting_guid] as Panel;

            string selected_parent_schemeguid = this.selected_parent_schemeguid;

            var curr_vals = setting_data.curr_setting_vals_by_scheme[selected_parent_schemeguid];
            setting_data.curr_setting_vals_by_scheme[selected_parent_schemeguid] = ((int)sender.SelectedIndex, curr_vals.dc_val);

            bool success = App.Current.power_manager.set_powersetting(selected_parent_schemeguid, setting_data._parent_groupguid, setting_guid, (int)sender.SelectedIndex);

            fire_success_animation(setting_elem, success);
        }


        // -----------------------------------------------------------------------------------------------------------------------------------------------------


        // Generate navigationview elements 
        private void generate_scheme_elements()
        {
            this.scheme_elements.Add(
                 new NavigationViewItemHeader()
                 {
                    Content = "Installed Power Schemes",
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.SlateBlue),
                 });

            foreach (var scheme_kvp in App.scheme_data_dict)
            {
                var elem = generate_schememenuitem(scheme_kvp.Value);
                this.scheme_elements_dict[scheme_kvp.Key] = elem;
                this.scheme_elements.Add(elem);
            }
        }

        // generate a menuitem from scheme_data object
        private NavigationViewItem generate_schememenuitem(SchemeStore scheme_data)
        {
            string scheme_guid = scheme_data.scheme_guid;

            var scheme_menuitem = (this.Resources["Scheme_NavViewItemTemplate"] as DataTemplate).LoadContent() as NavigationViewItem;
            scheme_menuitem.DataContext = scheme_data;

            // register animators
            register_animation(scheme_menuitem, Colors.MediumSpringGreen, ANIMATION_SUCCESS_KEY, storyboard_tag: scheme_guid);
            register_animation(scheme_menuitem, Colors.MediumVioletRed, ANIMATION_FAIL_KEY, storyboard_tag: scheme_guid);

            // each scheme menuitem gets a complete list of all groups as submenu items            
            foreach (var group_data in App.group_data_dict)
                scheme_menuitem.MenuItems.Add(new NavigationViewItem() { Content = group_data.Value._group_name, Tag = group_data.Key });

            return scheme_menuitem;
        }




        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // Animation; generate animators and register into uielement resources
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

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

            if (!element.Resources.ContainsKey(BACKGROUND_BRUSH_KEY))
            {
                SolidColorBrush background_brush = new SolidColorBrush(background_gray);
                element.Background = background_brush;
                element.Resources[BACKGROUND_BRUSH_KEY] = background_brush;
            }

            var color_tocolor = new LinearColorKeyFrame() { Value = color, KeyTime = TimeSpan.FromSeconds(0.025) };
            var color_hold = new LinearColorKeyFrame() { Value = color, KeyTime = TimeSpan.FromSeconds(0.1) };
            var color_togray = new LinearColorKeyFrame() { Value = background_gray, KeyTime = TimeSpan.FromSeconds(0.25) };

            var animation = new ColorAnimationUsingKeyFrames();
            animation.KeyFrames.Add(color_tocolor);
            animation.KeyFrames.Add(color_hold);
            animation.KeyFrames.Add(color_togray);

            Storyboard.SetTarget(animation, element.Resources[BACKGROUND_BRUSH_KEY] as SolidColorBrush);
            Storyboard.SetTargetProperty(animation, "Color");

            Storyboard story_board = new Storyboard() { Children = { animation } };
            if (storyboard_tag != null)
                Storyboard.SetTargetName(story_board, storyboard_tag);

            element.Resources[animation_name] = story_board;
        }
        private static void register_animation_panel(Panel element, Color color, string animation_name, string storyboard_tag = null)
        {
            Color background_gray = (App.Current.Resources["AppTitleBar_Grey"] as SolidColorBrush).Color;

            if (!element.Resources.ContainsKey(BACKGROUND_BRUSH_KEY))
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

            Storyboard.SetTarget(animation, element.Resources[BACKGROUND_BRUSH_KEY] as SolidColorBrush);
            Storyboard.SetTargetProperty(animation, "Color");

            Storyboard story_board = new Storyboard() { Children = { animation } };
            if (storyboard_tag != null)
                Storyboard.SetTargetName(story_board, storyboard_tag);

            element.Resources[animation_name] = story_board;
        }


        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        // todo: differentiate this instance code from static animation-firing code
        private void fire_success_animation_scheme(string scheme_guid, bool success)
        {
            NavigationViewItem scheme_elem = this.scheme_elements_dict[scheme_guid];

            // we assume that the re-select code will already be registered into this elem's storyboard, because it is selected
            if (scheme_elem.IsSelected)
                this.navigationview.SelectedItem = null;

            fire_success_animation(scheme_elem, success);
        }
        private static void fire_success_animation(FrameworkElement element, bool success)
        {
            if (element is Control)
                fire_success_animation_control(element as Control, success);
            else if (element is Panel)
                fire_success_animation_panel(element as Panel, success);
            else
                throw new ArgumentException();
        }
        private static void fire_success_animation_control(Control element, bool success)
        {
            if (success)
                (element.Resources[ANIMATION_SUCCESS_KEY] as Storyboard).Begin();
            else
                (element.Resources[ANIMATION_FAIL_KEY] as Storyboard).Begin();
        }
        private static void fire_success_animation_panel(Panel element, bool success)
        {
            if (success)
                (element.Resources[ANIMATION_SUCCESS_KEY] as Storyboard).Begin();
            else
                (element.Resources[ANIMATION_FAIL_KEY] as Storyboard).Begin();
        }





        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // Connect loaded UIElements from the UI to their component objects stored in this Page instance
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        // add stored power setting cards collection to main ListView
        private void listview_Loaded(object _sender, RoutedEventArgs e)
        {
            listview_Load_Elements();
        }

        private void listview_Load_Elements()
        {
            this.listview.ItemsSource = this.setting_elements;
        }


        // add stored navigation elements to SchemeNavigationView
        private void navigationview_Loaded(object _object, RoutedEventArgs e)
        {
            navigationview_Load_Elements();
        }

        private void navigationview_Load_Elements()
        {
            this.navigationview.MenuItemsSource = this.scheme_elements;
        }





        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // Flyout Handlers: operations on single power schemes
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        // set a scheme as active
        private void SchemeSetActiveFlyout_Clicked(object sender, RoutedEventArgs e)
        {
            string scheme_guid = (sender as MenuFlyoutItem).Tag.ToString();

            bool success = App.Current.power_manager.set_systemactive_scheme(scheme_guid);
            if (success)
            {
                this.systemactive_schemeguid = scheme_guid;
                UpdateUI_ShowSystemActiveScheme();
            }

            fire_success_animation_scheme(scheme_guid, success);
            fly_global_notification("setting active power scheme", success, flash: true);
        }

        // rename a scheme
        private async void SchemeRenameFlyout_Clicked(object _sender, RoutedEventArgs e)
        {
            var sender = _sender as MenuFlyoutItem;
            SchemeStore scheme_data = sender.DataContext as SchemeStore;
            string scheme_guid = scheme_data.scheme_guid;
            string old_name = scheme_data.scheme_name + "";

            RenameDialog rename_dialog = new RenameDialog(old_name);
            rename_dialog.XamlRoot = this.XamlRoot;
            await rename_dialog.ShowAsync();

            if (rename_dialog.result == RenameResult.RenameSuccess)
            {
                string new_name = rename_dialog.new_name;
                if (new_name != old_name)
                {
                    bool success = App.Current.power_manager.powercfg_rename_scheme(scheme_guid, new_name);
                    if (success)
                        scheme_data.scheme_name = new_name;

                    fire_success_animation_scheme(scheme_guid, success);
                    fly_global_notification("renaming power scheme \"" + old_name + "\" to \"" + new_name + "\"", success, flash: true);
                }
            }
        }

        // copy a scheme
        private async void SchemeCopyFlyout_Clicked(object _sender, RoutedEventArgs e)
        {
            // ask for a new scheme name with default 
            var sender = _sender as MenuFlyoutItem;
            string scheme_guid = sender.Tag.ToString();
            SchemeStore scheme_data = sender.DataContext as SchemeStore;

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

                bool success = success1 && success2;
                if (success)
                    NewScheme_UpdateAppData_UpdateUIElems(new_scheme_name, new_scheme_guid);

                fire_success_animation_scheme(scheme_guid, success);
                fly_global_notification("copying power scheme \"" + scheme_data.scheme_name + "\" to \"" + new_scheme_name + "\"", success, flash: true);
            }
        }

        // delete a scheme
        private async void SchemeDeleteFlyout_Clicked(object _sender, RoutedEventArgs e)
        {
            var sender = _sender as MenuFlyoutItem;
            string scheme_guid = sender.Tag.ToString();

            // active scheme cannot be deleted - notify with dialog
            if (scheme_guid == this.systemactive_schemeguid)
            {
                ContentDialog cannot_delete_dialog = new ContentDialog()
                {
                    Title = "Cannot Delete",
                    Content = "You cannot delete the system active power scheme.",
                    CloseButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Close,

                    XamlRoot = this.XamlRoot,
                };

                await cannot_delete_dialog.ShowAsync();
                return;
            }

            // confirm delete
            var scheme_data = sender.DataContext as SchemeStore;
            ContentDialog confirm_delete_dialog = new ContentDialog()
            {
                Title = "Confirm Delete",
                Content = "Are you sure you want to delete scheme: " + scheme_data.scheme_name + " ?",

                PrimaryButtonText = "Confirm",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,

                XamlRoot = this.XamlRoot,
            };

            ContentDialogResult result = await confirm_delete_dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                bool success = App.Current.power_manager.powercfg_del_scheme(scheme_guid);
                if (success)
                {
                    // if deleted scheme was selected by navigationview, select the systemactive scheme instead
                    var scheme_elem = this.scheme_elements_dict[scheme_guid];
                    if (scheme_elem.IsSelected)
                        this.navigationview.SelectedItem = this.scheme_elements_dict[this.systemactive_schemeguid];

                    // delete scheme from navigationview
                    this.scheme_elements.Remove(scheme_elem);
                    this.scheme_elements_dict.Remove(scheme_guid);

                    // delete scheme from application data
                    App.scheme_data_dict.Remove(scheme_guid);
                    App.Current.remove_setting_values_one_scheme(scheme_guid);
                }
                fly_global_notification("deleting power scheme \"" + scheme_data.scheme_name+"\"", success, flash: true);
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
            {
                bool success = PowercfgManager.powercfg_export_scheme(scheme_guid, file.Path);

                fire_success_animation_scheme(scheme_guid, success);
                fly_global_notification("exporting power scheme \"" + scheme_data.scheme_name + "\" to file \"" + file.Name + "\"", success, flash: true);
            }
        }




        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // PaneFooter Button Handlers: core application functionality
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

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
                bool success = NewScheme_ImportFromFile_UpdateApp(file.Path);
                fly_global_notification("importing scheme from \"" + file.Name+"\"", success, flash: true);
            }
        }

        private async void Scheme_InstallButton_Tapped(object _sender, TappedRoutedEventArgs e)
        {
            var file_panel = new StackPanel() { Orientation = Orientation.Vertical };
            file_panel.Children.Add(new TextBlock()
            {
                Text = "These buttons will install a new copy of the given default power scheme. Existing schemes will not be modified.",
                TextWrapping = TextWrapping.WrapWholeWords,
                Margin = new Thickness(20),
            });

            foreach (int i in App.Current.classic_order)
            {
                string path = App.Current.classic_filepaths[i];
                string filename = System.IO.Path.GetFileName(path);
                string schemename = filename.Substring(0, filename.IndexOf("."));

                var file_button = new Button() { Content = schemename, Margin = new Thickness(2), Tag = path };

                // register animators into buttons's Resources
                register_animation(file_button, Colors.MediumSpringGreen, ANIMATION_SUCCESS_KEY);
                register_animation(file_button, Colors.MediumVioletRed, ANIMATION_FAIL_KEY);

                // button click handler
                file_button.Click += Scheme_InstallDialog_InstallButtonTapped;

                file_panel.Children.Add(file_button);
            }

            ContentDialog install_dialog = new ContentDialog()
            {
                Title = "Install Classic Powerschemes",
                Content = file_panel,

                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Close,

                XamlRoot = this.XamlRoot,
            };

            await install_dialog.ShowAsync();
        }

        private void Scheme_InstallDialog_InstallButtonTapped(object _sender, RoutedEventArgs e)
        {
            var sender = _sender as Button;
            string schemepath = sender.Tag.ToString();

            bool success = NewScheme_ImportFromFile_UpdateApp(schemepath);

            string name = schemepath.Substring(schemepath.LastIndexOf(@"\") + 1, schemepath.Length - 4 - schemepath.LastIndexOf(@"\") - 1);

            fire_success_animation(sender, success);
            fly_global_notification("installing classic scheme \""+name+"\"", success, flash: true);
        }

        private async void Scheme_ResetButton_Tapped(object _sender, TappedRoutedEventArgs e)
        {
            ContentDialog reset_dialog = new ContentDialog()
            {
                Title = "Reset Default Powerschemes",
                Content = "Resetting your system schemes to default will uninstall any custom or copied schemes, and revert each scheme to all its default setting values. " +
                "The system active scheme will be set to a default scheme.",

                PrimaryButtonText = "Reset Schemes to Default",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,

                XamlRoot = this.XamlRoot,
            };

            ContentDialogResult result = await reset_dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                bool success = PowercfgManager.powercfg_resetdefaultschemes();
                if (success)
                {
                    await this.Application_Full_Refresh();
                    fly_global_notification("resetting default schemes", success, flash: true);
                }
            }
        }

        private async void RefreshButton_Tapped(object _sender, TappedRoutedEventArgs e)
        {
            ContentDialog refresh_dialog = new ContentDialog()
            {
                Title = "Refresh App Data",
                Content = "This may take a few seconds. Continue to refresh?",

                PrimaryButtonText = "Refresh",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,

                XamlRoot = this.XamlRoot,
            };

            ContentDialogResult result = await refresh_dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await this.Application_Full_Refresh();
                fly_global_notification("refreshing application data", true, flash: true);
            }
        }




        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // Navgation (via navigationview): view settings in selected scheme or group
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        private void navigationview_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer == null) return;

            string selected_tag = args.SelectedItemContainer.Tag.ToString();
            string new_parent_schemeguid;

            // selected is a scheme. update settings cards current values in listview
            if (App.scheme_data_dict.ContainsKey(selected_tag))
            {
                new_parent_schemeguid = selected_tag;

                this.clear_last_selected_schemeelement();
                this.register_new_selected_schemeelement(selected_tag);

                // add all elements back into listview (unless we are re-selecting scheme after animation)
                if (!this.settings_elements_locked_in_view)
                {
                    this.setting_elements.Clear();
                    foreach (var elem in this.setting_elements_dict.Values)
                        this.setting_elements.Add(elem);
                }
            }

            // selected_guid is a groupid. get selected scheme. check for parent scheme change. filter to group's settings in view.
            else
            {
                string selected_group_guid = selected_tag;
                new_parent_schemeguid = (args.SelectedItemContainer.DataContext as SchemeStore).scheme_guid;

                this.clear_last_selected_schemeelement();

                // filter setting elements down to only those members of the selected group
                this.setting_elements.Clear();
                foreach (var elem in this.setting_elements_by_group_dict[selected_group_guid])
                    this.setting_elements.Add(elem);
            }

            // the parent schemeguid has changed (the guid that determines which settings values are shown in listview)
            if (new_parent_schemeguid != this.selected_parent_schemeguid)
            {
                navigation_update_settings_displaydata_to_scheme(new_parent_schemeguid);
                this.selected_parent_schemeguid = new_parent_schemeguid;
            }

            // always clear searchbox on scheme or group clicked
            this.searchbox.Text = "";
        }

        // another nav element is selected; de-register the old one
        private void clear_last_selected_schemeelement()
        {
            if (this.selected_scheme_element_guid == null)
                this.selected_scheme_element_guid = this.systemactive_schemeguid + "";

            string old_guid = this.selected_scheme_element_guid;
            var old_elem = this.scheme_elements_dict[old_guid];
            (old_elem.Resources[ANIMATION_SUCCESS_KEY] as Storyboard).Completed -= ReSelectItem_AfterAnimation;
            (old_elem.Resources[ANIMATION_FAIL_KEY] as Storyboard).Completed -= ReSelectItem_AfterAnimation;
        }

        // whenever a scheme element is selected, register the reselector into the new one
        private void register_new_selected_schemeelement(string new_guid)
        {
            if (this.selected_scheme_element_guid == null)
                this.selected_scheme_element_guid = this.systemactive_schemeguid + "";

            this.selected_scheme_element_guid = new_guid + "";

            var new_elem = this.scheme_elements_dict[new_guid];
            (new_elem.Resources[ANIMATION_SUCCESS_KEY] as Storyboard).Completed += ReSelectItem_AfterAnimation;
            (new_elem.Resources[ANIMATION_FAIL_KEY] as Storyboard).Completed += ReSelectItem_AfterAnimation;
        }

        // after a de-selected nav item has animated, re-select it here
        private void ReSelectItem_AfterAnimation(object sender, object e)
        {
            // we have to store the scheme_guid of this storyboard's owning menuitem in its TargetName because it doesn't have a tag
            string scheme_guid = Storyboard.GetTargetName(sender as Storyboard);

            this.settings_elements_locked_in_view = true;
            this.navigationview.SelectedItem = this.scheme_elements_dict[scheme_guid];
            this.settings_elements_locked_in_view = false;
        }

        private void navigation_update_settings_displaydata_to_scheme(string new_parent_schemeguid)
        {
            foreach (SettingStore setting_data in App.setting_data_dict.Values)
            {
                var vals = setting_data.curr_setting_vals_by_scheme[new_parent_schemeguid];
                setting_data.setting_enabled = false;

                setting_data.curr_ac_val = vals.ac_val;
                setting_data.curr_dc_val = vals.dc_val;

                setting_data.setting_enabled = true;
            }
        }




        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // System, UI, and Application state-change helpers
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        private bool NewScheme_ImportFromFile_UpdateApp(string schemepath)
        {
            var new_scheme_guid = Guid.NewGuid().ToString();
            bool success = PowercfgManager.powercfg_import_scheme(new_scheme_guid, schemepath);

            if (success)
            {
                string new_scheme_name = App.Current.power_manager.powercfg_get_schemename(new_scheme_guid);
                NewScheme_UpdateAppData_UpdateUIElems(new_scheme_name, new_scheme_guid);
            }
            return success;
        }

        private void NewScheme_UpdateAppData_UpdateUIElems(string new_scheme_name, string new_scheme_guid)
        {
            // update Application datastructures for new scheme
            SchemeStore new_scheme_data = new SchemeStore(new_scheme_name, new_scheme_guid);
            App.scheme_data_dict[new_scheme_guid] = new_scheme_data;
            App.Current.store_setting_values_one_scheme(new_scheme_guid);

            // update view elements for the new scheme
            var new_scheme_elem = generate_schememenuitem(new_scheme_data);
            this.scheme_elements.Add(new_scheme_elem);
            this.scheme_elements_dict[new_scheme_guid] = new_scheme_elem;

            fire_success_animation(new_scheme_elem, true);
        }

        private void UpdateUI_ShowSystemActiveScheme()
        {
            string active_scheme_guid = this.systemactive_schemeguid;

            // all elements must be visited
            foreach (var kvp in App.scheme_data_dict)
            {
                if (kvp.Key == active_scheme_guid)
                    kvp.Value.activebox_visible = "Visible";
                else
                    kvp.Value.activebox_visible = "Collapsed";
            }
        }

        // will attempt to fly notification if there's a MainPage loaded into the Application's AppFrame
        private static void fly_global_notification(string message, bool success, bool flash = false)
        {
            if (App.AppFrame.Content is MainPage)
            {
                string title;
                if (success) title = "SUCCESS";
                else title = "FAILED";

                var active_mainpage = App.AppFrame.Content as MainPage;

                active_mainpage.globalinfo.Title = title;
                active_mainpage.globalinfo.Message = message;
                active_mainpage.globalinfo.IsOpen = true;

                if (flash)
                    fire_success_animation(active_mainpage.globalinfo, success);
            }
        }




        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // SearchBox 
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        private void searchbox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
                return;

            var query = sender.Text.ToLower().Trim();

            this.setting_elements.Clear();

            bool header_added = false;
            FrameworkElement curr_groupheader_elem = null;

            foreach (var elem in this.setting_elements_dict.Values)
            {
                if (elem.DataContext == null)
                {
                    // elem is a listviewheaderitem
                    curr_groupheader_elem = elem;
                    header_added = false;
                }
                else
                {
                    var setting_data = elem.DataContext as SettingStore;
                    string setting_name = setting_data._setting_name.ToLower();

                    if (setting_name.Contains(query))
                    {
                        if (!header_added)
                        {
                            this.setting_elements.Add(curr_groupheader_elem);
                            header_added = true;
                        }

                        this.setting_elements.Add(elem);
                    }
                }
            }

        }



        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        // Hotkeys
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        private void CtrlF_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            this.searchbox.Focus(FocusState.Programmatic);
        }
        private async void Refresh_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            await this.Application_Full_Refresh();
        }
    }
}
