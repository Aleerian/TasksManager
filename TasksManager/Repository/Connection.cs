using Npgsql;

namespace TaskManager.Repository;

public class Connection
{
    private readonly IConfiguration _configuration;
    
    public Connection(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public NpgsqlConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        var connection = new NpgsqlConnection(connectionString);
        return connection;
    }
}