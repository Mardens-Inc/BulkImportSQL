using System.Diagnostics;
using System.Text;
using BulkImportSQL.cli;
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
        Connection.Open();
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
    /// <returns>A BatchProcessResult object containing the processing result.</returns>
    public BatchProcessResult Process(string table, string[]? columns, string? element, int numberOfSequentialInserts, int numberOfProcesses, JArray json, EventHandler<ProcessUpdateEventArgs>? onUpdate = null)
    {
        // We start by declaring and starting a Stopwatch to keep track of processing times.
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Two counters are declared for tracking insert operations.
        int inserted = 0;
        int failed = 0;

        Timer? timer = null;
        if (onUpdate is not null)
        {
            // If the onUpdate event is not null, a new Timer object is created to update the progress of the process.
            timer = new Timer(TimeSpan.FromSeconds(1));
            int total = json.Count; // The total number of JSON elements is stored in a variable.
            // The Elapsed event is used to update the progress of the process.
            timer.Elapsed += (sender, args) => onUpdate.Invoke(this, new ProcessUpdateEventArgs() { Total = total, Processed = inserted + failed });
            timer.Start();
        }

        // A check is performed to determine if parallel execution should be utilized, based on the number of JSON elements.
        if (json.Count > 1000)
        {
            // For JSON collections larger than 1000, a parallel approach is used.
            // Parallelism is limited by the numberOfProcesses parameter.
            Parallel.ForEach(json.Children<JObject>(), new ParallelOptions() { MaxDegreeOfParallelism = numberOfProcesses }, i =>
            {
                // For every JObject in the parallel loop.
                JObject item = i;

                // Check if an element parameter has been passed in. If so, attempt to get this JObject from the current JObject.
                if (element is not null)
                {
                    // This operation might fail, as such it's wrapped in a try/catch block.
                    JObject? j = i[element]?.Value<JObject>();

                    // If the JObject retrieval operation failed, an exception is thrown.
                    item = j ?? throw new ArgumentException("The element provided does not exist in the JSON object.");
                }

                // A StringBuilder object is created for building the SQL insert statement.
                StringBuilder sequentialInserts = new();

                // For each iteration specified in the numberOfSequentialInserts parameter, append an SQL insert statement to the StringBuilder.
                for (int k = 0; k < numberOfSequentialInserts; k++)
                {
                    sequentialInserts.Append(BuildInsertQuery(table, columns, item));
                }

                // Attempt to execute the SQL insert statement. Success or failure increments the corresponding counter.
                if (Insert(sequentialInserts.ToString()))
                    Interlocked.Add(ref inserted, inserted + numberOfSequentialInserts);
                else
                    Interlocked.Add(ref failed, failed + numberOfSequentialInserts);
            });
        }
        else
        {
            // For JSON collections 1000 or smaller, a sequential approach is used.
            foreach (JObject i in json.Children<JObject>())
            {
                // This code is very similar to the parallel version above, but operates on a single thread.
                JObject item = i;
                if (element is not null)
                {
                    JObject? j = i[element]?.Value<JObject>();
                    item = j ?? throw new ArgumentException("The element provided does not exist in the JSON object or is empty/null.");
                }

                StringBuilder sequentialInserts = new();
                for (int k = 0; k < numberOfSequentialInserts; k++)
                {
                    sequentialInserts.Append(BuildInsertQuery(table, columns, item));
                }

                // Same as above, success or failure increments the corresponding counter. But this time no need for Interlocked as this is thread safe.
                if (Insert(sequentialInserts.ToString()))
                    inserted += numberOfSequentialInserts;
                else
                    failed += numberOfSequentialInserts;
            }
        }

        // The stopwatch is stopped.
        stopwatch.Stop();
        // The timer is stopped if it was created.
        timer?.Stop();

        // run the onUpdate event one last time to show the final result
        onUpdate?.Invoke(this, new ProcessUpdateEventArgs() { Total = json.Count, Processed = inserted + failed });

        // A new BatchProcessResult object is created and returned.
        return new BatchProcessResult()
        {
            Duration = stopwatch.Elapsed, // Duration of processing.
            Inserted = inserted, // Number of successful insert operations.
            Failed = failed, // Number of failed insert operations.
        };
    }

    public string ExportInsertQueries(string table, string[]? columns, string? element, JArray json)
    {
        object lockObject = new();
        // A StringBuilder object is created for building the SQL insert statement.
        StringBuilder inserts = new();
        // A check is performed to determine if parallel execution should be utilized, based on the number of JSON elements.
        if (json.Count > 1000)
        {
            // For JSON collections larger than 1000, a parallel approach is used.
            // Parallelism is limited by the numberOfProcesses parameter.
            Parallel.ForEach(json.Children<JObject>(), new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
            {
                // For every JObject in the parallel loop.
                JObject item = i;

                // Check if an element parameter has been passed in. If so, attempt to get this JObject from the current JObject.
                if (element is not null)
                {
                    // This operation might fail, as such it's wrapped in a try/catch block.
                    JObject? j = i[element]?.Value<JObject>();

                    // If the JObject retrieval operation failed, an exception is thrown.
                    item = j ?? throw new ArgumentException("The element provided does not exist in the JSON object.");
                }

                // Inserts the SQL insert statement to the StringBuilder.
                string query = BuildInsertQuery(table, columns, item);
                lock (lockObject) // Prevents multiple threads from writing to the StringBuilder at the same time.
                {
                    inserts.AppendLine(query);
                }
            });
        }
        else
        {
            // For JSON collections 1000 or smaller, a sequential approach is used.
            foreach (JObject i in json.Children<JObject>())
            {
                // This code is very similar to the parallel version above, but operates on a single thread.
                JObject item = i;
                if (element is not null)
                {
                    JObject? j = i[element]?.Value<JObject>();
                    item = j ?? throw new ArgumentException("The element provided does not exist in the JSON object or is empty/null.");
                }

                // Inserts the SQL insert statement to the StringBuilder.
                string query = BuildInsertQuery(table, columns, item);
                inserts.AppendLine(query);
            }
        }

        return inserts.ToString();
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
        string[] values = columns.Select(c => item[c]!.ToString()).ToArray();

        // Join the column names into a comma-separated string
        string columnString = string.Join(", ", columns);

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