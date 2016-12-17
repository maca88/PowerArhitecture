﻿using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using NHibernate;
using NUnit.Framework;
using PowerArhitecture.DataAccess;
using PowerArhitecture.DataAccess.Specifications;
using PowerArhitecture.Domain;
using PowerArhitecture.Tests.Common;
using PowerArhitecture.Tests.DataAccess.Entities;
using PowerArhitecture.Tests.DataAccess.Extensions;
using PowerArhitecture.Tests.DataAccess.MultiDatabase;
using PowerArhitecture.Tests.DataAccess.MultiDatabase.BazModels;
using PowerArhitecture.Validation;
using SimpleInjector;
using SimpleInjector.Extensions.ExecutionContextScoping;

namespace PowerArhitecture.Tests.DataAccess
{
    [TestFixture]
    public class MultiDatabasesTests : DatabaseBaseTest
    {
        public MultiDatabasesTests()
        {
            EntityAssemblies.Add(typeof(LifecycleTests).Assembly);
            TestAssemblies.Add(typeof(Entity).Assembly);
            TestAssemblies.Add(typeof(Database).Assembly);
            TestAssemblies.Add(typeof(ValidationRuleSet).Assembly);
            TestAssemblies.Add(typeof(MultiDatabasesTests).Assembly);
        }

        protected override IEnumerable<IFluentDatabaseConfiguration> GetDatabaseConfigurations()
        {
            yield return CreateDatabaseConfiguration()
                .AutomappingConfiguration(o => o.ShouldMapType(t => !t.Namespace.Contains("BazModel")));
            yield return CreateDatabaseConfiguration("bar", "bar")
                .RegisterRepository(typeof(IBarRepository<,>), typeof(BarRepository<,>))
                .AutomappingConfiguration(o => o.ShouldMapType(t => !t.Namespace.Contains("BazModel")));
            yield return CreateDatabaseConfiguration("baz", "baz")
                .RegisterRepository(typeof(IBazRepository<,>), typeof(BazRepository<,>))
                .RegisterRepository(typeof(IBazRepository<>), typeof(BazRepository<>))
                .AutomappingConfiguration(o => o.ShouldMapType(t => t.Namespace.Contains("BazModel")));
        }

        [Test]
        public void EachDatabaseMustHaveItsOwnSessionFactory()
        {
            var sfProvider = Container.GetInstance<ISessionFactoryProvider>();
            var fooSf = sfProvider.Get();
            var barSf = sfProvider.Get("bar");

            Assert.NotNull(fooSf);
            Assert.NotNull(barSf);
            Assert.AreNotEqual(fooSf, barSf);
        }

        [Test]
        public void BazModelShouldHaveOnlyOneDatabaseConfiguration()
        {
            var configs = Database.GetDatabaseConfigurationsForModel<BazModel>();
            Assert.AreEqual(1, configs.Count);
            Assert.AreEqual("baz", configs.First().Name);
        }

        [Test]
        public void BazModelRepositoryShouldHaveCorrectSession()
        {
            using (Container.BeginExecutionContextScope())
            {
                var bazRepo = Container.GetInstance<IRepository<BazModel>>();
                var bazRepo2 = Container.GetInstance<IBazRepository<BazModel>>();
                var sf = bazRepo.GetSession().SessionFactory;

                Assert.AreEqual(sf, bazRepo2.GetSession().SessionFactory);
                Assert.AreEqual(bazRepo.GetSession(), bazRepo2.GetSession());
                Assert.IsTrue(bazRepo.GetSession().Connection.ConnectionString.Contains("baz"));
            }
        }

        [Test]
        public void EachDatabaseMustHaveItsOwnSession()
        {
            var sProvider = Container.GetInstance<ISessionProvider>();
            var sfProvider = Container.GetInstance<ISessionFactoryProvider>();
            var sf = sfProvider.Get();
            var sfBar = sfProvider.Get("bar");

            sf.Statistics.Clear();
            sfBar.Statistics.Clear();

            using (Container.BeginExecutionContextScope())
            {
                var session = sProvider.Get();
                var barSession = sProvider.Get("bar");

                Assert.AreEqual(0, sf.Statistics.SessionOpenCount);
                Assert.AreEqual(0, sfBar.Statistics.SessionOpenCount);
                Assert.NotNull(session);
                Assert.NotNull(barSession);
                Assert.AreNotEqual(barSession, session);
                Assert.AreEqual(sf, session.SessionFactory);
                Assert.AreEqual(sfBar, barSession.SessionFactory);
                Assert.AreEqual(1, sf.Statistics.SessionOpenCount);
                Assert.AreEqual(1, sfBar.Statistics.SessionOpenCount);
                Assert.AreNotEqual(session.SessionFactory, barSession.SessionFactory);
                Assert.AreEqual(0, sf.Statistics.SessionCloseCount);
                Assert.AreEqual(0, sfBar.Statistics.SessionCloseCount);
            }
            Assert.AreEqual(1, sf.Statistics.SessionCloseCount);
            Assert.AreEqual(1, sfBar.Statistics.SessionCloseCount);
        }

        [Test]
        public void EachDatabaseMustHaveItsOwnGenericRepository()
        {
            using (Container.BeginExecutionContextScope())
            {
                var sf = Container.GetInstance<ISessionFactory>();
                sf.Statistics.Clear();

                var multiSf = Container.GetInstance<MultiGenericRepositories>();
                var defSession = multiSf.Repository.GetMemberValue("Session") as ISession;
                var barSession = multiSf.BarRepository.GetMemberValue("Session") as ISession;

                Assert.NotNull(defSession);
                Assert.NotNull(barSession);
                Assert.AreEqual(0, sf.Statistics.SessionOpenCount);
                Assert.AreEqual(sf, defSession.SessionFactory);
                Assert.AreEqual(1, sf.Statistics.SessionOpenCount);
                Assert.AreNotEqual(defSession, barSession);
                Assert.AreNotEqual(defSession.SessionFactory, barSession.SessionFactory);
            }
        }

