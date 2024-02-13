using BulkImportSQL.cli;
using BulkImportSQL.sql;

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