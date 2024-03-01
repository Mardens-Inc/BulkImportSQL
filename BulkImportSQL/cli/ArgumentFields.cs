using Newtonsoft.Json.Linq;

namespace BulkImportSQL.cli;

/// <summary>
/// Represents the fields for the command-line arguments.
/// </summary>
public struct ArgumentFields()
{
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
    /// Represents the optional JSON file parameter for the command line utility.
    /// </summary>
    /// <remarks>
    /// The JSON file parameter allows the user to specify a file that contains additional JSON data to be imported into the database.
    /// The JSON data should be in a format that matches the structure of the target table.
    /// This parameter is optional and can be null if not provided.
    /// </remarks>
    public string? JsonFile { get; set; } = null;

    /// <summary>
    /// Represents the JSON property of ArgumentFields.
    /// </summary>
    /// <remarks>
    /// This property holds the JSON data to be processed by the application.
    /// </remarks>
    public JArray Json { get; set; }

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


    /// <summary>
    /// Represents the FileMaker username for authentication.
    /// </summary>
    public string FilemakerUsername { get; set; }

    /// <summary>
    /// Represents the Filemaker password used for authentication.
    /// </summary>
    public string FilemakerPassword { get; set; }

    /// <summary>
    /// Represents the properties of a Filemaker database.
    /// </summary>
    public string FilemakerDatabase { get; set; }

    /// <summary>
    /// Represents a Filemaker layout property.
    /// </summary>
    public string FilemakerLayout { get; set; }
}