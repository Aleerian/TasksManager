using Npgsql;
using TasksManager.Models;

namespace TasksManager.Service;

public class TaskProjectService
{
    private readonly string _connectionString;
    private readonly ILogger<TaskProjectService> _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<TaskProjectService>();

    public TaskProjectService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<int> CreateTaskAsync(TaskCreateModel model, int creatorId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"INSERT INTO tasks (title, description, project_id, status_id, deadline, created_at) 
                      VALUES (@Title, @Description, @ProjectId, @StatusId, @Deadline, @CreateAt) RETURNING id";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("Title", model.Title);
        command.Parameters.AddWithValue("Description", model.Description);
        command.Parameters.AddWithValue("ProjectId", model.ProjectId);
        command.Parameters.AddWithValue("StatusId", model.StatusId);
        command.Parameters.AddWithValue("Deadline", model.Deadline ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("CreateAt", DateTime.Now);

        var taskId = (int)await command.ExecuteScalarAsync();

        if (model.Assignees != null && model.Assignees.Count > 0)
        {
            foreach (var userId in model.Assignees)
            {
                var assignQuery = "INSERT INTO task_user (task_id, user_id) VALUES (@TaskId, @UserId)";
                using var assignCommand = new NpgsqlCommand(assignQuery, connection);
                assignCommand.Parameters.AddWithValue("TaskId", taskId);
                assignCommand.Parameters.AddWithValue("UserId", userId);
                await assignCommand.ExecuteNonQueryAsync();
            }
        }

        return taskId;
    }

    public async Task<List<TaskModel>> GetPrivateTasksAsync(int userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
        SELECT t.id, t.title, t.description, t.status_id, t.deadline, t.created_at 
        FROM tasks t
        JOIN task_user tu ON t.id = tu.task_id
        WHERE tu.user_id = @UserId";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("UserId", userId);

        var tasks = new List<TaskModel>();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tasks.Add(new TaskModel
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                StatusId = reader.GetInt32(3),
                Deadline = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                CreatedAt = reader.GetDateTime(5),
            });
        }

        return tasks;
    }
    
    public async Task<TaskModelByID?> GetTaskByIdAsync(int taskId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Запрос для получения информации о задаче
        var query = @"
        SELECT t.id, t.title, t.description, t.project_id, t.status_id, t.deadline, t.created_at 
        FROM tasks t
        WHERE t.id = @TaskId";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("TaskId", taskId);

        using var reader = await command.ExecuteReaderAsync();
        if (!reader.Read()) return null;

        var task = new TaskModelByID
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            Description = reader.GetString(2),
            ProjectId = reader.GetInt32(3),
            StatusId = reader.GetInt32(4),
            Status = new Status(),
            Deadline = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
            CreatedAt = reader.GetDateTime(6),
            Assignees = new List<UserModel>()
        };

        reader.Close();

        var statusQuery = "SELECT id, name FROM task_statuses WHERE id = @StatusId";
        using var statusCommand = new NpgsqlCommand(statusQuery, connection);
        statusCommand.Parameters.AddWithValue("StatusId", task.StatusId);

        using var statusReader = await statusCommand.ExecuteReaderAsync();
        if (await statusReader.ReadAsync())
        {
            task.Status = new Status()
            {
                Id = statusReader.GetInt32(0),
                Name = statusReader.GetString(1)
            };
        }
        statusReader.Close();
        // Запрос для получения списка исполнителей задачи
        var assigneeQuery = @"
        SELECT u.id, u.name, u.email
        FROM users u
        JOIN task_user tu ON u.id = tu.user_id
        WHERE tu.task_id = @TaskId";

        using var assigneeCommand = new NpgsqlCommand(assigneeQuery, connection);
        assigneeCommand.Parameters.AddWithValue("TaskId", taskId);

        using var assigneeReader = await assigneeCommand.ExecuteReaderAsync();
        while (await assigneeReader.ReadAsync())
        {
            task.Assignees.Add(new UserModel
            {
                Id = assigneeReader.GetInt32(0),
                Name = assigneeReader.GetString(1),
                Email = assigneeReader.GetString(2)
            });
        }

        return task;
    }

    public async Task<List<TaskModel>> GetProjectTasksAsync(int projectId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"SELECT id, title, description, project_id, status_id, deadline, created_at
                      FROM tasks WHERE project_id = @ProjectId ORDER BY status_id, deadline";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("ProjectId", projectId);

        var tasks = new List<TaskModel>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tasks.Add(new TaskModel
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                ProjectId = reader.GetInt32(3),
                StatusId = reader.GetInt32(4),
                Deadline = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                CreatedAt = reader.GetDateTime(6),
            });
        }

        return tasks;
    }

    public async Task UpdateTaskAsync(int taskId, TaskUpdateModel model)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"UPDATE tasks SET status_id = @StatusId, deadline = @Deadline WHERE id = @TaskId";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("StatusId", model.StatusId);
        command.Parameters.AddWithValue("Deadline", model.Deadline ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("TaskId", taskId);

        await command.ExecuteNonQueryAsync();

        var deleteUsersQuery = "DELETE FROM task_user WHERE task_id = @TaskId";
        using var deleteUsersCommand = new NpgsqlCommand(deleteUsersQuery, connection);
        deleteUsersCommand.Parameters.AddWithValue("TaskId", taskId);
        await deleteUsersCommand.ExecuteNonQueryAsync();

        if (model.Assignees != null && model.Assignees.Count > 0)
        {
            foreach (var userId in model.Assignees)
            {
                var assignQuery = "INSERT INTO task_user (task_id, user_id) VALUES (@TaskId, @UserId)";
                using var assignCommand = new NpgsqlCommand(assignQuery, connection);
                assignCommand.Parameters.AddWithValue("TaskId", taskId);
                assignCommand.Parameters.AddWithValue("UserId", userId);
                await assignCommand.ExecuteNonQueryAsync();
            }
        }
    }
}