using System.Net.Http;
using System.Threading.Tasks;
using NetworkDetector.Services.Interfaces;
using Newtonsoft.Json.Linq;

namespace NetworkDetector.Services.Implementations
{
    public class NetworkInfoService : INetworkInfoService
    {
        private static readonly HttpClient _client = new HttpClient();

        public async Task<(string Ip, string Isp, bool Mobile)> GetWanIpAndIspAsync()
        {
            // Copy‐over your existing code that calls ip‑api.com
            var url = "https://pro.ip-api.com/json/?key=YOUR_KEY";
            var json = await _client.GetStringAsync(url);
            var obj = JObject.Parse(json);
            string ip = (string)obj["query"];
            string isp = (string)obj["isp"];
            bool mobile = (bool)obj["mobile"];
            return (ip, isp, mobile);
        }
    }
}
