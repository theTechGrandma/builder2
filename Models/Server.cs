using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DSTBuilder.Models
{
    public class Server
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public string ServerURL { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string SourceLocation { get; set; }
        public string DestLocation { get; set; }
        public string Port { get; set; }
    }
}