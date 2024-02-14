using BulkImportSQL.cli;
using BulkImportSQL.sql;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BulkImportSQL;

public static class Program
{
    public static void Main()
    {
        // Construct the command-line arguments for our program
        ArgumentFields fields = CommandLine.Build();

        // Try to establish a connection to the SQL server using the provided server details and user credentials
        if (SqlManager.Connect(fields.Server, fields.Port, fields.Database, fields.Username, fields.Password, out SqlManager manager))
        {
            // If the connection is successful, print a colorful success message to the console
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Successfully connected to the server {fields.Server} with the database {fields.Database} using the username {fields.Username} and the password provided.");
            Console.ResetColor();

            if (!fields.TestMode)
            {
                // Process the JSON data in the input file and insert it into the database, using the specified table and column names,
                // and the configured batch size and degree of parallelism
                BatchProcessResult result = manager.Process(
                    fields.Table,
                    fields.Columns,
                    fields.JsonElement,
                    fields.BatchSize,
                    fields.NumberOfProcesses,
                    JArray.Parse(File.ReadAllText(fields.InputFile)), fields.Silent
                        ? null
                        : OnUpdate);
                Console.WriteLine("Done!");

                if (fields.JsonFile is not null)
                {
                    Console.WriteLine($"Writing the result to '{fields.JsonFile}'");
                    File.WriteAllText(fields.JsonFile, JsonConvert.SerializeObject(result, Formatting.Indented));
                }
            }
            else
            {
                string[] result = SqlManager.BuildInsertQueries(fields.Table, fields.Columns, fields.JsonElement, fields.NumberOfProcesses, fields.Json, OnUpdate);
                var lines = result.Take(10);
                Console.WriteLine(string.Join('\n', lines));
                Console.WriteLine("...");
                string outputFile = Path.GetFullPath($"./{fields.Table}.sql");
                File.WriteAllLines(outputFile, result);
                Console.WriteLine($"The rest have been written to '{outputFile}'");
            }

            // After all operations are complete, dispose the SQLManager to close the connection and free up system resources
            manager.Dispose();
        }
        else // If the connection attempt failed
        {
            manager?.Dispose(); // Dispose of the SQLManager to close the connection and free up system resources
            // Print a colorful error message to the console
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Unable to connect to the server with the connection string '{manager?.ConnectionString}'.");
            Console.ResetColor();

            // Terminate the program with a non-zero exit code to indicate the failure
            Environment.Exit((byte)ExitCodes.InvalidArguments);
        }
    }

    /// <summary>
    /// Event handler for update progress during bulk import process.
    /// </summary>
    /// <param name="sender">The object that triggered the event.</param>
    /// <param name="e">The event arguments.</param>
    private static void OnUpdate(object? sender, ProcessUpdateEventArgs e)
    {
        Console.CursorVisible = false;
        // clear the current line
        Console.CursorLeft = 0;
        Console.Write(new string(' ', Console.WindowWidth - 1));
        Console.CursorLeft = 0;
        Console.WriteLine($"{e.State}: Processed {e.Processed} of {e.Total} records ({e.Percentage:P2})");
        Console.CursorTop -= 1;
    }
}