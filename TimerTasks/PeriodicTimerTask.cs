using System;
using System.Threading;
using System.Threading.Tasks;

namespace TimerTasks
{
    public class PeriodicTimerTask : IDisposable
    {
        private CancellationTokenSource _localCts;
        private readonly CancellationToken _systemToken;
        private readonly Action _action;

        public PeriodicTimerTask(Action action, CancellationToken systemToken = default)
        {
            _systemToken = systemToken;
            _action = action;
        }

        public async Task StartAsync(TimeSpan period)
        {
            _localCts = new CancellationTokenSource();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_localCts.Token, _systemToken);

            while (!linkedCts.Token.IsCancellationRequested)
            {
                await Task.Delay(period, linkedCts.Token);

                if (!linkedCts.Token.IsCancellationRequested)
                    _action();
            }
        }

        public void Stop()
        {
            _localCts.Cancel();
        }

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _localCts.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
    }

}
