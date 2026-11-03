using TelegramClient;

namespace YouTrackData
{
    public class DiagramGenerator : IDiagramGenerator
    {
        public byte[] GenerateDiagram(BurndownData burndownData, ChartTheme theme)
        {
            ScottPlot.Plot myPlot = new();
            myPlot.Axes.DateTimeTicksBottom();

            myPlot.Axes.Bottom.TickGenerator =
                new ScottPlot.TickGenerators.DateTimeFixedInterval(
                    new ScottPlot.TickGenerators.TimeUnits.Day(),
                    1);
            ScottPlot.Color bg, fg, grid, idealColor, remainingColor;

            if (theme == ChartTheme.Dark)
            {
                bg = ScottPlot.Color.FromHex("#1b1b1b");
                fg = ScottPlot.Color.FromHex("#d0d0d0");
                grid = ScottPlot.Color.FromHex("#444444");
                idealColor = ScottPlot.Color.FromHex("#8db600");
                remainingColor = ScottPlot.Color.FromHex("#29b6f6");
            }
            else
            {
                bg = ScottPlot.Color.FromHex("#ffffff");
                fg = ScottPlot.Color.FromHex("#222222");
                grid = ScottPlot.Color.FromHex("#d9d9d9");
                idealColor = ScottPlot.Color.FromHex("#5f8a00");    
                remainingColor = ScottPlot.Color.FromHex("#0277bd");
            }

            myPlot.FigureBackground.Color = bg;
            myPlot.DataBackground.Color = bg;
            myPlot.Axes.Color(fg);
            myPlot.Axes.Title.Label.ForeColor = fg;
            myPlot.Grid.MajorLineColor = grid;
            myPlot.Legend.BackgroundColor = bg;
            myPlot.Legend.FontColor = fg;

            myPlot.XLabel("День спринта");
            myPlot.YLabel("Оставшиеся задачи");

            var ideal = myPlot.Add.Scatter(burndownData.XDays, burndownData.IdealLine);
            ideal.Color = idealColor;
            ideal.LineWidth = 2;
            ideal.MarkerSize = 0;
            ideal.LegendText = "Ideal Burndown";

            var remaining = myPlot.Add.Scatter(burndownData.YDays, burndownData.ProgressLine);
            remaining.Color = remainingColor;
            remaining.LineWidth = 2;
            remaining.MarkerSize = 0;
            remaining.LegendText = "Remaining Effort";

            myPlot.ShowLegend(ScottPlot.Alignment.UpperRight);
            myPlot.Axes.SetLimitsX(
                burndownData.XDays.Min(),
                burndownData.XDays.Max());

            return myPlot.GetImageBytes(1200, 500, ScottPlot.ImageFormat.Png);
        }
    }
}