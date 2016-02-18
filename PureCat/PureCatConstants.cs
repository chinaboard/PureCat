namespace PureCat
{
    public class PureCatConstants
    {

        public const string SUCCESS = "0";

        public const int MAX_LENGTH = 1000;

        public const int MAX_ITEM_LENGTH = 50;

        /**
         * Cat instrument attribute names
         */
        public const string CAT_STATE = "cat-state";

        public const string CAT_PAGE_URI = "cat-page-uri";

        public const string CAT_PAGE_TYPE = "cat-page-type";

        /**
         * Pigeon Transation Type
         */

        public const string TYPE_CALL = "Call";

        public const string TYPE_RESULT = "Result";

        public const string TYPE_SERVICE = "Service";

        public const string TYPE_REMOTE_CALL = "RemoteCall";

        public const string TYPE_REQUEST = "Request";

        public const string TYPE_RESPONSE = "Respone";

        /**
         *  Error Type
         */

        public const string TYPE_ERROR = "Error";

        public const string TYPE_EXCEPTION = "Exception";

        public const string TYPE_RUNTIMEEXCEPTION = "RuntimeException";

        /**
         * Pigeon Event Type, it is used to record the param
         */

        public const string NAME_TIME_OUT = "ClientTimeOut";

        public const string NAME_Cache = "Cache";

        /**
         * Pigeon Context Info
         */
        public const string PIGEON_ROOT_MESSAGE_ID = "RootMessageId";

        public const string PIGEON_PARENT_MESSAGE_ID = "ParentMessageId";

        public const string PIGEON_CHILD_MESSAGE_ID = "ChildMessageId";

        public const string PIGEON_CURRENT_MESSAGE_ID = "CurrentMessageId";

        public const string PIGEON_SERVER_MESSAGE_ID = "ServerMessageId";

        public const string PIGEON_RESPONSE_MESSAGE_ID = "ResponseMessageId";

        public const string TYPE_SQL = "SQL";

        public const string TYPE_SQL_PARAM = "SQL.PARAM";

        public const string TYPE_SQL_METHOD = "SQL.Method";

        public const string TYPE_SQL_DATABASE = "SQL.Database";

        public const string TYPE_URL = "URL";

        public const string TYPE_URL_FORWARD = "URL.Forward";

        public const string TYPE_URL_SERVER = "URL.Server";

        public const string TYPE_URL_METHOD = "URL.Method";

        public const string TYPE_ACTION = "Action";

        public const string TYPE_METRIC = "MetricType";

        public const string TYPE_TRACE = "TraceMode";

        public const int ERROR_COUNT = 100;

        public const int SUCCESS_COUNT = 1000;
    }
}
