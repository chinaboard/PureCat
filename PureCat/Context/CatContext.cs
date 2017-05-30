using System;
using System.Collections.Generic;

namespace PureCat.Context
{
    public class CatContext
    {
        private readonly Dictionary<string, string> _dict = new Dictionary<string, string>();

        private const string _catRootId = "X-Cat-RootId";
        private const string _catParentId = "X-Cat-ParentId";
        private const string _catChildId = "X-Cat-Id";

        public string CatRootId { get { return this[_catRootId]; } set { this[_catRootId] = value; } }
        public string CatParentId { get { return this[_catParentId]; } set { this[_catParentId] = value; } }
        public string CatChildId { get { return this[_catChildId]; } set { this[_catChildId] = value; } }
        public string ContextName { get; set; }

        public CatContext(string contextName = null)
        {
            ContextName = contextName ?? Environment.MachineName;
        }

        public string this[string key]
        {
            get
            {
                _dict.TryGetValue(key, out string result);
                return result;
            }
            set
            {
                _dict[key] = value;
            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, string>>)_dict).GetEnumerator();
        }

        public override string ToString()
        {
            return $"{nameof(ContextName)}:{ContextName}\r\n{nameof(CatRootId)}:{CatRootId}\r\n{nameof(CatParentId)}:{CatParentId}\r\n{nameof(CatChildId)}:{CatChildId}";
        }
    }
}
