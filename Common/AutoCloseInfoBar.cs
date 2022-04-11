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
            _token = this.RegisterPropertyChangedCallback(IsOpenProperty, IsOpenChanged);
            if (IsOpen)
            {
                Open();
            }
        }

        private void AutoCloseInfoBar_Unloaded(object sender, RoutedEventArgs e)
        {
            this.UnregisterPropertyChangedCallback(IsOpenProperty, _token);
        }

        private void IsOpenChanged(DependencyObject o, DependencyProperty p)
        {
            var that = o as AutoCloseInfoBar;
            if (that == null)
            {
                return;
            }

            if (p != IsOpenProperty)
            {
                return;
            }

            if (that.IsOpen)
            {
                that.Open();
            }
            else
            {
                that.Close();
            }
        }

        private void Open()
        {
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = TimeSpan.FromSeconds(AutoCloseInterval);
            _timer.Start();
        }

        private void Close()
        {
            if (_timer == null)
            {
                return;
            }

            _timer.Stop();
            _timer.Tick -= Timer_Tick;
        }

        private void Timer_Tick(object sender, object e)
        {
            this.IsOpen = false;
        }
    }
}

