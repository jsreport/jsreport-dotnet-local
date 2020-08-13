using System;
using System.IO;
using System.Threading;
using jsreport.Types;
using jsreport.Shared;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace jsreport.Local.Internal
{
    internal class LocalUtilityReportingService : ILocalUtilityReportingService
    {
        private BinaryProcess _binaryProcess;
        private bool _disposed;
        internal string _tempPath;
        private bool _keepAlive;
        private IContractResolver _dataContractResolver;

        internal LocalUtilityReportingService(IReportingBinary binary, Configuration configuration, bool keepAlive, string cwd = null, IContractResolver dataContractResolver = null)
        {
            _dataContractResolver = dataContractResolver;
            _keepAlive = keepAlive;
            _tempPath = Path.Combine(configuration.TempDirectory, "autocleanup");
            Directory.CreateDirectory(_tempPath);

            _binaryProcess = new BinaryProcess(binary, configuration, cwd);

            AppDomain.CurrentDomain.DomainUnload += DomainUnloadOrProcessExit;
            AppDomain.CurrentDomain.ProcessExit += DomainUnloadOrProcessExit;
        }

        public Task<Report> RenderAsync(RenderRequest request, CancellationToken ct = default(CancellationToken))
        {
            return RenderAsync(SerializerHelper.SerializeRenderRequest(request, _dataContractResolver), ct);
        }

        public Task<Report> RenderAsync(string templateShortid, object data, CancellationToken ct = default(CancellationToken))
        {
            return RenderAsync(SerializerHelper.SerializeRenderRequest(templateShortid, data, _dataContractResolver), ct);
        }

        public Task<Report> RenderAsync(string templateShortid, string jsonData, CancellationToken ct = default(CancellationToken))
        {
            return RenderAsync(SerializerHelper.SerializeRenderRequest(templateShortid, jsonData, _dataContractResolver), ct);
        }

        public Task<Report> RenderAsync(object request, CancellationToken ct = default(CancellationToken))
        {
            return RenderAsync(SerializerHelper.SerializeRenderRequest(request, _dataContractResolver), ct);
        }

        public Task<Report> RenderByNameAsync(string templateName, string jsonData, CancellationToken ct = default(CancellationToken))
        {
            return RenderAsync(SerializerHelper.SerializeRenderRequestForName(templateName, jsonData, _dataContractResolver), ct);
        }

        public Task<Report> RenderByNameAsync(string templateName, object data, CancellationToken ct = default(CancellationToken))
        {
            return RenderAsync(SerializerHelper.SerializeRenderRequestForName(templateName, data, _dataContractResolver), ct);
        }

        private async Task<Report> RenderAsync(string requestString, CancellationToken ct = default(CancellationToken))
        {
            var reqFile = Path.Combine(_tempPath, $"req{Guid.NewGuid().ToString()}.json");
            File.WriteAllText(reqFile, requestString);

            var outFile = Path.Combine(_tempPath, $"out{Guid.NewGuid().ToString()}");
            var metaFile = Path.Combine(_tempPath, $"meta{Guid.NewGuid().ToString()}");
            var keepAliveParam = _keepAlive ? "--keepAlive" : "";

            var output = await _binaryProcess.ExecuteExe($"render {keepAliveParam} --request=\"{reqFile}\" --out=\"{outFile}\" --meta=\"{metaFile}\"").ConfigureAwait(false);
            if (output.IsError)
            {                
                throw new JsReportBinaryException("Error rendering report: " + output.Logs, output.Logs, output.Command);
            }

            var metaDictionary = new Dictionary<string, string>();
            var meta = JObject.Parse(File.ReadAllText(metaFile));
            meta.Properties().ToList().ForEach(p => metaDictionary[p.Name] = meta[p.Name].ToString());

            return new Report()
            {
                Content = new FileStream(outFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                Meta = SerializerHelper.ParseReportMeta(metaDictionary)
            };
        }

        public Task KillAsync()
        {
            return _binaryProcess.ExecuteExe("kill");
        }

        private async Task TryKill()
        {
            try
            {
                await _binaryProcess.ExecuteExe("kill");
            }
            catch (Exception e)
            {
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            AppDomain.CurrentDomain.DomainUnload -= DomainUnloadOrProcessExit;
            AppDomain.CurrentDomain.ProcessExit -= DomainUnloadOrProcessExit;

            TryKill().Wait();

            _disposed = true;
        }

        private void DomainUnloadOrProcessExit(object sender, EventArgs e)
        {
            Dispose();
        }
    }
}
