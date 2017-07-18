using jsreport.Local.Internal;
using jsreport.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace jsreport.Local
{
    public class LocalUtilityReporting
    {
        private Configuration _cfg;

        internal LocalUtilityReporting(Configuration cfg)
        {
            _cfg = cfg;
        }               
        
        public ILocalUtilityReportingService Create()
        {
            return new LocalUtilityReportingService(_cfg);
        }
    }
}
