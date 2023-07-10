using MySql.Data.MySqlClient;


/// <summary>
/// Manages the MariaDB database: Create DB, Create tables, insert and select data, ...
/// </summary>
internal static class Database
{
    private static MySqlConnection _connection = new();
    private const string _databaseName = "solar";

    /// <summary>
    /// No parallel database operations allowed
    /// </summary>
    private readonly static object _lock = new();

    private static bool Connect()
    {
        lock (_lock)
        {
            try
            {
                _connection.ConnectionString = GetConnectionString();
                _connection.Open();
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex);
                return false;
            }

        }

        return true;
    }

    private static void Disconnect()
    {
        lock (_lock)
        {
            _connection.Close();
        }
    }

    // /// <summary>
    // /// Establishes a conncetion to a MariaDB database with the provided <paramref name="connectionString"/>.
    // /// </summary>
    // /// <param name="connectionString"></param>
    // /// <param name="database"></param>
    // private static bool InitDatabase(out Database database)
    // {
    //     database = new Database();

    //     bool success = database.Connect();

    //     return success;
    // }

    // public void Shutdown()
    // {
    //     _connection.Dispose();
    // }

    /// <summary>
    /// Erzeugt die Datenbank "solar" und alle benötigten Tabellen.
    /// </summary>
    /// <returns></returns>
    public static bool CreateDatabaseIfNotExists()
    {
        lock (_lock)
        {
            try
            {
                if (!Connect())
                {
                    return false;
                }

                if (ExecuteNonQuery($"CREATE DATABASE IF NOT EXISTS {_databaseName};", out _) == false)
                {
                    return false;
                }

                if (!ExecuteNonQuery($"USE {_databaseName};", out _))
                {
                    return false;
                }

                if (ExecuteNonQuery(
                    "CREATE TABLE IF NOT EXISTS `solardata` (" +
                    "`ID` INT(11) NOT NULL AUTO_INCREMENT," +
                    "`watt` INT(11) NULL DEFAULT NULL," +
                    "`timestamp` TIMESTAMP NOT NULL DEFAULT utc_timestamp()," +
                    "PRIMARY KEY (`ID`) USING BTREE" +
                    ") ENGINE = 'InnoDB' AUTO_INCREMENT = 1;", out _) == false)
                {
                    return false;
                };

            }
            finally
            {
                Disconnect();
            }
        }

        return true;
    }

    private static MySqlDataReader? ExecuteQuery(string query, params MySqlParameter[] parameters)
    {
        lock (_lock)
        {
            try
            {
                using MySqlCommand command = _connection.CreateCommand();
                command.CommandText = query;
                command.Parameters.AddRange(parameters);
                return command.ExecuteReader();
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex, query);
                return null;
            }
        }
    }

    private static bool ExecuteNonQuery(string commandText, out long lastId, params MySqlParameter[] parameters)
    {
        lock (_lock)
        {
            try
            {
                Connect();

                using MySqlCommand command = _connection.CreateCommand();
                command.CommandText = commandText;
                command.Parameters.AddRange(parameters);
                command.ExecuteNonQuery();
                lastId = command.LastInsertedId;
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex, commandText);
                lastId = -1;
                return false;
            }
            finally
            {
                Disconnect();
            }
        }

        return true;
    }

    public static bool InsertWatt(int watt)
    {
        lock (_lock)
        {
            if (!ExecuteNonQuery($"INSERT INTO `solardata` (`watt`) VALUES ({watt})", out _))
                {
                return false;
            }
        }

        return true;
    }

    public static (int? Watt, DateTime? Timestamp) SelectLastWatt()
    {
        lock (_lock)
        {
            try
            {
                Connect();
                using MySqlDataReader? reader = ExecuteQuery("SELECT `watt`,`timestamp` from `solardata` ORDER BY `timestamp` DESC LIMIT 1;");

                while (reader?.Read() == true)
                {
                    return (reader.GetInt32("watt"), DateTime.SpecifyKind(reader.GetDateTime("timestamp"), DateTimeKind.Utc).ToLocalTime());
                }
            }
            finally
            {
                Disconnect();
            }
        }

        return (null, null);
    }

    public static (int? MaxWatt, DateTime? Timestamp) SelectMaxWatt()
    {
        lock (_lock)
        {
            try
            {
                Connect();
                using MySqlDataReader? reader = ExecuteQuery(
                    "SELECT d.watt, d.timestamp " +
                    "FROM solardata d " +
                    "INNER JOIN (" +
                        "SELECT MAX(watt) AS w " +
                        "FROM solardata " +
                    ") m ON d.watt = m.w;");

                while (reader?.Read() == true)
                {
                    return (reader.GetInt32("watt"), DateTime.SpecifyKind(reader.GetDateTime("timestamp"), DateTimeKind.Utc).ToLocalTime());
                }
            }
            finally
            {
                Disconnect();
            }
        }

        return (null, null);
    }
    
    public static DateTime? SelectMinTimestamp()
    {
        lock (_lock)
        {
            try
            {
                Connect();

                using MySqlDataReader? reader = ExecuteQuery("SELECT min(`timestamp`) as m from `solardata`;");
                while (reader?.Read() == true)
                {
                    return DateTime.SpecifyKind(reader.GetDateTime("m"), DateTimeKind.Utc);
                }
            }
            finally
            {
                Disconnect();
            }
        }

        return null;
    }

    private static string GetConnectionString()
    {
        return $"Server={Program.InputArgs.DbServer};Uid={Program.InputArgs.DbUser};Pwd={Program.InputArgs.DbPassword}";
    }
}