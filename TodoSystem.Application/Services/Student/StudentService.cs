using Microsoft.EntityFrameworkCore;
using TodoSystem.EntityFrameworkCore;

namespace TodoSystem.Application.Services.Student;

public class StudentService(TodoSystemDbContext db) : IStudentService
{
    public async Task<IEnumerable<string>> GenerateAsync()
    {
        var result = new List<string>();

        var ids = db.Students.AsQueryable().Select(x => x.Id);

        db.Students.RemoveRange(db.Students.Where(x => ids.Contains(x.Id)));

        await db.SaveChangesAsync();

        var list = new List<Domain.Entities.StudentEntity>();
        var count = new Random().Next(20, 50);
        for (var i = 0; i < count; i++)
        {
            var name = "考生" + (i + 1);
            var user = new Domain.Entities.StudentEntity()
            {
                Name = name
            };
            list.Add(user);
            result.Add(name);
        }

        await db.Students.AddRangeAsync(list);
        await db.SaveChangesAsync();

        return result;
    }

    public async Task<IEnumerable<string>> ListAsync()
    {
        return await db.Students.OrderBy(x => x.Id).Select(x => x.Name).ToListAsync();
    }
}
