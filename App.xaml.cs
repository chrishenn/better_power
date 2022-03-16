using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Management.Infrastructure;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Windows.ApplicationModel.Core;

using System.Collections;
using System.Management;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Management.Automation;

using ORMi;
using better_power.Common;


namespace better_power
{


    // TODO

    // group selection in navvigation view
    // flash green on success, red on failure (setting applied, scheme applied)
    // create a new scheme by copying an existing one
    // application icon and name in taskbar
    // search box
    // error handling

    // setting cards:
    //      indicate possible values to which we can set the setting
    //      data units + format     
    //      range checking
    //      ac + dc menus

    // (?) observe settings changes from the OS 
    // (?) change settings via registry key
    // (?) install the classic schemes - from "power saving" to "ultimate perf"
    // (?) pull power setting info from system objects


    public class SettingStore : BindableBase
    {
        public SettingStore(string setting_guid, string setting_name, string setting_descr, string parent_groupguid)
        {
            _setting_guid = setting_guid;
            _setting_name = setting_name;
            _setting_descr = setting_descr;
            _parent_groupguid = parent_groupguid;
        }

        public string _setting_guid { get; set; }
        public string _setting_name { get; set; }
        public string _setting_descr { get; set; }
        public string _parent_groupguid { get; set; }

        public bool is_range;

        public string min_val;
        public string max_val;
        public string increment;
        public string units;

        public Dictionary<string, string> possible_settings_index_dict = new Dictionary<string, string>();                        
        public Dictionary<string, (int ac_val, int dc_val)> curr_setting_vals_by_scheme = new Dictionary<string, (int ac_val, int dc_val)>();

        private int _curr_ac_val;
        private int _curr_dc_val;

        public int curr_ac_val
        {
            get { return this._curr_ac_val; }
            set { this.SetProperty(ref this._curr_ac_val, value); }
        }
        public int curr_dc_val
        {
            get { return this._curr_dc_val; }
            set { this.SetProperty(ref this._curr_dc_val, value); }
        }
    }

    public class GroupStore
    {
        public GroupStore(string group_guid, string group_name)
        {
            _group_guid = group_guid;
            _group_name = group_name;
            _child_guids = new List<string>();
        }

        public string _group_guid { get; set; }
        public string _group_name { get; set; }
        public List<string> _child_guids { get; set; }
    }

    public class SchemeStore : BindableBase
    {
        public string scheme_name;
        public string scheme_guid;
        private string _is_active_scheme;

        public SchemeStore(string scheme_name, string scheme_guid)
        {
            this.scheme_name = scheme_name;
            this.scheme_guid = scheme_guid;
            this._is_active_scheme = "Collapsed";
        }

        public string is_active_scheme
        {
            get { return this._is_active_scheme; }
            set { this.SetProperty(ref this._is_active_scheme, value); }
        }
    }



    //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------------------------------------------------



    public partial class App : Application
    {

        public static Window Window { get { return m_window; } }
        private static Window m_window;

        public static Dictionary<string, SettingStore> pub_setting_store_dict { get { return setting_store_dict; } }
        private static Dictionary<string, SettingStore> setting_store_dict = new Dictionary<string, SettingStore>();

        public static Dictionary<string, GroupStore> pub_subgroup_store_dict { get { return subgroup_store_dict; } }
        private static Dictionary<string, GroupStore> subgroup_store_dict = new Dictionary<string, GroupStore>();

        public static Dictionary<string, SchemeStore> pub_scheme_store_dict { get { return scheme_store_dict; } }
        private static Dictionary<string, SchemeStore> scheme_store_dict = new Dictionary<string, SchemeStore>();

        public static string pub_curr_scheme_guid { get { return curr_scheme_guid; } }
        private static string curr_scheme_guid;

        private PowerShell ps = PowerShell.Create();



        public App()
        {
            this.InitializeComponent();

            // todo: storing values in static vars, correct to run in instance constructor?
            this.get_current_scheme_guid();
            this.get_scheme_guids();
            this.get_powersettings();
            this.get_all_setting_vals_by_scheme();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();

            Frame rootFrame = m_window.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();

                m_window.Content = rootFrame;

                rootFrame.Navigate(typeof(Page1));
            }
        }

