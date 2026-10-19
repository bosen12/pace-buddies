namespace PaceBuddies.Core;

public static class StartupCommand
{
    public static string Build(string executable,bool background)
    {
        if(string.IsNullOrWhiteSpace(executable)||executable.Contains('"'))throw new ArgumentException("Invalid executable path.",nameof(executable));
        return "\""+executable+"\""+(background?" --background":"");
    }
}
