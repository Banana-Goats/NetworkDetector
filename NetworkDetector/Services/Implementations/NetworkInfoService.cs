using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;
using NetworkDetector.Services.Interfaces;
using Newtonsoft.Json.Linq;

namespace NetworkDetector.Services.Implementations
{
    public class NetworkInfoService : INetworkInfoService
    {
        private static readonly HttpClient _client = new HttpClient();

        private readonly DatabaseService _db;
        private bool _initialized;

        private string _baseUrl;
        private string _apiKey;
        private string _apiFields;

        public NetworkInfoService(DatabaseService db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task InitializeAsync()
        {
            const string sql = @"
                SELECT Config, Value
                  FROM Config.AppConfigs
                 WHERE Application = @appName
                   AND Config IN ('IpApiBaseUrl','IpApiKey','IpApiFields')";

            var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object>
            {
                ["@appName"] = "Network Detector"
            }).ConfigureAwait(false);

            foreach (DataRow row in dt.Rows)
            {
                var name = row.Field<string>("Config");
                var value = row.Field<string>("Value");

                switch (name)
                {
                    case "IpApiBaseUrl":
                        _baseUrl = value;
                        break;
                    case "IpApiKey":
                        _apiKey = value;
                        break;
                    case "IpApiFields":
                        _apiFields = value;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(_baseUrl)
             || string.IsNullOrWhiteSpace(_apiKey)
             || string.IsNullOrWhiteSpace(_apiFields))
            {
                throw new InvalidOperationException(
                    "Missing one or more IP-API settings for 'Network Detector' in database");
            }

            _initialized = true;
        }

        public async Task<(string Ip, string Isp, bool Mobile)> GetWanIpAndIspAsync()
        {
            if (!_initialized)
                throw new InvalidOperationException(
                    "NetworkInfoService not initialized. Call InitializeAsync() before use.");

            try
            {
                var url = $"{_baseUrl}?key={_apiKey}&fields={_apiFields}";
                var response = await _client.GetStringAsync(url).ConfigureAwait(false);
                var json = JObject.Parse(response);

                var ip = json["query"]?.ToString();
                var isp = json["isp"]?.ToString();
                var mobile = json["mobile"]?.ToObject<bool>() ?? false;

                return (ip, isp, mobile);
            }
            catch (Exception)
            {
                // consider logging the exception
                return (null, null, false);
            }
        }
    }
}
