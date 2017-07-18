using jsreport.Types;
using System;

namespace jsreport.Local
{
    /// <summary>
    /// Builder for local reporting service
    /// </summary>
    public class LocalReporting
    {
        private Configuration _cfg = new Configuration();        

        /// <summary>
        /// Use lambda function to configure additional jsreport properties
        /// </summary>        
        public LocalReporting Configure(Func<Configuration, Configuration> cfg)
        {
            _cfg = cfg.Invoke(_cfg);
            return this;
        }

        /// <summary>
        /// Run jsreport as additional web server using internaly http requests to render reports
        /// </summary>        
        public LocalWebReporting AsWebServer()
        {
            return new LocalWebReporting(_cfg);
        }

        /// <summary>
        /// Run jsreport as simple command line utility
        /// </summary>        
        public LocalUtilityReporting AsUtility()
        {
            return new LocalUtilityReporting(_cfg);
        }         
    }    
}
