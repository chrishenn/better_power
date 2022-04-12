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

namespace better_power.Common
{
    public class AutoCloseInfoBar : InfoBar
    {    
        private DispatcherTimer _timer;
        private long _token;

        public int AutoCloseInterval { get; set; } = 4;


        public AutoCloseInfoBar() : base()
        {
            this.Loaded += AutoCloseInfoBar_Loaded;
            this.Unloaded += AutoCloseInfoBar_Unloaded;
        }
                

        private void AutoCloseInfoBar_Loaded(object sender, RoutedEventArgs e)
        {
            this._token = this.RegisterPropertyChangedCallback(AutoCloseInfoBar.IsOpenProperty, IsOpenChanged);
            if (IsOpen)            
                Open();            
        }

        private void AutoCloseInfoBar_Unloaded(object sender, RoutedEventArgs e)
        {
            this.UnregisterPropertyChangedCallback(AutoCloseInfoBar.IsOpenProperty, this._token);
        }

        private void IsOpenChanged(DependencyObject _infobar, DependencyProperty p)
        {
            var infobar = _infobar as AutoCloseInfoBar;
            if (infobar == null)            
                return;            
            if (p != AutoCloseInfoBar.IsOpenProperty)            
                return;
            
            if (infobar.IsOpen)            
                infobar.Open();            
            else            
                infobar.Close();            
        }

        private void Open()
        {
            this._timer = new DispatcherTimer();
            this._timer.Tick += Timer_Tick;
            this._timer.Interval = TimeSpan.FromSeconds(AutoCloseInterval);
            this._timer.Start();
        }

        private void Close()
        {
            if (this._timer == null)            
                return;
            
            this._timer.Stop();
            this._timer.Tick -= Timer_Tick;
        }

        private void Timer_Tick(object sender, object e)
        {
            this.IsOpen = false;
        }
    }
}