        public static int str16_toint(string hex_string) { return Convert.ToInt32(hex_string, 16); }



        //-------------------------------------------------------------------------------------------------





        public void get_current_scheme_guid()
        {
            this.ps.AddCommand("powercfg").AddArgument("getactivescheme");
            App.curr_scheme_guid = this.ps.Invoke()[0].ToString().Trim().Substring(19, 36);
        }



        private void get_scheme_guids()
        {
            this.ps.AddCommand("powercfg").AddArgument("list");
            var result = this.ps.Invoke();

            foreach (var ps_ob in result) {

                string tmp = ps_ob.ToString().Trim();
                if (tmp.Length == 0) continue;

                string stem = tmp.Substring(0, 8);

                if (stem == "Power Sc") 
                {
                    string guid = tmp.Substring(19, 36);
                    string name = tmp.Substring(58);
                    name = name.TrimEnd( new char[] {')', '*', ' '} );

                    App.scheme_store_dict[guid] = new SchemeStore(name, guid);
                }
            }
        }




        private Collection<PSObject> powercfg_query(string scheme_guid, string group_guid)
        {
            this.ps.AddCommand("powercfg").AddArgument("q").AddArgument(scheme_guid).AddArgument(group_guid);
            var result = this.ps.Invoke();

            return result;
        }


        public bool set_powersetting(string scheme_guid, string group_guid, string setting_guid, int value)
        {
            this.ps.AddCommand("powercfg").AddArgument("setacvalueindex").AddArgument(scheme_guid).AddArgument(group_guid).AddArgument(setting_guid).AddArgument(value);
            var result = this.ps.Invoke();

            return (result.Count == 0);
        }

        public bool set_powerscheme(string scheme_guid)
        {
            this.ps.AddCommand("powercfg").AddArgument("setactive").AddArgument(scheme_guid);
            var result = this.ps.Invoke();

            return (result.Count == 0);
        }


        private void get_powersettings()
        {
            string curr_powerscheme = App.curr_scheme_guid;
            var all_settings = powercfg_query(curr_powerscheme, "");


            string[] all_strings = new string[all_settings.Count];
            int all_strings_size = 0;
            foreach (PSObject setting_ob in all_settings) {

                string tmp = setting_ob.ToString().Trim();
                if (tmp.Length == 0) continue;

                string stem = tmp.Substring(0, 8);

                if (stem == "GUID Ali") { continue; }
                if (stem == "Power Sc") { continue; }

                all_strings[all_strings_size] = tmp;
                all_strings_size++;
            }

            GroupStore curr_group = null;
            SettingStore curr_setting = null;

            int i = 0; 
            while (true)
            {
                if (i >= all_strings_size) { break; }

                string line = all_strings[i];
                string stem = line.Substring(0, 8);

                if (stem == "Subgroup") // new setting subgroup
                {
                    string group_guid = line.Substring(15, 36);
                    string group_name = line.Substring(54, line.Length-1-54);

                    curr_group = new GroupStore(group_guid, group_name);
                    subgroup_store_dict[group_guid] = curr_group;

                    i++;
                }
                else if (stem == "Power Se") // new power setting 
                {
                    string setting_guid = line.Substring(20, 36);
                    string setting_name = line.Substring(59, line.Length-1-59); 

                    curr_setting = new SettingStore(setting_guid, setting_name, "", curr_group._group_guid);
                    setting_store_dict[setting_guid] = curr_setting;

                    curr_group._child_guids.Add(setting_guid);

                    i++;
                }
                else if (stem == "Minimum ") // a setting's range-type value
                {
                    curr_setting.is_range = true;

                    string min_val =    line.Substring(26);
                    string max_val =    all_strings[i + 1].Substring(26);
                    string increment =  all_strings[i + 2].Substring(29);
                    string units =      all_strings[i + 3].Substring(25);
                                        
                    curr_setting.min_val = min_val;
                    curr_setting.max_val = max_val;
                    curr_setting.increment = increment;
                    curr_setting.units = units;    
                    
                    i += 4;
                }
                else if (stem == "Possible") // a setting's index-type value
                {
                    curr_setting.is_range = false;

                    while (true)
                    {
                        string subsetting_index = line.Substring(24);
                        string subsetting_name = all_strings[i + 1].Substring(32);

                        curr_setting.possible_settings_index_dict[subsetting_index] = subsetting_name;

                        i += 2;
                        if (i < all_strings.Length) {
                            line = all_strings[i];
                            stem = line.Substring(0, 8);

                            if (stem != "Possible") break;
                        }
                        else break;
                    }
                }
                else if (stem == "Current ") // current setting's values
                {
                    string curr_ac_setting = line.Substring(32);
                    string curr_dc_setting = all_strings[i + 1].Substring(32);

                    curr_setting.curr_ac_val = str16_toint(curr_ac_setting);
                    curr_setting.curr_dc_val = str16_toint(curr_dc_setting);

                    i += 2;
                }
            }
                                        
        }

