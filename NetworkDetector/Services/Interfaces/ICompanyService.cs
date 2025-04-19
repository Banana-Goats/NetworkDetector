namespace NetworkDetector.Services.Interfaces;

public interface ICompanyService
{
    Task<string> GetCompanyNameAsync(string machineName);
}