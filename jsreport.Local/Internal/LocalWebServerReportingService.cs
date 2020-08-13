using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using jsreport.Types;
using jsreport.Client;
using System.Diagnostics;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using jsreport.Shared;
using Newtonsoft.Json.Serialization;

namespace jsreport.Local.Internal
{
    internal class LocalWebServerReportingService : ILocalWebServerReportingService
    {
        private BinaryProcess _binaryProcess;    
        private bool _stopping;
        private Process _serverProcess;
        private bool _stopped;
        private bool _started;
        private string _startOutputLogs = "";
        private string _startErrorLogs = "";
        private bool _disposed;
        private IContractResolver _contractResolverForDataProperty;

        public TimeSpan StartTimeout { get; set; }
        public TimeSpan StopTimeout { get; set; }
        public string LocalServerUri { get; set; }
        public event DataReceivedEventHandler OutputDataReceived;    
        public IReportingService ReportingService { get; set; }

        internal LocalWebServerReportingService(IReportingBinary binary, Configuration configuration, string cwd = null, IContractResolver contractResolverForDataProperty = null)
        {
            _contractResolverForDataProperty = contractResolverForDataProperty;
            _binaryProcess = new BinaryProcess(binary, configuration, cwd);

            _binaryProcess.Configuration.HttpPort = _binaryProcess.Configuration.HttpPort ?? 5488;
            LocalServerUri = "http://localhost:" + _binaryProcess.Configuration.HttpPort;
            ReportingService = new ReportingService(LocalServerUri, _binaryProcess?.Configuration?.Extensions?.Authentication?.Admin?.Username, 
                _binaryProcess?.Configuration?.Extensions?.Authentication?.Admin?.Password);
            ((ReportingService)ReportingService).ContractResolverForDataProperty = _contractResolverForDataProperty;
            StartTimeout = new TimeSpan(0, 0, 0, 20);            
            StopTimeout = new TimeSpan(0, 0, 0, 3);
            _binaryProcess.OutputDataReceived += (s, e) => {                
                _startOutputLogs += e.Data;
                OutputDataReceived?.Invoke(s, e);
            };
            _binaryProcess.ErrorDataReceived += (s, e) => {                
                _startErrorLogs += e.Data;
                OutputDataReceived?.Invoke(s, e);
            };

            AppDomain.CurrentDomain.DomainUnload += DomainUnloadOrProcessExit;
            AppDomain.CurrentDomain.ProcessExit += DomainUnloadOrProcessExit;
        }

        public async Task KillAsync()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                _serverProcess.Kill();
                await _serverProcess.WaitForExitAsync();
            }
        }

        public Task<Report> RenderAsync(RenderRequest request, CancellationToken ct = default(CancellationToken))
        {
            EnsureStarted();
            return ReportingService.RenderAsync(request);
        }

        public Task<Report> RenderAsync(string templateShortid, object data, CancellationToken ct = default(CancellationToken))
        {
            EnsureStarted();
            return ReportingService.RenderAsync(templateShortid, data, ct);
        }

        public Task<Report> RenderAsync(string templateShortid, string jsonData, CancellationToken ct = default(CancellationToken))
        {
            EnsureStarted();
            return ReportingService.RenderAsync(templateShortid, jsonData, ct);
        }

        public Task<Report> RenderAsync(object request, CancellationToken ct = default(CancellationToken))
        {
            EnsureStarted();
            return ReportingService.RenderAsync(request, ct);
        }

        public Task<Report> RenderByNameAsync(string templateName, string jsonData, CancellationToken ct = default(CancellationToken))
        {
            EnsureStarted();
            return ReportingService.RenderByNameAsync(templateName, jsonData, ct);
        }

        public Task<Report> RenderByNameAsync(string templateName, object data, CancellationToken ct = default(CancellationToken))
        {
            EnsureStarted();
            return ReportingService.RenderByNameAsync(templateName, data, ct);
        }

        public async Task<ILocalWebServerReportingService> StartAsync()
        {            
            _serverProcess = (await _binaryProcess.ExecuteExe("start", false)).Process;
            await WaitForStarted();
            _started = true;
            return this;
        }

        private void EnsureStarted()
        {
            if (!_started)
            {
                throw new InvalidOperationException("LocalWebServerReportingService not yet started. Call Start() first.");
            }
        }

        private async Task WaitForStarted()
        {
            _stopping = false;
            _stopped = false;

            var timeoutSw = Stopwatch.StartNew();

            bool done = false;
            var client = CreateClient();

            var tcs = new TaskCompletionSource<object>();

            await Task.Run(async () =>
             {
                 while (!done)
                 {
                     if (_stopping || _stopped)
                         return;

                     if (!done && timeoutSw.Elapsed > StartTimeout)
                     {
                         tcs.SetException(
                             new Exception(
                                 "Failed to start jsreport server, output: " + _startErrorLogs +
                                 _startOutputLogs));
                         return;
                     }

                     try
                     {
                         HttpResponseMessage response = await client.GetAsync("/api/ping");
                         response.EnsureSuccessStatusCode();
                         done = true;
                         tcs.SetResult(new object());
                     }
                     catch (Exception e)
                     {
                        //waiting for server to startup
                    }

                     Thread.Sleep(20);
                 }
             });

            await tcs.Task.ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            AppDomain.CurrentDomain.DomainUnload -= DomainUnloadOrProcessExit;
            AppDomain.CurrentDomain.ProcessExit -= DomainUnloadOrProcessExit;

            if (_serverProcess != null)
            {
                try
                {
                    _serverProcess.Kill();
                } catch (Exception)
                {

                }
            }            

            _disposed = true;
        }

        private void DomainUnloadOrProcessExit(object sender, EventArgs e)
        {
            Dispose();
        }

        private HttpClient CreateClient()
        {
            var client = new HttpClient() { BaseAddress = new Uri(LocalServerUri) };
           
            if (_binaryProcess?.Configuration?.Extensions?.Authentication?.Admin?.Username != null)
            {
                var basicAuth =
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(String.Format("{0}:{1}", 
                        _binaryProcess?.Configuration?.Extensions?.Authentication?.Admin?.Username,
                        _binaryProcess?.Configuration?.Extensions?.Authentication?.Admin?.Password)));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
            }

            return client;
        }
    }
}