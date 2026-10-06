using Models;

namespace YouTrackData
{
    public interface IBurndownCalculator
    {
        BurndownData CalculateBurndown(Sprint sprint, Issue[] issues);
    }
}