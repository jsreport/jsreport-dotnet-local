﻿using jsreport.Shared;
using jsreport.Types;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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

        internal BinaryProcess(IReportingBinary binary, Configuration cfg, string cwd = null)
        {
            _binary = binary;
            Configuration = cfg ?? new Configuration();
            
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

            await _initLocker.WaitAsync().ConfigureAwait(false);            

            try
            {
                if (_initialized)
                {                
                    return;
                }                        
                                
                var jsreportBinaryDirectory = Path.Combine(Configuration.TempDirectory, "dotnet", "binary-" + _binary.UniqueId);          
                Directory.CreateDirectory(jsreportBinaryDirectory);
                                
                _exePath = Path.Combine(jsreportBinaryDirectory, "jsreport.exe");                

                if (File.Exists(_exePath) && new FileInfo(_exePath).Length > 0)
                {
                    return;
                }

                var tmpExePath = Path.Combine(jsreportBinaryDirectory, Shortid() + "jsreport.exe");

                using (var f = File.Create(tmpExePath))
                {
                    _binary.ReadContent().CopyTo(f);
                }

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    AddExecutePermissions(tmpExePath);
                }

                for (var i = 0; i < 10; i++)
                {
                    try
                    {
                        File.Move(tmpExePath, _exePath);
                        break;
                    }
                    catch (Exception e)
                    {
                        if (i == 9)
                        {
                            throw;
                        }

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

        internal async Task<ProcessOutput> ExecuteExe(string cmd, bool waitForExit = true, CancellationToken ct = default)
        {            
            await EnsureInitialized().ConfigureAwait(false);
            return await InnerExecute(cmd, waitForExit, ct).ConfigureAwait(false);
        }
        private async Task<ProcessOutput> InnerExecute(string cmd, bool waitForExit = true, CancellationToken ct = default)
        {
            var logs = "";
            var errLogs = "";
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
                    if (!worker.StartInfo.EnvironmentVariables.ContainsKey(e.Key))
                    {
                        worker.StartInfo.EnvironmentVariables.Add(e.Key, e.Value);
                    }
                }
            }

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
                    errLogs += e.Data;

                    if (ErrorDataReceived != null)
                    {
                        ErrorDataReceived.Invoke(sender, e);
                    }
                }
            };

            try
            {                
                worker.Start();
            } catch (Win32Exception e)
            {
                if (e.NativeErrorCode != 5)
                {
                    throw;
                }
                
                throw new JsReportBinaryException($@"Access denied to jsreport binary at {_exePath}
Make sure application user has permissions to execute from this location or change it using:
new LocalReporting().TempDirectory(Path.Combine(HostingEnvironment.MapPath(""~""), ""jsreport"", ""temp""))", e);                
            }

            worker.BeginOutputReadLine();
            worker.BeginErrorReadLine();

            if (waitForExit)
            {
                await worker.WaitForExitAsync(ct).ConfigureAwait(false);               
                return new ProcessOutput(worker, !worker.HasExited || worker.ExitCode != 0, _exePath + cmd, errLogs == "" ? logs : (errLogs + "\n" + logs));
            }

            return new ProcessOutput(worker, false, _exePath + cmd, errLogs == "" ? logs : (errLogs + "\n" + logs));
        }

        private static byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }       

        private string Shortid()
        {
            return Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");
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
