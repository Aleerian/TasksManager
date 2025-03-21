namespace TasksManager.Models;

public class CreateStatusModel
{
    public string Name { get; set; } = null!;
}

public class Status
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = null!;
}