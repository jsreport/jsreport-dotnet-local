using jsreport.Local.Internal;
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
        private Stream _binaryStream;
        private string _cwd;
        private bool _redirectOutput;

        internal LocalWebReporting(Stream binaryStream, string cwd, Configuration cfg)
        {
            _binaryStream = binaryStream;
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
            var res = new LocalWebServerReportingService(_binaryStream, _cwd, _cfg);
            
            if (_redirectOutput)
            {
                res.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);                
            }          

            return res;
        }
    }
}
