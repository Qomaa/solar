using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot;

internal class WattFetcher
{
    private Thread _fetchAndStoreThread;

    /// <summary>
    /// WORKAROUND: File brauchen wir nicht vermutlich.... einfach direkt base64 im Speicher halten alter
    /// </summary>
    /// <returns></returns>
    public static string ImagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.svg");

    public WattFetcher()
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

                CreatePlot();
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex);
            }

            Thread.Sleep(Program.InputArgs.StoreToDbInterval);
        }
    }

    private static void CreatePlot()
    {
        // Daten der dieser Woche holen
        var data = Database.SelectWatts(TimeSpan.FromDays(5));
        
        LineSeries series = new();
        series.Color = OxyColors.Blue;
        // series.StrokeThickness = 1;
        series.MarkerFill = OxyColors.Blue;
        series.MarkerType = MarkerType.Circle;
        
        series.Points.AddRange(data.Select(d =>
        {
            DataPoint point = new(DateTimeAxis.ToDouble(d.Timestamp), (double)d.Watt);
            return point;
        })
        .ToList());

        DateTimeAxis d = new();
        d.Minimum = DateTimeAxis.ToDouble(data.Min(d => d.Timestamp));
        d.Maximum = DateTimeAxis.ToDouble(data.Max(d => d.Timestamp));
        d.StringFormat = "ddd dd.MM.";
        d.IntervalType = DateTimeIntervalType.Days;
        d.IntervalLength = 100;
        d.FontSize = 20;
        d.TickStyle = TickStyle.Outside;
        d.AxisTickToLabelDistance = 30;
        d.MajorTickSize = 15;

        LinearAxis l = new();
        l.Minimum = data.Min(d => d.Watt);
        l.Maximum = data.Max(d => d.Watt);
        l.Title = "Watt";
        l.FontSize = 20;
        l.TitleFontSize = 20;
        l.TickStyle = TickStyle.Outside;
        
        PlotModel plot = new();

        plot.Axes.Add(d);
        plot.Axes.Add(l);
        plot.Series.Add(series);
        // plot.Background = OxyColors.White;

        using (var stream = File.Create(ImagePath))
        {
            var exporter = new OxyPlot.SvgExporter { Width = 1280, Height = 1024 };
            exporter.Export(plot, stream);
        }
    }

    private static string? GetSolarFullResult()
    {
        try
        {
            string url = $"http://{Program.InputArgs.SolarHost}";
            string base64Authentication = Convert.ToBase64String(
                System.Text.ASCIIEncoding.UTF8.GetBytes($"{Program.InputArgs.SolarUser}:{Program.InputArgs.SolarPassword}"));

            HttpClient s = new HttpClient();
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
        catch (System.Exception ex)
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

    public DateTime? MinTimestamp { get; private set; }
}