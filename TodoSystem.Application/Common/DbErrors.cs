using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TodoSystem.Application.Common;

/// <summary>
/// 数据库错误判定。集中一处，避免每个服务各写一遍同样的 SqlState 判断。
/// </summary>
public static class DbErrors
{
    /// <summary>是否为唯一约束冲突（PostgreSQL SQLSTATE 23505）。</summary>
    public static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}
