namespace PureCat.Configuration
{
    /// <summary>
    ///   描述当前系统的情况
    /// </summary>
    public class Domain
    {
        private string _id;
        private bool _mEnabled;

        public Domain(string id = null, bool enabled = true)
        {
            _id = string.IsNullOrWhiteSpace(id) ? "Unknown" : id;
            _mEnabled = enabled;
        }

        /// <summary>
        ///   当前系统的标识
        /// </summary>
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        ///   Cat日志是否开启，默认关闭
        /// </summary>
        public bool Enabled
        {
            get { return _mEnabled; }
            set { _mEnabled = value; }
        }
    }
}