﻿using System.Threading;
using System.Threading.Tasks;

namespace SharperArchitecture.Common.Events
{
    public interface IAsyncEventHandler<in TEvent> where TEvent : IAsyncEvent
    {
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
    }
}
