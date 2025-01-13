using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launcher.Core
{
    public static class ProcessUtil
    {
        public static string Run(string name, string args)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = name;
                process.StartInfo.Arguments = args;
                process.Start();
                process.WaitForExit();
                return process.StandardOutput.ReadToEnd();
            }
        }
    }
}
