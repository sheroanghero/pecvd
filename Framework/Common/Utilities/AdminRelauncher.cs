using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;

public static class AdminRelauncher
{
    public static void RelaunchIfNotAdmin()
    {
        if (!Debugger.IsAttached && !RunningAsAdmin())
        {
            Console.WriteLine("Running as admin required!");
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = Assembly.GetEntryAssembly().CodeBase;
            proc.Verb = "runas";
            try
            {
                Process.Start(proc);
            }
            catch (Exception ex)
            {
                Console.WriteLine("This program must be run as an administrator! \n\n" + ex.ToString());
            }
            Environment.Exit(0);
        }
    }

    private static bool RunningAsAdmin()
    {
        WindowsIdentity id = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(id);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}