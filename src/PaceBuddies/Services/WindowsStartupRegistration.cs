using Microsoft.Win32;

namespace PaceBuddies.Services;

public sealed class WindowsStartupRegistration(string keyPath=@"Software\Microsoft\Windows\CurrentVersion\Run",string name="PaceBuddies")
{
    public bool IsEnabled=>ReadCommand()!=null;
    public string? ReadCommand(){using var key=Registry.CurrentUser.OpenSubKey(keyPath);return key?.GetValue(name) as string;}
    public void SetEnabled(bool enabled,string executable,bool background)
    {
        if(enabled)
        {
            using var key=Registry.CurrentUser.CreateSubKey(keyPath);
            key.SetValue(name,StartupCommand.Build(executable,background),RegistryValueKind.String);
        }
        else{using var key=Registry.CurrentUser.OpenSubKey(keyPath,true);key?.DeleteValue(name,false);}
    }
}
