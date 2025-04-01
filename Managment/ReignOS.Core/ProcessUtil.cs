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
    private const string pass = "gamer";
    public delegate void ProcessOutputDelegate(string line);
    public delegate void ProcessInputDelegate(StreamWriter writer);
    public static event ProcessOutputDelegate ProcessOutput;
    
    public static string Run(string name, string args, Dictionary<string, string> enviromentVars = null, bool wait = true, bool asAdmin = false, bool enterAdminPass = false, bool useBash = true, ProcessOutputDelegate standardOut = null, ProcessInputDelegate getStandardInput = null, bool consoleLogOut = true)
    {
        return Run(name, args, out _, enviromentVars, wait, asAdmin, enterAdminPass, useBash, standardOut, getStandardInput, consoleLogOut);
    }

    public static string Run(string name, string args, out int exitCode, Dictionary<string,string> enviromentVars = null, bool wait = true, bool asAdmin = false, bool enterAdminPass = false, bool useBash = true, ProcessOutputDelegate standardOut = null, ProcessInputDelegate getStandardInput = null, bool consoleLogOut = true)
    {
        try
        {
            using (var process = new Process())
            {
                if (asAdmin)
                {
                    process.StartInfo.FileName = "sudo";
                    string adminArg = enterAdminPass ? "-S " : "";
                    if (useBash) process.StartInfo.Arguments = $"{adminArg}-- bash -c \"{name} {args}\"";
                    else process.StartInfo.Arguments = $"{adminArg}-- {name} {args}";
                }
                else
                {
                    if (useBash)
                    {
                        process.StartInfo.FileName = "bash";
                        process.StartInfo.Arguments = $"-c \"{name} {args}\"";
                    }
                    else
                    {
                        process.StartInfo.FileName = name;
                        process.StartInfo.Arguments = args;
                    }
                }

                if (enviromentVars != null)
                {
                    foreach (var v in enviromentVars) process.StartInfo.EnvironmentVariables[v.Key] = v.Value;
                }

                if (consoleLogOut)
                {
                    string l = $"ProcessUtil.Run: {process.StartInfo.FileName} {process.StartInfo.Arguments}";
                    Console.WriteLine(l);
                    ProcessOutput?.Invoke(l);
                }
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                if (getStandardInput != null) process.StartInfo.RedirectStandardInput = true;
                process.Start();
                
                if (asAdmin && enterAdminPass)
                {
                    process.StandardInput.WriteLine(pass);
                    process.StandardInput.Flush();
                }
                
                getStandardInput?.Invoke(process.StandardInput);

                if (wait)
                {
                    if (standardOut != null)
                    {
                        void ReadLine(object sender, DataReceivedEventArgs args)
                        {
                            if (args != null && args.Data != null)
                            {
                                try
                                {
                                    string value = args.Data;
                                    if (consoleLogOut)
                                    {
                                        Console.WriteLine(value);
                                        ProcessOutput?.Invoke(value);
                                    }
                                    standardOut(args.Data);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                    ProcessOutput?.Invoke(e.ToString());
                                }
                            }
                        }

                        process.OutputDataReceived += ReadLine;
                        process.BeginOutputReadLine();

                        process.ErrorDataReceived += ReadLine;
                        process.BeginErrorReadLine();
                    }
                    
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                    string resultOutput = string.Empty;
                    if (standardOut == null)
                    {
                        var builder = new StringBuilder();
                        builder.Append(process.StandardOutput.ReadToEnd());
                        builder.Append(process.StandardError.ReadToEnd());
                        resultOutput = builder.ToString();
                        if (consoleLogOut)
                        {
                            Console.WriteLine(resultOutput);
                            ProcessOutput?.Invoke(resultOutput);
                        }
                    }
                    return resultOutput;
                }
                else
                {
                    if (standardOut != null) throw new Exception("Callbacks can only be used with 'wait'");
                    exitCode = 0;
                    return string.Empty;
                }
            }
        }
        catch (Exception e)
        {
            exitCode = 0;
            Log.WriteLine(e.Message);
            ProcessOutput?.Invoke(e.Message);
            return e.Message;
        }
    }

    public static void CreateDirectoryAdmin(string path)
    {
        ProcessUtil.Run("mkdir", path, asAdmin:true);
    }

    public static string ReadAllTextAdmin(string path)
    {
        return Run("cat", path, asAdmin:true);
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