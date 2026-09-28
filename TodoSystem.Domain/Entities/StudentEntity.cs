using System.ComponentModel.DataAnnotations.Schema;
using TodoSystem.Domain.Base;

namespace TodoSystem.Domain.Entities;

[Table("students")]
public class StudentEntity : AppBaseEntity
{
    public string Name { get; set; } = default!;

}
