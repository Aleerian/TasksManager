using Npgsql;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TasksManager.Models;

namespace TasksManager.Service;

public class UserService
{
    private readonly string _connectionString;
    private readonly ILogger<UserService> _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<UserService>();
    private readonly IConfiguration _configuration;
    
    public UserService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _configuration = configuration;
    }

    public async Task RegisterUserAsync(string name, string email, string password)
    {
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        
        using var connection = new NpgsqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync();
            
            var query = "INSERT INTO users (name, email, password) VALUES (@Name, @Email, @Password)";
            using var command = new NpgsqlCommand(query, connection);
            
            command.Parameters.AddWithValue("Name", name);
            command.Parameters.AddWithValue("Email", email);
            command.Parameters.AddWithValue("Password", hashedPassword);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при регистрации пользователя.");
            throw;
        }
    }
    
   public async Task<string?> SignInUserAsync(string email, string password)
    {
    using var connection = new NpgsqlConnection(_connectionString);

    try
    {
        await connection.OpenAsync();
        
        var query = "SELECT id, password FROM users WHERE email = @Email";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("Email", email);

        using var reader = await command.ExecuteReaderAsync();
        if (!reader.Read())
        {
            _logger.LogError("Ошибка при авторизации пользователя.");
            return null;
        }

        var storedPassword = reader["password"] as string;
        var userId = reader.GetInt32(reader.GetOrdinal("id"));

        if (storedPassword == null || !BCrypt.Net.BCrypt.Verify(password, storedPassword))
        {
            _logger.LogError("Ошибка при авторизации пользователя.");
            return null;
        }
        
        return GenerateJwtToken(email, userId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при авторизации пользователя.");
        throw;
    }
    }
   
    public async Task<List<UserModel>> GetUsersByEmailAsync(string email, int userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
        SELECT u.id, u.name, u.email
        FROM users u
        WHERE u.email ILIKE '%' || @Email || '%'
        AND NOT EXISTS (
            SELECT 1 FROM project_user pu1
            JOIN project_user pu2 ON pu1.project_id = pu2.project_id
            WHERE pu1.user_id = u.id AND pu2.user_id = @UserId
        )";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("Email", email);
        command.Parameters.AddWithValue("UserId", userId);

        var users = new List<UserModel>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new UserModel
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2)
            });
        }

        return users;
    }
    
    public async Task LinkUserToProjectAsync(int userId, int projectId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = "INSERT INTO project_user (project_id, user_id) VALUES (@ProjectId, @UserId)";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("ProjectId", projectId);
        command.Parameters.AddWithValue("UserId", userId);

        await command.ExecuteNonQueryAsync();
    }
    
    public async Task<List<UserModel>> GetUsersByProjectIdAsync(int projectId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
        SELECT u.id, u.name, u.email
        FROM users u
        JOIN project_user pu ON u.id = pu.user_id
        WHERE pu.project_id = @ProjectId";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("ProjectId", projectId);

        var users = new List<UserModel>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new UserModel()
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2)
            });
        }

        return users;
    }
   
    private string GenerateJwtToken(string email, int userId)
    {
     var claims = new[]
        {
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(3),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}