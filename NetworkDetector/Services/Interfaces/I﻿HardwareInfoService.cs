namespace NetworkDetector.Services.Interfaces
{

    public interface IHardwareInfoService
    {
        string GetCpuInfo();
        string GetRamInfo();
        string GetStorageInfo();

        (string OsName, string BuildNumber) GetOsInfo();
    }
}
