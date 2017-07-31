using jsreport.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace jsreport.Local
{
    public interface ILocalWebServerReportingService : IRenderService
    {
        Task KillAsync();
        Task<ILocalWebServerReportingService> StartAsync();
        event DataReceivedEventHandler OutputDataReceived;        
    }
}
