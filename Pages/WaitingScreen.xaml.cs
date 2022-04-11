using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace better_power
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WaitingScreen : Page
    {
        internal Frame rootFrame;
        public WaitingScreen()
        {
            this.InitializeComponent();

            //this.rootFrame = new Frame();
        }

        public void DismissWaitingScreen()
        {
            // Navigate to mainpage
            this.rootFrame.Navigate(typeof(MainPage));
            // Place the frame in the current Window
            App.Window.Content = rootFrame;
        }
    }
}
