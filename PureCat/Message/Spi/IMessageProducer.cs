using System;

namespace PureCat.Message.Spi
{
    public interface IMessageProducer
    {
        string CreateMessageId();

        ///<summary>
        ///  Log an error.
        ///</summary>
        ///<param name="cause"> root cause exception </param>
        void LogError(Exception cause);

        ///<summary>
        ///  Log an event in one shot.
        ///</summary>
        ///<param name="type"> event type </param>
        ///<param name="name"> event name </param>
        ///<param name="status"> PureCatConstants.SUCCESS or "0" means success, otherwise means error code </param>
        ///<param name="nameValuePairs"> name value pairs in the format of "a=1&b=2&..." </param>
        void LogEvent(string type, string name, string status, string nameValuePairs);

        ///<summary>
        ///  Log a heartbeat in one shot.
        ///</summary>
        ///<param name="type"> heartbeat type </param>
        ///<param name="name"> heartbeat name </param>
        ///<param name="status"> PureCatConstants.SUCCESS or "0" means success, otherwise means error code </param>
        ///<param name="nameValuePairs"> name value pairs in the format of "a=1&b=2&..." </param>
        void LogHeartbeat(string type, string name, string status, string nameValuePairs);

        /// <summary>
        /// Log a metric in one shot.
        /// </summary>
        /// <param name="name">metric name</param>
        /// <param name="status">PureCatConstants.SUCCESS or "0" means success, otherwise means error code</param>
        /// <param name="nameValuePairs">name value pairs in the format of "a=1&b=2&..."</param>
        void LogMetric(string name, string status, string nameValuePairs);

        ///<summary>
        ///  Create a new event with given type and name.
        ///</summary>
        ///<param name="type"> event type </param>
        ///<param name="name"> event name </param>
        IEvent NewEvent(string type, string name);

        ///<summary>
        ///  Create a new trace with given type and name.
        ///</summary>
        ///<param name="type"> event type </param>
        ///<param name="name"> event name </param>
        ITrace NewTrace(string type, string name);

        ///<summary>
        ///  Create a new heartbeat with given type and name.
        ///</summary>
        ///<param name="type"> heartbeat type </param>
        ///<param name="name"> heartbeat name </param>
        IHeartbeat NewHeartbeat(string type, string name);

        ///<summary>
        ///  Create a new transaction with given type and name.
        ///</summary>
        ///<param name="type"> transaction type </param>
        ///<param name="name"> transaction name </param>
        ITransaction NewTransaction(string type, string name);

        ///<summary>
        ///  Create a new metric with given type and name.
        ///</summary>
        ///<param name="type"> metric type </param>
        ///<param name="name"> metric name </param>
        IMetric NewMetric(string type, string name);

        /// <summary>
        /// Create a new forkedTransaction with given type and name.
        /// </summary>
        /// <param name="type">forkedTransaction type</param>
        /// <param name="name">forkedTransaction name</param>
        /// <returns></returns>
        IForkedTransaction NewForkedTransaction(string type, string name);

        /// <summary>
        /// Create a new taggedTransaction with given type and name.
        /// </summary>
        /// <param name="type">taggedTransaction type</param>
        /// <param name="name">taggedTransaction name</param>
        /// <param name="tag">taggedTransaction tag</param>
        /// <returns></returns>
        ITaggedTransaction NewTaggedTransaction(string type, string name, string tag);

    }
}