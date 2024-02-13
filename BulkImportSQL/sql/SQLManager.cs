using MySql.Data.MySqlClient;

namespace BulkImportSQL.sql;

public sealed class SqlManager : IDisposable, IAsyncDisposable
{
    public string Server { get; }
    public string Database { get; }
    public string Username { get; }
    public string Password { get; }
    private MySqlConnection Connection { get; }

    private SqlManager(string server, string database, string username, string password)
    {
        Server = server;
        Database = database;
        Username = username;
        Password = password;

        Connection = new MySqlConnection($"Server={Server};Database={Database};Uid={Username};Pwd={Password};");
    }


    public static bool Connect(string server, string database, string username, string password, out SqlManager manager)
    {
        manager = new SqlManager(server, database, username, password);
        return manager.Connection.State == System.Data.ConnectionState.Open;
    }

    public void Dispose()
    {
        Connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}