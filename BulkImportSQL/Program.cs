using BulkImportSQL.cli;

namespace BulkImportSQL;

public static class Program
{
    public static void Main()
    {
        ArgumentFields fields = CommandLine.Build();
    }
}