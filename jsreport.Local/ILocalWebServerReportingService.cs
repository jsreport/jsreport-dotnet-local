using jsreport.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace jsreport.Local
{
    public interface ILocalWebServerReportingService : IRenderService
    {
        void Kill();
        Task<ILocalWebServerReportingService> StartAsync();
    }
}
