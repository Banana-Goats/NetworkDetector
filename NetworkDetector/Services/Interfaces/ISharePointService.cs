namespace NetworkDetector.Services.Interfaces
{
    public interface ISharePointService
    {
        /// <summary>
        /// Scans the user’s OneDrive/SharePoint folders under their profile
        /// and returns the date of the most recently modified file.
        /// </summary>
        string GetLatestSharePointFileDate();
    }
}