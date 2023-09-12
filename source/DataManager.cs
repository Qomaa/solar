using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot;

/// <summary>
/// Responsible for obtaining data from the Solar-Viech and holding the data in memory accessible through properties.
/// </summary>
internal class DataManager
{
    public const float PRICE_PER_KWH = 41.37F;

    private Thread _fetchAndStoreThread;

    public DataManager()
    {
        _fetchAndStoreThread = new Thread(() => FetchAndStoreLoop());
    }

    public bool StartThread()
    {
        _fetchAndStoreThread.IsBackground = true;
        _fetchAndStoreThread.Start();

        return true;
    }

    private void FetchAndStoreLoop()
    {
        while (true)
        {
            try
            {
                string? fullResult = GetSolarFullResult();
                int? watt = GetCurrentWatt(fullResult);
                if (double.TryParse(GetVariableValue(fullResult?.Split(Environment.NewLine), "var webdata_total_e"),
                                 System.Globalization.CultureInfo.InvariantCulture,
                                 out double totalkwh))
                {
                    TotalKwh = totalkwh;
                }

                if (watt.HasValue)
                {
                    Database.InsertWatt(watt.Value);
                }

                LastSelectedWatt = Database.SelectLastWatt();
                LastSelectedMaxWatt = Database.SelectMaxWatt();
                ProfitEuro = TotalKwh.HasValue ? TotalKwh.Value * PRICE_PER_KWH / 100 : 0;

                Plots.Clear();
                CreatePlot("24h", Database.SelectWatts(TimeSpan.FromDays(1)), "HH:mm", DateTimeIntervalType.Hours);
                CreatePlot("5 Tage", Database.SelectWatts(TimeSpan.FromDays(5)), "ddd dd.MM.", DateTimeIntervalType.Days);
                CreatePlot("Durchschnitt pro Tag", Database.SelectAverageWattPerDay(), "ddd dd.MM.", DateTimeIntervalType.Days);
                CreatePlot("Alle Tage", Database.SelectWatts(), "ddd dd.MM.", DateTimeIntervalType.Days);
                // CreatePlot("Erster Wert", Database.SelectFirstWattTimePerDay()
                //                                   .Select(d => (d.Date, DateTimeAxis.ToDouble(d.FirstWattTime)))
                //                                   .ToList()
                //                                   , "", DateTimeIntervalType.Days);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            Thread.Sleep(Program.InputArgs.StoreToDbInterval);
        }
    }

    private void CreatePlot(
        string title,
        List<(DateTime Timestamp, double Y)> data,
        string xAxisStringFormat,
        DateTimeIntervalType xAxisIntervalType)
    {
        LineSeries series = new();
        series.Color = OxyColors.Blue;
        // series.StrokeThickness = 1;
        series.MarkerFill = OxyColors.Blue;
        series.MarkerType = MarkerType.Circle;

        series.Points.AddRange(
            data.Select(d => new DataPoint(DateTimeAxis.ToDouble(d.Timestamp), d.Y))
                .ToArray()
        );

        DateTimeAxis d = new();
        d.Minimum = DateTimeAxis.ToDouble(data.Min(d => d.Timestamp));
        d.Maximum = DateTimeAxis.ToDouble(data.Max(d => d.Timestamp));
        d.StringFormat = xAxisStringFormat;
        d.IntervalType = xAxisIntervalType;
        d.IntervalLength = 100;
        d.FontSize = 20;
        d.TickStyle = TickStyle.Outside;
        d.AxisTickToLabelDistance = 30;
        d.MajorTickSize = 15;

        LinearAxis l = new();
        l.Minimum = data.Min(d => d.Y);
        l.Maximum = data.Max(d => d.Y);
        l.Title = "Watt";
        l.FontSize = 20;
        l.TitleFontSize = 20;
        l.TickStyle = TickStyle.Outside;

        PlotModel plot = new();
        plot.Title = title;
        plot.Axes.Add(d);
        plot.Axes.Add(l);
        plot.Series.Add(series);
        // plot.Background = OxyColors.White;

        using MemoryStream ms = new();

        SvgExporter exporter = new() { Width = 1280, Height = 1024 };
        exporter.Export(plot, ms);

        string base64Plot = Convert.ToBase64String(ms.ToArray());
        Plots.Add(base64Plot);
    }

    private static string? GetSolarFullResult()
    {
        try
        {
            string url = $"http://{Program.InputArgs.SolarHost}";
            string base64Authentication = Convert.ToBase64String(
                System.Text.ASCIIEncoding.UTF8.GetBytes($"{Program.InputArgs.SolarUser}:{Program.InputArgs.SolarPassword}"));

            HttpClient s = new();
            s.BaseAddress = new Uri(url);
            s.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Authentication);

            string? fullResult = s.GetStringAsync("status.html").Result;

            return fullResult;
        }
        catch (System.Exception ex)
        {
            Log.Trace(ex.Message); //Trace, weil der WebServer sich ab und zu verabschiedet
            return null;
        }
    }

    private static int? GetCurrentWatt(string? solarResult)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(solarResult))
            {
                return null;
            }

            string[] lines = solarResult.Split(Environment.NewLine);

            string? current = GetVariableValue(lines, "var webdata_now_p");

            if (!int.TryParse(current, out int wattNumber))
            {
                return null;
            };

            return wattNumber;

        }
        catch (Exception ex)
        {
            Log.Trace(ex.Message);
            return null;
        }
    }

    private static string? GetVariableValue(string[]? lines, string variable)
    {
        if (lines is null)
        {
            return null;
        }

        string? line = lines.FirstOrDefault(l => l.StartsWith(variable));
        string[]? parts = line?.Split('"');
        string? result = parts?[parts.Length - 2];

        return result;
    }

    public double? TotalKwh { get; private set; }

    public (int? Watt, DateTime? Timestamp) LastSelectedWatt { get; private set; }

    public (int? Watt, DateTime? Timestamp) LastSelectedMaxWatt { get; private set; }

    public List<string> Plots { get; private set; } = new();

    public double ProfitEuro { get; private set; }
}