using System;
using System.Configuration;        // <— for ConfigurationManager
using System.Net.Http;
using System.Threading.Tasks;
using NetworkDetector.Services.Interfaces;
using Newtonsoft.Json.Linq;

namespace NetworkDetector.Services.Implementations
{
    public class NetworkInfoService : INetworkInfoService
    {
        private static readonly HttpClient _client = new HttpClient();

        // Read these once, at startup
        private readonly string _baseUrl = ConfigurationManager.AppSettings["IpApiBaseUrl"];
        private readonly string _apiKey = ConfigurationManager.AppSettings["IpApiKey"];
        private readonly string _apiFields = ConfigurationManager.AppSettings["IpApiFields"];

        public async Task<(string Ip, string Isp, bool Mobile)> GetWanIpAndIspAsync()
        {
            try
            {
                // Compose URL from config
                var url = $"{_baseUrl}?key={_apiKey}&fields={_apiFields}";
                string response = await _client.GetStringAsync(url);
                var json = JObject.Parse(response);

                string ip = json["query"]?.ToString();
                string isp = json["isp"]?.ToString();
                bool mobile = json["mobile"]?.ToObject<bool>() ?? false;

                return (ip, isp, mobile);
            }
            catch (Exception)
            {
                // You may want to log ex here
                return (null, null, false);
            }
        }
    }
}
