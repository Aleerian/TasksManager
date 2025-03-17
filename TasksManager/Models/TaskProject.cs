namespace TasksManager.Models;

public class TaskProject
{
    
}

public class TaskModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public int StatusId { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<int> Assignees { get; set; } = new();
}

public class TaskModelByID
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public int StatusId { get; set; }
    public Status Status { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<UserModel> Assignees { get; set; } = new();
}

public class TaskCreateModel
{
    public string Title { get; set; }
    public string Description { get; set; }
    public int ProjectId { get; set; }
    public int StatusId { get; set; }
    public DateTime? Deadline { get; set; }
    public List<int>? Assignees { get; set; }
}

public class TaskUpdateModel
{
    public int StatusId { get; set; }
    public DateTime? Deadline { get; set; }
    public List<int>? Assignees { get; set; }
}