using System;

namespace PureCat.Util
{
    ///<summary>
    ///  This timer provides milli-second precise system time.
    ///</summary>
    public class MilliSecondTimer
    {
        public static long CurrentTimeMicros
        {
            get
            {
                // it's millisecond precise
                return DateTime.Now.Ticks / 10L;
            }
        }

        public static long CurrentTimeMillis
        {
            get
            {
                return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            }
        }

        public static long CurrentTimeHoursForJava
        {
            get
            {
                DateTime baseline = new DateTime(1970, 1, 1, 0, 0, 0);
                TimeSpan ts = new TimeSpan(DateTime.UtcNow.Ticks - baseline.Ticks);

                return ((long)ts.TotalMilliseconds / 3600000L);
            }
        }
    }
}