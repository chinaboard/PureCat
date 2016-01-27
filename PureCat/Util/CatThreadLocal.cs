using System.Collections;
using System.Threading;
using System.Collections.Concurrent;

namespace PureCat.Util
{
    public class CatThreadLocal<T> where T : class
    {

        private readonly ConcurrentDictionary<int, T> _mValues = new ConcurrentDictionary<int, T>();

        public T Value
        {
            get
            {
                T val = null;
                _mValues.TryGetValue(Thread.CurrentThread.ManagedThreadId, out val);
                return val;
            }
            set
            {
                _mValues[Thread.CurrentThread.ManagedThreadId] = value;
            }
        }

        public void Dispose()
        {
            T obj;
            _mValues.TryRemove(Thread.CurrentThread.ManagedThreadId, out obj);
        }
    }
}