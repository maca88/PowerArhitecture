﻿using System.Text.RegularExpressions;
using FluentValidation;
using SharperArchitecture.Common.Internationalization;
using SharperArchitecture.Validation;
using FluentValidation.Results;
using SharperArchitecture.Authentication.Specifications;

namespace SharperArchitecture.Authentication.Validators
{
    public class PermissionPatternValidator: Validator<IPermissionPattern>
    {
        public PermissionPatternValidator()
        {
            RuleSet(ValidationRuleSet.InsertUpdate, () =>
            {
                RuleFor(o => o).Custom((pattern, context) => AssertValidRegex(pattern));
            });
        }

        private ValidationFailure AssertValidRegex(IPermissionPattern permission)
        {
            try
            {
                new Regex(permission.Pattern);
                return null;
            }
            catch
            {
                return Failure(o => o.Pattern, I18N.Translate("'{0}' is not in the correct format.", I18N.Translate("Pattern")));
            }
        }
    }
}
