using System;

namespace PureCat.Configuration
{
    /// <summary>
    ///   描述当前系统的情况
    /// </summary>
    public class Domain
    {
        public Domain(string id = null, bool enabled = true, int maxQueueSize = 100000, int pool = 1)
        {
            Id = string.IsNullOrWhiteSpace(id) ? "Unknown" : id;
            Enabled = enabled;
            MaxQueueSize = maxQueueSize > 100000 || maxQueueSize < 1000 ? 100000 : maxQueueSize;
            pool = pool < 1 ? 1 : pool;
            pool = pool > Environment.ProcessorCount ? Environment.ProcessorCount : pool;
            ThreadPool = pool;
        }

        /// <summary>
        /// 当前系统的标识
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Cat日志是否开启，默认关闭
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Cat本地消息队列最大长度，最大10W最小1000
        /// </summary>
        public int MaxQueueSize { get; set; }

        /// <summary>
        /// Cat发送消息线程数，最小1最大为cpu核数
        /// </summary>
        public int ThreadPool { get; set; }
    }
}