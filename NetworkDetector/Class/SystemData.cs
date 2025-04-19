public class SystemData
{
    // Static info
    public string MachineName { get; set; }
    public string CPU { get; set; }
    public string Ram { get; set; }
    public string WindowsOS { get; set; }
    public string BuildNumber { get; set; }
    public string CompanyName { get; set; }

    // Dynamic info
    public string WanIp { get; set; }
    public string Isp { get; set; }
    public string StorageInfo { get; set; }

    // Additional fields
    public string LatestSharePointFileDate { get; set; }
    public string PendingUpdates { get; set; }

    // You can add more properties as needed, for example:
    public string Location { get; set; }

    public bool Mobile { get; set; }


    private static readonly HttpClient _client = new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    public static HttpClient Client => _client;

}