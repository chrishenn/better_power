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
    public class PowercfgManager
    {
        
        private PowerShell ps = PowerShell.Create();

        public PowercfgManager() { }

        public Collection<PSObject> get_powercfg_query(string scheme_guid, string group_guid)
        {
            this.ps.AddCommand("powercfg").AddArgument("q").AddArgument(scheme_guid).AddArgument(group_guid);
            return this.ps.Invoke();
        }

        public string get_systemactive_schemeguid()
        {
            this.ps.AddCommand("powercfg").AddArgument("getactivescheme");
            return this.ps.Invoke()[0].ToString().Trim().Substring(19, 36);
        }

        public Collection<PSObject> get_powercfg_list()
        {
            this.ps.AddCommand("powercfg").AddArgument("list");
            return this.ps.Invoke();
        }

        public bool set_powersetting(string scheme_guid, string group_guid, string setting_guid, int value)
        {
            this.ps.AddCommand("powercfg").AddArgument("setacvalueindex").AddArgument(scheme_guid).AddArgument(group_guid).AddArgument(setting_guid).AddArgument(value);
            var result = this.ps.Invoke();

            return (result.Count == 0);
        }

        public bool set_systemactive_powerscheme(string scheme_guid)
        {
            this.ps.AddCommand("powercfg").AddArgument("setactive").AddArgument(scheme_guid);
            var result = this.ps.Invoke();

            return (result.Count == 0);
        }

        public bool set_powerscheme_name(string scheme_guid, string name)
        {
            this.ps.AddCommand("powercfg").AddArgument("changename").AddArgument(scheme_guid).AddArgument(name);
            var result = this.ps.Invoke();

            return (result.Count == 0);
        }

        public bool powercfg_copy_powerscheme(string scheme_guid, string new_guid)
        {
            this.ps.AddCommand("powercfg").AddArgument("duplicatescheme").AddArgument(scheme_guid).AddArgument(new_guid);
            var result = this.ps.Invoke();

            return (result.Count == 1);
        }

        public bool powercfg_del_powerscheme(string scheme_guid)
        {
            this.ps.AddCommand("powercfg").AddArgument("delete").AddArgument(scheme_guid);
            var result = this.ps.Invoke();

            return (result.Count == 0);
        }

        public bool powercfg_export_scheme(string scheme_guid, string export_filename)
        {
            this.ps.AddCommand("powercfg").AddArgument("export").AddArgument(export_filename).AddArgument(scheme_guid);
            var result = this.ps.Invoke();

            return (result.Count == 0);
        }

    }
    
}
