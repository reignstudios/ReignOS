using System.IO;

namespace ReignOS.Core;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

public static class ProcessUtil
{
    public delegate void ProcessOutputDelegate(string line);
    public delegate void ProcessInputDelegate(StreamWriter writer);
    
    public static string Run(string name, string args, Dictionary<string, string> enviromentVars = null, bool wait = true, bool asAdmin = false, ProcessOutputDelegate standardOut = null, ProcessOutputDelegate errorOut = null, ProcessInputDelegate getStandardInput = null)
    {
        return Run(name, args, out _, enviromentVars, wait, asAdmin, standardOut, errorOut, getStandardInput);
    }

    public static string Run(string name, string args, out int exitCode, Dictionary<string,string> enviromentVars = null, bool wait = true, bool asAdmin = false, ProcessOutputDelegate standardOut = null, ProcessOutputDelegate errorOut = null, ProcessInputDelegate getStandardInput = null)
    {
        try
        {
            using (var process = new Process())
            {
                if (asAdmin)
                {
                    process.StartInfo.UseShellExecute = false;
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
                
                getStandardInput?.Invoke(process.StandardInput);

                if (wait)
                {
                    if (standardOut != null)
                    {
                        process.OutputDataReceived += (sender, args) =>
                        {
                            if (args != null && args.Data != null) standardOut(args.Data);
                        };
                        process.BeginOutputReadLine();
                    }
                    
                    if (errorOut != null)
                    {
                        process.ErrorDataReceived += (sender, args) =>
                        {
                            if (args != null && args.Data != null) errorOut(args.Data);
                        };
                        process.BeginErrorReadLine();
                    }
                    
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                    var builder = new StringBuilder();
                    if (standardOut == null) builder.Append(process.StandardOutput.ReadToEnd());
                    if (errorOut == null) builder.Append(process.StandardError.ReadToEnd());
                    return builder.ToString();
                }
                else
                {
                    if (standardOut != null || errorOut != null) throw new Exception("Callbacks can only be used with 'wait'");
                    exitCode = 0;
                    return string.Empty;
                }
            }
        }
        catch (Exception e)
        {
            exitCode = 0;
            Log.WriteLine(e.Message);
            return e.Message;
        }
    }

    public static void KillHard(string name, bool asAdmin, out int exitCode)
    {
        if (asAdmin)
        {
            Run("pkill", $"\"{name}\"", out exitCode, wait:true, asAdmin:true);
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

    public static void Wait(string name, int seconds)
    {
        var time = DateTime.Now;
        do
        {
            bool isAlive = false;
            foreach (var process in Process.GetProcessesByName(name))
            {
                isAlive = true;
                process.Dispose();
            }
            if (!isAlive) break;
            Thread.Sleep(1000);
        } while ((DateTime.Now - time).TotalSeconds < seconds);
    }
}