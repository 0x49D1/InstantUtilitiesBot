using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace utilitiesBotDotCore.Utilities
{
    public static class Utilities
    {
        public async static Task<string> GetExternalIp()
        {
           var externalip = await new HttpClient().GetAsync("https://ipinfo.io/ip");
            return await externalip.Content.ReadAsStringAsync();
        }
    }
}
