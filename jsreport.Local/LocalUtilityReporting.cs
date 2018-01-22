using jsreport.Local.Internal;
using jsreport.Shared;
using jsreport.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace jsreport.Local
{
    public class LocalUtilityReporting
    {
        private Configuration _cfg;
        private IReportingBinary _binary;
        private string _cwd;

        internal LocalUtilityReporting(IReportingBinary binary, string cwd, Configuration cfg)
        {
            _binary = binary;
            _cfg = cfg;
            _cwd = cwd;
        }                  
        
        public ILocalUtilityReportingService Create()
        {
            return new LocalUtilityReportingService(_binary, _cwd, _cfg);
        }
    }
}
