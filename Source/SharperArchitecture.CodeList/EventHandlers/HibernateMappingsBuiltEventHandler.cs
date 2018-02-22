﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentNHibernate;
using FluentNHibernate.MappingModel;
using FluentNHibernate.MappingModel.ClassBased;
using FluentNHibernate.MappingModel.Collections;
using FluentNHibernate.MappingModel.Identity;
using NHibernate.Cfg;
using NHibernate.Dialect;
using SharperArchitecture.CodeList.Attributes;
using SharperArchitecture.CodeList.Specifications;
using SharperArchitecture.Common.Events;
using SharperArchitecture.Common.Exceptions;
using SharperArchitecture.Common.Specifications;
using SharperArchitecture.DataAccess.Events;

namespace SharperArchitecture.CodeList.EventHandlers
{
    public class HibernateMappingsBuiltEventHandler : IEventHandler<HibernateMappingsBuiltEvent>
    {
        // TODO: configurable...COALESCE works everywhere but is slower than ISNULL which is available only for sql server
        private static readonly string _localizeFormula =
            "(SELECT COALESCE((SELECT curr.{0} FROM {1} curr WHERE curr.{5} = {7} AND curr.{6} = :{3}.{4})," +
            "(SELECT def.{0} FROM {1} def WHERE def.{5} = {7} AND def.{6} = :{3}.{2})))";

        public INamingStrategy NamingStrategy { get; private set; }

        public Dialect Dialect { get; private set; }

        public void Apply(ClassMappingBase classMapBase, Lazy<Dictionary<Type, ClassMapping>> lazyTypeMap)
        {
            var type = classMapBase.Type;

            foreach (var reference in classMapBase.References.Where(o => CanManipulateIdentifier(o.Member.PropertyType)))
            {
                var refColumn = Apply(reference, lazyTypeMap);
                var synteticColumn = classMapBase.Properties.SelectMany(o => o.Columns).FirstOrDefault(o => o.Name == refColumn.Name);
                if (synteticColumn != null)
                {
                    synteticColumn.Set(o => o.Length, Layer.UserSupplied, refColumn.Length);
                    synteticColumn.Set(o => o.NotNull, Layer.UserSupplied, refColumn.NotNull);
                }
            }

            foreach (var collection in classMapBase.Collections.Where(o => CanManipulateIdentifier(o.ContainingEntityType)))
            {
                Apply(collection, lazyTypeMap);
            }

            foreach (var subClass in classMapBase.Subclasses)
            {
                Apply(subClass, lazyTypeMap);
            }

            foreach (var component in classMapBase.Components)
            {
                Apply(component, lazyTypeMap);
            }

            if (!typeof(ICodeList).IsAssignableFrom(type) && !typeof(ILocalizableCodeListLanguage).IsAssignableFrom(type))
            {
                return;
            }

            var codeListAttr = type.GetCustomAttribute<CodeListConfigurationAttribute>(false) ?? new CodeListConfigurationAttribute();
            
            var classMap = classMapBase as ClassMapping;
            if (classMap == null)
            {
                return;
            }

            //Add Table prefix
            if (codeListAttr.CodeListPrefix)
            {
                classMap.Set(o => o.TableName, Layer.UserSupplied, GetTableName(classMap, codeListAttr));
            }

            if (typeof(ILocalizableCodeListLanguage).IsAssignableFrom(type))
            {
                return; // for localization table we set only the table name
            }

            //Set View
            //if (!string.IsNullOrEmpty(codeListAttr.ViewName))
            //{
            //    classMap.Set(o => o.Mutable, Layer.UserSupplied, false);
            //    classMap.Set(o => o.TableName, Layer.UserSupplied, codeListAttr.ViewName);
            //}

            //Change Id to Code
            var id = classMap.Id as IdMapping;
            if (id != null && codeListAttr.ManipulateIdentifier)
            {
                var idCol = id.Columns.First();
                idCol.Set(o => o.Name, Layer.UserSupplied, "Code");
                idCol.Set(o => o.Length, Layer.UserSupplied, codeListAttr.CodeLength);
                //We need to update the formula for the Code property
                var codeProp = classMap.Properties.First(o => o.Name == "Code");
                codeProp.Set(o => o.Formula, Layer.UserSupplied, $"({GetColumnName("Code")})");
            }

            foreach (var propMap in classMap.Properties)
            {
                var attr = propMap.Member.MemberInfo.GetCustomAttribute<FilterCurrentLanguageAttribute>(false);
                if (attr != null)
                {
                    var names =
                        classMap.Collections.FirstOrDefault(
                            o => typeof(ILocalizableCodeListLanguage).IsAssignableFrom(o.ChildType));
                    if (names == null)
                    {
                        throw new SharperArchitectureException("FilterCurrentLanguage must be applied on a type that implements ICodeListLoc<,>");
                    }
                    if (!lazyTypeMap.Value.ContainsKey(names.ChildType))
                    {
                        throw new SharperArchitectureException($"Mapping for codelist {names.ChildType} was not found, failed to apply the formula for the FilterCurrentLanguage attribute");
                    }
                    // Here the table name is already altered
                    var childMapping = lazyTypeMap.Value[names.ChildType];
                    var childTableName = childMapping.TableName;
                    propMap.Set(o => o.Formula, Layer.UserSupplied,
                        string.Format(_localizeFormula, 
                            attr.ColumnName ?? ConvertQuotes(GetColumnName(propMap.Name)), 
                            childTableName,
                            attr.FallbackLanguageParameterName, 
                            attr.FilterName, 
                            attr.CurrentLanguageParameterName, 
                            GetColumnName(codeListAttr.ManipulateIdentifier || id == null ? "CodeListCode" : $"CodeList{id.Name}"),
                            ConvertQuotes(GetColumnName("LanguageCode")),
                            ConvertQuotes(GetColumnName(codeListAttr.ManipulateIdentifier || id == null ? "Code" : id.Name))));
                }
            }

            if (!codeListAttr.NameLength.HasValue)
            {
                return;
            }
            var nameProp = classMap.Properties.First(o => o.Name == "Name");
            var nameCol = nameProp.Columns.First();
            nameCol.Set(o => o.Length, Layer.UserSupplied, codeListAttr.NameLength.Value);
        }

