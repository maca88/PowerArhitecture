﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperArchitecture.Common.Reporting
{
    public class ReportParameters<T> : Parameters<T>
    {
        public string ReportType { get; set; }
    }
}
