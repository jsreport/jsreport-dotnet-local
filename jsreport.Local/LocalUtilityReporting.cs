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

        internal LocalUtilityReporting(IReportingBinary binary, Configuration cfg)
        {
            _binary = binary;
            _cfg = cfg;
        }                  
        
        public ILocalUtilityReportingService Create()
        {
            return new LocalUtilityReportingService(_binary, _cfg);
        }
    }
}
