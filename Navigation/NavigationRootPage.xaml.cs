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



namespace better_power
{
    public sealed partial class NavigationRootPage : Page
    {
        public static NavigationRootPage Current;
        public static Frame RootFrame = null;

        //public VirtualKey ArrowKey;

        public Action NavigationViewLoaded { get; set; }

        public DeviceType DeviceFamily { get; set; }




        public NavigationRootPage()
        {
            this.InitializeComponent();
        }



        public string GetAppTitleFromSystem()
        {
            return "Better Power";
        }

        public bool CheckNewControlSelected()
        {
            return false;
        }

        public void EnsureNavigationSelection(string id)
        {

        }

        //private void AddNavigationMenuItems()
        //{
        //    foreach (var group in )
        //    {
        //        var itemGroup = new Microsoft.UI.Xaml.Controls.NavigationViewItem() { Content = group.Title, Tag = group.UniqueId, DataContext = group, Icon = GetIcon(group.ImagePath) };

        //        var groupMenuFlyoutItem = new MenuFlyoutItem() { Text = $"Copy Link to {group.Title} Samples", Icon = new FontIcon() { Glyph = "\uE8C8" }, Tag = group };
        //        groupMenuFlyoutItem.Click += this.OnMenuFlyoutItemClick;
        //        itemGroup.ContextFlyout = new MenuFlyout() { Items = { groupMenuFlyoutItem } };

        //        AutomationProperties.SetName(itemGroup, group.Title);

        //        foreach (var item in group.Items)
        //        {
        //            var itemInGroup = new Microsoft.UI.Xaml.Controls.NavigationViewItem() { Content = item.Title, Tag = item.UniqueId, DataContext = item, Icon = GetIcon(item.ImagePath) };

        //            var itemInGroupMenuFlyoutItem = new MenuFlyoutItem() { Text = $"Copy Link to {item.Title} Sample", Icon = new FontIcon() { Glyph = "\uE8C8" }, Tag = item };
        //            itemInGroupMenuFlyoutItem.Click += this.OnMenuFlyoutItemClick;
        //            itemInGroup.ContextFlyout = new MenuFlyout() { Items = { itemInGroupMenuFlyoutItem } };

        //            itemGroup.MenuItems.Add(itemInGroup);
        //            AutomationProperties.SetName(itemInGroup, item.Title);
        //        }

        //        //NavigationViewControl.MenuItems.Add(itemGroup);

        //        if (group.UniqueId == "AllControls")
        //        {
        //            this._allControlsMenuItem = itemGroup;
        //        }
        //        else if (group.UniqueId == "NewControls")
        //        {
        //            this._newControlsMenuItem = itemGroup;
        //        }
        //    }
        //    _newControlsMenuItem.Loaded += OnNewControlsMenuItemLoaded;
        //}

        private void OnMenuFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            switch ((sender as MenuFlyoutItem).Tag)
            {

            }
        }

        //private static IconElement GetIcon(string imagePath)
        //{
            //return imagePath.ToLowerInvariant().EndsWith(".png") ?
            //            (IconElement)new BitmapIcon() { UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute), ShowAsMonochrome = false } :
            //            (IconElement)new FontIcon()
            //            {
            //                // FontFamily = new FontFamily("Segoe MDL2 Assets"),
            //                Glyph = imagePath
            //            };
        //}

        private void SetDeviceFamily()
        {

        }

        private void OnNewControlsMenuItemLoaded(object sender, RoutedEventArgs e)
        {

        }



        private void OnNavigationViewControlLoaded(object sender, RoutedEventArgs e)
        {
            // Delay necessary to ensure NavigationView visual state can match navigation
            //Task.Delay(500).ContinueWith(_ => this.NavigationViewLoaded?.Invoke(), TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void OnNavigationViewItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            // Close any open teaching tips before navigation
            CloseTeachingTips();

            if (args.InvokedItemContainer.IsSelected)
            {
                // Clicked on an item that is already selected,
                // Avoid navigating to the same page again causing movement.
                return;
            }

            //if (args.IsSettingsInvoked)
            //{
            //    if (rootFrame.CurrentSourcePageType != typeof(SettingsPage))
            //    {
            //        rootFrame.Navigate(typeof(SettingsPage));
            //    }
            //}
            //else
            //{
            //    var invokedItem = args.InvokedItemContainer;

            //    if (invokedItem == _allControlsMenuItem)
            //    {
            //        if (rootFrame.CurrentSourcePageType != typeof(AllControlsPage))
            //        {
            //            rootFrame.Navigate(typeof(AllControlsPage));
            //        }
            //    }
            //    else if (invokedItem == _newControlsMenuItem)
            //    {
            //        if (rootFrame.CurrentSourcePageType != typeof(NewControlsPage))
            //        {
            //            rootFrame.Navigate(typeof(NewControlsPage));
            //        }
            //    }
            //    else
            //    {
            //        if (invokedItem.DataContext is ControlInfoDataGroup)
            //        {
            //            var itemId = ((ControlInfoDataGroup)invokedItem.DataContext).UniqueId;
            //            rootFrame.Navigate(typeof(SectionPage), itemId);
            //        }
            //        else if (invokedItem.DataContext is ControlInfoDataItem)
            //        {
            //            var item = (ControlInfoDataItem)invokedItem.DataContext;
            //            rootFrame.Navigate(typeof(ItemPage), item.UniqueId);
            //        }

            //    }
            //}
        }

        private void OnRootFrameNavigated(object sender, NavigationEventArgs e)
        {
            // Close any open teaching tips before navigation
            CloseTeachingTips();

        }
        private void CloseTeachingTips()
        {

        }

        private void OnControlsSearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
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

        private void OnControlsSearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {

        }

        public void EnsureItemIsVisibleInNavigation(string name)
        {

        }

        private void NavigationViewControl_PaneClosing(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewPaneClosingEventArgs args)
        {
            UpdateAppTitleMargin(sender);
        }

        private void NavigationViewControl_PaneOpening(Microsoft.UI.Xaml.Controls.NavigationView sender, object args)
        {
            UpdateAppTitleMargin(sender);
        }

        private void NavigationViewControl_DisplayModeChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs args)
        {

        }

        private void UpdateAppTitleMargin(Microsoft.UI.Xaml.Controls.NavigationView sender)
        {

        }

        private void UpdateHeaderMargin(Microsoft.UI.Xaml.Controls.NavigationView sender)
        {

        }

        private void CtrlF_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            //controlsSearchBox.Focus(FocusState.Programmatic);
        }
    }


    public enum DeviceType
    {
        Desktop,
        Mobile,
        Other,
        Xbox
    }
}
