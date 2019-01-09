using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RpcOverHttp.Internal
{
    public interface IQueueReader<T> : IDisposable
    {
        T Dequeue();
        void ReleaseReader();
    }

    public interface IQueueWriter<T> : IDisposable
    {
        void Enqueue(T data);
    }


    public class BlockingQueue<T> : IQueueReader<T>,
                                         IQueueWriter<T>, IDisposable
    {
        // use a .NET queue to store the data
        private Queue<T> mQueue = new Queue<T>();
        // create a semaphore that contains the items in the queue as resources.
        // initialize the semaphore to zero available resources (empty queue).
        private Semaphore mSemaphore = new Semaphore(0, int.MaxValue);
        // a event that gets triggered when the reader thread is exiting
        private ManualResetEvent mKillThread = new ManualResetEvent(false);
        // wait handles that are used to unblock a Dequeue operation.
        // Either when there is an item in the queue
        // or when the reader thread is exiting.
        private WaitHandle[] mWaitHandles;

        public BlockingQueue(CancellationToken cancellationToken)
        {
            cancellationToken.Register(ReleaseReader);
            mWaitHandles = new WaitHandle[2] { mSemaphore, mKillThread };
        }
        public void Enqueue(T data)
        {
            lock (mQueue) mQueue.Enqueue(data);
            // add an available resource to the semaphore,
            // because we just put an item
            // into the queue.
            mSemaphore.Release();
        }

        public bool Any()
        {
            return mQueue.Any();
        }

        public bool Any(Func<T, bool> predicate)
        {
            return mQueue.Any(predicate);
        }

        public int Count()
        {
            return mQueue.Count;
        }


        public int Count(Func<T, bool> predicate)
        {
            return this.mQueue.Count(predicate);
        }

        public T Dequeue()
        {
            // wait until there is an item in the queue
            WaitHandle.WaitAny(mWaitHandles);
            lock (mQueue)
            {
                if (mQueue.Count > 0)
                    return mQueue.Dequeue();
            }
            return default(T);
        }

        public void ReleaseReader()
        {
            mKillThread.Set();
        }


        void IDisposable.Dispose()
        {
            if (mSemaphore != null)
            {
                mSemaphore.Close();
                mQueue.Clear();
                mSemaphore = null;
            }
        }
    }
}
