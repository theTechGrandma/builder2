using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DSTBuilder.Models
{
    public class Log
    {
        private string _message;
        public string Message 
        { 
            get 
            {
                return _message;
            } 
            set 
            {
                this._message = value; 
            } 
        } 
    }
}