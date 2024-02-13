using BulkImportSQL.sql;
using cclip;
using Newtonsoft.Json.Linq;

namespace BulkImportSQL.cli;

/// <summary>
/// Represents a command line parser for the BulkImportSQL application.
/// </summary>
public sealed class CommandLine
{
    private readonly OptionsManager _optionsManager;

    private CommandLine()
    {
        // Initialize an OptionsManager object with the application name "BulkImportSQL"
        _optionsManager = new OptionsManager("BulkImportSQL");

        // Define command line arguments that are required for the application
        _optionsManager.Add(new Option("i", "input", true, true, "The input of the file to be imported"));
        _optionsManager.Add(new Option("s", "server", true, true, "The server to connect to"));
        _optionsManager.Add(new Option("d", "database", true, true, "The database to connect to"));
        _optionsManager.Add(new Option("t", "table", true, true, "The table to import to"));
        _optionsManager.Add(new Option("u", "username", true, true, "The username to connect with"));
        _optionsManager.Add(new Option("p", "password", true, true, "The password to connect with"));

        // Define command line arguments that are optional for the application
        _optionsManager.Add(new Option("c", "columns", false, true, "The columns to import, if not specified, all columns will be imported. The columns should be separated by a comma. Ex: column1,column2,column3"));
        _optionsManager.Add(new Option("e", "element", false, true, "If the input json has a sub element, specify the element to import. Ex: [{\"import_data\":{\"column1\":1,\"column2\":2}}] vs [{\"column1\":1,\"column2\":2}]"));
        _optionsManager.Add(new Option("b", "batch", false, true, "The batch size to import. Default is 1000"));
        _optionsManager.Add(new Option("ps", "processes", false, true, $"The number of processes to use. Default is {Environment.ProcessorCount}"));
        _optionsManager.Add(new Option("j", "json", false, true, "To output the results in json format, specify this flag and a file name. Ex: -j results.json"));
    }

    /// <summary>
    /// Parses the command-line arguments and returns the parsed argument fields.
    /// </summary>
    /// <returns>An instance of the ArgumentFields structure that contains the parsed argument values.</returns>
    private ArgumentFields Parse()
    {
        // Parsing command line options to check for presence of certain parameters
        OptionsParser parser = _optionsManager.Parse();
        parser.IsPresent("s", out string server);
        parser.IsPresent("d", out string database);
        parser.IsPresent("t", out string table);
        parser.IsPresent("u", out string username);
        parser.IsPresent("p", out string password);

        // Attempt to connect using the parsed server, database, username and password
        // If unable to connect, display a console error message and exit with InvalidArguments code
        if (!SQLManager.Connect(server, database, username, password))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Unable to connect to the server {server} with the database {database} using the username {username} and the password provided.");
            Console.ResetColor();
            Environment.Exit((byte)ExitCodes.InvalidArguments);
        }

