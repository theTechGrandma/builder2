using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DSTBuilder.Helpers;
using DSTBuilder.Controllers;
using Microsoft.VisualBasic.FileIO;
using Mercurial;
using System.Text;
using System.Collections;
using System.ServiceProcess;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Evaluation;
using System.Threading.Tasks;


namespace DSTBuilder.Models
{
    public class BuildRepository
    {
        #region Properties
        private readonly Log _buildLog = new Log();
        public Log BuildLog
        {
            get { return _buildLog; }
        }

        public readonly Files _files = new Files();
        public Files Files
        {
            get { return _files; }
        }

        private readonly BatchProcess _batchProcess = new BatchProcess();
        public BatchProcess BatchProcess
        {
            get { return _batchProcess; }
        }

        private readonly XmlController _xml = new XmlController();
        public XmlController Xml
        {
            get { return _xml; }
        }

        private readonly SendMail _sendMail = new SendMail();
        public SendMail SendMail
        {
            get { return _sendMail; }
        }

        private string _sourceRepo;
        public string SourceRepo
        {
            get { return _sourceRepo; }
            set { _sourceRepo = value; }
        }

        private string _product;
        public string Product
        {
            get { return _product; }
            set { _product = value; }
        }

        private string _release;
        public string Release
        {
            get { return _release; }
            set { _release = value; }
        }

        private string _remoteRepo;
        public string RemoteRepo
        {
            get { return _remoteRepo; }
            set { _remoteRepo = value; }
        }

        private string _helpRepo;
        public string HelpRepo
        {
            get { return _helpRepo; }
            set { _helpRepo = value; }
        }

        private string _siteUrl;
        public string SiteUrl
        {
            get { return _siteUrl; }
            set { _siteUrl = value; }
        }

        private string _solutionPath;
        public string SolutionPath
        {
            get { return _solutionPath; }
            set { _solutionPath = value; }
        }

        //private string _deploymentPath = string.Empty;
        //public string DeploymentPath
        //{
        //    get { return _deploymentPath; }
        //}

        private string _buildRepo;
        public string BuildRepo
        {
            get { return _buildRepo; }
            set { _buildRepo = value; }
        }

        private string _lastBuildVersion;
        public string LastBuildVersion
        {
            get { return _lastBuildVersion; }
            set { _lastBuildVersion = value; }
        }

        private string _masterDeployPath;
        public string MasterDeployPath
        {
            get { return _masterDeployPath; }
            set { _masterDeployPath = value; }
        }

        private string _deploymentLocation;
        public string DeploymentLocation
        {
            get { return _deploymentLocation; }
            set { _deploymentLocation = value; }
        }

        private string _workerReleaseFiles;
        public string WorkerReleaseFiles
        {
            get { return _workerReleaseFiles; }
            set { _workerReleaseFiles = value; }
        }

        private string _changeLog;
        public string ChangeLog
        {
            get { return _changeLog; }
            set { _changeLog = value; }
        }

        private string _solutionFile;
        public string SolutionFile
        {
            get { return _solutionFile; }
            set { _solutionFile = value; }
        }

        private string[] _fullVersion;
        public string[] FullVersion
        {
            get { return _fullVersion; }
            set { _fullVersion = value; }
        }

        private string ChangeSet { get; set; }
        private string EmailMessage { get; set; }

        private readonly string _buildEnv = Environment.CurrentDirectory = Environment.GetEnvironmentVariable("Build");
        public string BuildEnv
        {
            get { return _buildEnv; }
        }

        #endregion

        public Log GetStatus()
        {
            return _buildLog;
        }

