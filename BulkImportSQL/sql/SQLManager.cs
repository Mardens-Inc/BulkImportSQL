using System.Diagnostics;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using Timer = System.Timers.Timer;

namespace BulkImportSQL.sql;

/// <summary>
/// Represents a SQL manager used for connecting to a SQL server and performing database operations.
/// </summary>
public sealed class SqlManager : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the connection string used to connect to the SQL server.
    /// </summary>
    /// <value>
    /// The connection string.
    /// </value>
    public string ConnectionString { get; }

    /// <summary>
    /// Represents a connection to a SQL server.
    /// </summary>
    private MySqlConnection Connection { get; }

    /// <summary>
    /// Represents a SQL manager used for connecting to a SQL server and performing database operations.
    /// </summary>
    private SqlManager(string server, int port, string database, string username, string password)
    {
        ConnectionString = $"Server={server};Port={port};Database={database};Uid={username};Pwd={password};";
        Connection = new MySqlConnection(ConnectionString);
        try
        {
            Connection.Open();
        }
        catch (MySqlException e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Failed to connect to server: {e.Message}", e.StackTrace);
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Processes a batch of data and inserts it into a SQL server table.
    /// </summary>
    /// <param name="table">The name of the SQL server table.</param>
    /// <param name="columns">An optional array of column names to be inserted. If null, all columns will be inserted.</param>
    /// <param name="element">An optional element within each JSON object to be inserted. If null, the entire object will be inserted.</param>
    /// <param name="numberOfSequentialInserts">The number of sequential inserts to be performed per JSON object.</param>
    /// <param name="numberOfProcesses">The number of parallel processes to be used for inserting the data.</param>
    /// <param name="json">The JSON data to be inserted into the SQL server table.</param>
    /// <param name="onUpdate">An optional event handler to receive updates on the process.</param>
    /// <returns>A BatchProcessResult object containing the processing result.</returns>
    public BatchProcessResult Process(string table, string[]? columns, string? element, int numberOfSequentialInserts, int numberOfProcesses, JArray json, EventHandler<ProcessUpdateEventArgs>? onUpdate = null)
    {
        // We start by declaring and starting a Stopwatch to keep track of processing times.
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Two counters are declared for tracking insert operations.
        int inserted = 0;
        int failed = 0;

        // The SQL insert queries are built using the BuildInsertQueries method.
        string[] queries = BuildInsertQueries(table, columns, element, numberOfProcesses, json, onUpdate);

        Timer? timer = null;
        if (onUpdate is not null)
        {
            // If the onUpdate event is not null, a new Timer object is created to update the progress of the process.
            timer = new Timer(TimeSpan.FromSeconds(1));
            int total = json.Count; // The total number of JSON elements is stored in a variable.
            // The Elapsed event is used to update the progress of the process.
            timer.Elapsed += (_, _) => onUpdate.Invoke(this, new ProcessUpdateEventArgs() { Total = total, Processed = inserted + failed, State = "Insertion" });
            timer.Start();
        }


        // The BifurcateInsertQueries method is called to split the array of SQL insert queries into smaller chunks.
        int chunks = BifurcateInsertQueries(numberOfSequentialInserts, ref queries);
        if (chunks == 0)
        {
            throw new InvalidOperationException("No queries were generated.");
        }

        Parallel.ForEach(queries, new ParallelOptions() { MaxDegreeOfParallelism = numberOfProcesses }, query =>
        {
            // For each query in the queries array, the Insert method is called to execute the SQL insert statement.
            if (Insert(query))
            {
                // If the insert operation is successful, the inserted counter is incremented.
                Interlocked.Increment(ref inserted);
            }
            else
            {
                // If the insert operation fails, the failed counter is incremented.
                Interlocked.Increment(ref failed);
            }
        });

        // The stopwatch is stopped.
        stopwatch.Stop();
        // The timer is stopped if it was created.
        timer?.Stop();

        // run the onUpdate event one last time to show the final result
        onUpdate?.Invoke(this, new ProcessUpdateEventArgs() { Total = json.Count, Processed = inserted + failed, State = "Insertion" });

        // A new BatchProcessResult object is created and returned.
        return new BatchProcessResult()
        {
            Duration = stopwatch.Elapsed, // Duration of processing.
            Inserted = inserted, // Number of successful insert operations.
            Failed = failed, // Number of failed insert operations.
        };
    }

    /// <summary>
    /// Splits the array of SQL insert queries into smaller chunks based on the specified chunk size.
    /// </summary>
    /// <param name="chunkSize">The number of insert queries to be included in each chunk.</param>
    /// <param name="queries">The array of SQL insert queries to be split into chunks. This array will be modified to store the split chunks.</param>
    /// <returns>The number of chunks generated from the input array of SQL insert queries.</returns>
    private static int BifurcateInsertQueries(int chunkSize, ref string[] queries)
    {
        List<string> tmp = [];

        for (int i = 0; i < queries.Length; i += chunkSize)
        {
            tmp.Add(string.Join('\n', queries.Skip(i).Take(chunkSize)));
        }

        queries = [..tmp];

        return tmp.Count;
    }

    /// <summary>
    /// Builds insert queries for a given table and JSON data.
    /// </summary>
    /// <param name="table">The name of the table to insert data into.</param>
    /// <param name="columns">An optional array of column names to insert data into. If null, all columns will be used.</param>
    /// <param name="element">An optional JSON element to extract data from. If null, the entire JSON will be used.</param>
    /// <param name="numberOfProcesses">The number of parallel processes to perform when the JSON count exceeds 1000.</param>
    /// <param name="json">The JSON data to insert into the table.</param>
    /// <param name="onUpdate">An optional event handler to receive progress updates during the query building process. The event args for this handler is <see cref="ProcessUpdateEventArgs"/>.</param>
    /// <returns>True if an error occurred during the query building process, false otherwise.</returns>
    public static string[] BuildInsertQueries(string table, string[]? columns, string? element, int numberOfProcesses, JArray json, EventHandler<ProcessUpdateEventArgs>? onUpdate = null)
    {
        // Initialize a ConcurrentBag instance to contain the queue.
        // ConcurrentBag<string> queue = [];
        string[] queue = new string[json.Count];
        int processed = 0;

        // Initialize a nullable Timer instance.
        Timer? timer = null;

        // Check if an update is available.
        if (onUpdate is not null)
        {
            // Set the Timer instance to tick every second.
            timer = new Timer(TimeSpan.FromSeconds(1));

            // Get the total count of the JSON object.
            int total = json.Count;

            // Wire up the event of the Timer to call onUpdate during each tick.
            timer.Elapsed += (_, _) => onUpdate.Invoke(null, new ProcessUpdateEventArgs() { Total = total, Processed = processed, State = "Building" });

            // Start the timer.
            timer.Start();
        }

        for (int i = 0; i < queue.Length; i++)
        {
            // Initialize a nullable JObject instance.
            JObject? item = json[i].Value<JObject>();

            // Try to retrieve value of the "element" from the JObject and perform operations if successful.
            if (element is not null && item is not null && item.TryGetValue(element, out JToken? jToken))
            {
                item = jToken as JObject;
            }

            if (item is null)
            {
                processed++;
                continue;
            }


            // Call BuildInsertQuery method with the necessary arguments and add the return string to queue.
            queue[i] = BuildInsertQuery(table, columns, item);
            processed++;
        }

        // Stop the timer after processing is complete.
        timer?.Stop();

        // Return the error flag.
        return queue;
    }


    private bool Insert(string sql)
    {
        // Starts a try block to catch exceptions related to MySQL operations
        try
        {
            // 1. Declares a new MySqlCommand object with SQL query string and MySQL connection and initializes it
            //    MySqlCommand represents a SQL statement to execute against a MySQL database. It is used together with MySqlConnection class.
            // 2. 'using' keyword provides a convenient syntax that ensures the correct use of IDisposable objects like MySqlCommand.
            using MySqlCommand command = new MySqlCommand(sql, Connection);

            // Executes a SQL statement or stored procedure and returns the number of affected rows.
            // If the command is a SELECT statement, it returns -1. For all other types of SQL statements, the method returns the number of rows affected.
            command.ExecuteNonQuery();

            // If the SQL command executed successfully, return true.
            return true;
        }
        // Captures MySqlException which is thrown when MySQL Server returns a warning or error.
        catch (MySqlException)
        {
            // If MySqlException is caught, means there was an error in executing the SQL command, return false.
            return false;
        }
    }

    /// <summary>
    /// Builds an SQL insert query string for the specified table, columns, and JObject item.
    /// If the columns parameter is null or empty, all properties of the JObject item will be used as columns in the query.
    /// </summary>
    /// <param name="table">The name of the table in the database.</param>
    /// <param name="columns">An optional array of column names to be included in the query. If null or empty, all properties of the JObject item will be used.</param>
    /// <param name="item">The JObject item containing the data to be inserted into the table.</param>
    /// <returns>An SQL insert query string for the specified table, columns, and JObject item.</returns>
    private static string BuildInsertQuery(string table, string[]? columns, JObject item)
    {
        // If columns is null or has no elements, select the names of the properties of the JSON object 'item'
        if (columns is null || columns.Length == 0)
            columns = item.Properties().Select(p => p.Name).ToArray();

        // For each column, select the corresponding value from the JSON object 'item'
        string[] values = columns.Select(c => $"'{item[c]}'").ToArray();

        // Join the column names into a comma-separated string
        string columnString = $"`{string.Join("`, `", columns)}`";

        // Join the values into a comma-separated string
        string valueString = string.Join(", ", values);

        // Build and return the SQL INSERT INTO statement
        return $"INSERT INTO {table} ({columnString}) VALUES ({valueString});";
    }


    /// <summary>
    /// Connects to the SQL server using the provided server, database, username, and password.
    /// </summary>
    /// <param name="server">The server name or IP address.</param>
    /// <param name="port">The port number for the SQL server.</param>
    /// <param name="database">The name of the database.</param>
    /// <param name="username">The username for authentication.</param>
    /// <param name="password">The password for authentication.</param>
    /// <param name="manager">The instance of SqlManager used for the connection.</param>
    /// <returns>A boolean value indicating whether the connection is successfully established or not.</returns>
    public static bool Connect(string server, int port, string database, string username, string password, out SqlManager manager)
    {
        manager = new SqlManager(server, port, database, username, password);
        return manager.Connection.State == System.Data.ConnectionState.Open;
    }

    /// <summary>
    /// Releases the resources used by the SqlManager instance.
    /// </summary>
    public void Dispose()
    {
        Connection.Dispose();
    }

    /// <summary>
    /// Asynchronously cleans up any resources used by the SqlManager object.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}