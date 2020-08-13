using jsreport.Local.Internal;
using jsreport.Shared;
using jsreport.Types;
using Newtonsoft.Json.Serialization;
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
        private bool _keepAlive = true;
        private IContractResolver _contractResolverForDataProperty;

        internal LocalUtilityReporting(IReportingBinary binary, Configuration cfg, string cwd, IContractResolver contractResolverForDataProperty)
        {
            _binary = binary;
            _cfg = cfg;
            _cwd = cwd;
            _contractResolverForDataProperty = contractResolverForDataProperty;
        }

        /// <summary>
        /// By default jsreport.exe binary keeps running on the background between rendering requests.
        /// This gives better performance, however you can disable it by passing false here
        /// </summary>        
        public LocalUtilityReporting KeepAlive(bool keepAlive)
        {
            _keepAlive = keepAlive;
            return this;
        }

        public ILocalUtilityReportingService Create()
        {
            return new LocalUtilityReportingService(_binary, _cfg, _keepAlive, _cwd, _contractResolverForDataProperty);
        }
    }
}
