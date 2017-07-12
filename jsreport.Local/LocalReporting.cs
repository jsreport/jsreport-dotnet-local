using jsreport.Local.Internal;
using jsreport.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace jsreport.Local
{
    public class LocalReporting
    {
        private Configuration _cfg = new Configuration();        

        public LocalReporting Configure(Func<Configuration, Configuration> cfg)
        {
            _cfg = cfg.Invoke(_cfg);
            return this;
        }

        public LocalWebReporting AsWebServer()
        {
            return new LocalWebReporting(_cfg);
        }

        public LocalUtilityReporting AsUtility()
        {
            return new LocalUtilityReporting(_cfg);
        }         
    }    


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
