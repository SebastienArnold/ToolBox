using System;
using System.Collections.Generic;
using System.Threading;

namespace As.Toolbox.Threading
{
    /// <summary>
    /// ConsumedQueue is a queue automatically unstacked by an adjustable amount of thread worker.
    /// The action to do when enqueue is provided in constructor.
    /// 
    /// Initialize: var consumedQueue = new ConsumedQueue«string»(3,s => Console.WriteLine(s));
    /// Enqueue: consumedQueue.EnqueueItem("toto");
    /// Pause \ Resume: consumedQueue.Pause(); consumedQueue.Resume();
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConsumedQueue<T> : IDisposable
    {
        #region _Private properties_

        private bool _exitRequested;
        private bool _pauseRequested;

        private readonly object _locker = new object();
        private readonly List<Thread> _consumerThreadsPool;

        private readonly Queue<T> _itemsQueue = new Queue<T>();
        private readonly Action<T> _dequeueAction;

        #endregion _Private properties_

        #region _Public methods_

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsumedQueue{T}"/> class.
        /// </summary>
        /// <param name="workerCount">The amount of Thread worker to create in thread pool to dequeue items.</param>
        /// <param name="dequeueAction">The action to execute when dequeue an item.</param>
        public ConsumedQueue(int workerCount, Action<T> dequeueAction)
        {
            _dequeueAction = dequeueAction;
            _consumerThreadsPool = new List<Thread>(workerCount);
            
            // Initialize thread worker pool.
            for (int i=0; i<workerCount; i++)
            {
                var t = new Thread(Consume) { IsBackground = true, Name = string.Format("Worker {0}", i )};
                _consumerThreadsPool.Add(t);
                t.Start();
            }
        }

        /// <summary>
        /// Enqueues a new item.
        /// </summary>
        /// <param name="item"></param>
        public void EnqueueItem(T item)
        {
            lock (_locker)
            {
                _itemsQueue.Enqueue(item);
                Monitor.PulseAll(_locker);
            }
        }

        /// <summary>
        /// Close all worker thread properly.
        /// </summary>
        public void Dispose()
        {
            // Set exit requested to indicate thread have to exit.
            _exitRequested = true;
            // Send signal to wake up thread if waiting.
            Monitor.PulseAll(_locker);
            // Join thread.
            _consumerThreadsPool.ForEach(thread => thread.Join());
        }

        /// <summary>
        /// If dequeue in progress, continue to achieve but stop to dequeue other items.
        /// </summary>
        public void Pause()
        {
            _pauseRequested = true;
        }

        /// <summary>
        /// ReActivate dequeue if paused.
        /// </summary>
        public void Resume()
        {
            _pauseRequested = false;
            if (_itemsQueue.Count == 0) Monitor.Wait(_locker);
        }

        /// <summary>
        /// Count of items in Queue.
        /// </summary>
        public int ItemsCount
        {
            get
            {
                return _itemsQueue.Count;
            }
        }

        #endregion _Public methods_
        
        /// <summary>
        /// Automatically consumes the queue when containing items.
        /// </summary>
        private void Consume()
        {
            while (true)
            {
                T item;
                lock (_locker)
                {
                    // Unlock _locker but wait for signal.
                    while (_itemsQueue.Count == 0 || _pauseRequested) Monitor.Wait(_locker);
                    if (_exitRequested) return;

                    item = _itemsQueue.Dequeue();
                }

                // run dequeue method.
                _dequeueAction(item);
            }
        }
    }
}