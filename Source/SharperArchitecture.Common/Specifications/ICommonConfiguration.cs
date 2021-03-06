﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperArchitecture.Common.Specifications
{
    public interface ICommonConfiguration
    {
        string DefaultCulture { get; }
        string TranslationsByCulturePattern { get; }
        string TranslationsPath { get; }
    }
}
