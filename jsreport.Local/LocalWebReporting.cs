using jsreport.Local.Internal;
using jsreport.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace jsreport.Local
{
    public class LocalWebReporting
    {
        private Configuration _cfg;

        internal LocalWebReporting(Configuration cfg)
        {
            _cfg = cfg;
        }

        public ILocalWebServerReportingService Create()
        {
            return new LocalWebServerReportingService(_cfg);
        }
    }
}
