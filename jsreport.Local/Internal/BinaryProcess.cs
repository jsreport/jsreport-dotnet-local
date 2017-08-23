using jsreport.Shared;
using jsreport.Types;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace jsreport.Local.Internal
{   
    internal class BinaryProcess
    {
        private string _workingPath;
        private string _exePath;        
        private bool _initialized;
        private IReportingBinary _binary;        
        internal Configuration Configuration { get; private set; }         

        internal BinaryProcess(IReportingBinary binary, string cwd = null, Configuration cfg = null)
        {
            _binary = binary;
            Configuration = cfg ?? new Configuration();
            // GetEntryAssembly works in .net core, GetExecutingAssembly in the full .net asp.net
            var entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            string codeBase = entryAssembly.CodeBase.Replace("file:///", "");
            var binDir = Path.GetDirectoryName(codeBase);            

            _workingPath = cwd ?? Path.Combine(binDir, "jsreport");
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

                var jsreportHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".jsreport");
                if (!Directory.Exists(jsreportHome))
                {
                    Directory.CreateDirectory(jsreportHome);
                }

                var jsreportBinaryDirectory = Path.Combine(jsreportHome, "binary-" + _binary.UniqueId);
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
                        using (var f = File.Create(_exePath))
                        {
                            _binary.ReadContent().CopyTo(f);
                        }

                        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            AddExecutePermissions(_exePath);
                        }
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        Thread.Sleep(50);
                    }
                }
            }
            finally
            {
                _initialized = true;             
                _initLocker.Release();
            }
        }        

        internal async Task<ProcessOutput> ExecuteExe(string cmd, bool waitForExit = true)
        {            
            await EnsureInitialized().ConfigureAwait(false);
            return await InnerExecute(cmd, waitForExit).ConfigureAwait(false);
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
                await worker.WaitForExitAsync().ConfigureAwait(false); 
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

        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);

        private void AddExecutePermissions(string path)
        {
            const int S_IRUSR = 0x100;
            const int S_IWUSR = 0x80;
            const int S_IXUSR = 0x40;

            // group permission
            const int S_IRGRP = 0x20;
            const int S_IWGRP = 0x10;
            const int S_IXGRP = 0x8;

            // other permissions
            const int S_IROTH = 0x4;
            const int S_IWOTH = 0x2;
            const int S_IXOTH = 0x1;
                        
            const int _0755 =
                S_IRUSR | S_IXUSR | S_IWUSR
                | S_IRGRP | S_IXGRP
                | S_IROTH | S_IXOTH;
            chmod(path, (int)_0755);
        }
    }
}