        // If the connection is successful, then continue to try to parse and fetch additional information
        try
        {
            // Parsing additional parameters and organize them into an ArgumentFields struct
            string inputFile = GetInputFile(parser);
            return new ArgumentFields()
            {
                InputFile = inputFile,
                Server = server,
                Database = database,
                Table = table,
                Username = username,
                Password = password,
                Columns = GetColumns(parser),
                JsonElement = GetJsonElement(parser, inputFile),
                BatchSize = GetBatchSize(parser),
                NumberOfProcesses = GetNumberOfProcesses(parser),
                JsonFile = GetJsonOutputFile(parser)
            };
        }
        // If any Argument Exception is encountered, display error message and exit with InvalidArguments code
        catch (ArgumentException e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(e.Message);
            Console.ResetColor();
            Environment.Exit((byte)ExitCodes.InvalidArguments);
        }
        // If any other exception is encountered, display error message, print help and exit with UnhandledException code
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Unknown error has occurred: {e.Message}");
            Console.ResetColor();
            _optionsManager.PrintHelp();
            Environment.Exit((byte)ExitCodes.UnhandledException);
        }

        // If no exception is encountered, return a new ArgumentFields struct
        return new ArgumentFields();
    }


    // This method returns the number of processes needed for the application.
    private static int GetNumberOfProcesses(OptionsParser parser)
    {
        // Checking if the 'ps' option is present in the options parser
        if (!parser.IsPresent("ps", out string ps)) return Environment.ProcessorCount;
        // If present, try to convert the value of 'ps' to an integer
        if (int.TryParse(ps.Trim(), out int processes))
            return processes;
        // If 'ps' is not an integer, throw an exception
        throw new ArgumentException($"The number of processes must be an integer. {ps} is not an integer.");
    }

    private static string? GetJsonOutputFile(OptionsParser parser)
    {
        // Checking if the 'j' option is present in the options parser
        if (!parser.IsPresent("j", out string filePath) || string.IsNullOrEmpty(filePath)) return null;
        try
        {
            // If present, get the full path of the file
            filePath = Path.GetFullPath(filePath);
            // Create the file if it does not exist
            if (!File.Exists(filePath))
                File.CreateText(filePath).Close();
            return filePath;
        }
        catch (Exception ex)
        {
            // If there is a problem creating the file, throw an exception
            throw new ArgumentException($"The file '{filePath}' could not be created. {ex.Message}");
        }
    }

    /// <summary>
    /// Parses the command line options and retrieves the columns specified by the user.
    /// </summary>
    /// <param name="parser">The OptionsParser object used for parsing command line options.</param>
    /// <returns>An array of strings representing the columns specified by the user. If no columns are specified, an empty array is returned.</returns>
    private static string[] GetColumns(OptionsParser parser) => parser.IsPresent("c", out string columns) ? columns.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : [];

    /// <summary>
    /// Gets the full path of the input file.
    /// </summary>
    /// <param name="parser">The options parser.</param>
    /// <returns>The full path of the input file.</returns>
    private static string GetInputFile(OptionsParser parser)
    {
        // Checking if the 'i' option is present in the options parser
        parser.IsPresent("i", out string input);
        // Get the full path of the input file
        input = Path.GetFullPath(input);
        if (!File.Exists(input))
        {
            // Throw an exception if the file does not exist
            throw new ArgumentException($"Input file is not valid or does not exist: \"{input}\"");
        }

        return input;
    }

    /// <summary>
    /// Retrieves a JSON element from the input file based on the provided options parser.
    /// </summary>
    /// <param name="parser">The options parser containing the command line arguments.</param>
    /// <param name="input">The path to the input file.</param>
    /// <returns>The name of the JSON element.</returns>
    private static string GetJsonElement(OptionsParser parser, string input)
    {
        // Checking if the 'e' option is present in the options parser
        if (!parser.IsPresent("e", out string element)) return "";
        // Parse the JSON in the input file
        JArray json = JArray.Parse(File.ReadAllText(input));
        if (json.Count == 0)
            // Throw an exception if the JSON is empty
            throw new ArgumentException("The input file is empty.");
        JToken first = json[0];
        if (first[element] is null)
            // Throw an exception if the JSON element does not exist in the JSON
            throw new ArgumentException($"The element '{element}' does not exist in the input file.");
        return element;
    }

    /// <summary>
    /// Get the batch size for processing data.
    /// </summary>
    /// <param name="parser">The options parser.</param>
    /// <returns>The batch size.</returns>
    private static int GetBatchSize(OptionsParser parser)
    {
        // Checking if the 'b' option is present in the options parser
        if (!parser.IsPresent("b", out string b)) return 1000;
        // If present, try to convert the value of 'b' to an integer
        if (!int.TryParse(b, out int batchSize)) throw new ArgumentException($"The batch size must be an integer. {b} is not an integer.");
        return batchSize;
    }

    /// <summary>
    /// Executes the command line arguments parsing and returns the parsed arguments.
    /// </summary>
    /// <returns>An instance of the ArgumentFields struct containing the parsed command line arguments.</returns>
    public static ArgumentFields Build()
    {
        // Create a new instance of the CommandLine class and parse the command line arguments
        CommandLine commandLine = new();
        // Return the parsed command line arguments
        return commandLine.Parse();
    }
}