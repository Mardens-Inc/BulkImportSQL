namespace BulkImportSQL.sql;

public class ProcessUpdateEventArgs
{
    public required int Processed { get; init; }
    public int Failed { get; init; }
    public required int Total { get; init; }
    public float Percentage => (float)Processed / Total;
    public required string State { get; init; }
}