using System;
using System.Collections.Generic;

namespace jsreport.Local
{
    public class JsReportBinaryException : Exception
    {
        public JsReportBinaryException(string message, string logs, string command) : base(message)
        {
            Logs = logs;
        }

        public string Logs { get; set; }
    }
}
