﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DSTBuilder.Models
{
    public interface ILogRepository
    {
        Log GetStatus();
    }
}