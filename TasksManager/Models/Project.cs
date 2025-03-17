namespace TasksManager.Models;

public class Project
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int OwnerId { get; set; }
}

public class CreateProjectModel
{
    public string Title { get; set; }
    public string Description { get; set; }
}