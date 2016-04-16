﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using PowerArhitecture.Common.Events;
using PowerArhitecture.Common.Helpers;
using PowerArhitecture.Common.Specifications;
using PowerArhitecture.DataAccess.Attributes;
using PowerArhitecture.DataAccess.Enums;
using PowerArhitecture.DataAccess.Events;
using PowerArhitecture.DataAccess.Specifications;
using PowerArhitecture.Domain;
using PowerArhitecture.Domain.Specifications;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Event.Default;
using NHibernate.Intercept;
using NHibernate.Persister.Entity;

namespace PowerArhitecture.DataAccess.NHEventListeners
{
    [NhEventListener(ReplaceListener = typeof(DefaultSaveOrUpdateEventListener))]
    public class NhSaveOrUpdateEventListener : DefaultSaveOrUpdateEventListener
    {
        private readonly IAuditUserProvider _auditUserProvider;
        protected readonly IEventAggregator EventAggregator;

        public NhSaveOrUpdateEventListener(IAuditUserProvider auditUserProvider, IEventAggregator eventAggregator)
        {
            _auditUserProvider = auditUserProvider;
            EventAggregator = eventAggregator;
        }

        /// <summary>
        /// If a new versioned entity will be inserted we must set all audit properties here because of NH nullability check
        /// http://ayende.com/blog/3987/nhibernate-ipreupdateeventlistener-ipreinserteventlistener
        /// </summary>
        /// <param name="event"></param>
        /// <param name="async"></param>
        /// <returns></returns>
        protected async override Task<object> EntityIsTransient(SaveOrUpdateEvent @event, bool async) //override this for fixing not-null transient property
        {
            //If entry is set then entity will be deleted otherwise will be inserted
            if (@event.Entry == null)
            {
                //We have to set only for transient entites here just for fixing not-null transient property exception from NH
                //for persistent entites we will update the audit properties in the preupdate listener (here we dont know if the entity is dirty)
                SetAuditProperties(@event.Entity, @event.Session);
                EventAggregator.SendMessage(new EntitySavingEvent(@event));
            }
            return await base.EntityIsTransient(@event, async);
        }

        //This method can be called multiple times for a transient entity (because of cascades) so we will update the audit values only if they are not set
        private void SetAuditProperties(object obj, ISession session)
        {
            var entity = obj as IVersionedEntity;
            if (entity == null) return;
            var dbSettings = Database.GetSessionFactoryInfo(session)?.DatabaseConfiguration;
            var requiredLastModifiedProp = dbSettings?.Conventions?.RequiredLastModifiedProperty == true;
            var currentDate = DateTime.UtcNow;
            if (entity.CreatedDate == DateTime.MinValue)
                obj.SetMemberValue("CreatedDate", currentDate);
            if (requiredLastModifiedProp && !entity.LastModifiedDate.HasValue)
                obj.SetMemberValue("LastModifiedDate", currentDate);

            var entityType = entity.GetTypeUnproxied();
            var genType = entityType.GetGenericType(typeof(IVersionedEntityWithUser<>));
            if (genType == null)
                return;

            var userType = genType.GetGenericArguments()[0];
            var currentUser = _auditUserProvider.GetCurrentUser(session, userType);
            if (obj.GetMemberValue("CreatedBy") == null)
                obj.SetMemberValue("CreatedBy", currentUser);
            if (obj.GetMemberValue("LastModifiedBy") == null && requiredLastModifiedProp)
                obj.SetMemberValue("LastModifiedBy", currentUser);
        }

        protected override object EntityIsPersistent(SaveOrUpdateEvent @event)
        {
            //SetAuditProperties(@event.Entity, @event.Session, false);
            EventAggregator.SendMessage(new EntityUpdatingEvent(@event));
            return base.EntityIsPersistent(@event);
        }
        /*
        protected override void EntityIsDetached(SaveOrUpdateEvent @event)
        {
            //if (HasDirtyProperties(@event))
            SetAuditProperties(@event.Entity, @event.Session);
            base.EntityIsDetached(@event);
        }
         
        private static bool HasDirtyProperties(SaveOrUpdateEvent @event)
        {
            ISessionImplementor session = @event.Session;
            var entry = @event.Entry;
            var entity = @event.Entity;
            if (entry == null || !entry.RequiresDirtyCheck(entity) || !entry.ExistsInDatabase || entry.LoadedState == null)
            {
                return false;
            }
            var persister = entry.Persister;

            var currentState = persister.GetPropertyValues(entity, session.EntityMode);

            var loadedState = entry.LoadedState;

            return persister.EntityMetamodel.Properties
                            .Where(
                                (property, i) =>
                                !LazyPropertyInitializer.UnfetchedProperty.Equals(currentState[i]) &&
                                property.Type.IsDirty(loadedState[i], currentState[i], session))
                            .Any();
        }
         
         */


    }
}
