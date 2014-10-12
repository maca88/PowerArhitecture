﻿using System;
using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.Instances;

namespace PowerArhitecture.DataAccess.Conventions
{
    public class NotNullConvention : IPropertyConvention
    {
        public void Apply(IPropertyInstance instance)
        {
            if (!IsNullable(instance.Property.PropertyType)) //true for struct
                instance.Not.Nullable();
        }

        static bool IsNullable(System.Type type)
        {
            if (!type.IsValueType) return true; // ref-type
            return Nullable.GetUnderlyingType(type) != null;
        }
    } 
}
