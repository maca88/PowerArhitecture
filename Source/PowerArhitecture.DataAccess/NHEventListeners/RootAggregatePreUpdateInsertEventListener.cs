﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Event;
using PowerArhitecture.DataAccess.Specifications;

namespace PowerArhitecture.DataAccess.NHEventListeners
{
    public class RootAggregatePreUpdateInsertEventListener : IPreUpdateEventListener, IPreInsertEventListener
    {
        public bool OnPreUpdate(PreUpdateEvent @event)
        {
            LockRoot(@event.Entity, @event.Session);
            return false;
        }

        public bool OnPreInsert(PreInsertEvent @event)
        {
            LockRoot(@event.Entity, @event.Session);
            return false;
        }

        private void LockRoot(object entity, IEventSource session)
        {
            var currentChild = entity as IAggregateChild;
            if (currentChild == null) return;
            
            while (currentChild != null)
            {
                var root = currentChild.AggregateRoot;
                if(root == null) break;
                var rootEntry = session.PersistenceContext.GetEntry(root);
                if (rootEntry == null || !LockMode.Force.Equals(rootEntry.LockMode))
                {
                    session.Lock(root, LockMode.Force);
                }
                currentChild = root as IAggregateChild;
            }
        }
    }
}
