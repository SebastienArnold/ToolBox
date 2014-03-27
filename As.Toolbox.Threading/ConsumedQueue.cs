﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace As.Toolbox.Threading
{
    /// <summary>
    /// ConsumedQueue is a queue automatically unstacked by an adjustable amount of thread.
    /// The unqueue action is provided in constructor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConsumedQueue<T> : IDisposable where T : class
    {
        #region _Private properties_

        private bool _exitRequested;
        private bool _pauseRequested;

        private readonly object _locker = new object();
        private readonly List<Thread> _consumerThreads;

        private readonly Queue<T> _itemsQueue = new Queue<T>();
        private readonly Action<T> _dequeueAction;

        #endregion _Private properties_

        #region _Public methods_

        /// <summary>
        /// Initializes a new instance of the <see cref="SuperQueue{T}"/> class.
        /// </summary>
        /// <param name="workerCount">The amount of Thread worker to create to dequeue items.</param>
        /// <param name="dequeueAction">The dequeue action.</param>
        public ConsumedQueue(int workerCount, Action<T> dequeueAction)
        {
            _dequeueAction = dequeueAction;
            _consumerThreads = new List<Thread>(workerCount);
            
            // Initialize thread worker pool.
            for (int i=0; i<workerCount; i++)
            {
                var t = new Thread(Consume) { IsBackground = true, Name = string.Format("Worker {0}",i )};
                _consumerThreads.Add(t);
                t.Start();
            }
        }

        /// <summary>
        /// Enqueues a new item.
        /// </summary>
        /// <param name="item"></param>
        public void EnqueueTask(T item)
        {
            lock (_locker)
            {
                _itemsQueue.Enqueue(item);
                Monitor.PulseAll(_locker);
            }
        }

        /// <summary>
        /// Close all worker thread.
        /// </summary>
        public void Dispose()
        {
            // Set exit requested to indicate thread have to exit.
            _exitRequested = true;
            // Send signal to wake up thread if waiting.
            Monitor.PulseAll(_locker);
            // Join thread.
            _consumerThreads.ForEach(thread => thread.Join());
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
        /// Automatically consumes the queue when contain items.
        /// </summary>
        private void Consume()
        {
            while (true)
            {
                T item;
                lock (_locker)
                {
                    while (_itemsQueue.Count == 0 || _pauseRequested) Monitor.Wait(_locker);
                    if (_exitRequested) return;

                    item = _itemsQueue.Dequeue();
                }

                // run actual method
                _dequeueAction(item);
            }
        }
    }
}