        public void Apply(IComponentMapping componentMap, Lazy<Dictionary<Type, ClassMapping>> lazyTypeMap)
        {
            foreach (var reference in componentMap.References.Where(o => CanManipulateIdentifier(o.Member.PropertyType)))
            {
                var refColumn = Apply(reference, lazyTypeMap);
                var synteticColumn = componentMap.Properties.SelectMany(o => o.Columns).FirstOrDefault(o => o.Name == refColumn.Name);
                if (synteticColumn == null) continue;
                synteticColumn.Set(o => o.Length, Layer.UserSupplied, refColumn.Length);
                synteticColumn.Set(o => o.NotNull, Layer.UserSupplied, refColumn.NotNull);
            }

            foreach (var collection in componentMap.Collections.Where(o => CanManipulateIdentifier(o.ContainingEntityType)))
            {
                Apply(collection, lazyTypeMap);
            }

            foreach (var component in componentMap.Components)
            {
                Apply(component, lazyTypeMap);
            }
        }

        public ColumnMapping Apply(CollectionMapping colectionMap, Lazy<Dictionary<Type, ClassMapping>> lazyTypeMap)
        {
            var keyName = GetKeyName(null, colectionMap.ContainingEntityType);
            if (typeof(ILocalizableCodeListLanguage).IsAssignableFrom(colectionMap.ChildType))
            {
                keyName = "CodeListCode";
            }
            var codeListAttr = colectionMap.ContainingEntityType.GetCustomAttribute<CodeListConfigurationAttribute>(false);
            var length = codeListAttr?.CodeLength ?? 20;
            var col = colectionMap.Key.Columns.First();
            col.Set(o => o.Name, Layer.UserSupplied, keyName);
            col.Set(o => o.Length, Layer.UserSupplied, length);
            return col;
        }

        public ColumnMapping Apply(ManyToOneMapping manyToOneMap, Lazy<Dictionary<Type, ClassMapping>> lazyTypeMap)
        {
            var codeListAttr = manyToOneMap.Member.PropertyType.GetCustomAttribute<CodeListConfigurationAttribute>(false);
            var length = codeListAttr?.CodeLength ?? 20;
            var keyName = GetKeyName(manyToOneMap.Member, manyToOneMap.Class.GetUnderlyingSystemType());
            var col = manyToOneMap.Columns.First();
            col.Set(o => o.Name, Layer.UserSupplied, keyName);
            col.Set(o => o.Length, Layer.UserSupplied, length);
            return col;
        }

        private string GetColumnName(string name)
        {
            return NamingStrategy.ColumnName(name);
        }

        private string ConvertQuotes(string name)
        {
            if (name.StartsWith("`") || name.EndsWith("`"))
            {
                return $"{Dialect.OpenQuote}{name.Trim('`')}{Dialect.CloseQuote}"; 
            }
            return name;
        }

        protected string GetKeyName(Member property, Type type)
        {
            return (property != null ? property.Name : type.Name) + "Code";
        }

        public void Handle(HibernateMappingsBuiltEvent e)
        {
            NamingStrategy = e.Configuration.NamingStrategy;
            Dialect = Dialect.GetDialect(e.Configuration.Properties);
            var lazyTypeMap = new Lazy<Dictionary<Type, ClassMapping>>(() =>
            {
                return e.Mappings.SelectMany(o => o.Classes).ToDictionary(o => o.Type);
            });
            foreach (var classMap in e.Mappings.SelectMany(o => o.Classes))
            {
                Apply(classMap, lazyTypeMap);
            }
        }


        private bool CanManipulateIdentifier(Type type)
        {
            if (!typeof (ICodeList).IsAssignableFrom(type)) return false;
            var attr = type.GetCustomAttribute<CodeListConfigurationAttribute>(false);
            return attr == null || attr.ManipulateIdentifier;
        }

        private string GetTableName(ClassMapping classMap, CodeListConfigurationAttribute attr = null)
        {
            attr = attr ?? classMap.Type.GetCustomAttribute<CodeListConfigurationAttribute>(false) ?? new CodeListConfigurationAttribute();
            var tableName = classMap.TableName.Trim('`').TrimStart(Dialect.OpenQuote).TrimEnd(Dialect.CloseQuote);
            if (!attr.CodeListPrefix)
            {
                return ConvertQuotes(NamingStrategy.TableName(tableName));
            }
            if (tableName.EndsWith("CodeList"))
            {
                tableName = tableName.Substring(0, tableName.IndexOf("CodeList", StringComparison.InvariantCulture));
                tableName = "CodeList" + tableName;
            }
            else if (!tableName.StartsWith("CodeList"))
            {
                tableName = "CodeList" + tableName;
            }
            return ConvertQuotes(NamingStrategy.TableName(tableName));
        }
    }
}

