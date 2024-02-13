using BulkImportSQL.cli;
using BulkImportSQL.sql;
using Newtonsoft.Json.Linq;

namespace BulkImportSQL;

public static class Program
{
    public static void Main()
    {
        ArgumentFields fields = CommandLine.Build();
        if (SqlManager.Connect(fields.Server, fields.Database, fields.Username, fields.Password, out SqlManager manager))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Successfully connected to the server {fields.Server} with the database {fields.Database} using the username {fields.Username} and the password provided.");
            Console.ResetColor();

            BatchProcessResult result = manager.Process(
                fields.Table,
                fields.Columns,
                fields.JsonElement,
                fields.BatchSize,
                fields.NumberOfProcesses,
                JArray.Parse(File.ReadAllText(fields.InputFile)), fields.Silent ? null : (sender, args) => { Progressbar.Draw(args.Processed, args.Total); });


            // Dispose of the SQLManager instance
            manager.Dispose();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Unable to connect to the server {fields.Server} with the database {fields.Database} using the username {fields.Username} and the password provided.");
            Console.ResetColor();
            Environment.Exit((byte)ExitCodes.InvalidArguments);
        }
    }
}