using System.Threading.Tasks;

namespace NetworkDetector.Services.Interfaces
{
    public interface INetworkInfoService
    {
        Task InitializeAsync();
        Task<(string Ip, string Isp, bool Mobile)> GetWanIpAndIspAsync();
    }
}
