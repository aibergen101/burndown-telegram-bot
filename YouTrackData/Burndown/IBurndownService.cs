namespace YouTrackData
{
    public interface IBurndownService
    {
        Task<BurndownData> GetCurrentBurndown();
    }
}