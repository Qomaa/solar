internal class WebServer
{
    private Thread _webServerThread;
    private WattFetcher _wattFetcher;

    public WebServer(WattFetcher wattFetcher)
    {
        _wattFetcher = wattFetcher;
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
                return GetFinalResult();
            });

            app.Run();
        }
        catch (System.Exception ex)
        {
            Log.Exception(ex);
        }
    }

    private string GetFinalResult()
    {
        try
        {
            // string? solarResult = GetSolarFullResult();

            // if (solarResult is null)
            // {
            //     return "Grad keine Daten vorhanden... Voll Schade, aber so ist das Leben eben.";
            // }

            // var watts = GetWatts(solarResult);
            // string? technical = GetTechnicalResult(solarResult);


            return GetHtml();

            //  Environment.NewLine + Environment.NewLine + Environment.NewLine +
            //        $"{watt}" + Environment.NewLine + Environment.NewLine + Environment.NewLine + Environment.NewLine +
            //        $"############################################################################################" + Environment.NewLine +
            //        $"Techische Daten:" + Environment.NewLine + Environment.NewLine +
            //        technical;
        }
        catch (System.Exception ex)
        {
            return "Etwas ging schief..." + Environment.NewLine + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace;
        }
    }

    private string GetHtml()
    {
        return @$"
            <!DOCTYPE html>
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
                    <p>Jetzt: <strong>{_wattFetcher.LastSelectedWatt.Watt?.ToString() ?? "??"} W</strong> ({_wattFetcher.LastSelectedWatt.Timestamp?.ToString("HH:mm:ss") ?? "??"})</p>
                </div>

                <div class=""energy-stats"">
                    <p>Insgesamt: <strong>{_wattFetcher.TotalKwh?.ToString("0.0") ?? "??"} kWh</strong></p>
                </div>

                <div class=""energy-stats"">
                    <p>Max: <strong>{_wattFetcher.LastSelectedMaxWatt.Watt.ToString() ?? "??"} W</strong> ({_wattFetcher.LastSelectedMaxWatt.Timestamp?.ToString("ddd dd.MM.yyyy HH:mm:ss", new System.Globalization.CultureInfo("de-DE")) ?? "??"})</p>
                </div>
            </body>
            </html>";
    }
}