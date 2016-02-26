using System;
using System.Collections;
using System.Collections.Generic;

namespace PureCat.Context
{
    public class CatContext : IEnumerable<KeyValuePair<string, string>>
    {
        private Dictionary<string, string> _dict = new Dictionary<string, string>();

        private const string _catRootId = "X-Cat-RootId";
        private const string _catParentId = "X-Cat-ParentId";
        private const string _catChildId = "X-Cat-Id";

        public string CatRootId { get { return this[_catRootId]; } set { this[_catRootId] = value; } }
        public string CatParentId { get { return this[_catParentId]; } set { this[_catParentId] = value; } }
        public string CatChildId { get { return this[_catChildId]; } set { this[_catChildId] = value; } }
        public string ContextName { get; private set; }

        public CatContext(string contextName = null)
        {
            ContextName = contextName ?? Environment.MachineName;
        }

        public string this[string key]
        {
            get { return _dict.ContainsKey(key) ? _dict[key] : null; }
            set { _dict[key] = value; }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (var item in _dict)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
