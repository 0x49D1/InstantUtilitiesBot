using System.Net;

namespace UtilitiesBot.Utilities
{
    public static class Utilities
    {
        public static string GetExternalIp()
        {
            string externalip = new WebClient().DownloadString("https://ipinfo.io/ip");
            return externalip.Trim();
        }
    }
}
