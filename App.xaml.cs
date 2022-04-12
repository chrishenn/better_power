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
using System.Collections.ObjectModel;
using System.Management.Automation;
using Truncon.Collections;

using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

using better_power.Common;

using Microsoft.Windows.ApplicationModel.DynamicDependency;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace better_power
{

    // TODO

    // default ordering from power saver to ultimate

    // setting cards:
    //      indicate possible values to which we can set the setting
    //      data units + format     
    //      range checking
    //      ac + dc menus

    // explanatory tooltips
    // error handling
    // write exceptions to recover, display errors to user, crash if needed 
    // check that new data objects with GUIDs have valid GUIDS

    // packaging - modern install, portable install, taskbar icon, taskbar app name
    // installer must run power unhide scripts (reporpose system-object code?)
    // compatibility testing
    // make the waiting animation smooth? It hitches while the main UI thread is refreshing the UIElements

    // search behavior: include all settings under group header if header name matches search query?
    // [wait: future winui version] override window theme-color border
    // [wait: future winui version] drag-n-drop reordering of schemes in navigationview
    // [appears to be intentional] buttons on panel added as Content to InstallScheme dialog steals Enter key

    // (?) change settings via registry key    
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

        private bool _setting_enabled = true;

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
        public bool setting_enabled
        {
            get { return this._setting_enabled; }
            set { this.SetProperty(ref this._setting_enabled, value); }
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
        public string scheme_guid;
        private string _scheme_name;        
        private string _activebox_visible;

        public SchemeStore(string scheme_name, string scheme_guid)
        {
            this.scheme_guid = scheme_guid;
            this._scheme_name = scheme_name;            
            this._activebox_visible = "Collapsed";
        }

        public string scheme_name
        {
            get { return this._scheme_name; }
            set { this.SetProperty(ref this._scheme_name, value); }
        }
        public string activebox_visible
        {
            get { return this._activebox_visible; }
            set { this.SetProperty(ref this._activebox_visible, value); }
        }
    }



    //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------------------------------------------------

    

    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;

        public static Window Window { get { return _window; } }
        private static Window _window;

        public static Frame AppFrame { get { return _appframe; } }
        private static Frame _appframe;

        public IntPtr _hwnd;

        public static OrderedDictionary<string, SettingStore> setting_data_dict { get { return _setting_data_dict; } }
        private static OrderedDictionary<string, SettingStore> _setting_data_dict = new OrderedDictionary<string, SettingStore>();

        public static OrderedDictionary<string, GroupStore> group_data_dict { get { return _group_data_dict; } }
        private static OrderedDictionary<string, GroupStore> _group_data_dict = new OrderedDictionary<string, GroupStore>();

        public static OrderedDictionary<string, SchemeStore> scheme_data_dict { get { return _scheme_data_dict; } }
        private static OrderedDictionary<string, SchemeStore> _scheme_data_dict = new OrderedDictionary<string, SchemeStore>();

        public PowercfgManager power_manager = new PowercfgManager();

        public string[] classic_filepaths;
        public string[] classic_guids;
        public int[] classic_order;



        public App()
        {
            this.InitializeComponent();

            this.read_classic_schemes_fromfiles();            

            this.Refresh_App_Data();
        }

        public void Refresh_App_Data()
        {
            _setting_data_dict.Clear();
            _group_data_dict.Clear();
            _scheme_data_dict.Clear();

            this.build_schemedata();
            this.build_settingdata();
            this.store_setting_values_all_schemes();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            App._window = new MainWindow();
            App._window.ExtendsContentIntoTitleBar = true;

            var shellpage = new ShellPage();
            App._window.Content = shellpage;

            App._appframe = shellpage.appframe;
            _appframe.Navigate(typeof(MainPage));

            App._window.Activate();

            this._hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App._window);
        }

        public static int str16_toint(string hex_string) { return Convert.ToInt32(hex_string, 16); }

        public static string GetAppRoot()
        {
            var exePath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Regex appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
            var appRoot = appPathMatcher.Match(exePath).Value;
            return appRoot;
        }

        public void read_classic_schemes_fromfiles()
        {
            var proj_path = App.GetAppRoot();
            var config_path = proj_path + @"\classic_configs\";

            this.classic_filepaths = Directory.GetFiles(config_path, "*.pow", SearchOption.TopDirectoryOnly);
            this.classic_guids = File.ReadAllLines(config_path + @"classic_scheme_guids.txt");

            this.classic_order = new int[] { 2, 0, 1, 3 };
        }
               


        //-------------------------------------------------------------------------------------------------
        // Build App data structs and objects
        //-------------------------------------------------------------------------------------------------

        // parses strings returned from powershell powercfg
        private void build_schemedata()
        {
            var result = this.power_manager.powercfg_get_schemelist();

            foreach (var ps_ob in result)
            {
                string tmp = ps_ob.ToString().Trim();
                if (tmp.Length == 0) continue;

                string stem = tmp.Substring(0, 8);

                if (stem == "Power Sc")
                {
                    string guid = tmp.Substring(19, 36);
                    string name = tmp.Substring(58);
                    name = name.TrimEnd(new char[] { ')', '*', ' ' });

                    App._scheme_data_dict[guid] = new SchemeStore(name, guid);
                }
            }
        }

        private void build_settingdata()
        {
            string systemactive_schemeguid = PowercfgManager.get_systemactive_schemeguid();
            var all_settings = this.power_manager.get_powercfg_query(systemactive_schemeguid, "");

            string[] all_strings = new string[all_settings.Count];
            int all_strings_size = 0;
            foreach (PSObject setting_ob in all_settings)
            {

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
                    string group_name = line.Substring(54, line.Length - 1 - 54);

                    curr_group = new GroupStore(group_guid, group_name);
                    _group_data_dict[group_guid] = curr_group;

                    i++;
                }
                else if (stem == "Power Se") // new power setting 
                {
                    string setting_guid = line.Substring(20, 36);
                    string setting_name = line.Substring(59, line.Length - 1 - 59);

                    curr_setting = new SettingStore(setting_guid, setting_name, "", curr_group._group_guid);
                    _setting_data_dict[setting_guid] = curr_setting;

                    curr_group._child_guids.Add(setting_guid);

                    i++;
                }
                else if (stem == "Minimum ") // a setting's range-type value
                {
                    curr_setting.is_range = true;

                    string min_val = line.Substring(26);
                    string max_val = all_strings[i + 1].Substring(26);
                    string increment = all_strings[i + 2].Substring(29);
                    string units = all_strings[i + 3].Substring(25);

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
                        if (i < all_strings.Length)
                        {
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


        // populate the existing settings objs in the settings dict with system's currently-set values
        private void store_setting_values_all_schemes()
        {
            foreach (var kvp in App._scheme_data_dict)
            {
                string curr_scheme_guid = kvp.Key;
                store_setting_values_one_scheme(curr_scheme_guid);
            }
        }
        
        public void store_setting_values_one_scheme(string scheme_guid)
        {
            var res_objs = this.power_manager.get_powercfg_query(scheme_guid, "");

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
                    int ac_value = str16_toint(line.Substring(32));
                    int dc_value = str16_toint(res_objs[i + 1].ToString().Trim().Substring(32));

                    App._setting_data_dict[curr_setting_guid].curr_setting_vals_by_scheme[scheme_guid] = (ac_val: ac_value, dc_val: dc_value);

                    i += 2;
                }
                else i++;
            }
        }

        public void remove_setting_values_one_scheme(string scheme_guid)
        {
            foreach (var setting_data in App._setting_data_dict.Values)            
                setting_data.curr_setting_vals_by_scheme.Remove(scheme_guid);            
        }


    }


}
