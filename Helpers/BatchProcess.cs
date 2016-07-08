using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation.Runspaces;
using System.Management.Automation;
using System.ServiceProcess;
using DSTBuilder.Controllers;
using DSTBuilder.Models;

namespace DSTBuilder.Helpers
{
    public class BatchProcess
    {
        #region Fields/Properties
        private readonly XmlController _xml = new XmlController();
        private readonly SendMail _sendMail = new SendMail();
        #endregion Fields/Properties

        #region Methods

        public void RunPowerShellWithParameter(string scriptFile, string param)
        {
            Runspace runspace = null;
            Pipeline pipeline = null;

            try
            {
                RunspaceConfiguration runspaceConfiguration = RunspaceConfiguration.Create();
                runspace = RunspaceFactory.CreateRunspace(runspaceConfiguration);
                runspace.Open();
                RunspaceInvoke scriptInvoker = new RunspaceInvoke(runspace);
                pipeline = runspace.CreatePipeline();

                String scriptfile = scriptFile;
                Command myCommand = new Command(scriptfile, false);
                //CommandParameter testParam = new CommandParameter(param);
                //myCommand.Parameters.Add(testParam);
                pipeline.Commands.Add(myCommand);
                Collection<PSObject> psObjects;
                psObjects = pipeline.Invoke();
                runspace.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("Powershell had an issue running.", ex);
            }
            finally
            {
                if (pipeline != null) pipeline.Dispose();
                if (runspace != null) runspace.Dispose();
            }
        }

        public void RunPowershell(string scriptFile, string baseDir)
        {
            RunspaceConfiguration runspaceConfiguration = RunspaceConfiguration.Create();

            Runspace runspace = RunspaceFactory.CreateRunspace(runspaceConfiguration);
            runspace.Open();

            var scriptInvoker = new RunspaceInvoke(runspace);
            scriptInvoker.Invoke("Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Unrestricted");

            Pipeline pipeline = runspace.CreatePipeline();
            var myCommand = new Command(scriptFile);
            pipeline.Commands.Add(myCommand);
            try
            {
                pipeline.Invoke();
            }
            catch (Exception ex)
            {
                throw new Exception("Powershell had an issue running the script.", ex);
            }

            pipeline.Stop();
            runspace.Close();

        }

        public bool CompileSolution(string solution, string sourceRepo)
        {
            {
                string solutionFileName = string.Format("\"{0}\"", solution);
                var startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = false,
                    UseShellExecute = false,
                    FileName = @"C:\Program Files (x86)\MSBuild\12.0\Bin\msbuild.exe",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments =
                        solutionFileName +
                        @" /t:Clean;Rebuild /p:Configuration=Release /p:DeployOnBuild=True /p:VisualStudioVersion=12.0 /p:DeployDefaultTarget=WebPublish /p:WebPublishMethod=FileSystem /p:DeleteExistingFiles=True /p:publishUrl=" +
                        sourceRepo + @"/Application/Deployment /l:FileLogger,Microsoft.Build;verbosity=normal;logfile=" + sourceRepo +
                        @"\Application\buildlog.txt"
                };

                try
                {
                    // Start the process with the info we specified.
                    // Call WaitForExit and then the using-statement will close.
                    using (var exeProcess = Process.Start(startInfo))
                    {
                        if (exeProcess != null) exeProcess.WaitForExit();
                        //if (exeProcess != null)
                        //{
                        //    if (!exeProcess.WaitForExit()) //5 mins 1000 * 60 * 5
                        //    {
                        //        exeProcess.Kill();
                        //        throw new Exception("Build process was killed.");
                        //    }
                        //}

                    }
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public static int ExecuteCommand(string command, int timeout)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/C " + command)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = "C:\\",
            };

            var process = Process.Start(processInfo);
            process.WaitForExit(timeout);
            var exitCode = process.ExitCode;
            process.Close();
            return exitCode;
        }

        public void ManageDstService(string product, string release, bool serviceState)
        {
            var serviceItem = _xml.GetServices(product, release);
            foreach (var services in serviceItem)
            {
                if (services == null) continue;
                try
                {
                    ServiceAction(services, serviceState);
                }
                catch (Exception ex)
                {
                    throw new Exception("The service has a problem.", ex);
                }
            }
        }

        public void ServiceAction(Services services, bool serviceStatus)
        {
            var sc = new ServiceController
            {
                ServiceName = services.ServiceName,
                MachineName = services.Server
            };
            const int timeout = 3000;

            try
            {
                if (serviceStatus == false)
                {
                    if (sc.Status == ServiceControllerStatus.Running) return;
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, timeout, 0));
                }
                else
                {
                    if (sc.Status == ServiceControllerStatus.Stopped) return;
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, timeout, 0));
                }
            }
            catch (InvalidOperationException io)
            {
                _sendMail.EmailTammyOnly("Service Failure", io.Message);
                throw new Exception("The service has a problem.", io);
            }
        }

        #endregion Methods
    }
}