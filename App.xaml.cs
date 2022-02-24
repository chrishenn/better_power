using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace better_power
{
    using System;
    using System.Management;
    using Microsoft.Management.Infrastructure;
    using ORMi;

    //using System;
    using System.Diagnostics;
    using System.ComponentModel;
    using System.Text.RegularExpressions;

    public class Processor : WMIInstance
    {
        public string Name { get; set; }

        [WMIProperty("NumberOfCores")]
        public int Cores { get; set; }

        public string Description { get; set; }
    }



    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            //m_window.Activate();



            // read and store scheme_GUID, sub_GUID, setting_GUID, friendly name, help string - for each available setting

            // format powercfg command strings to set battery or plugged setting state 

            // changing a setting runs through powercfg with given GUID
            // Process.Start("powercfg", "args");

            //      future improvement: edit the appropriate registry key to set the correct setting via GUID


            // UI allows user to select a scheme to edit
            // UI allows user to change to a given scheme
            // UI allows user to install the classic schemes - from "power saving" to "ultimate perf"

            // populate scheme editing view with objects to change each setting 
            // shows friendly setting name
            // should indicate possible values to which we can set the setting
            // allow user to inspect help string for a given setting (clearly! not in a flyout!)



            // powercfg /query scheme_GUID, sub_GUID
            // returns all possible setting values for all settings within the specified subgroup for that scheme

            // setting on plug power
            // powercfg /setacvalueindex scheme_GUID sub_GUID setting_GUID setting_index

            // setting on battery power
            // powercfg /setdcvalueindex scheme_GUID sub_GUID setting_GUID setting_index


            this.Read_powerguids();


        }

        private Window m_window;

        private void Read_powerguids()
        {  
            WMIHelper helper = new WMIHelper("root\\CimV2\\power");
            var powerSettings = helper.Query("SELECT * FROM Win32_PowerSetting").ToList();
            var powerSettingsInSubgroups = helper.Query("SELECT * FROM Win32_PowerSettingInSubgroup").ToList();
            var powerCapabilities = helper.Query("SELECT * FROM Win32_PowerSettingCapabilities").ToList();

            Regex guid_reg = new Regex(@"\{([^\}]+)\}");
            char[] cut_chars = { '{', '}' };


            for (int i = 0; i < powerCapabilities.Count; i++)
            {
                var power_cap = powerCapabilities[i];

                string guid = guid_reg.Match(power_cap.ManagedElement).Value.Trim(cut_chars);

                var match_res = powerSettingsInSubgroups.Find(match_o => match_o.PartComponent.Contains(guid) );

                if (match_res == null) { continue; }

                var groupguid = match_res.GroupComponent.Trim(cut_chars);

                var match_setting = powerSettings.Find(match_o => match_o.InstanceID.Contains(guid));

                var descr = match_setting.ElementName;

                System.Diagnostics.Debug.WriteLine("descr: {}", descr);
                System.Diagnostics.Debug.WriteLine("guid: {0}, groupguid: {1}", guid, groupguid);
            }
        }

        // Uses the ProcessStartInfo class to start new processes, minimized
        void OpenWithStartInfo()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("IExplore.exe");
            //startInfo.WindowStyle = ProcessWindowStyle.Minimized;
            startInfo.Arguments = "www.northwindtraders.com";
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            Process.Start(startInfo);
        }
    }



}
