using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DSTBuilder.Models;

namespace DSTBuilder.Controllers
{
    public class VersionController : ApiController
    {
        static readonly IVersionsRepository versionRepository = new VersionsRepository();

        [ActionName("DefaultAction")] 
        public IEnumerable<Versions> GetVersion(string product)
        {            
            return versionRepository.GetVersions(product);
        }       
    }
}
