using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoSystem.Application.Services.Student;

namespace TodoSystem.Web.Controllers;

[ApiController]
[Authorize]
[Route($"[controller]")]
public class StudentController(
    ILogger<StudentController> logger,
    IStudentService studentService
    ): ControllerBase
{
    [Authorize(Roles = "ADMIN")]
    [HttpPost(Name = "generate")]
    public async Task<IEnumerable<string>> GenerateAsync()
    {
        return await studentService.GenerateAsync();
    }

    [Authorize(Roles = "USER,ADMIN")]
    [HttpGet]
    public async Task<IEnumerable<string>> ListAsync()
    {
        return await studentService.ListAsync();
    }
}
