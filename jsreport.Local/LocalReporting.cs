using jsreport.Shared;
using jsreport.Types;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace jsreport.Local
{
    /// <summary>
    /// Builder for local reporting service
    /// </summary>
    public class LocalReporting
    {
        private Configuration _cfg = new Configuration();
        private IReportingBinary _binary;
        private string _cwd;

        /// <summary>
        /// Use lambda function to configure additional jsreport properties
        /// </summary>        
        public LocalReporting Configure(Func<Configuration, Configuration> cfg)
        {
            _cfg = cfg.Invoke(_cfg);
            return this;
        }

        public LocalReporting UseBinary(IReportingBinary binary)
        {
            _binary = binary;
            return this;
        }             

        /// <summary>
        /// The jsreport.exe runs by default in bin/jsreport working directory
        /// This can be used to run for example from the VS project where the templates are stored
        /// </summary>
        /// <example>
        /// LocalReporting()
        ///     .UseBinary(JsReportBinary.GetStream())        
        ///     .RunInDirectory(Path.Combine(Directory.GetCurrentDirectory(), "jsreport"))
        /// </example>
        /// <param name="cwd"></param>        
        public LocalReporting RunInDirectory(string cwd)
        {
            _cwd = cwd;

            return this;
        }

        /// <summary>
        /// Kill all previously running jsreport orphan processes.
        /// This usefull mainly when running local jsreport web server in debug, because VS doesn't properly unload 
        /// program domains and doesn't kill child jsreport processes.
        /// </summary>        
        public LocalReporting KillRunningJsReportProcesses()
        {
            try
            {
                Process.GetProcesses().ToList().Where(p => p.ProcessName.Contains("jsreport").ToList().ForEach(p => p.Kill());
            } catch (Exception e)
            {
                // avoid access denied errors
            }
            return this;
        }

        /// <summary>
        /// Run jsreport as additional web server using internaly http requests to render reports
        /// </summary>        
        public LocalWebReporting AsWebServer()
        {
            if (_binary == null)
            {
                throw new InvalidOperationException("LocalReporting.UseBinary must be used to specify jsreport.exe.");
            }

            return new LocalWebReporting(_binary, _cwd, _cfg);
        }

        /// <summary>
        /// Run jsreport as simple command line utility
        /// </summary>        
        public LocalUtilityReporting AsUtility()
        {
            if (_binary == null)
            {
                throw new InvalidOperationException("LocalReporting.UseBinary must be used to specify jsreport.exe.");
            }

            return new LocalUtilityReporting(_binary, _cwd, _cfg);
        }         
    }    
}
