using System;
using System.Collections.Concurrent;
using System.Threading;


/// <copyright file="WorkQueue.cs">
///  Copyright (c) All Rights Reserved
///  <author>Martin Kukura</author>
/// </copyright>

namespace MTX.Utilities.Concurrent
{
    /// <summary>
    ///  <para>
    ///   Provides static methods for queueing jobs that will be executed on a
    ///   separate thread.
    ///  </para>
    ///  <para>
    ///   This class differs from ThreadPool in the fact that it guarantees that
    ///   jobs that were queued first will both execute and finish before any
    ///   jobs that were queued later. This is accomplished by using a single
    ///   worker thread to execute them.
    ///  </para>
    ///  You need to call the Initialize() method before calling any other methods
    ///  of this class.
    /// </summary>
    public static class WorkQueue
    {
        private static bool _initialized = false;
        private static Thread _thread;
        private volatile static AutoResetEvent _signal;
        private volatile static ConcurrentQueue<Tuple<Action<object>, object>> _queue;
        private static readonly Object _lock = new Object();


        /// <summary>
        /// Initializes the work queue.
        /// This function must be called before any calls to Enqueue().
        /// </summary>
        public static void Initialize()
        {
            lock (WorkQueue._lock)
            {
                if (WorkQueue._initialized) { return; }
                WorkQueue._queue = new ConcurrentQueue<Tuple<Action<object>, object>>();
                WorkQueue._signal = new AutoResetEvent(false);

                WorkQueue._thread = new Thread(WorkQueue._work);
                WorkQueue._thread.IsBackground = true;
                WorkQueue._thread.Name = "WorkQueue";
                WorkQueue._thread.Start();

                WorkQueue._initialized = true;
            }
        }

        /// <summary>
        /// Queues a job for execution on a separate thread.
        /// </summary>
        /// <param name="func">The function to be executed, accepting an object as a parameter</param>
        /// <param name="param">Instance of object that will be passed to the function</param>
        public static void Enqueue(Action<object> func, object param)
        {
            bool signal = (WorkQueue._queue.Count < 1);
            WorkQueue._queue.Enqueue(
                new Tuple<Action<object>, object>(
                    func,
                    param
                )
            );
            WorkQueue._signal.Set();
        }

        /// <summary>
        /// Queues a job for execution on a separate thread.
        /// </summary>
        /// <param name="func">The function to be executed</param>
        public static void Enqueue(Action func) { WorkQueue.Enqueue((object o) => func(), null); }

        private static void _work()
        {
            while (true)
            {
                if (WorkQueue._queue.Count < 1) { WorkQueue._signal.WaitOne(); }

                Tuple<Action<object>, object> func;
                if (WorkQueue._queue.TryDequeue(out func))
                {
                    func.Item1(func.Item2);
                }
            }
        }
    }
}
