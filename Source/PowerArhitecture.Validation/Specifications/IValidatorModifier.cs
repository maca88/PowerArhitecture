﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Validators;

namespace PowerArhitecture.Validation.Specifications
{
    public interface IValidatorModifier : IValidator
    {
        void AddPropertyValidator(IPropertyValidator propValidator, PropertyInfo prop, string ruleSet, bool includePropertyName);
    }
}