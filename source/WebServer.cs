internal class WebServer
{
    private Thread _webServerThread;
    private DataManager _dataManager;

    public WebServer(DataManager wattFetcher)
    {
        _dataManager = wattFetcher;
        _webServerThread = new(() => StartWebServerAndWait());
        _webServerThread.IsBackground = true;
    }

    public void Start()
    {
        _webServerThread.Start();
    }

    private void StartWebServerAndWait()
    {
        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var app = builder.Build();

            app.MapGet("/", (HttpContext c) =>
            {
                c.Response.Headers.Add("Content-Type", "text/html");

                return GetResultHtml();
            });

            app.Run();
        }
        catch (System.Exception ex)
        {
            Log.Exception(ex);
        }
    }

    private string GetResultHtml()
    {
        return @$"
            <html>
            <head>
                <title>Solar sun600g3-eu-230</title>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        background-color: #f5f5f5;
                        margin: 0;
                        padding: 20px;
                    }}

                    h1 {{
                        color: #333;
                        text-decoration-line: underline;
                    }}

                    .currentWatt {{
                        font-size: 30px;
                        margin-bottom: 20px;
                    }}

                    .energy-stats {{
                        margin-bottom: 20px;
                    }}

                    .energy-stats p {{
                        margin: 0;
                        font-size: 18px;
                    }}

                    .energy-stats strong {{
                        color: #333;
                    }}

                    .technical {{
                        font-size: 8px;
                        color: #333;
                    }}
                </style>
            </head>
            <body>
                <h1>Solaranlage sun600g3-eu-230</h1>

                <div class=""currentWatt"">
                    <p>Jetzt: <strong>{_dataManager.LastSelectedWatt.Watt?.ToString() ?? "??"} W</strong> ({_dataManager.LastSelectedWatt.Timestamp?.ToString("HH:mm:ss") ?? "??"})</p>
                </div>

                <div class=""energy-stats"">
                    <p>Insgesamt: <strong>{_dataManager.TotalKwh?.ToString("0.0") ?? "??"} kWh</strong></p>
                    <p>Profit: <strong>{_dataManager.ProfitEuro.ToString("0.00")} &euro;</strong></p>
                    <p>Max: <strong>{_dataManager.LastSelectedMaxWatt.Watt.ToString() ?? "??"} W</strong> ({_dataManager.LastSelectedMaxWatt.Timestamp?.ToString("ddd dd.MM.yyyy HH:mm:ss", new System.Globalization.CultureInfo("de-DE")) ?? "??"})</p>
                </div>
                " +
                string.Join(Environment.NewLine, _dataManager.Plots.Select(
                    i => @$"<img src=""data:image/svg+xml;base64,{i}""/>")) +
        @" 
        </body>
        </html>";
    }
}