using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TasksManager.Handler;
using TasksManager.Models;
using TasksManager.Service;
using TaskStatus = TasksManager.Service.TaskStatus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
    
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
        };
    });

var app = builder.Build();

app.UseCors("AllowLocalhost");

var userService = new UserService(builder.Configuration);
var projectService = new ProjectService(builder.Configuration);
var taskStatusService = new TaskStatus(builder.Configuration);
var taskService = new TaskProjectService(builder.Configuration);

app.MapPost("/sign-up", async ([FromBody] RegisterModel model) =>
{
    try
    {
        await userService.RegisterUserAsync(model.Name, model.Email, model.Password);
        return Results.Ok("Пользователь успешно зарегистрирован.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Ошибка при регистрации: " + ex.Message);
    }
});

app.MapPost("/sign-in", async ([FromBody] LoginModel model) =>
{
    var token = await userService.SignInUserAsync(model.Email, model.Password);
    if (token == null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new { Token = token });
});

app.MapPost("/create-project", async (HttpContext httpContext, [FromBody] CreateProjectModel model) =>
{
    try
    {
        if (!httpContext.User.Identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }
        
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Results.Unauthorized();
        }
        
        var userId = int.Parse(userIdClaim.Value);
        await projectService.CreateProject(model.Title, model.Description, userId);
        return Results.Ok("Проект успешно создан.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Ошибка при создании проекта: " + ex.Message);
    }
});

app.MapGet("/projects", async (HttpContext httpContext) =>
{
    try
    {
        if (!httpContext.User.Identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }
        
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Results.Unauthorized();
        }
        
        var userId = int.Parse(userIdClaim.Value);
        
        var projects = await projectService.GetUserProjectsAsync(userId);
        
        if (projects == null || projects.Count == 0)
        {
            return Results.Ok("У пользователя нет проектов.");
        }

        return Results.Ok(projects);
    }
    catch (Exception ex)
    {
        return Results.Problem("Ошибка при получении проектов: " + ex.Message);
    }
});

app.MapGet("/project/{id}", async (HttpContext httpContext, int id) =>
{
    try
    {
        if (!httpContext.User.Identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }
        
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Results.Unauthorized();
        }
        
        var userId = int.Parse(userIdClaim.Value);
        
        var project = await projectService.GetProjectByIdAsync(id);
        return project is not null ? Results.Ok(project) : Results.NotFound("Проект не найден.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Ошибка при получении проекта: " + ex.Message);
    }
});

app.MapPost("/project/{projectId}/status", async (HttpContext httpContext, int projectId, [FromBody] CreateStatusModel model) =>
{
    try
    {
        if (!httpContext.User.Identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }
        
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Results.Unauthorized();
        }
        
        var userId = int.Parse(userIdClaim.Value);
        
        int statusId = await taskStatusService.CreateStatusAsync(projectId, model.Name);
        return Results.Created($"/status/{statusId}", new { id = statusId, name = model.Name });
    }
    catch (Exception ex)
    {
        return Results.Problem("Ошибка при создании статуса: " + ex.Message);
    }
});

app.MapGet("/project/{projectId}/statuses", async (HttpContext httpContext, int projectId) =>
{
    try
    {
        if (!httpContext.User.Identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }
        
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Results.Unauthorized();
        }
        
        var userId = int.Parse(userIdClaim.Value);
        
        var statuses = await taskStatusService.GetStatusesByProjectIdAsync(projectId);
        return Results.Ok(statuses);
    }
    catch (Exception ex)
    {
        return Results.Problem("Ошибка при получении статусов: " + ex.Message);
    }
});

app.MapDelete("/status/{statusId}", async (HttpContext httpContext, int statusId) =>
{
    try
    {
        if (!httpContext.User.Identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }
        
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Results.Unauthorized();
        }
        
        var userId = int.Parse(userIdClaim.Value);
        
        bool deleted = await taskStatusService.DeleteStatusAsync(statusId);
        return deleted ? Results.Ok("Статус удален.") : Results.NotFound("Статус не найден.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Ошибка при удалении статуса: " + ex.Message);
    }
});

app.MapGet("/users/search", async (HttpContext httpContext, string email) =>
{
    try
    {
        if (!httpContext.User.Identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }
        
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Results.Unauthorized();
        }
        
        var userId = int.Parse(userIdClaim.Value);
        
        var users = await userService.GetUsersByEmailAsync(email, userId);
        
        return Results.Ok(users);
    }
    catch (Exception ex)
    {
        return Results.Problem("Ошибка при поиске пользователей: " + ex.Message);
    }
});

app.MapPost("/project/{projectId}/assign-user", async (HttpContext httpContext, int projectId, [FromBody] AssignUserModel model) =>
{
    try
    {
        if (!httpContext.User.Identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Results.Unauthorized();
        }

        var userId = int.Parse(userIdClaim.Value);
        
        await userService.LinkUserToProjectAsync(model.Id, projectId);

        return Results.Ok("Пользователь успешно связан с проектом.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Ошибка при связывании пользователя с проектом: " + ex.Message);
    }
});

app.MapGet("/project/{projectId}/users", async (HttpContext httpContext, int projectId) =>
{
    try
    {
        if (!httpContext.User.Identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        var users = await userService.GetUsersByProjectIdAsync(projectId);
        
        return Results.Ok(users);
    }
    catch (Exception ex)
    {
        return Results.Problem("Ошибка при получении пользователей проекта: " + ex.Message);
    }
});

app.MapPost("/tasks", async ([FromBody] TaskCreateModel model, HttpContext httpContext) =>
{
    try
    {
        if (!httpContext.User.Identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Results.Unauthorized();
        }

        var userId = int.Parse(userIdClaim.Value);

        var taskId = await taskService.CreateTaskAsync(model, userId);
        return Results.Ok(new { TaskId = taskId });
    }
    catch (Exception ex)
    {
        return Results.Problem("Ошибка при создании задачи: " + ex.Message);
    }
});

app.MapGet("/tasks/{id}", async (int id, HttpContext httpContext) =>
{
    try
    {
        if (!httpContext.User.Identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        var task = await taskService.GetTaskByIdAsync(id);
        return task != null ? Results.Ok(task) : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem("Ошибка при получении задачи: " + ex.Message);
    }
});

app.MapGet("/tasks/private", async (HttpContext httpContext) =>
{
    try
    {
        if (!httpContext.User.Identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Results.Unauthorized();
        }

        var userId = int.Parse(userIdClaim.Value);

        var task = await taskService.GetPrivateTasksAsync(userId);
        return task != null ? Results.Ok(task) : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem("Ошибка при получении задачи: " + ex.Message);
    }
});


app.MapGet("/tasks/project/{projectId}", async (int projectId, HttpContext httpContext) =>
{
    try
    {
        if (!httpContext.User.Identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        var tasks = await taskService.GetProjectTasksAsync(projectId);
        return Results.Ok(tasks);
    }
    catch (Exception ex)
    {
        return Results.Problem("Ошибка при получении задач проекта: " + ex.Message);
    }
});

app.MapPut("/tasks/{id}", async (int id, [FromBody] TaskUpdateModel model, HttpContext httpContext) =>
{
    try
    {
        if (!httpContext.User.Identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        await taskService.UpdateTaskAsync(id, model);
        return Results.Ok("Задача успешно обновлена.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Ошибка при обновлении задачи: " + ex.Message);
    }
});

app.Run();