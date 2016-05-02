# Concurrent Work Queue
The **WorkQueue** and **MultiWorkQueue** classes provide static methods for easy management of long running worker queue threads. They are ideal if you repeatedly need to execute jobs on a separate thread.

**Note:** The spawned threads are *background* threads,  they won't block termination of the application and will exit with it. This also means that any unexecuted jobs will be discarded and the current job will be immediately terminated.

The namespace for these classes is `MTX.Utilities.Concurrent`.


## WorkQueue
Spins up a single worker thread, allowing jobs to be executed in sequence. Guarantees that jobs queued first will execute and finish before any jobs queued later.

Usage:
 - Call `WorkQueue.Initialize()` (e.g. in your application init)
 - Call `WorkQueue.Enqueue()` whenever you need to execute a job on a separate thread.

The `WorkQueue.Enqueue()` method accepts an `Action`, or an `Action<object>` and an `object`, thus in can be used as either of the following:
```
WorkQueue.Enqueue(() => Console.WriteLine("Executed on a separate thread"));
WorkQueue.Enqueue((object o) => Console.WriteLine((string)o), "Executed on a separate thread");
```


## MultiWorkQueue
An advanced version of **WorkQueue** that allows multiple worker queues to run in parallel. Each queue is uniquely identified by a user supplied object.

Usage:
 - Call `MultiWorkQueue.Initialize()` (e.g. in you application init)
 - Call `MultiWorkQueue.InitializeThread()` for each worker thread you need, supplying an identifier.
 - Call `MultiWorkQueue.Enqueue()` whenever you need to execute a job on a separate thread.

Example:
```
MultiWorkQueue.Initialize();
MultiWorkQueue.InitializeThread("t1");
MultiWorkQueue.InitializeThread("t2");

for (int i = 0; i < 50; i++)
{
    if ((i % 2) == 0) { MultiWorkQueue.Enqueue("t1", () => { Thread.Sleep(9); Console.WriteLine("Executed on thread 1"); }); }
    if ((i % 2) == 1) { MultiWorkQueue.Enqueue("t2", () => { Thread.Sleep(13); Console.WriteLine("Executed on thread 2"); }); }
}
```
