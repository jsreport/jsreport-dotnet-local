using jsreport.Local.Internal;
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
        private Stream _binaryStream;

        internal LocalUtilityReporting(Stream binaryStream, Configuration cfg)
        {
            _binaryStream = binaryStream;
            _cfg = cfg;
        }               
        
        public ILocalUtilityReportingService Create()
        {
            return new LocalUtilityReportingService(_binaryStream, _cfg);
        }
    }
}
