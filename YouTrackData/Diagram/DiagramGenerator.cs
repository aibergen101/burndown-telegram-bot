namespace YouTrackData
{
    public class DiagramGenerator : IDiagramGenerator
    {
        public byte[] GenerateDiagram(BurndownData burndownData)
        {
            ScottPlot.Plot myPlot = new();
            myPlot.Axes.DateTimeTicksBottom();

            myPlot.Axes.Bottom.TickGenerator =
                new ScottPlot.TickGenerators.DateTimeFixedInterval(
                    new ScottPlot.TickGenerators.TimeUnits.Day(),
                    1);
            var bg = ScottPlot.Color.FromHex("#1b1b1b");
            var fg = ScottPlot.Color.FromHex("#d0d0d0");
            myPlot.FigureBackground.Color = bg;
            myPlot.DataBackground.Color = bg;
            myPlot.Axes.Color(fg);
            myPlot.Axes.Title.Label.ForeColor = fg;
            myPlot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#444444");
            myPlot.Legend.BackgroundColor = bg;
            myPlot.Legend.FontColor = fg;

            myPlot.XLabel("Срок спринта");
            myPlot.YLabel("Оставшиеся задачи");

            var ideal = myPlot.Add.Scatter(burndownData.XDays, burndownData.IdealLine);
            ideal.Color = ScottPlot.Color.FromHex("#8db600");
            ideal.LineWidth = 2;
            ideal.MarkerSize = 0;
            ideal.LegendText = "Ideal Burndown";

            var remaining = myPlot.Add.Scatter(burndownData.YDays, burndownData.ProgressLine);
            remaining.Color = ScottPlot.Color.FromHex("#29b6f6");
            remaining.LineWidth = 2;
            remaining.MarkerSize = 0;
            remaining.LegendText = "Remaining Effort";

            myPlot.ShowLegend(ScottPlot.Alignment.UpperRight);
            myPlot.Axes.SetLimitsX(
                burndownData.XDays.Min(),
                burndownData.XDays.Max());
            double maxPoints = burndownData.IdealLine[0];

            return myPlot.GetImageBytes(1200, 500, ScottPlot.ImageFormat.Png);
        }
    }
}