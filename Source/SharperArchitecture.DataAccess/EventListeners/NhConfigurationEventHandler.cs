﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using SharperArchitecture.Common.Events;
using SharperArchitecture.DataAccess.Attributes;
using SharperArchitecture.DataAccess.Events;
using NHibernate.Event;
using SharperArchitecture.Common.Specifications;

namespace SharperArchitecture.DataAccess.EventListeners
{
    public class NhConfigurationEventHandler : IEventHandler<NhConfigurationEvent>
    {
        private readonly IEnumerable<ISaveOrUpdateEventListener> _saveOrUpdateEventListeners;
        private readonly IEnumerable<IFlushEventListener> _flushEventListeners;
        private readonly IEnumerable<IDeleteEventListener> _deleteEventListeners;
        private readonly IEnumerable<IAutoFlushEventListener> _autoFlushEventListeners;

        private readonly IEnumerable<IPreInsertEventListener> _preInsertEventListeners;
        private readonly IEnumerable<IPreUpdateEventListener> _preUpdateEventListeners;
        private readonly IEnumerable<IPreDeleteEventListener> _preDeleteEventListeners;
        private readonly IEnumerable<IPreCollectionUpdateEventListener> _preCollectionUpdateEventListeners;
        private readonly IEnumerable<IPostInsertEventListener> _postInsertEventListeners;
        private readonly IEnumerable<IPostUpdateEventListener> _postUpdateEventListeners;
        private readonly IEnumerable<IPostDeleteEventListener> _postDeleteEventListeners;
        private readonly IEnumerable<IPostCollectionUpdateEventListener> _postCollectionUpdateEventListeners;

        
        public NhConfigurationEventHandler(
            IEnumerable<ISaveOrUpdateEventListener> saveOrUpdateEventListeners,
            IEnumerable<IPreInsertEventListener> preInsertEventListeners,
            IEnumerable<IPreUpdateEventListener> preUpdateEventListeners,
            IEnumerable<IPreCollectionUpdateEventListener> preCollectionUpdateEventListeners,
            IEnumerable<IFlushEventListener> flushEventListeners,
            IEnumerable<IDeleteEventListener> deleteEventListeners,
            IEnumerable<IAutoFlushEventListener> autoFlushEventListeners,
            IEnumerable<IPreDeleteEventListener> preDeleteEventListeners,
            IEnumerable<IPostInsertEventListener> postInsertEventListeners,
            IEnumerable<IPostUpdateEventListener> postUpdateEventListeners,
            IEnumerable<IPostCollectionUpdateEventListener> postCollectionUpdateEventListeners,
            IEnumerable<IPostDeleteEventListener> postDeleteEventListeners
            )
        {
            _saveOrUpdateEventListeners = saveOrUpdateEventListeners;
            _preInsertEventListeners = preInsertEventListeners;
            _preUpdateEventListeners = preUpdateEventListeners;
            _preCollectionUpdateEventListeners = preCollectionUpdateEventListeners;
            _flushEventListeners = flushEventListeners;
            _deleteEventListeners = deleteEventListeners;
            _autoFlushEventListeners = autoFlushEventListeners;
            _preDeleteEventListeners = preDeleteEventListeners;
            _postInsertEventListeners = postInsertEventListeners;
            _postUpdateEventListeners = postUpdateEventListeners;
            _postCollectionUpdateEventListeners = postCollectionUpdateEventListeners;
            _postDeleteEventListeners = postDeleteEventListeners;
        }

