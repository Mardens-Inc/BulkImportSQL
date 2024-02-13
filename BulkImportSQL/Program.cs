using BulkImportSQL.cli;
using BulkImportSQL.sql;
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

            // Process the JSON data in the input file and insert it into the database, using the specified table and column names,
            // and the configured batch size and degree of parallelism
            BatchProcessResult result = manager.Process(
                fields.Table,
                fields.Columns,
                fields.JsonElement,
                fields.BatchSize,
                fields.NumberOfProcesses,
                JArray.Parse(File.ReadAllText(fields.InputFile)), fields.Silent ? null : (sender, args) => { Progressbar.Draw(args.Processed, args.Total); });

            // After all operations are complete, dispose the SQLManager to close the connection and free up system resources
            manager.Dispose();
        }
        else // If the connection attempt failed
        {
            // Print a colorful error message to the console
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Unable to connect to the server with the connection string '{manager.ConnectionString}'.");
            Console.ResetColor();

            // Terminate the program with a non-zero exit code to indicate the failure
            Environment.Exit((byte)ExitCodes.InvalidArguments);
        }
    }
}