        #region PushToLogsa
        public bool PushCodeToLogsa(string product, string release, string version)
        {
            try
            {
                _buildLog.Message = "Deploying " + version + " - Starting deployment tasks";
                //Task<bool> tskGetLocalParameters =
                  //      Task.Factory.StartNew<bool>(() => GetBuildPaths(product, release));

                GetBuildPaths();

                _buildLog.Message = "Deploying " + version + " - Removing old files.";

                _files.DeleteDir(_deploymentLocation);

                if (_lastBuildVersion != version)
                {
                    _buildLog.Message = "Deploying " + version + " - Cloning to revision.";
                    CloneToRevision(_remoteRepo, version);
                    _sourceRepo = @"C:\Mercurial\DSTSM" + version;
                }

                _buildLog.Message = "Deploying " + version + " - Checking folders exist";
                _files.CheckDirExists(_deploymentLocation + @"WHS");
                _files.CheckDirExists(_deploymentLocation + @"Web");
                _files.CheckDirExists(_deploymentLocation + @"Web\Help");
                _files.CheckDirExists(_deploymentLocation + @"Configs");
                _files.CheckDirExists(_deploymentLocation + @"Scripts");
                _files.CheckDirExists(_deploymentLocation + @"OracleCode");

                _buildLog.Message = "Deploying " + version + " - Copying over files for zipping.";
                IEnumerable<Server> serverList = _xml.GetServers(product, release);
                foreach (Server server in serverList)
                {
                    IEnumerable<Location> locationItem = _xml.GetLocations(product, release, server.Name);
                    foreach (Location location in locationItem)
                    {
                        if (server.Name == "Help")
                        {
                            FileSystem.CopyDirectory(location.Source, _deploymentLocation + @"Web\Help", true);
                        }
                    }
                }

                FileSystem.CopyDirectory(_sourceRepo + @"Application\Tools\CmdDirectoryCompare", _deploymentLocation + @"Scripts", true);
                FileSystem.CopyDirectory(_sourceRepo + @"Deployment\Logsa", _deploymentLocation + @"Scripts", true);
                FileSystem.CopyDirectory(_sourceRepo + @"Database\LogsaSubversion", _deploymentLocation + @"OracleCode", true);

                FileSystem.CopyDirectory(_buildRepo + @"Application\" + version + @"\", _deploymentLocation + @"Web", true);
                FileSystem.CopyDirectory(_buildRepo + @"WHS\" + version + @"\", _deploymentLocation + @"WHS", true);

                _buildLog.Message = "Deploying " + version + " - Moving config files.";
                File.Move(_deploymentLocation + @"Web\Web.config", _deploymentLocation + @"Configs\Web.config");
                File.Move(_deploymentLocation + @"Web\nlog.config", _deploymentLocation + @"Configs\web-nlog.config");
                File.Move(_deploymentLocation + @"WHS\AdvWorker.exe.config", _deploymentLocation + @"Configs\whs.config");
                File.Move(_deploymentLocation + @"WHS\nlog.config", _deploymentLocation + @"Configs\whs-nlog.config");

                _buildLog.Message = "Zipping them up.";
                _files.ExecuteCommand(@"7z.exe a " + _deploymentLocation + @"DSTSM" + version + @".zip " + _deploymentLocation + "*.* -r -x!*.zip", 300000);

                _buildLog.Message = "Deploying " + version + " - Transfer in progress.";
                _files.ExecuteCommand(@"CoreFTP.exe -s -O -site Egnyte -u " + _deploymentLocation + @"DSTSM" + version + @".zip -p /Shared/promodel/users/zAnnArbor/", 600000);

                _buildLog.Message = "Deploying " + version + " - All done!";
                return true;
            }

            catch (System.Exception ex)
            {
                _buildLog.Message = "Egynte deployment failed " + " - " + ex.ToString();
                return false;
            }
        }
        #endregion

        #region MasterDeploy
        public bool GenerateMasterDeploy(string product, string release, string fromVersion, string toVersion)
        {
            try
            {
                IEnumerable<Versions> version = _xml.GetVersion(product, release);
                //Task<bool> tskGetLocalParameters =
                //        Task.Factory.StartNew<bool>(() => GetBuildPaths(product, release));

                GetBuildPaths();

                //add new version method here

                _buildLog.Message = "Oracle Only - Generating master deploy";

                Pull(_sourceRepo, _deploymentLocation);

                //deletes working files in both the original DOT folder and new working folder for oracle only builds.
                _files.DeleteFiles(_sourceRepo + @"Application\Installation Scripts\DOT", "*.txt");
                _files.DeleteFiles(_masterDeployPath, "*.txt");

                //Status(_sourceRepo + @"Application\", fromVersion, toVersion);
                _batchProcess.RunPowershell(_sourceRepo + @"Application\Installation Scripts\DOT\DOTByVersion.ps1");

                _buildLog.Message = "Oracle Only - Emailing master deploy files";

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
                    _sendMail.EmailSender(_xml.GetOracleEmailGroup(product), "DST Master Deploy", "Master deploy scripts attached for versions " + fromVersion + " to " + toVersion, attachments.ToArray());
                }
                else
                {
                    _sendMail.EmailSender(_xml.GetOracleEmailGroup(product), "No oracle to deploy.", "No master deploy scripts for versions " + fromVersion + " to " + toVersion);
                }

                _buildLog.Message = "Oracle Only - Done! Damn that was fast!";

                return true;
            }

            catch (System.Exception ex)
            {
                _buildLog.Message = "Oracle Only - Generate master deploy failed " + " - " + ex.ToString();
                return false;
            }

        }
        #endregion

        public async Task<bool> StartBuild(string product, string release, string version, bool sendNotification, int minutes)
        {
            try
            {
                while (true)
                {
                    _product = product;
                    _release = release;

                    _buildLog.Message = "Building " + version + " - Starting build";

                    GetBuildPaths();

                    GetVersionInfo();

                    Files.MsBuildCommand(BuildEnv, SolutionFile, SourceRepo);
                    //Files.ExecuteCommand(BuildEnv + @"\msbuild.exe", 1);
                   
                    //if (sendNotification == true)
                    //  SendNotification(product, release, version, minutes, "BINARY AND ORACLE");

                    //_buildLog.Message = "Building " + version + " - Pulling from remote repo.";
                    //Pull(_sourceRepo, _remoteRepo);

                    //Pull(_sourceRepo, _helpRepo);

                    //Task<bool> tskCheckForMultipleHeads =
                    //    Task.Factory.StartNew<bool>(() => CheckForMultipleHeads(_sourceRepo, _remoteRepo));

                    //foreach (string assemblyInfo in File.ReadAllLines(local + "Installation Scripts\\AssemblyInfo - Product.txt"))
                    //{
                    //    _files.AssemblyInfoChanger(assemblyInfo, version);
                    //}

                    //Log(local, lastBuildVersion);

                    //CommitAndPushTag(local, remote, "Assembly commit", version);

                    //Status(local, lastBuildVersion, version);

                    //ManageDSTService(product, release, true);

                    //Task<bool> tskBuildTask =
                    //  Task.Factory.StartNew<bool>(MsBuild);



                    //}


                    // _files.CheckDirExists(deploymentPath);
                    // _buildLog.Message = "Building " + version + " - Compiling DST";
                    // if (CheckFileExistsAndExecute(buildEnv + "\msbuild.exe", local + "DST - All Projects.sln " + @"/t:rebuild /p:VisualStudioVersion=12.0 > ",
                    //     local + @"DSTBuildLog.txt", product + "-" + version + " Compile DST failed")) { return false; }
                    //if (CheckFileExistsAndExecute(buildEnv + @"\msbuild.exe", @"'" + local + "DST - All Projects.sln " + @"/t:rebuild /p:VisualStudioVersion=12.0 > ", local + @"DSTBuildLog.txt", product + "-" + version + " Compile DST failed")) { return false; }
                    //if (CheckFileExistsAndExecute(local + "Installation Scripts\\Compile.DST.cmd", local + "\\DSTBuildLog.txt", product + "-" + version + " Compile DST failed")) { return false; }
                    //_buildLog.Message = "Building " + version + " - Compiling DST Web";
                    //if (CheckFileExistsAndExecute(local + "Installation Scripts\\Compile.DST.Web.cmd", local + "\\DSTWebBuildLog.txt", product + "-" + version + " Compile DST Web failed")) { return false; }
                    //_buildLog.Message = "Building " + version + " - Compiling WHS";
                    // if (CheckFileExistsAndExecute(_local + "Installation Scripts\\Compile.WHS.cmd", _local + "\\WHSBuildLog.txt", _product + "-" + _version + " Compiled WHS failed")) { return false; }

                    //Check Staging folders exist
                    _files.CheckDirExists(_buildRepo + @"Application\" + version + @"\");
                    _files.CheckDirExists(_buildRepo + @"WHS\" + version + @"\");

                    _files.CheckDirExists(_buildRepo + @"Application\Application");
                    _files.CheckDirExists(_buildRepo + @"WHS\Application");

                    //Delete files from the Application and from Application\Release
                    _files.DeleteDir(_buildRepo + @"Application\" + version + @"\");
                    _files.DeleteDir(_buildRepo + @"WHS\" + version + @"\");

                    _files.DeleteDir(_buildRepo + @"Application\Application\");
                    _files.DeleteDir(_buildRepo + @"WHS\Application\");

                    //Copy Compiled code to local repo in both a version folder and main folder.
                    _buildLog.Message = "Building " + version + " - Copying compiled files";
                    FileSystem.CopyDirectory(_buildRepo + @"Application\",
                        _buildRepo + @"Application\Application\", true);
                    FileSystem.CopyDirectory(_buildRepo + @"Application\",
                        _buildRepo + @"Application\" + version + "\\", true);

                    ////WHS
                    _buildLog.Message = "Building " + version + " - Copying WHS files";
                    await
                        _files.CopyFiles(_workerReleaseFiles, _buildRepo + @"WHS\Application\", "*.exe*");
                    await
                        _files.CopyFiles(_workerReleaseFiles,
                            _buildRepo + @"WHS\Application\", "*.dll");
                    await
                        _files.CopyFiles(_workerReleaseFiles,
                            _buildRepo + @"WHS\Application\", "*.config");
                    await
                        _files.CopyFiles(_workerReleaseFiles,
                            _buildRepo + @"WHS\Application\", "*.bat");
                    FileSystem.CopyDirectory(_buildRepo + @"WHS\Application\", _buildRepo + @"WHS\" + version + "\\",
                        true);

                    //Stop each Service
                    _buildLog.Message = "Building " + version + " - Stopping Services";


                    //Delete all files from all Test locations - Then copy in the new stuff
                    IEnumerable<Server> serverList = _xml.GetServers(product, release);
                    foreach (Server server in serverList)
                    {
                        string serverIP = server.IP;
                        IEnumerable<Location> locationItem = _xml.GetLocations(product, release, server.Name);
                        foreach (Location location in locationItem)
                        {
                            string fullPath = @"\\" + serverIP + location.Path;
                            _buildLog.Message = "Building " + version + " - Deleting " + location.Path;
                            _files.DeleteDir(fullPath);

                            //if (server.Name == "Help")
                            //{
                            //    _buildLog.Message = "Building " + version + " - Copying Help";
                            //    FileSystem.CopyDirectory(location.Source, fullPath, true);
                            //}
                            //else
                            //{
                            //    _buildLog.Message = "Building " + version + " - Copying " + location.Source + " to " +
                            //                        fullPath;
                            //    FileSystem.CopyDirectory(_stagingPath + location.Source, fullPath, true);
                            //}

                            if (location.Source == @"Application\Application")
                            {
                                _buildLog.Message = "Building " + version + " - Modifying webserver key";
                                _xml.ChangeXMLConfigs(fullPath + @"\web.config", server.Name,
                                    @"/configuration/appSettings/add", "WebServerName", "key");
                            }

                            if (location.Source.Contains("WHS"))
                            {
                                _buildLog.Message = "Building " + version + " - Creating WHS unique name";
                                _xml.ChangeXMLConfigs(fullPath + @"\AdvWorker.exe.config", location.Name,
                                    @"/configuration/appSettings/add", "WorkerUniqueName", "key");
                            }
                        }
                    }

                    //Start the services back up
                    _buildLog.Message = "Building " + version + " - Starting services backup - Almost there!";
                    ManageDstService(product, release, false);
                    _xml.SetProductVersion(product, release, version);

                    _buildLog.Message = "Binaries/Oracle - Building " + version + " - Success!";
                    _sendMail.EmailSender(_xml.GetEmailGroup(product), product + "-" + version,
                        "Deploy  - Build and deploy is completed! " + EmailMessage);
                    return true;
                }
            }

            catch (System.Exception ex)
            {
                _buildLog.Message = "Building " + version + " - " + ex.ToString();
                _sendMail.EmailSender(_xml.GetEmailGroup(product), product + " " + version + " build has failed!", _buildLog.Message);
                return false;
            }
        }

        #region Helper Methods

        #region Mercurial Helpers

        private bool CheckForMultipleHeads()
        {
            var repo = new Repository(_sourceRepo);
            Changeset[] log = repo.Heads().ToArray();

            if (log.Length > 1)
                _sendMail.EmailSender(_xml.GetEmailGroup("DST"), "DST build failure", "Multiple heads detected. Build halted. Please contact someone who can fix it.");

            return true;
        }

        private void Status(string fromVersion, string toVersion)
        {
            try
            {
                var repo = new Repository(_sourceRepo);
                StringBuilder builder = new StringBuilder();

                string filename = _sourceRepo + "Installation Scripts\\DOT\\IncomingOracleChanges.txt";
                using (StreamWriter writeFile = new StreamWriter(filename, true))
                {
                    FileStatus[] status = repo.Status(new StatusCommand().WithAdditionalArgument("--rev " + fromVersion + ":" + toVersion + " --modified --added")).ToArray();
                    foreach (FileStatus changes in status)
                    {
                        if (changes.State.ToString() != "Removed")
                        {
                            builder.Append(changes.Path);
                            builder.AppendLine();
                        }
                    }
                    writeFile.Write(builder);
                }
            }
            catch (MercurialExecutionException ex)
            {
                // Send email
                _sendMail.EmailSender(_xml.GetOracleEmailGroup("DST"), "Oracle deployment failure", "The master_deployNew could not be created because " + ex.Message + ". The build is continuing.");
            }
        }

        private void Log(string local, string lastBuildVersion)
        {
            try
            {
                var repo = new Repository(local);
                StringBuilder builder = new StringBuilder();
                Changeset[] status = repo.Log(new LogCommand().WithAdditionalArgument("--rev " + lastBuildVersion + ":tip")).ToArray();
                foreach (Changeset changes in status)
                {
                    builder.AppendLine();
                    builder.Append(changes.AuthorName);
                    builder.Append('-');
                    builder.Append(changes.CommitMessage);
                    builder.Append('-');
                    builder.Append(changes.Revision);
                    builder.AppendLine();
                }

                EmailMessage = builder.ToString();
            }
            catch (MercurialExecutionException ex)
            {
                // Swallow this one as it will throw an exception if there is nothing to commit.
                _buildLog.Message = ex.ToString();
                //var exitcode = ex.ExitCode.ToString();
            }
        }

        private void Pull(string local, string remote)
        {
            var repo = new Repository(local);

            repo.Pull(remote, new PullCommand
            {
                Update = true,
            });

        }

        private void CloneToRevision(string remote, string version)
        {
            var repoPath = @"C:\Mercurial\DSTSM\" + version;

            _files.DeleteDir(repoPath);
            Directory.CreateDirectory(repoPath);
            var repo = new Repository(repoPath);
            repo.Clone(remote,
                new CloneCommand()
                    .WithUpdateToRevision(version)
                    .WithTimeout(60000));
        }

        private void CommitAndPushTag(string local, string remote, string message, string version)
        {

            var repo = new Repository(local);

            try
            {
                repo.Commit(new CommitCommand().WithMessage(message).WithAddRemove(true));
                repo.Tag(version);

                repo.Push(remote, new PushCommand
                {
                    AllowCreatingNewBranch = false,
                    Force = false,
                });
            }
            catch (MercurialExecutionException ex)
            {
                // Swallow this one as it will throw an exception if there is nothing to commit.
                _buildLog.Message = ex.ToString();
                //var exitcode = ex.ExitCode.ToString();
            }
        }

        private void CommitAndPush(string local, string remote, string message)
        {

            var repo = new Repository(local);

            try
            {
                repo.Commit(new CommitCommand().WithMessage(message).WithAddRemove(true));
                repo.Push(remote, new PushCommand
                {
                    AllowCreatingNewBranch = false,
                    Force = false,
                });
            }
            catch (MercurialExecutionException ex)
            {
                // Swallow this one as it will throw an exception if there is nothing to commit.
                _buildLog.Message = ex.ToString();
                //var exitcode = ex.ExitCode.ToString();
            }
        }

        private void GetIncomingChanges(string local, string remote)
        {
            var repo = new Repository(local);
            StringBuilder builder = new StringBuilder();

            IEnumerable<Changeset> changeList = repo.Incoming(new IncomingCommand().Source = remote);
            foreach (Changeset changes in changeList)
            {
                builder.Append(changes.CommitMessage);
                builder.Append('-');
                builder.Append(changes.AuthorName);
                builder.AppendLine();
                //ChangeSet = changes.ToString();                
            }

            ChangeSet = builder.ToString();
        }
        #endregion

        #region Methods

        private bool GetBuildPaths()
        {
            if (Product == null) 
                return false;

            var path = _xml.GetPath(Product, Release);
            var buildpaths = path as Path[] ?? path.ToArray();
            foreach (var buildpath in buildpaths)
            {
                if (buildpath.Name == "SourceRepo")
                    SourceRepo = buildpath.Location;

                if (buildpath.Name == "DeploymentLocation")
                    DeploymentLocation = buildpath.Location;

                if (buildpath.Name == "BuildRepo")
                    BuildRepo = buildpath.Location;

                if (buildpath.Name == "RemoteRepo")
                    RemoteRepo = buildpath.Location;

                if (buildpath.Name == "WorkerReleaseFiles")
                    WorkerReleaseFiles = buildpath.Location;

                if (buildpath.Name == "MasterDeployPath")
                    MasterDeployPath = buildpath.Location;
            }
            return true;
        }

        private bool GetVersionInfo()
        {
            if (Product == null)
                return false;

                var version = _xml.GetVersion(Product, Release);
                var versions = version as Versions[] ?? version.ToArray();

                foreach (var item in versions)
                {
                    FullVersion = item.Version.Split('.');
                    LastBuildVersion = _fullVersion[0] + "." + _fullVersion[1] + "." + _fullVersion[2] + "." +
                                        (Convert.ToInt16(_fullVersion[3]) - 1);
                    SiteUrl = item.SiteUrl;
                    SolutionFile = item.SolutionFile;
                }

            return true;
        }

        public bool ServiceAction(Services services, bool serviceStatus)
        {
            ServiceController sc = new ServiceController();
            sc.ServiceName = services.ServiceName;
            sc.MachineName = services.Server;
            const int timeout = 3000;

            try
            {
                if (serviceStatus == false)
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, timeout, 0));
                    }
                }
                else
                {
                    if (sc.Status != ServiceControllerStatus.Stopped)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, timeout, 0));
                    }
                }
            }
            catch (InvalidOperationException io)
            {
                _buildLog.Message = io.Message.ToString();
                _sendMail.EmailTammyOnly("Service Failure", io.Message.ToString());
                return false;
            }
            return true;
        }

        private void ManageDstService(string product, string release, bool serviceState)
        {
            try
            {
                var serviceItem = _xml.GetServices(product, release);
                foreach (var services in serviceItem)
                {
                    if (services == null) continue;
                    try
                    {
                        ServiceAction(services, serviceState);
                    }

                    catch (Exception e)
                    {
                        _buildLog.Message = e.Message.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                _buildLog.Message = e.Message.ToString();
            }
        }

        private bool CheckFileExistsAndExecute(string filePath, string commandline, string logPath, string message)
        {
            // are there errors in the build logs?
            bool flag = false;
            //if (File.Exists(filePath))
            {
                //_batchProcess.RunProcess(filePath);
                _batchProcess.RunProcess(filePath, commandline + logPath);
                if (_files.CheckProcessLog(logPath) == true)
                {
                    _buildLog.Message = message;
                    _sendMail.EmailSender(_xml.GetEmailGroup("DST"), "DST Compile Failure", message, logPath);
                    flag = true;
                }
                else { flag = false; }
            }
            //else
            //{
            //    _buildLog.Message = filePath + " does not exist.";
            //    flag = true;
            //}

            return flag;
        }

        private void SendNotification(string product, string release, string version, int minutes, string type)
        {
            string minuteText = "";
            minuteText = minutes > 1 ? "minutes." : "minute.";

            _buildLog.Message = "Sending notification email";
            _sendMail.EmailSender(_xml.GetEmailGroup(product), "Starting " + product + " - " + version + " build in " + minutes + " " + minuteText, "Starting " + type + " build in " + minutes.ToString() + " " + minuteText);
            _buildLog.Message = "Waiting " + minutes + " " + minuteText;
            Thread.Sleep(minutes * 60000);
        }

        private bool MsBuildApi()
        {
            ProjectCollection pc = new ProjectCollection();
            Dictionary<string, string> globalProperty = new Dictionary<string, string>
            {
                {"Configuration", "Release"},
                {"Platform", "Any CPU"},
                {"PipelinePreDeployCopyAllFilesToOneFolder", "true"},
                {"TransformWebConfig", "true"},
                {"AutoParameterizationWebConfigConnectionStrings", "false"},
                {"OutputPath", SourceRepo + @"\Deployment\"},
                {"_PackageTempDir", SourceRepo + @"\Deployment\"}
            };

            BuildParameters bp = new BuildParameters(pc);
            bp.Loggers = new[] { new MSBuildLogger  {
                    Verbosity = LoggerVerbosity.Normal
                    }
                };

            BuildRequestData buildRequest = new BuildRequestData(_solutionFile, globalProperty, "12.0", new string[] { "Clean", "Build" }, null);
            BuildResult buildResult = BuildManager.DefaultBuildManager.Build(bp, buildRequest);
            if (buildResult.OverallResult == BuildResultCode.Failure)
            {
                _buildLog.Message = buildResult.Exception.ToString();
                return false;
            }
            else
            {
                return true;
            }
        }

    }
        #endregion
        #endregion
}