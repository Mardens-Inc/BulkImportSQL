using BulkImportSQL.cli;

namespace BulkImportSQL.sql;

public struct BatchProcessResult
{
    /// <summary>
    /// Represents the duration of a batch process.
    /// </summary>
    /// <remarks>
    /// The duration is measured in elapsed time and indicates how long a batch process took to complete.
    /// </remarks>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Represents the number of rows that were inserted during a batch processing operation.
    /// </summary>
    public int Inserted { get; set; }

    /// <summary>
    /// Represents a SQL manager used for connecting to a SQL server and performing database operations.
    /// </summary>
    public int Failed { get; set; }
}