using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
 
namespace Aitex.Core.Util
{
    public class FixSizeQueue<T>
    {
        public int FixedSize { get; set; }

        public int Count {
            get
            {
                lock (_locker)
                {
                    return _innerQueue.Count;
                }
            } 
        }

        private Queue<T> _innerQueue;

        private object _locker = new object();

        public FixSizeQueue(int size)
        {
            FixedSize = size;

            _innerQueue = new Queue<T>();
        }

        public void Enqueue(T obj)
        {
            lock (_locker)
            {
                _innerQueue.Enqueue(obj);
                while (_innerQueue.Count > FixedSize)
                {
                    _innerQueue.Dequeue();
                }
            }
        }

        public bool TryDequeue(out T obj)
        {
            lock (_locker)
            {
                obj = default (T);
                if (_innerQueue.Count > 0)
                {
                    obj = _innerQueue.Dequeue();
                    return true;
                }
                return false;
            }
        }

        public List<T> ToList()
        {
            lock (_locker)
            {
                return _innerQueue.ToList();
            }
        }

        public void Clear()
        {
            lock (_locker)
            {
                _innerQueue.Clear();
            }
        }

        public T ElementAt(int index)
        {
            lock (_locker)
            {
                return _innerQueue.ElementAt(index);
            }
        }

        public bool IsEmpty()
        {
            lock (_locker)
            {
                return _innerQueue.Count==0;
            }
        }
}
}
