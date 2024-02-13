namespace BulkImportSQL.cli;

/// <summary>
/// Represents the fields for the command-line arguments.
/// </summary>
public struct ArgumentFields()
{
    /// <summary>
    /// Represents the input file for bulk import.
    /// </summary>
    public string InputFile { get; set; } = "";

    /// <summary>
    /// Represents the server property used for connecting to a database server.
    /// </summary>
    public string Server { get; set; } = "";

    /// <summary>
    /// Represents the port used for the connection.
    /// </summary>
    public int Port { get; set; } = 3306;

    /// <summary>
    /// Represents a database connection and related properties.
    /// </summary>
    public string Database { get; set; } = "";

    /// <summary>
    /// Represents the properties for the table being imported.
    /// </summary>
    public string Table { get; set; } = "";

    /// <summary>
    /// Represents the username used for authentication.
    /// </summary>
    public string Username { get; set; } = "";

    /// <summary>
    /// Represents a password used for authentication.
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// Represents the columns used in the import process.
    /// </summary>
    public string[]? Columns { get; set; } = null;

    /// <summary>
    /// Represents a property for storing the JSON element.
    /// </summary>
    public string JsonElement { get; set; } = "";

    /// <summary>
    /// Represents the batch size for bulk importing SQL data.
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the number of processes to use for importing data in parallel.
    /// </summary>
    /// <remarks>
    /// The default value is the number of processors on the system.
    /// </remarks>
    public int NumberOfProcesses { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Represents the optional JSON file parameter for the command line utility.
    /// </summary>
    /// <remarks>
    /// The JSON file parameter allows the user to specify a file that contains additional JSON data to be imported into the database.
    /// The JSON data should be in a format that matches the structure of the target table.
    /// This parameter is optional and can be null if not provided.
    /// </remarks>
    public string? JsonFile { get; set; } = null;

    /// <summary>
    /// Gets or sets a value indicating whether the operation should run silently.
    /// </summary>
    public bool Silent { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the table should be emptied before performing the insertion operation.
    /// </summary>
    /// <value>
    /// <c>true</c> if the table should be emptied before insertion; otherwise, <c>false</c>.
    /// </value>
    public bool EmptyBeforeInsertion { get; set; } = false;

    /// <summary>
    /// This mode will not insert any data into the database, perfect for testing the connection and parsing the input file.
    /// </summary>
    public bool TestMode { get; set; } = false;
}