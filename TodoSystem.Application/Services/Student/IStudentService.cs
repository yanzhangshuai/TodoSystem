namespace TodoSystem.Application.Services.Student;

public interface IStudentService
{
    // Task<IEnumerable<string>> sendCode(String email, )

    Task<IEnumerable<string>> GenerateAsync();

    Task<IEnumerable<string>> ListAsync();
}