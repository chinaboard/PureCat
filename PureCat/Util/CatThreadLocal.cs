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
                _mValues.TryGetValue(Thread.CurrentThread.ManagedThreadId, out T val);
                return val;
            }
            set
            {
                _mValues[Thread.CurrentThread.ManagedThreadId] = value;
            }
        }

        public void Dispose()
        {
            _mValues.TryRemove(Thread.CurrentThread.ManagedThreadId, out T obj);
        }
    }
}