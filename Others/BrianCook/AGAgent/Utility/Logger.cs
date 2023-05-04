using GeometryFriends;
using System.Diagnostics;

namespace GeometryFriendsAgents
{
    public class Logger
    {
        public static void Write(string message)
        {
            Debug.WriteLine(message);
            Log.LogInformation(message);
        }
    }
}
