﻿#pragma checksum "C:\Users\Admin\source\repos\better_power\Pages\Page1.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "3DE2A718D12F94F5AC5C565EF5578C432F810A288F69138C7F52DA9EB56367C1"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace better_power
{
    partial class Page1 : 
        global::Microsoft.UI.Xaml.Controls.Page, 
        global::Microsoft.UI.Xaml.Markup.IComponentConnector
    {

        /// <summary>
        /// Connect()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 1.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 2: // Pages\Page1.xaml line 17
                {
                    global::Microsoft.UI.Xaml.Controls.Grid element2 = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.Grid>(target);
                    ((global::Microsoft.UI.Xaml.Controls.Grid)element2).Loaded += this.Page1_GridLoaded;
                }
                break;
            case 3: // Pages\Page1.xaml line 33
                {
                    this.AppTitleBar = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.Grid>(target);
                }
                break;
            case 4: // Pages\Page1.xaml line 52
                {
                    this.SchemeNavigationView = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.NavigationView>(target);
                    ((global::Microsoft.UI.Xaml.Controls.NavigationView)this.SchemeNavigationView).DisplayModeChanged += this.SchemeNavigationView_DisplayModeChanged;
                    ((global::Microsoft.UI.Xaml.Controls.NavigationView)this.SchemeNavigationView).ItemInvoked += this.SchemeNavigationView_ItemInvoked;
                    ((global::Microsoft.UI.Xaml.Controls.NavigationView)this.SchemeNavigationView).Loaded += this.SchemeNavigationView_Loaded;
                    ((global::Microsoft.UI.Xaml.Controls.NavigationView)this.SchemeNavigationView).PaneOpening += this.SchemeNavigationView_PaneOpen;
                    ((global::Microsoft.UI.Xaml.Controls.NavigationView)this.SchemeNavigationView).PaneClosing += this.SchemeNavigationView_PaneClose;
                }
                break;
            case 5: // Pages\Page1.xaml line 97
                {
                    this.ScrollViewer = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.ScrollViewer>(target);
                    ((global::Microsoft.UI.Xaml.Controls.ScrollViewer)this.ScrollViewer).Loaded += this.ScrollViewer_Loaded;
                }
                break;
            case 6: // Pages\Page1.xaml line 78
                {
                    this.SchemeNavigationView_SearchBox = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.AutoSuggestBox>(target);
                    ((global::Microsoft.UI.Xaml.Controls.AutoSuggestBox)this.SchemeNavigationView_SearchBox).QuerySubmitted += this.SchemeNavigationView_SearchBoxQuerySubmitted;
                    ((global::Microsoft.UI.Xaml.Controls.AutoSuggestBox)this.SchemeNavigationView_SearchBox).TextChanged += this.SchemeNavigationView_SearchBoxTextChanged;
                }
                break;
            case 7: // Pages\Page1.xaml line 89
                {
                    global::Microsoft.UI.Xaml.Input.KeyboardAccelerator element7 = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Input.KeyboardAccelerator>(target);
                    ((global::Microsoft.UI.Xaml.Input.KeyboardAccelerator)element7).Invoked += this.CtrlF_Invoked;
                }
                break;
            case 8: // Pages\Page1.xaml line 44
                {
                    this.AppTitleTextBlock = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.TextBlock>(target);
                }
                break;
            default:
                break;
            }
            this._contentLoaded = true;
        }

        /// <summary>
        /// GetBindingConnector(int connectionId, object target)
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 1.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Microsoft.UI.Xaml.Markup.IComponentConnector GetBindingConnector(int connectionId, object target)
        {
            global::Microsoft.UI.Xaml.Markup.IComponentConnector returnValue = null;
            return returnValue;
        }
    }
}

