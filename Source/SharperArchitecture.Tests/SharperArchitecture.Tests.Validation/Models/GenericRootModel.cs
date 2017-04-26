﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using FluentValidation;
using FluentValidation.Results;
using SharperArchitecture.Validation;

namespace SharperArchitecture.Tests.Validation.Models
{
    public class GenericRootModel
    {
        public string Name { get; set; }

        public List<GenericRootModel> Children { get; set; } = new List<GenericRootModel>();

        public GenericRootModel Relation { get; set; }
    }


    public class GenericRootModelValidator : Validator<GenericRootModel>
    {
        public GenericRootModelValidator()
        {
            RuleFor(o => o.Children).SetCollectionValidator(this);
            RuleFor(o => o.Relation).SetValidator(this);
        }
    }

    public class GenericRootBusinessRule<TRoot> : AbstractBusinessRule<TRoot>
        where TRoot : GenericRootModel
    {
        public override ValidationFailure Validate(TRoot child, ValidationContext context)
        {
            return string.IsNullOrEmpty(child.Name) ? Failure("Name should not be empty", context) : Success;
        }

        public override bool CanValidate(TRoot child, ValidationContext context)
        {
            return true;
        }

        public override string[] RuleSets => new string[] { };
    }

}
