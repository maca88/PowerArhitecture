﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject;
using Ninject.Extensions.NamedScope;
using Ninject.Parameters;
using Ninject.Syntax;
using PowerArhitecture.DataAccess.Configurations;
using PowerArhitecture.DataAccess.Parameters;
using PowerArhitecture.DataAccess.Specifications;

namespace PowerArhitecture.DataAccess.Factories
{
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        readonly IResolutionRoot _resolutionRoot;

        public UnitOfWorkFactory(IResolutionRoot resolutionRoot)
        {
            _resolutionRoot = resolutionRoot;
        }

        public IUnitOfWork GetNew(IsolationLevel isolationLevel = IsolationLevel.Unspecified, string dbConfigName = null)
        {
            dbConfigName = dbConfigName ?? DatabaseConfiguration.DefaultName;
            return _resolutionRoot.Get<IUnitOfWork>(
                new ConstructorArgument("isolationLevel", isolationLevel),
                new DatabaseConfigurationParameter(dbConfigName));
        }
    }
}
