using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;


/// <copyright file="MultiWorkQueue.cs">
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
    ///   This is an advanced version of WorkQueue, allowing multiple queues
    ///   running in parallel.
    ///  </para>
    ///   You need to call the Initialize() method before calling any other methods
    ///   of this class.
    /// </summary>
    public static class MultiWorkQueue
    {
        private static bool _initialized = false;
        private volatile static Dictionary<object, Thread> _thread;
        private volatile static Dictionary<object, AutoResetEvent> _signal;
        private volatile static ConcurrentDictionary<object, ConcurrentQueue<Tuple<Action<object>, object>>> _queue;
        private static readonly Object _lock = new Object();


        /// <summary>
        ///  Initializes the work queue.
        ///  You need to call InitializeThread() before you can start queueing jobs.
        /// </summary>
        public static void Initialize()
        {
            lock (MultiWorkQueue._lock)
            {
                if (MultiWorkQueue._initialized) { return; }
                MultiWorkQueue._queue = new ConcurrentDictionary<object, ConcurrentQueue<Tuple<Action<object>, object>>>();
                MultiWorkQueue._signal = new Dictionary<object, AutoResetEvent>();
                MultiWorkQueue._thread = new Dictionary<object, Thread>();

                MultiWorkQueue._initialized = true;
            }
        }

        /// <summary>
        ///  Initializes a worker thread and its queue.
        /// </summary>
        /// <param name="identifier">
        ///  An object used to uniquely identify this worker thread.
        /// </param>
        public static void InitializeThread(object identifier)
        {
            lock (MultiWorkQueue._lock)
            {
                if (!MultiWorkQueue._initialized) { return; }
                if (!MultiWorkQueue._signal.ContainsKey(identifier))
                {
                    MultiWorkQueue._signal.Add(identifier, new AutoResetEvent(false));
                }

                if (!MultiWorkQueue._queue.ContainsKey(identifier))
                {
                    MultiWorkQueue._queue.TryAdd(identifier, new ConcurrentQueue<Tuple<Action<object>, object>>());
                }

                if (!MultiWorkQueue._thread.ContainsKey(identifier))
                {
                    Thread tmp = new Thread(MultiWorkQueue._work);
                    tmp.IsBackground = true;
                    tmp.Name = String.Format("MultiWorkQueue [{0}]", identifier.ToString());
                    tmp.Start(identifier);
                    MultiWorkQueue._thread.Add(identifier, tmp);
                }
            }
        }

        /// <summary>
        ///  Queues a job for execution on a separate thread.
        /// </summary>
        /// <param name="identifier">Identifier of the thread to run the job on.</param>
        /// <param name="func">The function to be executed.</param>
        /// <param name="param">An object that is passed to the function as its parameter.</param>
        public static void Enqueue(object identifier, Action<object> func, object param)
        {
            ConcurrentQueue<Tuple<Action<object>, object>> queue;
            if (MultiWorkQueue._queue.TryGetValue(identifier, out queue))
            {
                queue.Enqueue(
                    new Tuple<Action<object>, object>(
                        func,
                        param
                    )
                );
            }
            MultiWorkQueue._signal[identifier].Set();
        }

        /// <summary>
        ///  Queues a job for execution on a separate thread.
        /// </summary>
        /// <param name="identifier">Identifier of the thread to run the job on.</param>
        /// <param name="func">The function to be executed.</param>
        public static void Enqueue(object identifier, Action func) { MultiWorkQueue.Enqueue(identifier, (object o) => func(), null); }

        private static void _work(object identifier)
        {
            AutoResetEvent signal = MultiWorkQueue._signal[identifier];
            ConcurrentQueue<Tuple<Action<object>, object>> queue = null;
            while (queue == null) { MultiWorkQueue._queue.TryGetValue(identifier, out queue); }
            while (true)
            {
                if (queue.Count < 1) { signal.WaitOne(); }

                Tuple<Action<object>, object> func;
                if (queue.TryDequeue(out func))
                {
                    func.Item1(func.Item2);
                }
            }
        }
    }
}
