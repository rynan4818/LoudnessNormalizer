using System;
using System.Diagnostics;

namespace LoudnessNormalizer.Util
{
    internal class TimeoutTimer
    {
        public readonly Stopwatch _timer;
        public readonly long _timeoutTicks;

        public bool HasTimedOut => _timer.ElapsedTicks >= _timeoutTicks;

        public TimeoutTimer(float timeoutSec)
        {
            _timer = new Stopwatch();
            _timer.Start();
            _timeoutTicks = (long)(timeoutSec * TimeSpan.TicksPerSecond);
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}
