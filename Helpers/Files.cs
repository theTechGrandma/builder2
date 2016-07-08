using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DSTBuilder.Helpers
{
    public class Files
    {
        public void CheckDirExists(string dirToCheck)
        {
            if (Directory.Exists(dirToCheck)) return;
            try
            {
                Directory.CreateDirectory(dirToCheck);
            }
            catch
            { 
                throw new Exception("Cannot create directory" + dirToCheck);
            }
        }

        public bool DeleteFiles(string filesToDelete, string wildcard)
        {
            if (Directory.Exists(filesToDelete))
            {
                try
                {
                    foreach (
                    var file in Directory.GetFiles(filesToDelete, wildcard, System.IO.SearchOption.TopDirectoryOnly))
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                   
                }
                catch
                {
                    throw new Exception("Cannot delete files" + filesToDelete);
                }
            }
            return true;
        }

        public bool DeleteDir(string pathToDelete)
        {
            foreach (var directory in Directory.GetDirectories(pathToDelete))
            {
                DeleteDir(directory);
            }

            try
            {
                Directory.Delete(pathToDelete, true);
            }
            catch (IOException)
            {
                Directory.Delete(pathToDelete, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(pathToDelete, true);
            }
            return true;
        }

        public void AssemblyInfoChanger(string assemblyInfoPath, string version)
        {
            if (!File.Exists(assemblyInfoPath)) return;
            // Set Permission to writable
            File.SetAttributes(assemblyInfoPath, FileAttributes.Normal);

            // string array to hold information
            string[] beginFile = File.ReadAllLines(assemblyInfoPath);

            // new List to write out new information
            var endFile = new List<string>();

            try
            {
                // loop to change out information we need
                foreach (var currentString in beginFile)
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
            }
            catch (Exception ex)
            {
                throw new Exception("The service has a problem.", ex);
            }
        }

        public bool CheckProcessLog(string logPath)
        {
            //are there errors in the logs?
            if (logPath == null || !logPath.Contains("buildLog.txt")) return true;
            if (!File.ReadAllText(logPath).Contains("0 Error(s)"))
                throw new Exception("Compile Errors!");

            if (File.ReadAllText(logPath).Contains("FAILED"))
                throw new Exception("Compile Errors!");
            return true;
        }

        public string IncrementVersion(string currentString)
        {
            var version = currentString;
            try
            {
                var parseQuotes = currentString.Split('\"');

                foreach (string versions in parseQuotes)
                {
                    // if it's the one we want "1.0.0.0"
                    if (!versions.Contains('.')) continue;
                    string[] allVersions = versions.Split('.');

                    int incVersion;
                    if (!int.TryParse(allVersions[3], out incVersion)) continue;
                    incVersion++;
                    version = allVersions[0] + "." + allVersions[1] + "." + allVersions[2] + "." +
                              incVersion;
                }
            }
            catch
            {
                throw new Exception("Problem updating assemblies references.");
            }
           
            return version;
        }

        public bool CopyPdf(string start, string finish)
        {
            if (!Directory.Exists(finish))
                return false;

            var files = Directory.EnumerateFiles(start, "*.*", SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".pdf"));
            foreach (var file in files)
            {
                try
                {
                    var newFile = file.Replace(start, finish);
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Copy(file, newFile, true);
                }
                catch(Exception ex)
                {
                    throw new Exception("Problems copying xap.", ex);
                }
            }
            return true;
        }

        public bool CopyXap(string start, string finish)
        {
            if (!Directory.Exists(finish))
                return false;

            var files = Directory.EnumerateFiles(start, "*.*", SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".xap"));
            foreach (var file in files)
            {
                try
                {
                    var newFile = file.Replace(start, finish);
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Copy(file, newFile, true);
                }
                catch(Exception ex)
                {
                    throw new Exception("Problems copying xap.", ex);
                }
            }
            return true;
        }

        public bool CopyFiles(string start, string finish)
        {
            Directory.CreateDirectory(finish);
            var files = Directory.EnumerateFiles(start, "*.*", SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".exe") || s.EndsWith(".dll") || s.EndsWith(".config") || s.EndsWith(".bat") || s.EndsWith(".xap"));
            foreach (var file in files)
            {
                try
                {
                    var newFile = file.Replace(start, finish);
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Copy(file, newFile, true);
                }
                catch
                {
                    throw new Exception("Problems copying files.");
                }
            }
            return true;
        }
        
    }
}