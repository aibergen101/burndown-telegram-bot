using Models;

namespace YouTrackData
{
    public class BurndownCalculator : IBurndownCalculator
    {
        public BurndownData CalculateBurndown(Sprint sprint, Issue[] issues)
        {
            var startDate = DateTimeOffset.FromUnixTimeMilliseconds(sprint.Start).DateTime;
            var finishDate = DateTimeOffset.FromUnixTimeMilliseconds(sprint.Finish).DateTime;

            int totalStoryPoints = CalculateTotalStoryPoints(issues);

            int currentDays = (DateTime.Now - startDate).Days;
            double[] progressLine = CalculateProgressLine(issues, startDate, totalStoryPoints, currentDays);

            int totalDays = (finishDate - startDate).Days;
            double[] idealLine = CalculateIdealLine(totalStoryPoints, totalDays);

            double[] yDays = Enumerable.Range(0, currentDays + 1).Select(i => (double)i).ToArray();
            double[] xDays = Enumerable.Range(0, totalDays + 1).Select(i => (double)i).ToArray();

            return new BurndownData
            {
                XDays = xDays,
                YDays = yDays,
                IdealLine = idealLine,
                ProgressLine = progressLine
            };
        }

        private static int CalculateTotalStoryPoints(Issue[] issues)
        {
            int totalStoryPoints = 0;
            foreach (var issue in issues)
            {
                totalStoryPoints += issue.StoryPoints;
            }

            return totalStoryPoints;
        }

        private static double[] CalculateIdealLine(int totalStoryPoints, int totalDays)
        {
            double[] idealLine = new double[totalDays + 1];

            for (int i = 0; i <= totalDays; i++)
            {
                idealLine[i] = totalStoryPoints - (totalStoryPoints / totalDays) * i;
            }

            return idealLine;
        }

        private static double[] CalculateProgressLine(Issue[] issues, DateTime startDate, int totalStoryPoints, int currentDays)
        {
            double[] progressLine = new double[currentDays + 1];
            progressLine[0] = totalStoryPoints;

            for (int day = 1; day <= currentDays; day++)
            {
                DateTime dayDate = startDate.AddDays(day);

                double doneSum = issues
                    .Where(iss => iss.Resolved.HasValue &&
                           DateTimeOffset.FromUnixTimeMilliseconds(iss.Resolved.Value).DateTime <= dayDate)
                    .Sum(iss => iss.StoryPoints);

                progressLine[day] = totalStoryPoints - doneSum;
            }

            return progressLine;
        }
    }
}