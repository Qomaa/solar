using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;

internal class Program
{
    internal static InputArgs InputArgs { get; private set; } = new InputArgs();
    private static ManualResetEventSlim _quitProgramEvent = new();

    private static void Main(string[] args)
    {
        try
        {
            Log.Info($"{Version.ProgramVersion} started.");

            if (!InputArgs.Initialize())
            {
                return;
            }
            
            if (!Database.CreateDatabaseIfNotExists())
            {
                return;
            }

            DataManager fetcher = new();
            if (!fetcher.StartThread())
            {
                return;
            }

            WebServer webServer = new(fetcher);
            webServer.Start();

            _quitProgramEvent.Wait();
        }
        catch (System.Exception ex)
        {
            Log.Exception(ex);
            Environment.ExitCode = 86;
        }
        finally
        {
            
        }
    }

// private static void WaitForUserInput()
//     {
//         Log.Info("'q': Quit program");
//         Log.Info("'s': Store location statistics");
//         Log.Info("'p': Print named pipe path");

//         ConsoleKeyInfo key = new ConsoleKeyInfo();

//         while (_quitProgramEvent.IsSet == false)
//         {
//             try
//             {
//                 key = Console.ReadKey();
//                 Console.WriteLine();
//             }
//             catch (InvalidOperationException ex)
//             {
//                 Log.Warning(ex.Message);
//                 _quitProgramEvent.Wait();
//             }

//             switch (key.Key)
//             {
//                 case ConsoleKey.Q:
//                     Log.Info("Quitting...");
//                     return;
//                 case ConsoleKey.S:
//                     Log.Info("Storing statistics...");
//                     _captureProcessing?.StoreStatistics(null);
//                     break;
//                 case ConsoleKey.P:
//                     Log.Info($"Named pipe path: {PipePath}");
//                     break;
//                 default:
//                     break;
//             }
//         }
//     }

    

    private static string? GetTechnicalResult(string solarResult)
    {
        var lines = solarResult.Split(Environment.NewLine).Where(l => l.StartsWith("var"));
        return string.Join("<br />", lines);
    }

    private static string? GetSolarFullResult()
    {
        string url = "http://192.168.178.47/status.html";
        string base64Authentication = Convert.ToBase64String(System.Text.ASCIIEncoding.UTF8.GetBytes("admin:admin"));

        HttpClient s = new HttpClient();
        s.BaseAddress = new Uri(url);
        s.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Authentication);

        string? fullResult = s.GetStringAsync("status.html").Result;

        return fullResult;
    }

    private static (string? currentWatt, string? todayKwh, string? totalKwh) GetWatts(string solarResult)
    {
        string[] lines = solarResult.Split(Environment.NewLine);

        string? current = getWatt("var webdata_now_p") + " W";
        string? today = getWatt("var webdata_today_e") + " kWh";
        string? total = getWatt("var webdata_total_e") + " kWh";

        return (current, today, total);

        // return $"Jetzt: {current}" + Environment.NewLine + Environment.NewLine +
        //        $"Heute (stimmt irgendwie nicht): {today}" + Environment.NewLine + Environment.NewLine +
        //        $"Insgesamt: {total}";

        string? getWatt(string variable)
        {
            string? line = lines.FirstOrDefault(l => l.StartsWith(variable));
            string[]? parts = line?.Split('"');
            string? result = parts?[parts.Length - 2];

            return result;
        }
    }
}