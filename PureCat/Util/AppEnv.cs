namespace PureCat.Util
{
    public static class AppEnv
    {
        public static string IP { get { return GetLocalIP(); } }

        private static string GetLocalIP()
        {
            try
            {
                return NetworkInterfaceManager.GetLocalHostAddress();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
