using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DSTBuilder.Models;

namespace DSTBuilder.Controllers
{
    public class LogController : ApiController
    {
        static readonly ILogRepository logRepository = new LogRepository();

        [ActionName("DefaultAction")]
        public Log GetBuildStatus(string product)
        {
            return logRepository.GetStatus();
        }
    }
}
