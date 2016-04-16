﻿using System;
using System.Threading;
using System.Web;
using Microsoft.AspNet.Identity;
using NHibernate.Extensions;
using PowerArhitecture.Authentication.Specifications;
using PowerArhitecture.Common.Attributes;
using PowerArhitecture.Common.Events;
using PowerArhitecture.DataAccess.Events;
using PowerArhitecture.Domain;
using IUser = PowerArhitecture.Authentication.Specifications.IUser;

namespace PowerArhitecture.Authentication.EventListeners
{
    [Priority(1000)]
    public class PopulateDbListener : IListener<PopulateDbEvent>
    {
        private readonly IAuthenticationConfiguration _authSettings;
        private readonly IPasswordHasher _passwordHasher;
        private readonly Type _userType;

        public PopulateDbListener(IAuthenticationConfiguration authSettings, IPasswordHasher passwordHasher)
        {
            _authSettings = authSettings;
            _passwordHasher = passwordHasher;
            _userType = Type.GetType(authSettings.UserClass, true);
        }

        public void Handle(PopulateDbEvent e)
        {
            var unitOfWork = e.Message;
            var unitOfWorkImpl = unitOfWork.GetUnitOfWorkImplementation();

            var systemUser = (IEntity)Activator.CreateInstance(_userType);

            _userType.GetProperty("TimeZoneId").SetValue(systemUser, TimeZoneInfo.Utc.Id);
            _userType.GetProperty("UserName").SetValue(systemUser, _authSettings.SystemUserName);
            _userType.GetProperty("PasswordHash").SetValue(systemUser, _passwordHasher.HashPassword(_authSettings.SystemUserPassword));

            unitOfWork.Save(systemUser);

            var copySystemUser = (IUser)unitOfWorkImpl.Session.DeepClone(systemUser);
            Thread.CurrentPrincipal = copySystemUser;
            if (HttpContext.Current != null)
                HttpContext.Current.User = copySystemUser;


            //session.Flush();
            //PrincipalHelper.SetSystemUser(session.DeepCopy(systemUser));
            //PrincipalHelper.SetCurrentUser(PrincipalHelper.GetSystemUser());

        }
    }
}
