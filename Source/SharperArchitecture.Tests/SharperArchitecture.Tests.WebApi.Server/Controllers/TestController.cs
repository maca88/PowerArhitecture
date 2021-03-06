﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using NHibernate;
using NHibernate.Linq;
using SharperArchitecture.Tests.WebApi.Server.Entities;
using SimpleInjector;

namespace SharperArchitecture.Tests.WebApi.Server.Controllers
{
    public class TestController : ApiController
    {
        private readonly ISession _session;

        public TestController(ISession session, Container container)
        {
            _session = session;
        }

        public int GetQueryCount()
        {
            return _session.Query<TestEntity>().Count();
        }

        public int GetBrokenQueryCount()
        {
            var result = _session.Query<TestEntity>().Count();
            throw new InvalidOperationException("Fake exception");
        }
    }
}