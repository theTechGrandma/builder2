using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DSTBuilder.Models
{
    public class Versions
    {
        public string Version { get; set; }
        public string Release { get; set; }
        public string LastBuildDateTime { get; set; }
        public string SiteUrl { get; set; }
        public string SolutionFile { get; set; }
    }

    public class Services
    {
        public string Server { get; set; }
        public string Name { get; set; }
        public string ServiceName { get; set; }
    }

    public class Server
    {
        public string Name { get; set; }
    }

    public class Product
    {
        public string Name { get; set; }
    }

    public class Location
    {
        public string Source { get; set; }
        public string SharePath { get; set; }
        public string Name { get; set; }
    }

    public class Path
    {
        //public string SourceRepo { get; set; }
        //public string BuildRepo { get; set; }
        //public string HelpRepo { get; set; }
        //public string RemoteRepo { get; set; }
        //public string MasterDeployPath { get; set; }
        //public string DeploymentLocation { get; set; }
        //public string ChangeLog { get; set; }
        //public string WorkerReleaseFiles { get; set; }

        public string Name { get; set; }
        public string Location { get; set; }
    }

    public class EmailGroup
    {
        public string EmailGroupName { get; set; }
    }

    public class OracleEmailGroup
    {
        public string OracleEmailGroupName { get; set; }
    }
}