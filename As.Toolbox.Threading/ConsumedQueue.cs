using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace As.Toolbox.Threading
{
    /// <summary>
    /// ConsumedQueueOptimizedMono is a queue automatically unstacked by a thread worker.
    /// The action to do when enqueue is provided in constructor.
    /// 
    /// Initialize: var consumedQueue = new ConsumedQueue«string»(s => Console.WriteLine(s));
    /// Enqueue: consumedQueue.EnqueueItem("toto");
    /// Pause \ Resume: consumedQueue.Pause(); consumedQueue.Resume();
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConsumedQueue<T> : IDisposable
    {
        #region _Private properties_

        readonly ManualResetEvent _manualResetEvent = new ManualResetEvent(true); 

        private bool _exitRequested;
        private bool _pauseRequested;
        
        private readonly List<Thread> _consumerThreadsPool;

        private readonly ConcurrentQueue<T> _itemsQueue = new ConcurrentQueue<T>();
        private readonly Action<T> _dequeueAction;

        #endregion _Private properties_

        #region _Public methods_

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsumedQueue{T}"/> class.
        /// </summary>
        /// <param name="workerCount"></param>
        /// <param name="dequeueAction">The action to execute when dequeue an item.</param>
        public ConsumedQueue(int workerCount, Action<T> dequeueAction)
        {
            _dequeueAction = dequeueAction;

            _consumerThreadsPool = new List<Thread>(workerCount);
            for (var i = 0; i < workerCount; i++)
            {
                var t = new Thread(Consume) { IsBackground = true, Name = string.Format("Worker {0}", i) };
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
            _itemsQueue.Enqueue(item);
            if(!_pauseRequested) _manualResetEvent.Set();
        }

        /// <summary>
        /// Close all worker thread properly.
        /// </summary>
        public void Dispose()
        {
            _exitRequested = true;
            _manualResetEvent.Set();

            _consumerThreadsPool.ForEach(thread => thread.Join());
        }

        /// <summary>
        /// If dequeue in progress, continue to achieve but stop to dequeue other items.
        /// </summary>
        public void Pause()
        {
            _pauseRequested = true;
            _manualResetEvent.Reset();
        }

        /// <summary>
        /// ReActivate dequeue if paused.
        /// </summary>
        public void Resume()
        {
            _pauseRequested = false;
            _manualResetEvent.Set();
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
                if (_itemsQueue.Count == 0 || _pauseRequested) _manualResetEvent.Reset();
                _manualResetEvent.WaitOne();

                if (_exitRequested) return;

                T item;
                if (_itemsQueue.TryDequeue(out item)) _dequeueAction(item);
            }
        }
    }
}