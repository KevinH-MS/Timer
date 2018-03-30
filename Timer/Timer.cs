using System;
using System.Threading.Tasks;
using Priority_Queue;

public class Timer
{
    private static readonly object _lock = new object();
    private static readonly _Timer _internalTimer = new _Timer();
    private static readonly SimplePriorityQueue<PendingTimerInfo, long> _pending = new SimplePriorityQueue<PendingTimerInfo, long>();
    private static bool _isRunning;
    private static long _lastScheduledTimerTicks;
    private static Callback _lastScheduledCallback;

    private Callback _callback;
    private bool _isCancelled;

    public void ScheduleTimer(int xMs, Callback callback)
    {
        if (xMs <= 0)
        {
            throw new ArgumentException();
        }

        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        lock (_lock)
        {
            var nowTicks = DateTime.Now.Ticks;
            var nextScheduledTimerTicks = nowTicks + TimeSpan.FromMilliseconds(xMs).Ticks;

            _callback += callback;
            _isCancelled = false;

            if (_isRunning)
            {
                if (nextScheduledTimerTicks < _lastScheduledTimerTicks)
                {
                    _internalTimer._cancel();
                    _pending.Enqueue(new PendingTimerInfo(_lastScheduledTimerTicks, _lastScheduledCallback), _lastScheduledTimerTicks);
                    ScheduleTimerInternal(xMs, nextScheduledTimerTicks, this.InternalCallback);
                }
                else
                {
                    _pending.Enqueue(new PendingTimerInfo(nextScheduledTimerTicks, this.InternalCallback), nextScheduledTimerTicks);
                }
            }
            else
            {
                ScheduleTimerInternal(xMs, nextScheduledTimerTicks, this.InternalCallback);
            }
        }
    }

    public void CancelTimer()
    {
        lock (_lock)
        {
            _isCancelled = true;
        }
    }

    private void InternalCallback()
    {
        lock (_lock)
        {
            if (!_isCancelled)
            {
                _callback?.Invoke();
            }

            _callback = null;

            if (_pending.TryDequeue(out var nextScheduledTimer))
            {
                var xMs = (int)TimeSpan.FromTicks(nextScheduledTimer.Ticks - DateTime.Now.Ticks).TotalMilliseconds;
                if (xMs <= 0)
                {
                    nextScheduledTimer.Callback.Invoke();
                }
                else
                {
                    ScheduleTimerInternal(xMs, nextScheduledTimer.Ticks, nextScheduledTimer.Callback);
                }
            }
            else
            {
                _isRunning = false;
                _lastScheduledCallback = null;
                _lastScheduledTimerTicks = 0;
            }
        }
    }

    private void ScheduleTimerInternal(int xMs, long xTicks, Callback callback)
    {
        _internalTimer._scheduleTimer(xMs, callback);
        _isRunning = true;
        _lastScheduledCallback = callback;
        _lastScheduledTimerTicks = xTicks;
    }

    /// <summary>
    /// For testing purposes only...wait until no callback is scheduled or 5 seconds expire...
    /// </summary>
    internal async Task WaitForAllPendingTimers()
    {
        var startWait = DateTime.Now;
        while (_callback != null && (DateTime.Now - startWait) < TimeSpan.FromSeconds(5))
        {
            await Task.Delay(50);
        }
    }

    private class PendingTimerInfo
    {
        public Callback Callback { get; }
        public long Ticks { get; }

        public PendingTimerInfo(long ticks, Callback callback)
        {
            Ticks = ticks;
            Callback = callback;
        }
    }

    private class _Timer
    {
        private static object _lock = new object();
        private static System.Timers.Timer _timer;

        public void _scheduleTimer(int xMs, Callback callback)
        {
            lock (_lock)
            {
                if (_timer == null)
                {
                    _timer = new System.Timers.Timer(xMs) { AutoReset = false };
                    _timer.Elapsed += (o, args) => { _cancel(); callback(); };
                    _timer.Start();
                }
            }
        }

        public void _cancel()
        {
            lock (_lock)
            {
                _timer.Close();
                _timer = null;
            }
        }
    }
}

public delegate void Callback();