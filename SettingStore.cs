using static better_power.App;

namespace better_power
{

    // class needs it's own file - if defined in App.xaml.cs, other xaml files can't find it because it's not really in better_power,
    // it's kinda really in better_power.App which should be in the xaml search path for local: but is kinda not
    public class SettingStore
    {
        public SettingStore(string setting_guid, string setting_name, string setting_descr, string parent_groupguid)
        {
            _setting_guid = setting_guid;
            _setting_name = setting_name;
            _setting_descr = setting_descr;
            _parent_groupguid = parent_groupguid;

            _setting_possible_vals = new possible_vals();
            _setting_current_vals = new current_vals();
        }

        public string _setting_guid { get; set; }
        public string _setting_name { get; set; }
        public string _setting_descr { get; set; }
        public string _parent_groupguid { get; set; }
        public possible_vals _setting_possible_vals { get; set; }
        public current_vals _setting_current_vals { get; set; }
    }

}
