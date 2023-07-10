/// <summary>
/// Parsed commandline args, accessible through properties.
/// </summary>
internal class InputArgs
{
    private string[] _args = { };

    public InputArgs()
    {
        _args = Environment.GetCommandLineArgs();
    }

    public bool Initialize()
    {
        if (_args.Length < 2)
        {
            Log.Error("No arguments provided.");
            Log.Info(GetParametersHelp());
            return false;
        }

        if (TryGetValue("-d", "--dbserver", false, out string dbServer) == false)
        {
            return false;
        }
        DbServer = dbServer;
        if (TryGetValue("-u", "--dbuser", false, out string dbUser) == false)
        {
            return false;
        }
        DbUser = dbUser;
        if (TryGetValue("-p", "--dbpassword", false, out string dbPassword) == false)
        {
            return false;
        }
        DbPassword = dbPassword;

        if (TryGetValue("-s", "--solarhost", false, out string solarHost) == false)
        {
            return false;
        }
        SolarHost = solarHost;

        if (TryGetValue("-v", "--solaruser", false, out string solaruser) == false)
        {
            return false;
        }
        SolarUser = solaruser;

        if (TryGetValue("-x", "--solarpassword", false, out string solarPassword) == false)
        {
            return false;
        }
        SolarPassword = solarPassword;

        if (TryGetValue("-n", "--interval", true, out string interval))
        {
            if (int.TryParse(interval, out int i) == false)
            {
                Log.Error($"Cannot parse interval '{interval}' as integer.");
                return false;
            }

            StoreToDbInterval = TimeSpan.FromMinutes(i);
        }

        if (TryGetValue("-l", "--loglevel", true, out string logLevel))
        {
            if (Enum.TryParse<Log.Level>(logLevel, true, out Log.Level level))
            {
                LogLevel = level;
                Log.ActiveLevel = level;
            }
            else
            {
                Log.Info("Logging was deactivated. No more log messages from now on.");
                Log.ActiveLevel = Log.Level.N;
            }
        }

        if (TryGetValue("-f", "--logFile", true, out string logFile))
        {
            LogDirectory = logFile;
        }

        return true;
    }

    public string GetParametersHelp()
    {
        return Environment.NewLine +
               $"{Version.ProgramVersion}" + Environment.NewLine + Environment.NewLine +
               "Usage:" + Environment.NewLine +
               "-d, --dbserver: Server address of MySQL database, e.g.: 127.0.0.1" + Environment.NewLine +
               "-u, --dbuser: user for MySQL database, e.g.: user" + Environment.NewLine +
               "-p, --dbpassword: password for database user, e.g. ********" + Environment.NewLine +
               "-n, --interval: interval in minutes to fetch and store to DB, default: 5" + Environment.NewLine +
               "-s, --solarhost: hostname to solar host, e.g.: 192.68.178.47" + Environment.NewLine +
               "-v, --solaruser: username for solarhost, e.g.: admin" + Environment.NewLine +
               "-x, --solarpassword: password for username for solarhost, e.g.: admin" + Environment.NewLine +
               "-f, --logdir: log directory. if none is provied or not able to write, logs to console" + Environment.NewLine;
    }

    public string GetFullHelp()
    {
        return GetParametersHelp();
    }

    public bool TryGetValue(string argName, string argNameFull, bool optional, out string value)
    {
        value = string.Empty;

        try
        {
            int argNameIndex = -1;

            bool hasArgName = _args.Where((a, i) =>
                {
                    if (string.Equals(a, argName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a, argNameFull, StringComparison.OrdinalIgnoreCase))
                    {
                        argNameIndex = i;
                        return true;
                    }
                    return false;
                }
            )
            .Any();

            if (!hasArgName)
            {
                if (!optional)
                {
                    Log.Error($"Argument {argName} not provided.");
                }
                return false;
            }

            if (_args.Length < argNameIndex + 2)
            {
                Log.Error($"Cannot parse input args: No value given for '{argName}'");
                return false;
            }

            value = _args[argNameIndex + 1];

            if (value.StartsWith('-'))
            {
                Log.Error($"Cannot parse input args: No value given for '{argName}'");
                return false;
            }

            return true;
        }
        catch (System.Exception ex)
        {
            Log.Exception(ex);
            value = string.Empty;
            return false;
        }
    }

    public string DbServer { get; private set; } = string.Empty;
    public string DbUser { get; private set; } = string.Empty;
    public string DbPassword { get; private set; } = string.Empty;
    public string SolarHost { get; private set; } = string.Empty;
    public string SolarUser { get; private set; } = string.Empty;
    public string SolarPassword { get; private set; } = string.Empty;
    public TimeSpan StoreToDbInterval { get; private set; } = TimeSpan.FromMinutes(5);
    public Log.Level LogLevel { get; private set; } = Log.Level.I;
    public string LogDirectory { get; private set; } = string.Empty;
}