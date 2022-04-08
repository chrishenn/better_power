#### Generate and run the advanced power options unhide script
#### We can generate and run the unhide script with this script unlimited
##    times, and the power option swill reman unhidden

$unhide_script = "unhide_script.ps1"


if ( -not (test-path -path $unhide_script) )
{
    " Generating power unhide script "

    $powerSettingTable = Get-WmiObject -Namespace root\cimv2\power -Class Win32_PowerSetting
    $powerSettingInSubgroubTable = Get-WmiObject -Namespace root\cimv2\power -Class Win32_PowerSettingInSubgroup

    Get-WmiObject -Namespace root\cimv2\power -Class Win32_PowerSettingCapabilities | ForEach-Object {
      $tmp = $_.ManagedElement
      $tmp = $tmp.Remove(0, $tmp.LastIndexOf('{') + 1)
      $tmp = $tmp.Remove($tmp.LastIndexOf('}'))

      $guid = $tmp

      $s = ($powerSettingInSubgroubTable | Where-Object PartComponent -Match "$guid")

      if (!$s) {
        return
      }

      $tmp = $s.GroupComponent
      $tmp = $tmp.Remove(0, $tmp.LastIndexOf('{') + 1)
      $tmp = $tmp.Remove($tmp.LastIndexOf('}'))

      $groupguid = $tmp

      $s = ($powerSettingTable | Where-Object InstanceID -Match "$guid")

      $descr = [string]::Format("# {0}", $s.ElementName)
      $runcfg = [string]::Format("powercfg -attributes {0} {1} -ATTRIB_HIDE", $groupguid, $guid)

      out-file -Encoding ASCII -FilePath $unhide_script -append -InputObject $descr
      out-file -Encoding ASCII -FilePath $unhide_script -append -InputObject $runcfg
      out-file -Encoding ASCII -FilePath $unhide_script -append -InputObject ""
    }
}

" Running power unhide script"
& "$PSScriptRoot\$unhide_script"
