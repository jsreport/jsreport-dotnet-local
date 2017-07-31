using jsreport.Shared;
using jsreport.Types;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace jsreport.Local.Internal
{   
    internal class BinaryProcess
    {
        private string _workingPath;
        private string _exePath;        
        private bool _initialized;
        private Stream _binaryStream;        
        internal Configuration Configuration { get; private set; }         

        internal BinaryProcess(Stream binaryStream, string cwd = null, Configuration cfg = null)
        {
            _binaryStream = binaryStream;
            Configuration = cfg ?? new Configuration();          


            _workingPath = cwd ?? Path.Combine(Path.GetDirectoryName(typeof(LocalUtilityReportingService).Assembly.Location), "jsreport");
            if (!Directory.Exists(_workingPath))
            {
                Directory.CreateDirectory(_workingPath);
            }            
        }

        private static SemaphoreSlim _initLocker = new SemaphoreSlim(1);

        internal event DataReceivedEventHandler OutputDataReceived;
        internal event DataReceivedEventHandler ErrorDataReceived;

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
                CleanEmptyDataFolders();
                var exeBuffer = ReadFully(_binaryStream);

                var jsreportHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".jsreport");
                if (!Directory.Exists(jsreportHome))
                {
                    Directory.CreateDirectory(jsreportHome);
                }

                var jsreportBinaryDirectory = Path.Combine(jsreportHome, "binary-" + exeBuffer.Length);
                if (!Directory.Exists(jsreportBinaryDirectory))
                {
                    Directory.CreateDirectory(jsreportBinaryDirectory);
                }

                _exePath = Path.Combine(jsreportBinaryDirectory, "jsreport.exe");

                if (File.Exists(_exePath))
                {
                    return;
                }

                for (var i = 0; i < 10; i++)
                {
                    try
                    {
                        File.WriteAllBytes(_exePath, exeBuffer);                        
                        break;
                    }
                    catch (Exception e)
                    {
                        Thread.Sleep(50);
                    }
                }
            }
            finally
            {
                _initialized = true;
                _binaryStream.Dispose();
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

        private static byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        // visual studio always keeps some empty folders after build even the whole jsreport is set to Copy Always
        // we need to delete these old empty folders to avoid nedb failures on start
        private void CleanEmptyDataFolders()
        {
            var data = Path.Combine(_workingPath, "data");

            if (!Directory.Exists(data))
            {
                return;
            }

            Directory.GetDirectories(data).ToList().ForEach(d => Directory.GetDirectories(d).ToList().ForEach(nd =>
            {
                // nd is entity folder like Template1
                if (!Directory.EnumerateFiles(nd).Any())
                {
                    Directory.Delete(nd);
                }
            }));
        }
    }
}
