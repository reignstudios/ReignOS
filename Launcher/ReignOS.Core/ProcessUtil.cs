using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReignOS.Core
{
    public static class ProcessUtil
    {
        public static string Run(string name, string args, Dictionary<string,string>? enviromentVars = null, bool wait = true)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = name;
                process.StartInfo.Arguments = args;
                if (enviromentVars != null)
                {
                    foreach (var v in enviromentVars) process.StartInfo.EnvironmentVariables[v.Key] = v.Value;
                }
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                if (wait)
                {
                    process.WaitForExit();
                    return process.StandardOutput.ReadToEnd();
                }
                else
                {
                    return string.Empty;
                }
            }
        }
    }
}
