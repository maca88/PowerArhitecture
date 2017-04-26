﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using FluentValidation.Validators;

namespace SharperArchitecture.Validation.Internal
{
    internal class RuleSetValidationRule : IValidationRule
    {
        private static readonly ConcurrentDictionary<string, RuleSetValidationRule> Instances =
            new ConcurrentDictionary<string, RuleSetValidationRule>(); 

        public static IValidationRule GetRule(string ruleSet)
        {
            return Instances.GetOrAdd(ruleSet, o => new RuleSetValidationRule(o));
        }

        static RuleSetValidationRule()
        {
        }

        private RuleSetValidationRule(string ruleSet)
        {
            RuleSet = ruleSet;
        }

        IEnumerable<ValidationFailure> IValidationRule.Validate(ValidationContext context)
        {
            throw new NotSupportedException();
        }

        Task<IEnumerable<ValidationFailure>> IValidationRule.ValidateAsync(ValidationContext context, CancellationToken cancellation)
        {
            throw new NotSupportedException();
        }

        void IValidationRule.ApplyCondition(Func<object, bool> predicate, ApplyConditionTo applyConditionTo)
        {
            throw new NotSupportedException();
        }

        void IValidationRule.ApplyAsyncCondition(Func<object, Task<bool>> predicate, ApplyConditionTo applyConditionTo)
        {
            throw new NotSupportedException();
        }

        IEnumerable<IPropertyValidator> IValidationRule.Validators { get { yield break; } }

        public string RuleSet { get; set; }
    }
}
