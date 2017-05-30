using System;

namespace PureCat.Util
{
    public static class AppEnv
    {
        public static string IP => NetworkInterfaceManager.GetLocalHostAddress();

        public static string MachineName => Environment.MachineName;
    }
}
