using System;
using System.Threading;
using System.Threading.Tasks;

namespace TimerTasks
{
    /// <summary>
    /// using TimerTask oneShot = new TimerTask(MyAction1, token);
    /// _ = oneShot.StartAsync(TimeSpan.FromSeconds(5));
    /// or
    /// oneShot.StartAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
    ///
    /// using TimerTask periodic = new TimerTask(MyAction2);
    /// _ = periodic.StartAsync(TimeSpan.FromSeconds(1), true);
    /// </summary>
    public class TimerTask : IDisposable
    {
        private CancellationTokenSource _localCts;
        private readonly CancellationToken _systemToken;
        private readonly Action _action;
        private bool _disposed = false; // To detect redundant calls

        /// <summary>
        /// Instantiate a Timer
        /// </summary>
        /// <param name="action">action to call when timer fires</param>
        /// <param name="systemToken">Optional System cancellation token</param>
        public TimerTask(Action action, CancellationToken systemToken = default)
        {
            _systemToken = systemToken;
            _action = action;
        }

        /// <summary>
        /// Start the timer running and call the action when timer fires
        /// </summary>
        /// <param name="period">The length of time before the timer fires</param>
        /// <param name="periodic">true: repeat timing after it fires; false: one shot</param>
        /// <returns></returns>
        public async Task StartAsync(TimeSpan period, bool periodic = false)
        {
            _localCts = new CancellationTokenSource();
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_localCts.Token, _systemToken);

            do
            {
                await Task.Delay(period, linkedCts.Token);

                if (!linkedCts.Token.IsCancellationRequested)
                    _action();
            } while (periodic && !linkedCts.Token.IsCancellationRequested);
        }

        /// <summary>
        /// Stops timer
        /// </summary>
        public void Stop()
        {
            _localCts?.Cancel();
        }

        /// <summary>
        /// This code added to correctly implement the disposable pattern
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _localCts?.Cancel();
                    _localCts?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
