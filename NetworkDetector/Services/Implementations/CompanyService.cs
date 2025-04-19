using NetworkDetector.Services.Interfaces;

namespace NetworkDetector.Services.Implementations
{
    public class CompanyService : ICompanyService
    {
        public async Task<string> GetCompanyNameAsync(string machineName)
        {
            string companyName = "Unknown Company";

            try
            {
                var dbService = new DatabaseService();
                string query = "SELECT CompanyName FROM computers WHERE machinename = @MachineName";
                var parameters = new Dictionary<string, object>
                {
                    { "@MachineName", machineName }
                };

                var result = await dbService.ExecuteScalarAsync(query, parameters);
                if (result != null)
                {
                    companyName = result.ToString();
                }
            }
            catch (Exception ex)
            {

            }

            return companyName;
        }
    }
}
