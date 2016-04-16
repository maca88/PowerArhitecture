﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArhitecture.Domain;

namespace PowerArhitecture.Tests.DataAccess.Entities.Versioning
{
    public class VersionWheel : VersionedEntity
    {
        public virtual int Dimension { get; set; }

        public virtual VersionCar VersionCar { get; set; }
    }
}
