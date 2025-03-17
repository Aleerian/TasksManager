namespace TasksManager.Models;

public class RegisterModel
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class LoginModel
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class UserModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class UserSearchResponse
{
    public List<UserModel> UserModel { get; set; }
}

public class AssignUserModel
{
    public int Id { get; set; }
}