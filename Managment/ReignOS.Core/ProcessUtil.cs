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
    
    public static string Run(string name, string args, Dictionary<string, string> enviromentVars = null, bool wait = true, bool asAdmin = false, bool enterAdminPass = false, bool useBash = true, ProcessOutputDelegate standardOut = null, ProcessInputDelegate getStandardInput = null, string workingDir = null, bool log = true, bool verboseLog = false, bool disableStdRead = false, int killAfterSec = -1)
    {
        return Run(name, args, out _, enviromentVars, wait, asAdmin, enterAdminPass, useBash, standardOut, getStandardInput, workingDir, log, verboseLog, disableStdRead, killAfterSec);
    }

    public static string Run(string name, string args, out int exitCode, Dictionary<string,string> enviromentVars = null, bool wait = true, bool asAdmin = false, bool enterAdminPass = false, bool useBash = true, ProcessOutputDelegate standardOut = null, ProcessInputDelegate getStandardInput = null, string workingDir = null, bool log = true, bool verboseLog = false, bool disableStdRead = false, int killAfterSec = -1)
    {
        try
        {
            using (var process = new Process())
            {
                if (asAdmin)
                {
                    process.StartInfo.FileName = "sudo";
                    string adminArg = enterAdminPass ? "-S -- " : "";
                    if (useBash) process.StartInfo.Arguments = $"{adminArg}bash -c \"{name} {args}\"";
                    else process.StartInfo.Arguments = $"{adminArg}{name} {args}";
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

                if (log)
                {
                    string l = $"ProcessUtil.Run: {process.StartInfo.FileName} {process.StartInfo.Arguments}";
                    Log.WriteLine(l);
                    ProcessOutput?.Invoke(l);
                }
                process.StartInfo.UseShellExecute = false;
                if (!disableStdRead)
                {
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                }
                if (getStandardInput != null) process.StartInfo.RedirectStandardInput = true;
                if (workingDir != null) process.StartInfo.WorkingDirectory = workingDir;
                process.Start();
                
                if (asAdmin && enterAdminPass)
                {
                    process.StandardInput.WriteLine(pass);
                    process.StandardInput.Flush();
                }
                
                getStandardInput?.Invoke(process.StandardInput);

                if (wait)
                {
                    if (standardOut != null && !disableStdRead)
                    {
                        void ReadLine(object sender, DataReceivedEventArgs args)
                        {
                            if (args != null && args.Data != null)
                            {
                                try
                                {
                                    string value = args.Data;
                                    if (log)
                                    {
                                        if (verboseLog) Log.WriteLine(value);
                                        ProcessOutput?.Invoke(value);
                                    }
                                    standardOut(value);
                                }
                                catch (Exception e)
                                {
                                    Log.WriteLine(e);
                                    ProcessOutput?.Invoke(e.ToString());
                                }
                            }
                        }

                        process.OutputDataReceived += ReadLine;
                        process.ErrorDataReceived += ReadLine;
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                    }
                    
                    if (killAfterSec <= 0)
                    {
                        process.WaitForExit();
                        exitCode = process.ExitCode;
                    }
                    else
                    {
                        try
                        {
                            if (process.WaitForExit(killAfterSec * 1000)) exitCode = process.ExitCode;
                            else exitCode = -1;
                        }
                        catch
                        {
                            exitCode = -1;
                        }
                    }

                    string resultOutput = string.Empty;
                    if (standardOut == null && !disableStdRead)
                    {
                        var builder = new StringBuilder();

                        try
                        {
                            if (killAfterSec > 0)
                            {
                                var task = process.StandardOutput.ReadToEndAsync();
                                if (task.Wait(killAfterSec * 1000)) builder.Append(task.Result);
                                
                                task = process.StandardError.ReadToEndAsync();
                                if (task.Wait(killAfterSec * 1000)) builder.Append(task.Result);
                            }
                            else
                            {
                                builder.Append(process.StandardOutput.ReadToEnd());
                                builder.Append(process.StandardError.ReadToEnd());
                            }
                        }
                        catch (Exception e)
                        {
                            Log.WriteLine(e);
                        }

                        resultOutput = builder.ToString();
                        if (log)
                        {
                            if (verboseLog) Log.WriteLine(resultOutput);
                            ProcessOutput?.Invoke(resultOutput);
                        }
                    }
                    
                    if (killAfterSec > 0 && !process.HasExited)
                    {
                        Thread.Sleep(1000);// process may exit after reading input (so wait 1 extra sec)
                        try
                        {
                            process.Kill();
                        }
                        catch {}
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
        Run("mkdir", path, asAdmin:true);
    }

    public static string ReadAllTextAdmin(string path)
    {
        return Run("cat", path, asAdmin:true);
    }

    public static void WriteAllTextAdmin(string path, string text)
    {
        void getStandardInput(StreamWriter writer)
        {
            writer.WriteLine(text);
            writer.Flush();
            writer.Close();
        }
        Run("tee", path, asAdmin:true, getStandardInput:getStandardInput);
    }

    public static void WriteAllTextAdmin(string path, StringBuilder builder)
    {
        void getStandardInput(StreamWriter writer)
        {
            writer.WriteLine(builder);
            writer.Flush();
            writer.Close();
        }
        Run("tee", path, asAdmin:true, getStandardInput:getStandardInput);
    }

    public static string DeleteFileAdmin(string path)
    {
        return Run("rm", path, asAdmin:true);
    }

    public static string DeleteFolderAdmin(string path)
    {
        return Run("rm", $"-rf {path}", asAdmin:true);
    }

    public static void KillHard(string name, bool asAdmin, out int exitCode)
    {
        if (asAdmin)
        {
            Run("pkill", $"'{name}'", out exitCode, wait:true, asAdmin:true);
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