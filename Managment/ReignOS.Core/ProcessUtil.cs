namespace ReignOS.Core;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class ProcessUtil
{
    public static string Run(string name, string args, out int exitCode, Dictionary<string,string> enviromentVars = null, bool wait = true, bool asAdmin = false)
    {
        using (var process = new Process())
        {
            if (asAdmin)
            {
                process.StartInfo.FileName = "sudo";
                process.StartInfo.Arguments = $"-S -- {name} {args}";
                process.StartInfo.RedirectStandardInput = true;
            }
            else
            {
                process.StartInfo.FileName = name;
                process.StartInfo.Arguments = args;
            }
            if (enviromentVars != null)
            {
                foreach (var v in enviromentVars) process.StartInfo.EnvironmentVariables[v.Key] = v.Value;
            }
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            if (asAdmin)
            {
                process.StandardInput.WriteLine("gamer");
                process.StandardInput.Flush();
            }

            if (wait)
            {
                process.WaitForExit();
                exitCode = process.ExitCode;
                var builder = new StringBuilder();
                builder.Append(process.StandardOutput.ReadToEnd());
                builder.Append(process.StandardError.ReadToEnd());
                return builder.ToString();
            }
            else
            {
                exitCode = 0;
                return string.Empty;
            }
        }
    }

    public static void KillHard(string name, bool asAdmin, out int exitCode)
    {
        if (asAdmin)
        {
            Run("pkill", name, out exitCode, wait:true, asAdmin:true);
        }
        else
        {
            exitCode = 0;
            foreach (var process in Process.GetProcessesByName(name))
            {
                process.Kill();
                process.Dispose();
            }
        }
    }

    public static void KillSoft(string name, bool asAdmin, out int exitCode)
    {
        Run("pkill", $"-SIGINT \"{name}\"", out exitCode, wait:true, asAdmin:asAdmin);
    }
}