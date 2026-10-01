using Models;

namespace YouTrackData
{
    public class BurndownCalculator
    {
        public BurndownData CalculateBurndown(Sprint sprint, Issue[] issues)
        { 
            var startDate = DateTimeOffset.FromUnixTimeMilliseconds(sprint.Start).DateTime;
            var finishDate = DateTimeOffset.FromUnixTimeMilliseconds(sprint.Finish).DateTime;
    
            int totalStoryPoints = 0;
            foreach (var issue in issues)
            {
                totalStoryPoints += issue.StoryPoints;
            }

            int totalDays = (finishDate - startDate).Days;
            double totalPoints = totalStoryPoints;

            int currentDays = (DateTime.Now - startDate).Days;

            double[] progressLine = new double[currentDays + 1];
            progressLine[0] = totalStoryPoints;

            // Calculate the progress line based on resolved issues
            for (int day = 1; day <= currentDays; day++)
            {
                DateTime dayDate = startDate.AddDays(day);

                double doneSum = issues
                    .Where(iss => iss.Resolved.HasValue &&
                           DateTimeOffset.FromUnixTimeMilliseconds(iss.Resolved.Value).DateTime <= dayDate)
                    .Sum(iss => iss.StoryPoints);

                progressLine[day] = totalStoryPoints - doneSum;
            }

            double[] yDays = Enumerable.Range(0, currentDays + 1).Select(i => (double)i).ToArray();
            double[] xDays = Enumerable.Range(0, totalDays + 1).Select(i => (double)i).ToArray();
            double[] idealLine = new double[totalDays + 1];

            for (int i = 0; i <= totalDays; i++)
            {
                idealLine[i] = totalPoints - (totalPoints / totalDays) * i;
            }
            return new BurndownData
            {
                XDays = xDays,
                YDays = yDays,
                IdealLine = idealLine,
                ProgressLine = progressLine
            };
        }
    }
}