        public void Handle(NhConfigurationEvent e)
        {
            var config = e.Configuration;
            var eventListeners = config.EventListeners;

            //ISaveOrUpdateEventListener
            config.SetListeners(ListenerType.SaveUpdate, MergeListeners(eventListeners.SaveOrUpdateEventListeners, _saveOrUpdateEventListeners));
            config.SetListeners(ListenerType.Save, MergeListeners(eventListeners.SaveEventListeners, _saveOrUpdateEventListeners));
            config.SetListeners(ListenerType.Update, MergeListeners(eventListeners.UpdateEventListeners, _saveOrUpdateEventListeners));

            //IFlushEventListener
            config.SetListeners(ListenerType.Flush, MergeListeners(eventListeners.FlushEventListeners, _flushEventListeners));

            //IDeleteEventListener
            config.SetListeners(ListenerType.Delete, MergeListeners(eventListeners.DeleteEventListeners, _deleteEventListeners));

            //IAutoFlushEventListener
            config.SetListeners(ListenerType.Autoflush, MergeListeners(eventListeners.AutoFlushEventListeners, _autoFlushEventListeners));

            //IPreInsertEventListener
            config.AppendListeners(ListenerType.PreInsert, MergeListeners(eventListeners.PreInsertEventListeners, _preInsertEventListeners));

            //IPreUpdateEventListener
            config.AppendListeners(ListenerType.PreUpdate, MergeListeners(eventListeners.PreUpdateEventListeners, _preUpdateEventListeners));

            //IPreCollectionUpdateEventListener
            config.AppendListeners(ListenerType.PreCollectionUpdate, MergeListeners(eventListeners.PreCollectionUpdateEventListeners, _preCollectionUpdateEventListeners));

            //IPreDeleteEventListener
            config.AppendListeners(ListenerType.PreDelete, MergeListeners(eventListeners.PreDeleteEventListeners, _preDeleteEventListeners));

            //IPostInsertEventListener
            config.AppendListeners(ListenerType.PostInsert, MergeListeners(eventListeners.PostInsertEventListeners, _postInsertEventListeners));

            //IPostUpdateEventListener
            config.AppendListeners(ListenerType.PostUpdate, MergeListeners(eventListeners.PostUpdateEventListeners, _postUpdateEventListeners));

            //IPostCollectionUpdateEventListener
            config.AppendListeners(ListenerType.PostCollectionUpdate, MergeListeners(eventListeners.PostCollectionUpdateEventListeners, _postCollectionUpdateEventListeners));

            //IPostDeleteEventListener
            config.AppendListeners(ListenerType.PostDelete, MergeListeners(eventListeners.PostDeleteEventListeners, _postDeleteEventListeners));
        }

        private static T[] MergeListeners<T>(IList<T> currentListeners, IEnumerable<T> newListeners) 
            where T : class
        {
            currentListeners = currentListeners.ToList();
            newListeners = newListeners ?? new List<T>();
            foreach (var newListener in newListeners)
            {
                var evntListnrTypeAttr = newListener.GetType().GetCustomAttributes<NhEventListenerTypeAttribute>(false)
                    .FirstOrDefault(o => o.Type == typeof (T));
                var evntListnrAttr = newListener.GetType().GetCustomAttribute<NhEventListenerAttribute>(false) ?? new NhEventListenerAttribute();

                var replaceListener = evntListnrTypeAttr?.ReplaceListener ?? evntListnrAttr.ReplaceListener;
                if (replaceListener != null)
                {
                    var toReplace = currentListeners.FirstOrDefault(o => o.GetType() == replaceListener);
                    if (toReplace != null)
                        currentListeners.Replace(toReplace, newListener);
                }
                else
                {
                    currentListeners.Add(newListener);
                }
            }

            return currentListeners.OrderBy(o =>
            {
                var type = o.GetType();
                var attr = type.GetCustomAttribute<NhEventListenerAttribute>(false) ?? new NhEventListenerAttribute();
                var order = attr.Order;
                var evntListnrTypeAttr = type.GetCustomAttributes<NhEventListenerTypeAttribute>(false)
                    .FirstOrDefault(a => a.Type == typeof(T));
                if (evntListnrTypeAttr != null)
                {
                    order = evntListnrTypeAttr.Order;
                }
                return order;
            }).ToArray();
        }
    }
}
