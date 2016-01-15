using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSTBuilder.Models;
using DSTBuilder.Controllers;
using System.Diagnostics;
using System.Security;
using System.Threading.Tasks;

namespace DSTBuilder.Helpers
{
    public class Files
    {

        private BatchProcess _pmProcess = new BatchProcess();
        private Log _buildLog = new Log();
        private SendMail _sendMail = new SendMail();
        private XmlController _xml = new XmlController();

        public bool CheckDirExists(string dirToCheck)
        {
            if (!Directory.Exists(dirToCheck))
            {
                Directory.CreateDirectory(dirToCheck);
            }
            return true;
        }

        public bool DeleteFiles(string filesToDelete, string wildcard)
        {
            if (Directory.Exists(filesToDelete))
            {
                foreach (
                    string file in Directory.GetFiles(filesToDelete, wildcard, System.IO.SearchOption.TopDirectoryOnly))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool DeleteDir(string pathToDelete)
        {
            if (Directory.Exists(pathToDelete))
            {
                DateTime date = DateTime.Now.AddDays(-3);

                DirectoryInfo diTop = new DirectoryInfo(pathToDelete);

                foreach (var fi in diTop.EnumerateFiles())
                {
                    try
                    {
                        if (!fi.Name.Contains("dstmlog"))
                        {
                            fi.Delete();
                        }

                        if (fi.Name.Contains("dstmlog"))
                        {
                            if (date > fi.CreationTime)
                                fi.Delete();
                        }
                    }
                    catch (UnauthorizedAccessException UnAuthTop)
                    {
                        Console.WriteLine("{0}", UnAuthTop.Message);
                    }
                }

                foreach (var di in diTop.EnumerateDirectories("*"))
                {
                    try
                    {
                        di.Delete(true);
                    }
                    catch (UnauthorizedAccessException UnAuthSubDir)
                    {
                        Console.WriteLine("UnAuthSubDir: {0}", UnAuthSubDir.Message);
                    }
                }
            }

            return true;
        }

        public bool AssemblyInfoChanger(string assemblyInfoPath, string version)
        {
            if (File.Exists(assemblyInfoPath))
            {
                // Set Permission to writable
                File.SetAttributes(assemblyInfoPath, FileAttributes.Normal);

                // string array to hold information
                string[] beginFile = File.ReadAllLines(assemblyInfoPath);

                // new List to write out new information
                List<string> endFile = new List<string>();

                // loop to change out information we need
                foreach (string currentString in beginFile)
                {
                    if (currentString.Contains("AssemblyVersion") && !currentString.Contains(@"//"))
                    {
                        endFile.Add(@"[assembly: AssemblyVersion(""" + version + @""")]");
                    }
                    else if (currentString.Contains("AssemblyFileVersion") && !currentString.Contains(@"//"))
                    {
                        endFile.Add(@"[assembly: AssemblyFileVersion(""" + version + @""")]");
                    }
                    else
                    {
                        endFile.Add(currentString);
                    }
                }

                // write out information to new AssemblyInfo.cs
                File.WriteAllLines(assemblyInfoPath, endFile.ToArray());
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IncrementAssemblyVersion(string assemblyInfoPath)
        {
            if (File.Exists(assemblyInfoPath))
            {
                // Set Permission to writable
                File.SetAttributes(assemblyInfoPath, FileAttributes.Normal);

                // string array to hold information
                string[] beginFile = File.ReadAllLines(assemblyInfoPath);

                // new List to write out new information
                List<string> endFile = new List<string>();

                // loop to change out information we need
                foreach (string currentString in beginFile)
                {
                    // Increment AssemblyVersion
                    if (currentString.Contains("AssemblyVersion") && !currentString.Contains(@"//"))
                    {
                        endFile.Add(@"[assembly: AssemblyVersion(""" + IncrementVersion(currentString) + @""")]");
                    }

                    // Increment AssemblyFileVersion
                    else if (currentString.Contains("AssemblyFileVersion") && !currentString.Contains(@"//"))
                    {
                        endFile.Add(@"[assembly: AssemblyFileVersion(""" + IncrementVersion(currentString) + @""")]");
                    }

                    // else keep it as is
                    else
                    {
                        endFile.Add(currentString);
                    }

                }
                // write out information to new AssemblyInfo.cs
                File.WriteAllLines(assemblyInfoPath, endFile.ToArray());
                return true;
            }
            else
            {
                _buildLog.Message = "Cannot increment assembly version.";
                return false;
            }
        }

        public void CheckFileExistsAndExecute(string filePath)
        {
            if (File.Exists(filePath))
            {
                _pmProcess.RunProcess(filePath);
            }
        }

        public bool CheckProcessLog(string logPath)
        {
            if (File.Exists(logPath))
            {
                //are there errors in the logs?
                bool flag = false;

                if (logPath.Contains("DSTBuildLog.txt"))
                {
                    if (File.ReadAllText(logPath).Contains("0 failed"))
                    {
                        flag = false;
                    }
                    else
                    {
                        flag = true;
                    }
                }
                else if (logPath.Contains("DSTWebBuildLog.txt"))
                {
                    if (File.ReadAllText(logPath).Contains("0 Error(s)"))
                    {
                        flag = false;
                    }
                    else
                    {
                        flag = true;
                    }
                }
                else if (logPath.Contains("WHSBuildLog.txt"))
                {
                    if (File.ReadAllText(logPath).Contains("0 failed"))
                    {
                        flag = false;
                    }
                    else
                    {
                        flag = true;
                    }
                }

                return flag;
            }
            else
            {
                return false;
            }

        }

        public string IncrementVersion(string currentString)
        {
            string version = currentString;

            // First Parse Quotes 
            string[] parseQuotes = currentString.Split('\"');

            foreach (string versions in parseQuotes)
            {
                // if it's the one we want "1.0.0.0"
                if (versions.Contains('.'))
                {
                    string[] AllVersions = versions.Split('.');

                    int incVersion;
                    if (int.TryParse(AllVersions[3], out incVersion))
                    {
                        incVersion++;
                        version = AllVersions[0] + "." + AllVersions[1] + "." + AllVersions[2] + "." +
                                  incVersion.ToString();
                    }
                }
            }
            return version;
        }

        public bool CheckFileExists(string pathToCheck)
        {
            if (File.Exists(pathToCheck))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //public bool DeleteChangeLog()
        //{
        //    // deletes the log
        //    if (File.Exists("C:\\Logs\\ChangedSourceFiles.txt"))
        //    {
        //        File.Delete("C:\\Logs\\ChangedSourceFiles.txt");
        //        return true;
        //    }
        //    else
        //    {
        //        return true;
        //    }
        //}

        public async Task<bool> CopyFiles(string startDirectory, string endDirectory, string fileType)
        {
            Directory.CreateDirectory(endDirectory);
            foreach (string filename in Directory.EnumerateFiles(startDirectory))
            {
                using (FileStream sourceStream = File.Open(filename, FileMode.Open))
                {
                    using (
                        FileStream destinationStream =
                            File.Create(endDirectory + filename.Substring(filename.LastIndexOf('\\'))))
                    {
                        await sourceStream.CopyToAsync(destinationStream);
                    }
                }
            }
            return false;
        }

        public int ExecuteCommand(string command, int timeout)
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

        public int MsBuildCommand(string buildLocation, string solution, string buildRepo)
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";
            try
            {
                proc.Start();
                //var msbuildpath = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";
                //var msbuildExeArguments = " /t:rebuild /p:Configuration=Release";

                //var processInfo = new ProcessStartInfo()
                //{
                //    //FileName = msbuildpath,
                //    WorkingDirectory = "C:\\",
                //    Arguments = "/t:rebuild /p:Configuration=Release",
                //    CreateNoWindow = true,
                //    UseShellExecute = false
                //};

                //Process process = Process.Start(processInfo);
                //Console.WriteLine(process.StandardOutput.ReadToEnd());
            }
            catch (Exception)
            {
                
                throw;
            }

            try
            {
                if (proc.HasExited)
                {
                    //....
                }
            }
            catch (System.InvalidOperationException e)
            {
                //cry and weep about it here.
            }

            finally
            {
                proc.Close();
            }

            return proc.ExitCode;
        }


        
    }
}