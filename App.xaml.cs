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
using Windows.Foundation;
using Windows.Foundation.Collections;

using System.Collections;
using System.Management;
using Microsoft.Management.Infrastructure;
using ORMi;

using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;

using System.Collections.ObjectModel;
using System.Management.Automation;
using Windows.ApplicationModel.Core;
using System.Threading.Tasks;

namespace better_power
{

    public partial class App : Application
    {

        public static Window Window { get { return m_window; } }
        private static Window m_window;

        public static Dictionary<string, SettingStore> pub_setting_store_dict { get { return setting_store_dict; } }
        private static Dictionary<string, SettingStore> setting_store_dict = new Dictionary<string, SettingStore>();

        public static Dictionary<string, GroupStore> pub_subgroup_store_dict { get { return subgroup_store_dict; } }
        private static Dictionary<string, GroupStore> subgroup_store_dict = new Dictionary<string, GroupStore>();

        public static List<string> pub_scheme_guids { get { return scheme_guids; } }
        private static List<string> scheme_guids = new List<string>();



        public App()
        {
            this.InitializeComponent();

            //setting_store_dict = new Dictionary<string, setting_store>();
            //subgroup_store_dict = new Dictionary<string, group_store>();
            //scheme_guids = new List<string>();

            this.get_existing_scheme_guids();
            this.get_powersettings();

            m_window = new MainWindow();
            m_window.Activate();
        }


        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // read all current power setting values
            // TODO: query current values for each setting in each power scheme and store those per-scheme

            // returns all possible setting values for all settings within the specified subgroup for that scheme
            // powercfg /query scheme_GUID, sub_GUID

            // get all possible and current settings under a power scheme
            // powercfg / query scheme_GUID

            // changing a setting runs through powercfg with given GUID
            // powercfg /setacvalueindex scheme_GUID sub_GUID setting_GUID setting_index
            // powercfg /setdcvalueindex scheme_GUID sub_GUID setting_GUID setting_index
            // TODO: edit the appropriate registry key to set the correct setting via GUID (?)

            // create a new scheme by copying an old one
            // select a scheme to edit
            // edit selected scheme
            // apply a given scheme to system
            // (?) install the classic schemes - from "power saving" to "ultimate perf"

            // populate scheme editing view with objects to change each setting 
            // shows friendly setting name
            // should indicate possible values to which we can set the setting
            // allow user to inspect help string for a given setting (clearly! not in a flyout!)                       
        }

        

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------



        public class possible_vals
        {
            public bool is_range;

            public string min_val;
            public string max_val;
            public string increment;
            public string units;

            public Dictionary<string, string> index_dict = new Dictionary<string, string>();
        }
        public class current_vals
        {
            public int ac_value;
            public int dc_value;
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

        
                
        private PowerShell ps = PowerShell.Create();

        private Regex guid_reg = new Regex(@"(?<=GUID:\s*)[^\s]+(?=\s)");





        private void get_existing_scheme_guids()
        {
            this.ps.AddCommand("powercfg").AddArgument("list");
            var result = this.ps.Invoke();

            foreach (var res in result) {

                var newout = this.guid_reg.Match(res.BaseObject.ToString()).Value;

                if (newout.Count() > 0) scheme_guids.Add(newout);                
            }
        }


        public string get_current_powerscheme()
        {
            this.ps.AddCommand("powercfg").AddArgument("getactivescheme");
            var result = this.ps.Invoke();
      
            return this.guid_reg.Match(result[0].BaseObject.ToString()).Value;
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

            //this.ps.AddCommand("powercfg").AddArgument("setdcvalueindex").AddArgument(scheme_guid).AddArgument(group_guid).AddArgument(setting_guid).AddArgument(value);
            //result = this.ps.Invoke();

            return (result.Count == 0);
        }


        public static int str16_toint(string hex_string) { return Convert.ToInt32(hex_string, 16); }



        private void get_powersettings()
        {
            string curr_powerscheme = get_current_powerscheme();
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
                    string group_name = line.Substring(54);

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
                    curr_setting._setting_possible_vals.is_range = true;

                    string min_val =    line.Substring(26);
                    string max_val =    all_strings[i + 1].Substring(26);
                    string increment =  all_strings[i + 2].Substring(29);
                    string units =      all_strings[i + 3].Substring(25);
                                        
                    curr_setting._setting_possible_vals.min_val = min_val;
                    curr_setting._setting_possible_vals.max_val = max_val;
                    curr_setting._setting_possible_vals.increment = increment;
                    curr_setting._setting_possible_vals.units = units;    
                    
                    i += 4;
                }
                else if (stem == "Possible") // a setting's index-type value
                {
                    curr_setting._setting_possible_vals.is_range = false;

                    while (true)
                    {
                        string subsetting_index = line.Substring(24);
                        string subsetting_name = all_strings[i + 1].Substring(32);

                        curr_setting._setting_possible_vals.index_dict[subsetting_index] = subsetting_name;

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

                    curr_setting._setting_current_vals.ac_value = str16_toint(curr_ac_setting);
                    curr_setting._setting_current_vals.dc_value = str16_toint(curr_dc_setting);

                    i += 2;
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
