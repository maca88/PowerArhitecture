﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArhitecture.Common.Events;

namespace PowerArhitecture.Tests.Common.Events
{
    public class TwoEventsPerHandlerEvent : BaseEvent
    {
        public bool Success { get; set; }
    }

    public class TwoEventsPerHandler2Event : BaseEvent
    {
        public bool Success { get; set; }
    }

    public class TwoEventsPerHandlerEventHander : BaseEventsHandler<TwoEventsPerHandlerEvent, TwoEventsPerHandler2Event>
    {
        public override void Handle(TwoEventsPerHandlerEvent @event)
        {
            @event.Success = true;
        }

        public override void Handle(TwoEventsPerHandler2Event @event)
        {
            @event.Success = true;
        }
    }
}
