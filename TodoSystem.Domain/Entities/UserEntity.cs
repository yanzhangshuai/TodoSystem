using System.ComponentModel.DataAnnotations.Schema;
using TodoSystem.Domain.Base;

namespace TodoSystem.Domain.Entities;

[Table("users")]
public class UserEntity : AppBaseEntity
{
    public string Name { get; set; } = default!;

    /// <summary>密码哈希，禁止存明文</summary>
    public string Password { get; set; } = default!;

    public string Email { get; set; } = default!;

    /// <summary>角色：USER / ADMIN</summary>
    public string Role { get; set; } = "USER";

    public bool IsActivated { get; set; }

}
