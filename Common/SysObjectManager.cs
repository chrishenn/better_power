using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace better_power.Common
{
    internal class SysObjectManager
    {
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
