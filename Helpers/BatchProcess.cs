using System;
using System.Diagnostics;
using System.Security;
using System.Management.Automation.Runspaces;
using System.Management.Automation;
using DSTBuilder.Models;
using System.Configuration;

namespace DSTBuilder.Helpers
{
    public class BatchProcess
    {
        #region Fields/Properties
        #endregion Fields/Properties

        #region Methods

        
        public bool RunProcess(string path)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = path;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.Verb = "runas";

            proc.Start();

            if (!proc.WaitForExit(1500000))
            {
                proc.Kill();
                return false;
            }

            int exitCode = proc.ExitCode;
            proc.Close();
            if (exitCode == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool RunProcess(string path, string commandline)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = path;
            proc.StartInfo.Arguments = commandline;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.Verb = "runas";
            proc.Start();
            if (!proc.WaitForExit(1500000))
            {
                proc.Kill();
                return false;
            }
            int exitCode = proc.ExitCode;
            proc.Close();
            if (exitCode == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void RunPowershell(string scriptFile)
        {            
            RunspaceConfiguration runspaceConfiguration = RunspaceConfiguration.Create();

            Runspace runspace = RunspaceFactory.CreateRunspace(runspaceConfiguration);
            runspace.Open();

            RunspaceInvoke scriptInvoker = new RunspaceInvoke(runspace);
            scriptInvoker.Invoke("Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Unrestricted");

            Pipeline pipeline = runspace.CreatePipeline();
            Command myCommand = new Command(scriptFile);
            pipeline.Commands.Add(myCommand);
            
            try
            {
                pipeline.Invoke();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            pipeline.Stop();
            runspace.Close();           

        }
        public void RunPowerShellScript(Services delegatorItem, bool stopService)
        {
            string shellUri = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell";
            var username = ConfigurationManager.AppSettings["filePathUsername"];
            var password = ConfigurationManager.AppSettings["filePathPassword"];

            SecureString secureString = new SecureString();
            string myPassword = password;

            foreach (char c in myPassword)
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly();

            PSCredential remoteCredential = new PSCredential(username, secureString);

            WSManConnectionInfo connectionInfo = new WSManConnectionInfo(false, delegatorItem.Server, 5985, "/wsman", shellUri, remoteCredential);
            connectionInfo.AuthenticationMechanism = AuthenticationMechanism.Default;

            using (Runspace runspace = RunspaceFactory.CreateRunspace(connectionInfo))
            {
                runspace.Open();

                using (PowerShell powershell = PowerShell.Create())
                {
                    string passwordScript = "$password = ConvertTo-SecureString \"" + password + "\" -AsPlainText -Force";
                    string credentialScript = "$cred= New-Object System.Management.Automation.PSCredential(\"" + delegatorItem.Server + @"\" + username + "\", $password)";
                    string enterSessionScript = "Enter-PSSession " + delegatorItem.Server + " -credential $cred";
                    string policy = "Set-ExecutionPolicy Unrestricted";
                    string stopServiceScript = "Stop-Service -InputObject $(Get-Service -Computer " + delegatorItem.Server + " -Name " + "\"" + delegatorItem.ServiceName + "\"" + ")";
                    string startServiceScript = "Start-Service -InputObject $(Get-Service -Computer " + delegatorItem.Server + " -Name " + "\"" + delegatorItem.ServiceName + "\"" + ")";
                    
                    powershell.Runspace = runspace;
                    powershell.Commands.AddScript(passwordScript);
                    powershell.Commands.AddScript(credentialScript);
                    powershell.Commands.AddScript(enterSessionScript);
                    powershell.Commands.AddScript(policy);
                    if (stopService)
                    {
                        powershell.Commands.AddScript(stopServiceScript);

                    }
                    else
                    {
                        powershell.Commands.AddScript(startServiceScript);
                    }
                    powershell.Invoke();

                }

                // close the runspace
                runspace.Close();
            }
        }
        #endregion Methods
    }
}