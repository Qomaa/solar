internal class WattFetcher
{
    private Thread _fetchAndStoreThread;

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
                    LastSelectedWatt = Database.SelectLastWatt();
                }

                LastSelectedMaxWatt = Database.SelectMaxWatt();

                //MinTimestamp = Database.SelectMinTimestamp();
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex);
            }

            Thread.Sleep(Program.InputArgs.StoreToDbInterval);
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
            Log.Trace(ex.Message); //Trace, weil der WEbServer sich ab und zu verabschiedet
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