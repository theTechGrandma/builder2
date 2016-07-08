using DSTBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace DSTBuilder.Controllers
{
    public class BuildController : ApiController
    {
        static readonly BuildRepository buildRepository = new BuildRepository();

        // POST api/build
        [Route("api/build")]
        [HttpPost]
        public void StartBuild(string product, string release, string version, bool sendNotification, int minutes)
        {
            buildRepository.StartBuild(product, release, version, sendNotification, minutes);
        }

        [Route("api/buildOracle")]
        [HttpPost]
        public void GenerateMasterDeploy(string product, string release, string fromVersion, string toVersion)
        {
            buildRepository.GenerateMasterDeploy(product, release, fromVersion, toVersion, true);
        }

        [Route("api/pushCode")]
        [HttpPost]
        public void PushLogsaCode(string product, string release, string version)
        {
            buildRepository.PushCodeToLogsa(product, release, version);
        }

        [Route("api/buildStatus")]
        public Log GetBuildStatus(string product)
        {
            return buildRepository.GetStatus();
        }
        
    }
}
