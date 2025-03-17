using Npgsql;
using TasksManager.Models;

namespace TasksManager.Service;

public class TaskStatus
{
    private readonly string _connectionString;
    private readonly ILogger<TaskStatus> _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<TaskStatus>();
    private readonly IConfiguration _configuration;
    
    public TaskStatus(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _configuration = configuration;
    }
    
    public async Task<int> CreateStatusAsync(int projectId, string name)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = "INSERT INTO task_statuses (project_id, name) VALUES (@ProjectId, @Name) RETURNING id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("ProjectId", projectId);
        command.Parameters.AddWithValue("Name", name);

        return (int)await command.ExecuteScalarAsync();
    }

    // Получение всех статусов по проекту
    public async Task<List<Status>> GetStatusesByProjectIdAsync(int projectId)
    {
        var statuses = new List<Status>();

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = "SELECT id, project_id, name FROM task_statuses WHERE project_id = @ProjectId";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("ProjectId", projectId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            statuses.Add(new Status
            {
                Id = reader.GetInt32(0),
                ProjectId = reader.GetInt32(1),
                Name = reader.GetString(2)
            });
        }

        return statuses;
    }

    // Удаление статуса
    public async Task<bool> DeleteStatusAsync(int statusId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = "DELETE FROM task_statuses WHERE id = @StatusId";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("StatusId", statusId);

        return await command.ExecuteNonQueryAsync() > 0;
    }
    
}