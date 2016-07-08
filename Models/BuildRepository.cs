using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSTBuilder.Helpers;
using DSTBuilder.Controllers;
using Microsoft.VisualBasic.FileIO;

namespace DSTBuilder.Models
{
    public class BuildRepository
    {
        #region Properties

        private readonly Log _buildLog = new Log();

        public readonly Files Files = new Files();

        private readonly BatchProcess _batchProcess = new BatchProcess();

        private readonly XmlController _xml = new XmlController();

        private readonly SendMail _sendMail = new SendMail();

        private readonly Mercurial _mercurial = new Mercurial();

        private string _localRepo;

        public string Product
        {
            get { return _product; }
            set { _product = value; }
        }

        private string _remoteRepo;

        private string _remoteHelpRepo;

        private string _localHelpRepo;

        private string _buildRepo;

        private string _lastBuildVersion;

        private string _masterDeployPath;

        private string _deploymentLocation;

        private string _workerReleaseFiles;

        private string _solutionFile;

        private string _release;

        private string[] _fullVersion;

        private string _stagingPath;
        private string _product;
        private string _version;
        private string _currentBuildVersion;

        public Log GetStatus()
        {
            return _buildLog;
        }

        #endregion

        #region PushToLogsa

        public void PushCodeToLogsa(string product, string release, string version)
        {
            try
            {
                _buildLog.Message = "Deploying " + version + " - Starting deployment tasks";
                _product = product;
                _version = version;
                _release = release;

                GetBuildPaths();
                GetVersionInfo();

                _buildLog.Message = "Deploying " + version + " - Removing old files.";

                Files.DeleteDir(_deploymentLocation);

                _buildLog.Message = "Deploying " + version + " - Checking folders exist";
                Files.CheckDirExists(_deploymentLocation + @"WHS");
                Files.CheckDirExists(_deploymentLocation + @"Web");
                Files.CheckDirExists(_deploymentLocation + @"Configs");
                Files.CheckDirExists(_deploymentLocation + @"Scripts");
                Files.CheckDirExists(_deploymentLocation + @"OracleCode");

                _buildLog.Message = "Deploying " + version + " - Copying over files for zipping.";

                FileSystem.CopyDirectory(_localRepo + @"\\Deployment\Logsa", _deploymentLocation + @"Scripts", true);
                FileSystem.CopyDirectory(_localRepo + @"\\Database\LogsaSubversion", _deploymentLocation + @"OracleCode",
                    true);

                FileSystem.CopyDirectory(_stagingPath + @"Application\Application\", _deploymentLocation + @"Web",
                    true);
                FileSystem.CopyDirectory(_stagingPath + @"WHS\Application\", _deploymentLocation + @"WHS", true);

                _buildLog.Message = "Deploying " + version + " - Moving config files.";
                File.Move(_deploymentLocation + @"Web\Web.config", _deploymentLocation + @"Configs\Web.config");
                File.Move(_deploymentLocation + @"Web\nlog.config", _deploymentLocation + @"Configs\web-nlog.config");
                File.Move(_deploymentLocation + @"WHS\AdvWorker.exe.config", _deploymentLocation + @"Configs\whs.config");
                File.Move(_deploymentLocation + @"WHS\nlog.config", _deploymentLocation + @"Configs\whs-nlog.config");

                _buildLog.Message = "Zipping them up.";
                BatchProcess.ExecuteCommand(@"7z.exe a " + _deploymentLocation + @"DSTSM" + version + @".zip " + _deploymentLocation + "*.* -r -x!*.zip", 300000);

                _buildLog.Message = "Deploying " + version + " - All done!";
                
            }

            catch (Exception ex)
            {
                _buildLog.Message = "Deployment failed " + " - " + ex;
            }

            BuildLogNull();
        }

        #endregion

        #region MasterDeploy

        public void GenerateMasterDeploy(string product, string release, string fromVersion, string toVersion, bool pullForOracle)
        {
            try
            {
                _buildLog.Message = "Oracle - Generating master deploy";

                if (pullForOracle)
                {
                    _product = product;
                    _release = release;

                    GetBuildPaths();
                    GetVersionInfo();
                    _mercurial.Pull(_localRepo, _remoteRepo);
                }

                //deletes working files in both the original DOT folder and new working folder for oracle only builds.
                if (!Files.DeleteFiles(_localRepo + @"\Application\Installation Scripts\DOT", "*.txt"))
                    return;

                if (!Files.DeleteFiles(_masterDeployPath, "*.txt"))
                    return;

                _mercurial.Status(fromVersion, toVersion, _localRepo);

                _batchProcess.RunPowershell(_localRepo + @"\Application\Installation Scripts\DOT\DOTByVersion.ps1 ", _localRepo);

                _buildLog.Message = "Oracle - Emailing master deploy files";

                //looks for evidence of script files that were generated.
                List<string> attachments = new List<string>();
                if (File.Exists(_masterDeployPath + "master_deploySm.sql"))
                {
                    attachments.Add(_masterDeployPath + "master_deploySm.sql");
                }
                if (File.Exists(_masterDeployPath + "master_deployApp.sql"))
                {
                    attachments.Add(_masterDeployPath + "master_deployApp.sql");
                }
                if (File.Exists(_masterDeployPath + "master_deploySvc.sql"))
                {
                    attachments.Add(_masterDeployPath + "master_deploySvc.sql");
                }
                if (File.Exists(_masterDeployPath + "master_deployBatch.sql"))
                {
                    attachments.Add(_masterDeployPath + "master_deployBatch.sql");
                }

                if (attachments.Count != 0)
                {
                    _sendMail.EmailSender(_xml.GetOracleEmailGroup(product), "DST Master Deploy",
                        "Master deploy scripts attached for versions " + fromVersion + " to " + toVersion,
                        attachments.ToArray());
                }
                else
                {
                    _sendMail.EmailSender(_xml.GetOracleEmailGroup(product), "No oracle to deploy.",
                        "No master deploy scripts for versions " + fromVersion + " to " + toVersion);
                }

                _buildLog.Message = "Oracle Only - Done! Damn that was fast!";

                BuildLogNull();
            }

            catch (Exception ex)
            {
                _buildLog.Message = "Oracle Only - Generate master deploy failed " + " - " + ex;
                // Send email
                _sendMail.EmailSender(_xml.GetOracleEmailGroup("DST"), "Oracle deployment failure",
                    "The master_deployNew could not be created because " + ex.Message + ". The build is continuing.");

                BuildLogNull();
            }

        }

        #endregion

        #region Build

        public void StartBuild(string product, string release, string version, bool sendNotification, int minutes)
        {
            try
            {
                _product = product;
                _release = release;
                _version = version;

                //send Email notification
                if (sendNotification)
                    SendNotification(_version, minutes);

                _buildLog.Message = "Building " + _version + " - Starting build";

                if (GetBuildPaths())
                    return;

                if (GetVersionInfo())
                    return;

                //pulling source
                _buildLog.Message = "Building " + _version + " - Pulling from remote repos.";
                _mercurial.Pull(_localRepo, _remoteRepo);

                _mercurial.PullByBranch(_localHelpRepo, _remoteHelpRepo, _currentBuildVersion);

                _mercurial.CheckForMultipleHeads(_localRepo);

                _buildLog.Message = "Building " + _version + " - Updating assembly info.";
                foreach (string assemblyInfo in File.ReadAllLines(_localRepo + "\\Application\\Installation Scripts\\AssemblyInfo - Product.txt"))
                {
                    Files.AssemblyInfoChanger(assemblyInfo, _version);
                }

                //build solution file
                _buildLog.Message = "Building " + _version + " - Compiling solution file";

                //build the solution
                if (!_batchProcess.CompileSolution(_solutionFile, _localRepo))
                    return;

                _buildLog.Message = "Building " + _version + " - Looking for errors.";
                if (!Files.CheckProcessLog(_localRepo + @"\Application\buildLog.txt"))
                    return;

                _buildLog.Message = "Building " + _version + " - Pushing assembly updates.";
                _mercurial.CommitAndPushTag(_localRepo, _remoteRepo, "Assembly commit", _version);
                _xml.SetProductVersion(_product, _release, _version);

                //Sets bool to false and then calls the oracle deploy process without doing another source pull.
                _buildLog.Message = "Building " + version + " - Doing oracle stuff.";
                GenerateMasterDeploy(_product, _release, _lastBuildVersion, _version, false);

                _buildLog.Message = "Building " + _version + " - Copying crap to staging areas.";

                // copy xap to web folder
                Files.CopyXap(_localRepo + @"\Application\App\Bin\Release",
                    _localRepo + @"\Application\Web\ClientBin");

                Files.CheckDirExists(_buildRepo);

                //Check Staging folders exist
                Files.CheckDirExists(_stagingPath + @"Application\Application");
                Files.CheckDirExists(_stagingPath + @"Application\Application\Help");
                Files.CheckDirExists(_stagingPath + @"WHS\Application");

                //Delete files from the Application
                Files.DeleteDir(_stagingPath + @"Application\Application\");
                Files.DeleteDir(_stagingPath + @"WHS\Application\");

                //Copy Compiled code to local repo.
                _buildLog.Message = "Building " + version + " - Copying compiled files";
                FileSystem.CopyDirectory(_localRepo + @"\Application\Deployment",
                    _stagingPath + @"Application\Application\", true);

                //Copy Help to Staging Path
                _buildLog.Message = "Building " + _version + " - Copying help files to staging path";
                FileSystem.CopyDirectory(_localHelpRepo + @"\Help\Output\Output\UXDEXP5\",
                    _stagingPath + @"Application\Application\Help\", true);

                //Copy the Help PDF
                Files.CopyPdf(_localHelpRepo + @"\Help\Output\Output\DST-SM User Guide\",
                    _stagingPath + @"Application\Application\Help\");

                ////WHS
                _buildLog.Message = "Building " + _version + " - Copying WHS files";
                Files.CopyFiles(_workerReleaseFiles, _stagingPath + @"WHS\Application\");

                //Stop each Service
                _buildLog.Message = "Building " + _version + " - Stopping Services";

                _batchProcess.ManageDstService(_product,release,true);

                //Delete all files from all Test locations - Then copy in the new stuff
                IEnumerable<Server> serverList = _xml.GetServers(_product, _release);
                foreach (Server server in serverList)
                {
                    string serverName = server.Name;
                    IEnumerable<Location> locationItem = _xml.GetLocations(_product, _release, server.Name);
                    foreach (Location location in locationItem)
                    {
                        string fullPath = @"\\" + serverName + location.SharePath;
                        _buildLog.Message = "Building " + _version + " - Deleting " + location.SharePath;
                        Files.DeleteDir(fullPath);

                        _buildLog.Message = "Building " + _version + " - Copying " + location.Source + " to " + location.SharePath;
                        FileSystem.CopyDirectory(_stagingPath + location.Source, fullPath, true);

                        if (location.Source == @"Application\Application")
                        {
                            _buildLog.Message = "Building " + _version + " - Modifying webserver key";
                            _xml.ChangeXMLConfigs(fullPath + @"\web.config", server.Name,
                                @"/configuration/appSettings/add", "WebServerName", "key");
                        }

                        if (location.Source.Contains("WHS"))
                        {
                            _buildLog.Message = "Building " + _version + " - Creating WHS unique name";
                            _xml.ChangeXMLConfigs(fullPath + @"\AdvWorker.exe.config", location.Name,
                                @"/configuration/appSettings/add", "WorkerUniqueName", "key");
                        }
                    }
                }

                //Start the services back up
                _buildLog.Message = "Building " + _version + " - Starting services backup - Almost there!";

                _batchProcess.ManageDstService(_product, release, false);

                _buildLog.Message = "Binaries/Oracle - Building " + _version + " - Success!";
                _sendMail.EmailSender(_xml.GetEmailGroup(product), _product + "-" + _version,
                    "Deploy  - Build and deploy is completed! " + _mercurial.Log(_localRepo, _lastBuildVersion));

                BuildLogNull();
            }

            catch (Exception ex)
            {
                _buildLog.Message = "Building " + _version + " - " + ex;
                _sendMail.EmailSender(_xml.GetEmailGroup(Product), Product + " " + _version + " build has failed!",
                   ex.Message, _localRepo + @"\Application\buildlog.txt");

                BuildLogNull();
            }
        }

        #endregion

        #region Helper Methods

        #region Methods

        private bool GetBuildPaths()
        {
            try
            {
                var path = _xml.GetPath(Product, _release);
                var buildpaths = path as Path[] ?? path.ToArray();
                foreach (var buildpath in buildpaths)
                {
                    if (buildpath.Name == "RemoteRepo")
                        _remoteRepo = buildpath.Location;

                    if (buildpath.Name == "LocalRepo")
                        _localRepo = buildpath.Location;

                    if (buildpath.Name == "DeploymentLocation")
                        _deploymentLocation = buildpath.Location;

                    if (buildpath.Name == "BuildRepo")
                        _buildRepo = buildpath.Location;

                    if (buildpath.Name == "WorkerReleaseFiles")
                        _workerReleaseFiles = buildpath.Location;

                    if (buildpath.Name == "MasterDeployPath")
                        _masterDeployPath = buildpath.Location;

                    if (buildpath.Name == "RemoteHelpRepo")
                        _remoteHelpRepo = buildpath.Location;

                    if (buildpath.Name == "LocalHelpRepo")
                        _localHelpRepo = buildpath.Location;
                }

                _stagingPath = _buildRepo + _release + @"\Staging\";
                return false;
            }
            catch (Exception e)
            {
                _buildLog.Message = e.Message;
                throw;
            }
        }

        private bool GetVersionInfo()
        {
            try
            {
                var version = _xml.GetVersion(_product, _release);
                var versions = version as Versions[] ?? version.ToArray();

                foreach (var item in versions)
                {
                    _lastBuildVersion = item.Version;
                    _fullVersion = item.Version.Split('.');
                    //_lastBuildVersion = _fullVersion[0] + "." + _fullVersion[1] + "." + _fullVersion[2] + "." + (Convert.ToInt16(_fullVersion[3]) - 1);
                    _solutionFile = item.SolutionFile;
                    _currentBuildVersion = _fullVersion[0] + "." + _fullVersion[1] + "." + _fullVersion[2];
                }
                return false;
            }
            catch (Exception e)
            {
                _buildLog.Message = e.Message;
                throw;
            }
        }

        private void SendNotification(string version, int minutes)
        {
            try
            {
                string minuteText = "";
                minuteText = minutes > 1 ? "minutes." : "minute.";

                _buildLog.Message = "Sending notification email";
                _sendMail.EmailSender(_xml.GetEmailGroup(Product),
                    "Starting " + Product + " - " + version + " build in " + minutes + " " + minuteText,
                    "Starting build in " + minutes + " " + minuteText);
                _buildLog.Message = "Waiting " + minutes + " " + minuteText;
                Thread.Sleep(minutes * 60000);
            }
            catch (Exception e)
            {
                _buildLog.Message = e.Message;
            }
        }

        private void BuildLogNull()
        {
            Task.Delay(5000);
            _buildLog.Message = null;
        }

        //msbuild method that doesn't work

        //private void msbuildExecute(string solutionFile, string sourceRepo)
        //{
        //    ProjectCollection pc = new ProjectCollection();
        //    Dictionary<string, string> globalProperty = new Dictionary<string, string>
        //    {
        //        {"Configuration", "Release"},
        //        {"Platform", "Any CPU"},
        //        {"OutputPath" ,  @"C:\Mercurial\DSTSM\TestBuild\Application\App\" + "\\bin\\Release"},
        //        {"TransformWebConfig", "true"},
        //        {"AutoParameterizationWebConfigConnectionStrings", "false"}
        //    };

        //    BuildParameters bp = new BuildParameters(pc)
        //    {
        //        Loggers = new[]
        //        {
        //            new FileLogger
        //            {
        //                Verbosity = LoggerVerbosity.Diagnostic,
        //                ShowSummary = true,
        //                SkipProjectStartedText = false,
        //                Parameters = @"logfile=C:\Mercurial\DSTSM\TestBuild\Application\buildLog.txt"
        //            }
        //        }
        //    };

        //    BuildManager.DefaultBuildManager.BeginBuild(bp);
        //    BuildRequestData buildRequest = new BuildRequestData(solutionFile, globalProperty, "12.0", new string[] { "Rebuild" }, null);
        //    BuildSubmission buildSubmission = BuildManager.DefaultBuildManager.PendBuildRequest(buildRequest);
        //    buildSubmission.Execute();
        //    BuildManager.DefaultBuildManager.EndBuild();
        //    if (buildSubmission.BuildResult.OverallResult == BuildResultCode.Failure)
        //    {
        //        throw new Exception();
        //    }
        //}

        #endregion


        #endregion
    }
}

