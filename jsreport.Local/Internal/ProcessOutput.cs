using System.Collections.Generic;
using System.Diagnostics;

namespace jsreport.Local.Internal
{
    public class ProcessOutput
    {
        public ProcessOutput(Process process, bool isError, string command, string logs)
        {
            IsError = isError;
            Logs = logs;
            Command = command;
            Process = process;
        }

        public bool IsError { get; }
        public string Logs { get; }
        public string Command { get; set; }
        public Process Process { get; set; }
    }
}
