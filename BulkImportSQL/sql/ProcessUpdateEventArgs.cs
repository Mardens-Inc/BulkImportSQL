namespace BulkImportSQL.sql;

public class ProcessUpdateEventArgs
{
    public int Processed { get; init; }
    public int Total { get; init; }
    public float Percentage => (float)Processed / Total;
}