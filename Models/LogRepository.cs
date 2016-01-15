using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DSTBuilder.Models
{
    public class LogRepository : ILogRepository
    {
        private readonly Log _log;
        //public LogRepository()
        //{
        //    _log.Message = "Waiting";
        //}

        public Log GetStatus()
        {           
            return _log;
        }
    }
}