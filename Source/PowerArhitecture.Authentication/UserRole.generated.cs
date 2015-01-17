using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.CodeDom.Compiler;
using System.Linq.Expressions;
using System.Reflection;
using FluentNHibernate.Automapping;

namespace PowerArhitecture.Authentication.Entities
{
	[GeneratedCode("T4Template", "1.0")]
	public partial class UserRole
	{

		#region User

        [ReadOnly(true)]
        public virtual long? UserId { get; set; }

		#endregion

		#region Role

        [ReadOnly(true)]
        public virtual long? RoleId { get; set; }

        public virtual void SetRole(Role role)
        {
            this.SetManyToOne(o => o.Role, role, o => o.RemoveUserRole, o => o.UserRoles);
        }

        public virtual void UnsetRole()
        {
            this.UnsetManyToOne(o => o.Role, o => o.UserRoles);
        }

		#endregion

	}
}
