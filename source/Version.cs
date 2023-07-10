using System.Reflection;

internal  static class Version
{
    public const string VERSION = "0.0.1";
    public static string ProgramVersion =>  $"{Assembly.GetExecutingAssembly().GetName().Name} {VERSION}";
}