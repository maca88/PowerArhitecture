﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentValidation.Validators;
using SharperArchitecture.Common.Events;
using SharperArchitecture.Common.Exceptions;
using SharperArchitecture.Common.Specifications;
using SharperArchitecture.Domain.Specifications;
using SharperArchitecture.Validation;
using SharperArchitecture.Validation.Attributes;
using SharperArchitecture.Validation.Events;
using SharperArchitecture.Validation.Specifications;

namespace SharperArchitecture.DataAccess.EventListeners
{
    public class ValidatorCreatedEventHandler : IEventHandler<ValidatorCreatedEvent>
    {
        public void Handle(ValidatorCreatedEvent e)
        {
            if (e.ModelType == null || !typeof(IVersionedEntity).IsAssignableFrom(e.ModelType))
            {
                return;
            }
            var config = Database.GetDatabaseConfigurationsForModel(e.ModelType).FirstOrDefault(); // TODO: handle multi db
            if (config == null)
            {
                throw new SharperArchitectureException($"There is no database configuration that contains the entity of type {e.ModelType}");
            }
            if (!config.Conventions.RequiredLastModifiedProperty)
            {
                return;
            }
            var extendedValidator = e.Validator as IValidatorModifier;
            if (extendedValidator == null)
            {
                throw new SharperArchitectureException($"Entity validator for model {e.ModelType} must inherit from a Validator<>");
            }

            var ignoreAttrsAttr = e.ModelType.GetCustomAttributes(typeof(IgnoreValidationAttributesAttribute), true)
                .FirstOrDefault() as IgnoreValidationAttributesAttribute;
            var ignoreProps = ignoreAttrsAttr != null
                ? new HashSet<string>(ignoreAttrsAttr.Properties ?? new string[0])
                : new HashSet<string>();

            PropertyInfo propInfo;
            if (!ignoreProps.Contains("LastModifiedDate"))
            {
                propInfo = e.ModelType.GetProperty("LastModifiedDate");
                extendedValidator.AddPropertyValidator(new NotNullValidator(), propInfo, ValidationRuleSet.Attribute, false);
            }

            if (!e.ModelType.IsAssignableToGenericType(typeof(IVersionedEntityWithUser<>)) || ignoreProps.Contains("LastModifiedBy"))
            {
                return;
            }

            propInfo = e.ModelType.GetProperty("LastModifiedBy");
            extendedValidator.AddPropertyValidator(new NotNullValidator(), propInfo, ValidationRuleSet.Attribute, false);
        }
    }
}
