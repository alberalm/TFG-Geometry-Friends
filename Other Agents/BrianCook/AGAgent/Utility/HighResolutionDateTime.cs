using System;
using System.Runtime.InteropServices;

namespace GeometryFriendsAgents
{
    public static class HighResolutionDateTime
    {
        public static bool IsAvailable { get; private set; }

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetSystemTimePreciseAsFileTime(out long filetime);

        public static DateTime Now
        {
            get
            {
                if (!IsAvailable)
                    return DateTime.Now;
                long filetime;
                GetSystemTimePreciseAsFileTime(out filetime);
                return DateTime.FromFileTime(filetime);
            }
        }

        static HighResolutionDateTime()
        {
            try
            {
                long filetime;
                GetSystemTimePreciseAsFileTime(out filetime);
                IsAvailable = true;
            }
            catch (Exception)
            {
                IsAvailable = false;
            }
        }
    }
}
