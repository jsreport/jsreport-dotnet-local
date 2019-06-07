using jsreport.Local.Internal;
using jsreport.Shared;
using jsreport.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace jsreport.Local
{
    public class LocalWebReporting
    {
        private Configuration _cfg;
        private IReportingBinary _binary;
        private string _cwd;
        private bool _redirectOutput;

        internal LocalWebReporting(IReportingBinary binary, Configuration cfg, string cwd)
        {
            _binary = binary;
            _cfg = cfg;
            _cwd = cwd;
        }

        public LocalWebReporting RedirectOutputToConsole()
        {
            _redirectOutput = true;
            return this;
        }

        public ILocalWebServerReportingService Create()
        {
            var res = new LocalWebServerReportingService(_binary, _cfg, _cwd);
            
            if (_redirectOutput)
            {
                res.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);                
            }          

            return res;
        }
    }
}
