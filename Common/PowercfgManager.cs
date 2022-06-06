using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using Truncon.Collections;

namespace better_power.Common
{
    public static class PowercfgManager
    {       
        public static Collection<PSObject> get_powercfg_query(string scheme_guid, string group_guid)
        {
            var ps = PowerShell.Create().AddCommand("powercfg").AddArgument("q").AddArgument(scheme_guid).AddArgument(group_guid);
            return ps.Invoke();
        }

        public static string get_systemactive_schemeguid()
        {
            var ps = PowerShell.Create().AddCommand("powercfg").AddArgument("getactivescheme");
            return ps.Invoke()[0].ToString().Trim().Substring(19, 36);
        }

        public static Collection<PSObject> powercfg_get_schemelist()
        {
            var ps = PowerShell.Create().AddCommand("powercfg").AddArgument("list");
            return ps.Invoke();
        }

        public static bool set_powersetting(string scheme_guid, string group_guid, string setting_guid, int value)
        {
            var ps = PowerShell.Create().AddCommand("powercfg").AddArgument("setacvalueindex").AddArgument(scheme_guid).AddArgument(group_guid).AddArgument(setting_guid).AddArgument(value);
            var result = ps.Invoke();

            return (result.Count == 0);
        }

        public static bool set_systemactive_scheme(string scheme_guid)
        {
            var ps = PowerShell.Create().AddCommand("powercfg").AddArgument("setactive").AddArgument(scheme_guid);
            var result = ps.Invoke();

            return (result.Count == 0);
        }

        public static bool powercfg_rename_scheme(string scheme_guid, string name)
        {
            var ps = PowerShell.Create().AddCommand("powercfg").AddArgument("changename").AddArgument(scheme_guid).AddArgument(name);
            var result = ps.Invoke();

            return (result.Count == 0);
        }

        public static bool powercfg_copy_scheme(string scheme_guid, string new_guid)
        {
            var ps = PowerShell.Create().AddCommand("powercfg").AddArgument("duplicatescheme").AddArgument(scheme_guid).AddArgument(new_guid);
            var result = ps.Invoke();

            return (result.Count == 1);
        }

        public static bool powercfg_del_scheme(string scheme_guid)
        {
            var ps = PowerShell.Create().AddCommand("powercfg").AddArgument("delete").AddArgument(scheme_guid);
            var result = ps.Invoke();

            return (result.Count == 0);
        }

        public static string powercfg_get_schemename(string scheme_guid)
        {
            var ps = PowerShell.Create().AddCommand("powercfg").AddArgument("query").AddArgument(scheme_guid);
            var result = ps.Invoke();

            string schemename = result[0].ToString().Trim();
            schemename = schemename.Substring(58, schemename.Length - 1 - 58); 
            return schemename;
        }

        public static bool powercfg_resetdefaultschemes()
        {
            var ps = PowerShell.Create().AddCommand("powercfg").AddArgument("restoredefaultschemes");

            return ps.Invoke().Count == 0;
        }


        // -------------------------------------------------------------------------------------------------------------------------------------
        public static bool powercfg_import_scheme(string new_guid, string import_filepath)
        {
            var ps = PowerShell.Create().AddCommand("powercfg").AddArgument("import").AddArgument(import_filepath).AddArgument(new_guid);
            var result = ps.Invoke();

            return result[0].ToString().ToLower().Contains("successfully");                
        }
        public static bool powercfg_export_scheme(string scheme_guid, string export_filename)
        {
            var ps = PowerShell.Create().AddCommand("powercfg").AddArgument("export").AddArgument(export_filename).AddArgument(scheme_guid);
            var result = ps.Invoke();

            return (result.Count == 0);
        }

    }
    
}
