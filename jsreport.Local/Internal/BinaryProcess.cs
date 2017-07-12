using jsreport.Binary;
using jsreport.Shared;
using jsreport.Types;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace jsreport.Local.Internal
{   
    internal class BinaryProcess
    {
        private string _workingPath;
        private string _exePath;        
        private bool _initialized;

        internal string TempPath { get; private set; }        
        internal Configuration Configuration { get; private set; }

        internal BinaryProcess(Configuration cfg = null)
        {
            Configuration = cfg ?? new Configuration();

            TempPath = Path.Combine(Path.GetTempPath(), "jsreport-temp");
            if (!Directory.Exists(TempPath))
            {
                Directory.CreateDirectory(TempPath);
            }

            _workingPath = Path.Combine(Path.GetDirectoryName(typeof(LocalUtilityReportingService).Assembly.Location), "jsreport");

            if (!Directory.Exists(_workingPath))
            {
                Directory.CreateDirectory(_workingPath);
            }

            _exePath = Path.Combine(_workingPath, "jsreport.Local");
        }

        private static SemaphoreSlim _initLocker = new SemaphoreSlim(1);

        public event DataReceivedEventHandler OutputDataReceived;
        public event DataReceivedEventHandler ErrorDataReceived;

        internal async Task EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            await _initLocker.WaitAsync();

            if (_initialized)
            {
                return;
            }

            try
            {
                var stream = JsReportBinary.Extract();

                for (var i = 0; i < 10; i++)
                {
                    try
                    {
                        using (var fs = File.Create(_exePath))
                        {
                            stream.CopyTo(fs);
                        }
                        break;
                    }
                    catch (Exception e)
                    {
                        Thread.Sleep(50);
                    }
                }
                
                _initialized = true;
            }
            finally
            {
                _initLocker.Release();
            }

        }        

        internal async Task<ProcessOutput> ExecuteExe(string cmd, bool waitForExit = true)
        {            
            await EnsureInitialized();
            return await InnerExecute(cmd, waitForExit);
        }

        private async Task<ProcessOutput> InnerExecute(string cmd, bool waitForExit = true)
        {
            var logs = "";
            var worker = new Process()
            {
                StartInfo = new ProcessStartInfo(_exePath)
                {
                    Arguments = cmd,
                    WorkingDirectory = _workingPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };

            worker.StartInfo.EnvironmentVariables.Remove("COMPLUS_Version");
            worker.StartInfo.EnvironmentVariables.Remove("COMPLUS_InstallRoot");

            if (Configuration != null)
            {
                foreach (var e in SerializerHelper.SerializeConfigToDictionary(Configuration))
                {
                    worker.StartInfo.EnvironmentVariables.Add(e.Key, e.Value);
                }
            }

            var vars = worker.StartInfo.EnvironmentVariables;

            worker.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logs += e.Data;

                    if (OutputDataReceived != null) {
                        OutputDataReceived.Invoke(sender, e);
                    }
                }
            };

            worker.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logs += e.Data;

                    if (ErrorDataReceived != null)
                    {
                        ErrorDataReceived.Invoke(sender, e);
                    }
                }
            };

            worker.Start();

            worker.BeginOutputReadLine();
            worker.BeginErrorReadLine();

            if (waitForExit)
            {
                await worker.WaitForExitAsync(); 
                return new ProcessOutput(worker, !worker.HasExited || worker.ExitCode != 0, _exePath + cmd, logs);
            }

            return new ProcessOutput(worker, false, _exePath + cmd, logs);
        }        
    }
}
