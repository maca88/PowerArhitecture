﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NHibernate;
using PowerArhitecture.Authentication.Entities;
using PowerArhitecture.CodeList;
using PowerArhitecture.CodeList.Specifications;
using Ninject;
using NUnit.Framework.Internal;
using PowerArhitecture.Authentication;
using PowerArhitecture.Authentication.Specifications;
using PowerArhitecture.DataAccess.Factories;
using PowerArhitecture.Tests.Common;
using PowerArhitecture.Tests.CodeList.Entities;
using NUnit.Framework;
using PowerArhitecture.DataAccess;
using PowerArhitecture.DataAccess.Specifications;
using PowerArhitecture.Domain;
using PowerArhitecture.Validation.Specifications;

namespace PowerArhitecture.Tests.CodeList
{
    [TestFixture]
    public class CodeListTests : DatabaseBaseTest
    {
        private const string SlLanguage = "sl";
        private const string EnLanguage = "en";
        private const string ItLanguage = "it";

        public CodeListTests()
        {
            EntityAssemblies.Add(Assembly.GetAssembly(typeof(ICodeList)));
            EntityAssemblies.Add(Assembly.GetAssembly(typeof(CodeListTests)));
            ConventionAssemblies.Add(Assembly.GetAssembly(typeof(ICodeList)));
            AddMappingStepAssembly(Assembly.GetAssembly(typeof(ICodeList)));
            TestAssemblies.Add(typeof(CodeListTests).Assembly);
            TestAssemblies.Add(typeof(Entity).Assembly);
            TestAssemblies.Add(typeof(Database).Assembly);
            TestAssemblies.Add(typeof(IValidatorEngine).Assembly);
        }

        [Test]
        public void TestLocalizationWithDefaultLanguageFilter()
        {
            using (var unitOfWork = Kernel.Get<IUnitOfWork>().GetUnitOfWorkImplementation())
            {
                unitOfWork.Session.EnableFilter("Language")
                    .SetParameter("Current", "sl")
                    .SetParameter("Fallback", "en");
                var bmw = unitOfWork.Query<Car, string>()
                    .First(o => o.Code == "BMW");
                Assert.AreEqual("BMW Slo", bmw.Name);
            }
        }

        [Test]
        public void TestLocalizationWithCustomLanguageFilter()
        {
            using (var unitOfWork = Kernel.Get<IUnitOfWork>().GetUnitOfWorkImplementation())
            {
                unitOfWork.Session.EnableFilter("Language")
                    .SetParameter("Current", "sl")
                    .SetParameter("Fallback", "en");
                var cl = unitOfWork.Query<CustomLanguageFilter, string>()
                    .First(o => o.Code == "Code1");
                Assert.AreEqual("SL", cl.Name);
                Assert.AreEqual("CustomSL", cl.Custom);
                Assert.AreEqual("Custom2SL", cl.CurrentCustom2);

                // Fallback
                cl = unitOfWork.Query<CustomLanguageFilter, string>()
                    .First(o => o.Code == "Code2");
                Assert.AreEqual("EN", cl.Name);
                Assert.AreEqual("Custom2EN", cl.CurrentCustom2);
            }
        }

        protected override IFluentDatabaseConfiguration CreateDatabaseConfiguration(string dbName = "Test")
        {
            return base.CreateDatabaseConfiguration(dbName)
                .Conventions(c => c
                    .HiLoId(o => o.Enabled(false)
                    )
                );
        }

        protected override void FillData(ISessionFactory sessionFactory)
        {
            var entities = new List<object>();

            FillCars(entities);
            FillCustomLanguageFilters(entities);
            FillSimpleCodeList(entities);

            using (var unitOfWork = Kernel.Get<IUnitOfWork>())
            {
                try
                {
                    unitOfWork.Save(entities.ToArray());
                }
                catch (Exception e)
                {
                    
                }
            }
        }

        #region Car

        private void FillSimpleCodeList(List<object> entities)
        {
            var cl = new SimpleCodeList
            {
                Name = "Code1",
                Code = "Code1"
            };
            entities.Add(cl);

            var cl2 = new SimpleCodeList
            {
                Name = "Code2",
                Code = "Code2"
            };
            entities.Add(cl2);
        }

        #endregion

        #region Car

        private void FillCars(List<object> entities)
        {
            var car1 = new Car { Code = "BMW" };
            car1.AddName(new CarLanguage
            {
                LanguageCode = SlLanguage,
                Name = "BMW Slo"
            });
            car1.AddName(new CarLanguage
            {
                LanguageCode = EnLanguage,
                Name = "BMW Eng"
            });
            entities.Add(car1);

            var car2 = new Car { Code = "AUDI" };
            car2.AddName(new CarLanguage
            {
                LanguageCode = ItLanguage,
                Name = "Audi Ita"
            });
            entities.Add(car2);

            var car2LocSl = new CarLanguage
            {
                LanguageCode = SlLanguage,
                Name = "Audi Slo"
            };
            car2LocSl.SetCodeList(car2);
        }

        #endregion

        #region CustomLanguageFilter

        private void FillCustomLanguageFilters(List<object> entities)
        {
            var cl1 = new CustomLanguageFilter { Code = "Code1" };
            cl1.AddName(new CustomLanguageFilterLanguage
            {
                LanguageCode = SlLanguage,
                Name = "SL",
                Custom = "CustomSL",
                Custom2 = "Custom2SL"
            });
            cl1.AddName(new CustomLanguageFilterLanguage
            {
                LanguageCode = ItLanguage,
                Name = "IT",
                Custom = "CustomIT",
                Custom2 = "Custom2IT"
            });
            entities.Add(cl1);

            var cl2 = new CustomLanguageFilter { Code = "Code2" };
            cl2.AddName(new CustomLanguageFilterLanguage
            {
                LanguageCode = ItLanguage,
                Name = "IT",
                Custom = "CustomIT",
                Custom2 = "Custom2IT"
            });
            cl2.AddName(new CustomLanguageFilterLanguage
            {
                LanguageCode = EnLanguage,
                Name = "EN",
                Custom = "CustomEN",
                Custom2 = "Custom2EN"
            });
            entities.Add(cl2);
        }

        #endregion

    }
}
