using Npgsql;
using TasksManager.Models;

namespace TasksManager.Handler;

public class ProjectService
{
    private readonly string _connectionString;
    private readonly ILogger<ProjectService> _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ProjectService>();
    private readonly IConfiguration _configuration;
    
    public ProjectService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _configuration = configuration;
    }
    
    public async Task CreateProject(string title, string description, int ownerId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
            
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var insertProjectQuery = "INSERT INTO projects (title, description, owner_id) " +
                                         "VALUES (@Title, @Description, @OwnerId) RETURNING id";
            using var insertProjectCommand = new NpgsqlCommand(insertProjectQuery, connection);
            insertProjectCommand.Parameters.AddWithValue("Title", title);
            insertProjectCommand.Parameters.AddWithValue("Description", description ?? (object)DBNull.Value);
            insertProjectCommand.Parameters.AddWithValue("OwnerId", ownerId);
                
            var projectId = (int)await insertProjectCommand.ExecuteScalarAsync();
                
            var insertUserQuery = "INSERT INTO project_user (project_id, user_id) VALUES (@ProjectId, @UserId)";
            using var insertUserCommand = new NpgsqlCommand(insertUserQuery, connection);
            insertUserCommand.Parameters.AddWithValue("ProjectId", projectId);
            insertUserCommand.Parameters.AddWithValue("UserId", ownerId);

            await insertUserCommand.ExecuteNonQueryAsync();
                
            await transaction.CommitAsync();

            _logger.LogInformation($"Проект с ID {projectId} успешно создан.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Ошибка при создании проекта.");
            throw;
        }
    }
    
    public async Task<List<Project>> GetUserProjectsAsync(int userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var projects = new List<Project>();

        try
        {
            await connection.OpenAsync();

            var query = @"
            SELECT p.id, p.title, p.description, p.owner_id
            FROM projects p
            INNER JOIN project_user pu ON p.id = pu.project_id
            WHERE pu.user_id = @UserId";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("UserId", userId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var project = new Project
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    OwnerId = reader.GetInt32(3),
                };
                projects.Add(project);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении проектов пользователя.");
            throw;
        }

        return projects;
    }
    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = "SELECT id, title, description, owner_id FROM projects WHERE id = @Id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("Id", NpgsqlTypes.NpgsqlDbType.Integer, id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Project
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                OwnerId = reader.GetInt32(3)
            };
        }

        return null;
    }
}