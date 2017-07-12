using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace PureCat.Demo
{
    public static class SQLDemo
    {
        private static readonly Random _rand = new Random();

        public static void Demo()
        {
            var sql = "select * from table";
            PureCatClient.DoTransaction("SQL", "SQLName", () =>
            {
                using (var conn = new SqlConnection("connString"))
                {
                    conn.Open();
                    LogEvent(conn, sql, null);

                    //do sql....
                    Thread.Sleep(_rand.Next(2000));
                }
            });


            //格式就是
            //T SQL,name
            //E SQL.Method
            //E SQL.Database
            //t SQL,name

        }

        private static void LogEvent(DbConnection conn, string sql, dynamic entity)
        {
            sql = sql.Trim();
            var method = sql.Substring(0, sql.IndexOf(' '));

            PureCatClient.LogEvent("SQL.Method", method);
            //以Mysql为例
            PureCatClient.LogEvent("SQL.Database", $"jdbc:mysql://{conn.DataSource}/{conn.Database}?useUnicode=true&autoReconnect=true");

            //可以记录完整带参数的sql
            var sqlFull = $"{sql}\r\n{GetNameAndValue(entity)}";

            //最好是对sql或者sqlFull进行hash
            var sqlHash = Md5(sql);
            PureCatClient.LogEvent("SQL", sqlHash, "0", sqlFull);
        }

        private static string Md5(string str)
        {
            return BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(str))).Replace("-", "").ToLower();
        }

        private static string GetNameAndValue<T>(T tValue)
        {
            var tStr = string.Empty;
            if (tValue == null)
            {
                return tStr;
            }
            var properties = tValue.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (properties.Length <= 0)
            {
                return tStr;
            }
            foreach (var item in properties)
            {
                var name = item.Name; //名称
                var value = item.GetValue(tValue, null);  //值

                if (item.PropertyType.IsValueType || item.PropertyType.Name.StartsWith("String"))
                {
                    tStr += $"{name}:{value}\r\n";
                }
                else
                {
                    tStr += GetNameAndValue(value);
                }
            }
            return tStr;
        }

    }
}