        // populate the existing settings objs in the settings dict with current setting values
        private void get_all_setting_vals_by_scheme()
        {
            foreach (var kvp in App.scheme_store_dict)
            {
                string curr_scheme_guid = kvp.Key;
                var res_objs = powercfg_query(curr_scheme_guid, "");

                string curr_setting_guid = null;
                int i = 0;
                while (true)
                {
                    if (i >= res_objs.Count) break;

                    string line = res_objs[i].ToString().Trim();
                    
                    if (line.Length == 0) { i++; continue; }

                    string stem = line.Substring(0, 8);

                    if (stem == "Power Se")
                    {
                        curr_setting_guid = line.Substring(20, 36);
                        i++;
                    }
                    else if (stem == "Current ")
                    {
                        int ac_value = str16_toint( line.Substring(32) );
                        int dc_value = str16_toint( res_objs[i+1].ToString().Trim().Substring(32) );

                        App.setting_store_dict[curr_setting_guid].curr_setting_vals_by_scheme[curr_scheme_guid] = (ac_val: ac_value, dc_val: dc_value);

                        i += 2;
                    }
                    else i++;
                }
            }
        }



        //private void get_powerguids_from_classes()
        //{
        //    WMIHelper helper = new WMIHelper("root\\CimV2\\power");

        //    var powerSettings = helper.Query("SELECT InstanceID, ElementName, Description FROM Win32_PowerSetting").ToList();
        //    var powerSettingSubgroups = helper.Query("SELECT InstanceID, ElementName, Description FROM Win32_PowerSettingSubgroup").ToList();
        //    var powerSettingsInSubgroups = helper.Query("SELECT PartComponent, GroupComponent FROM Win32_PowerSettingInSubgroup").ToList();

        //    for (int i = 0; i < powerSettings.Count; i++)
        //    {
        //        string setting_guid = braces_reg.Match(powerSettings[i].InstanceID).Value;
        //        string setting_name = powerSettings[i].ElementName;
        //        string setting_descr = powerSettings[i].Description;

        //        var store = new setting_store(setting_guid, "", setting_name, setting_descr);
        //        this.setting_store_dict[setting_guid] = store;
        //    }

        //    for (int i = 0; i < powerSettingSubgroups.Count; i++)
        //    {
        //        string subgroup_guid = braces_reg.Match(powerSettingSubgroups[i].InstanceID).Value;
        //        string subgroup_name = powerSettings[i].ElementName;
        //        string subgroup_descr = powerSettings[i].Description;

        //        var store = new group_store(subgroup_guid, subgroup_name, subgroup_descr);
        //        this.subgroup_store_dict[subgroup_guid] = store;
        //    }

        //    for (int i = 0; i < powerSettingsInSubgroups.Count; i++)
        //    {
        //        string setting_guid = braces_reg.Match(powerSettingsInSubgroups[i].PartComponent).Value;

        //        if (this.setting_store_dict.ContainsKey(setting_guid))
        //        {
        //            string group_guid = braces_reg.Match(powerSettingsInSubgroups[i].GroupComponent).Value;
        //            this.setting_store_dict[setting_guid]._group_guid = group_guid;
        //        }
        //    }
        //}


    }


}
