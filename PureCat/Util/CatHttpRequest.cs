using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PureCat.Util
{
    internal class CatHttpRequest
    {
        public static async Task<string> GetRequest(string url)
        {
            var client = new HttpClient();

            try
            {
                client.Timeout = TimeSpan.FromSeconds(5);

                client.DefaultRequestHeaders.IfNoneMatch.TryParseAdd(Guid.NewGuid().ToString());

                return await client.GetStringAsync(url);
            }
            catch
            {
                return null;
            }
            finally
            {
                client.Dispose();
            }
        }
    }
}