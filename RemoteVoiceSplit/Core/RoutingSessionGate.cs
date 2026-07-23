using System;
using System.Threading;

namespace RemoteVoiceSplit.Core;

internal sealed class RoutingSessionGate
{
    private readonly object _transitionGate = new();
    private readonly AtomicRegistration<RoutingSessionEpoch> _current = new();

    public bool IsReady => _current.Read() is not null;

    public void Activate()
    {
        lock (_transitionGate)
        {
            if (_current.Read() is not null)
            {
                throw new InvalidOperationException("A routing session was already active.");
            }

            _current.Exchange(new RoutingSessionEpoch());
        }
    }

    public RoutingSubmissionLease? TryBeginSubmission()
    {
        RoutingSessionEpoch? epoch = _current.Read();
        if (epoch is null || !epoch.Usage.TryBegin())
        {
            return null;
        }

        if (!_current.IsCurrent(epoch))
        {
            epoch.Usage.End();
            return null;
        }

        return new RoutingSubmissionLease(epoch.Usage);
    }

    public void Deactivate()
    {
        lock (_transitionGate)
        {
            RoutingSessionEpoch? epoch = _current.Exchange(null);
            epoch?.Usage.Retire();
        }
    }

    private sealed class RoutingSessionEpoch
    {
        public AtomicUsageLease Usage { get; } = new();
    }
}

internal sealed class RoutingSubmissionLease : IDisposable
{
    private AtomicUsageLease? _usage;

    public RoutingSubmissionLease(AtomicUsageLease usage)
    {
        _usage = usage;
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _usage, null)?.End();
    }
}
