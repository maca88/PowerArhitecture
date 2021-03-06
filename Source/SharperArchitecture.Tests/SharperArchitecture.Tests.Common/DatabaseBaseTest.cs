﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Cfg.Db;
using HibernatingRhinos.Profiler.Appender.NHibernate;
using NHibernate;
using NHibernate.Cfg;
using SharperArchitecture.DataAccess;
using SharperArchitecture.DataAccess.Configurations;
using SharperArchitecture.DataAccess.EventListeners;
using SharperArchitecture.DataAccess.Extensions;
using SharperArchitecture.DataAccess.Specifications;
using SharperArchitecture.Domain;
using SimpleInjector;
using SimpleInjector.Extensions;
using SimpleInjector.Extensions.ExecutionContextScoping;

namespace SharperArchitecture.Tests.Common
{
    public abstract class DatabaseBaseTest : BaseTest
    {
        protected List<IFluentDatabaseConfiguration> DatabaseConfigurations = new List<IFluentDatabaseConfiguration>();
        protected List<Assembly> EntityAssemblies = new List<Assembly>
        {
            Assembly.GetExecutingAssembly(),
            Assembly.GetAssembly(typeof (Entity))
        };
        protected List<Assembly> ConventionAssemblies = new List<Assembly>
        {
            Assembly.GetAssembly(typeof (Database)),
            Assembly.GetAssembly(typeof (Entity))
        };
        protected List<ISessionFactory> SessionFactories = new List<ISessionFactory>();
        
        protected virtual IFluentDatabaseConfiguration CreateDatabaseConfiguration(string dbName = "foo", string name = null)
        {
            return FluentDatabaseConfiguration.Create(new Configuration()
                    .SetProperty(NHibernate.Cfg.Environment.GenerateStatistics, "true"), name)
                .AddConventionAssemblies(ConventionAssemblies)
                .AddEntityAssemblies(EntityAssemblies)
                .FluentNHibernate(f => f
                    .Database(MsSqlConfiguration
                        .MsSql2008
                        //.ConnectionString(string.Format("Data Source=(local);Initial Catalog={0};Integrated Security=true", dbName))
                        .ConnectionString(o => o
                            .Database(dbName)
                            .Server("(local)")
                            .TrustedConnection()
                        )
                    )
                )
                .RecreateAtStartup()
                .HbmMappingsPath($".\\Mappings{name}")
                .Conventions(c => c
                    .IdDescending()
                    .UniqueWithMultipleNulls()
                    .HiLoId(h => h
                        .Enabled()
                        .MaxLo(100)
                        .TableName("TestHiLoIdentity")
                    )
                );
        }

        protected virtual IEnumerable<IFluentDatabaseConfiguration> GetDatabaseConfigurations()
        {
            yield return CreateDatabaseConfiguration();
        }

        protected virtual void ConfigureDatabaseConfiguration(DatabaseConfiguration configuration)
        {
        }

        protected override void ConfigureContainer(Container container)
        {
            container.Options.DefaultScopedLifestyle = new ExecutionContextScopeLifestyle();
            foreach (var config in GetDatabaseConfigurations())
            {
                DatabaseConfigurations.Add(config);
                var dbConfig = config.Build();
                ConfigureDatabaseConfiguration(dbConfig);
                container.RegisterDatabaseConfiguration(dbConfig);
            }
        }

        protected override void Configure()
        {
            NHibernateProfiler.Initialize();

            base.Configure();

            var sfProvider = Container.GetInstance<ISessionFactoryProvider>();
            foreach (var config in DatabaseConfigurations)
            {
                var sessionFactory = sfProvider.Get(config.Name);
                SessionFactories.Add(sessionFactory);
                FillData(sessionFactory);
            }
        }

        protected virtual void FillData(ISessionFactory sessionFactory)
        {
            
        }

        protected override void Cleanup()
        {
            /*
            foreach (var sessionFactory in SessionFactories)
            {
                Database.RecreateTables(sessionFactory);
            }*/
            Database.Cleanup();
            SessionFactories.Clear();
            DatabaseConfigurations.Clear();
            base.Cleanup();
        }
    }
}
