using System;
using System.Threading;
using System.Threading.Tasks;

namespace TimerTasks
{
    class Program
    {
        private readonly CancellationTokenSource periodicSystemCts = new CancellationTokenSource();
        private readonly CancellationTokenSource oneShotSystemCts = new CancellationTokenSource();

        static void Main()
        {
            Program p = new Program();
            p.Test1();
        }

        private void MyAction1()
        {
            Console.WriteLine($"OneShot: {DateTime.Now}");
        }

        private void MyAction2()
        {
            Console.WriteLine($"Periodic: {DateTime.Now}");
        }

        public void Test1()
        {
            using TimerTask oneShot = new TimerTask(MyAction1, oneShotSystemCts.Token);
            _ = oneShot.StartAsync(TimeSpan.FromSeconds(5));

            using TimerTask periodic = new TimerTask(MyAction2, periodicSystemCts.Token);
            _ = periodic.StartAsync(TimeSpan.FromSeconds(1), true);

            Console.WriteLine("Press p/q to cancel periodic, s/o to cancel OneShot, cancel/stop or any other key to exit.");

            // New-style UI thread.
            bool goAgain = true;
            while (goAgain)
            {
                char ch = Console.ReadKey(true).KeyChar;

                switch (ch)
                {
                    case 'p':
                        // Periodic Token can only be canceled once.
                        periodicSystemCts.Cancel();
                        break;

                    case 'q':
                        // Periodic stop.
                        periodic.Stop();
                        break;

                    //case 'd':
                    //    //Token can only be canceled once.
                    //    periodicSystemCts.CancelAfter(TimeSpan.FromSeconds(3));
                    //    break;

                    case 'o':
                        oneShot.Stop();
                        break;

                    case 's':
                        // System Token can only be canceled once.
                        oneShotSystemCts.Cancel();
                        break;

                    default:
                        goAgain = false;
                        break;
                }

                Thread.Sleep(100);

            }

            oneShotSystemCts.Dispose();
            periodicSystemCts.Dispose();
        }

        //public static void Test2()
        //{
        //    using OneShotTimerTask oneShot = new OneShotTimerTask(() =>
        //    {
        //        Console.WriteLine($"OneShot: {DateTime.Now}");
        //    }, oneShotSystemCts.Token);

        //    _ = oneShot.StartAsync(TimeSpan.FromSeconds(5));

        //    using PeriodicTimerTask timertask = new PeriodicTimerTask(() =>
        //    {
        //        Console.WriteLine(DateTime.Now);
        //    }, periodicSystemCts.Token);

        //    _ = timertask.StartAsync(TimeSpan.FromSeconds(1));

        //    Console.WriteLine("Press p/q to cancel periodic, s/o to cancel OneShot, cancel/stop or any other key to exit.");

        //    // New-style UI thread.
        //    bool goAgain = true;
        //    while (goAgain)
        //    {
        //        char ch = Console.ReadKey(true).KeyChar;

        //        switch (ch)
        //        {
        //            case 'p':
        //                // Periodic Token can only be canceled once.
        //                periodicSystemCts.Cancel();
        //                break;

        //            case 'q':
        //                // Periodic stop.
        //                timertask.Stop();
        //                break;

        //            //case 'd':
        //            //    //Token can only be canceled once.
        //            //    periodicSystemCts.CancelAfter(TimeSpan.FromSeconds(3));
        //            //    break;

        //            case 'o':
        //                oneShot.Stop();
        //                break;

        //            case 's':
        //                // System Token can only be canceled once.
        //                oneShotSystemCts.Cancel();
        //                break;

        //            default:
        //                goAgain = false;
        //                break;
        //        }

        //        Thread.Sleep(100);

        //    }

        //    oneShotSystemCts.Dispose();
        //    periodicSystemCts.Dispose();
        //}
    }

    /// <summary>
    /// Factory class to create a periodic Task to simulate a <see cref="System.Threading.Timer"/> using <see cref="Task">Tasks.</see>
    /// </summary>
    public static class PeriodicTaskFactory
    {
        /// <summary>
        /// Starts the periodic task.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="intervalInMilliseconds">The interval in milliseconds.</param>
        /// <param name="cancelToken">The cancel token.</param>
        /// <returns>A <see cref="Task"/></returns>
        public static Task Start(Action action, int intervalInMilliseconds, CancellationToken cancelToken)
        {
            return Task.Factory.StartNew(() => MainPeriodicTaskAction(intervalInMilliseconds, action, cancelToken),
                                               cancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        /// <summary>
        /// Mains the periodic task action.
        /// </summary>
        /// <param name="intervalInMilliseconds">The interval in milliseconds.</param>
        /// <param name="cancelToken">The cancel token.</param>
        /// <param name="wrapperAction">The wrapper action.</param>
        private static async void MainPeriodicTaskAction(int intervalInMilliseconds, Action wrapperAction, CancellationToken cancelToken)
        {
            ////////////////////////////////////////////////////////////////////////////
            // using a ManualResetEventSlim as it is more efficient in small intervals.
            // In the case where longer intervals are used, it will automatically use 
            // a standard WaitHandle....
            // see http://msdn.microsoft.com/en-us/library/vstudio/5hbefs30(v=vs.100).aspx
            using ManualResetEventSlim periodResetEvent = new ManualResetEventSlim(false);

            try
            {
                do
                {
                    try
                    {
                        periodResetEvent.Wait(intervalInMilliseconds, cancelToken);
                    }
                    finally
                    {
                        periodResetEvent.Reset();
                    }

                    await Task.Factory.StartNew(wrapperAction, cancelToken, TaskCreationOptions.AttachedToParent, TaskScheduler.Current);
                } while (!cancelToken.IsCancellationRequested);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cancelled!");
            }
        }
    }
}