        [Test]
        public void UnitOfWorkShouldFallbackToDefaultDatabaseWhenRetrievingEntityThatIsContainedInMultipleDatabases()
        {
            using (var unitOfWork = Container.GetInstance<IUnitOfWork>())
            {
                unitOfWork.GetRepository<AttrLazyLoad>();
            }
        }

        [Test]
        public void UnitOfWorkShouldThrowWhenAnInvalidDatabaseConfigurationNameIsProvided()
        {
            Assert.Throws<ActivationException>(() =>
            {
                using (var unitOfWork = Container.GetInstance<IUnitOfWork>())
                {
                    unitOfWork.GetCustomRepository<IBazRepository<AttrLazyLoad>>();
                }
            });
        }

        [Test]
        public void UnitOfWorkShouldWorkWithMultipleDatabasesAndCustomRepositories()
        {
            using (var unitOfWork = Container.GetInstance<IUnitOfWork>().GetUnitOfWorkImplementation())
            {
                try
                {
                    Assert.AreEqual(0, unitOfWork.GetActiveSessions().Count());

                    var repo = unitOfWork.GetRepository<AttrLazyLoad>();
                    var repo2 = unitOfWork.GetCustomRepository<IBarRepository<AttrLazyLoad, long>>();
                    var repo3 = unitOfWork.GetCustomRepository<IBarAttrLazyLoadRepository>();

                    Assert.AreNotEqual(repo, repo2);
                    Assert.AreNotEqual(repo.GetSession(), repo2.GetSession());
                    Assert.AreEqual(repo2.GetSession(), repo3.GetSession());
                    Assert.AreEqual(2, unitOfWork.GetActiveSessions().Count());
                    Assert.IsTrue(unitOfWork.GetActiveSessions().All(o => o.Transaction.IsActive));

                    var model1 = new AttrLazyLoad {Name = "Test"};
                    var model2 = new AttrLazyLoad { Name = "Test2" };
                    repo.Save(model1);
                    repo3.Save(model2);

                    Assert.AreNotEqual(0, model1.Id);
                    Assert.AreEqual(model1.Id, model2.Id);
                    unitOfWork.Commit();
                }
                catch
                {
                    unitOfWork.Rollback();
                    throw;
                }
            }
        }

        [Test]
        public void UnitOfWorkShouldWorkWithMultipleDatabasesAndGenericRepositories()
        {
            using (var unitOfWork = Container.GetInstance<IUnitOfWork>().GetUnitOfWorkImplementation())
            {
                try
                {
                    Assert.AreEqual(0, unitOfWork.GetActiveSessions().Count());
                    Assert.IsTrue(unitOfWork.GetActiveSessions().All(o => o.Transaction.IsActive));

                    var repo = unitOfWork.GetRepository<AttrIndexAttribute>();
                    var repo2 = unitOfWork.GetCustomRepository<IBarRepository<AttrIndexAttribute, long>>();
                    var repo3 = unitOfWork.GetCustomRepository<IBarRepository<AttrIndexAttribute, long>>();

                    Assert.AreEqual(2, unitOfWork.GetActiveSessions().Count());
                    Assert.AreNotEqual(repo.GetSession(), repo2.GetSession());
                    Assert.AreEqual(repo2.GetSession(), repo3.GetSession());
                    Assert.IsTrue(unitOfWork.GetActiveSessions().All(o => o.Transaction.IsActive));

                    var model1 = new AttrIndexAttribute { Index1 = "Test", SharedIndex1 = DateTime.Now };
                    var model2 = new AttrIndexAttribute { Index1 = "Test2", SharedIndex1 = DateTime.Now };
                    repo.Save(model1);
                    repo3.Save(model2);

                    Assert.AreNotEqual(0, model1.Id);
                    Assert.AreEqual(model1.Id, model2.Id);

                    unitOfWork.Commit();
                }
                catch
                {
                    unitOfWork.Rollback();
                    throw;
                }
            }
        }

        [Test]
        public void UnitOfWorkShouldRevertAllDataIfAnySessionFails()
        {
            Assert.Throws<SqlTypeException>(() =>
            {
                using (var unitOfWork = Container.GetInstance<IUnitOfWork>().GetUnitOfWorkImplementation())
                {
                    try
                    {
                        var repo = unitOfWork.GetRepository<AttrIndexAttribute>();
                        var repo2 = unitOfWork.GetCustomRepository<IBarRepository<AttrIndexAttribute, long>>();

                        var model1 = new AttrIndexAttribute { Index1 = "Test", SharedIndex1 = DateTime.Now };
                        var model2 = new AttrIndexAttribute { Index1 = "Test2" };
                        repo.Save(model1);
                        repo2.Save(model2);

                        unitOfWork.Commit();
                    }
                    catch
                    {
                        unitOfWork.Rollback();
                        throw;
                    }
                }

            });

            using (var unitOfWork = Container.GetInstance<IUnitOfWork>().GetUnitOfWorkImplementation())
            {
                try
                {
                    var repo = unitOfWork.GetRepository<AttrIndexAttribute>();
                    var repo2 = unitOfWork.GetCustomRepository<IBarRepository<AttrIndexAttribute, long>>();

                    Assert.AreEqual(0, repo.Query().Count());
                    Assert.AreEqual(0, repo2.Query().Count());
                    unitOfWork.Commit();
                }
                catch
                {
                    unitOfWork.Rollback();
                    throw;
                }
            }

        }
    }
}