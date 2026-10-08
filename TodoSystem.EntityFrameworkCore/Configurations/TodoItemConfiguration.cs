using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoSystem.Domain.Constants;
using TodoSystem.Domain.Entities;

namespace TodoSystem.EntityFrameworkCore.Configurations;

public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItemEntity>
{
    public void Configure(EntityTypeBuilder<TodoItemEntity> builder)
    {
        // 状态、优先级取值在数据库层兜底，防止绕过 EF 写入脏数据。
        // 取值列表直接取自常量类，保证「C# 常量」和「数据库约束」不会各自漂移。
        builder.ToTable("todo_items", t =>
        {
            t.HasCheckConstraint(
                "ck_todo_items_status",
                $"status IN ({Quote(TodoStatuses.All)})");

            t.HasCheckConstraint(
                "ck_todo_items_priority",
                $"priority IN ({Quote(TodoPriorities.All)})");

            // 自己不能是自己的父任务（自引用外键挡不住这种环）
            t.HasCheckConstraint(
                "ck_todo_items_parent_not_self",
                "parent_id IS NULL OR parent_id <> id");

        // completed_at 与 status 必须同进同退。少了这条就允许出现「status=DONE 但完成时间
        // 是空」这种只能靠应用层自律的脏数据，而所有按完成时间做的统计都会静默漏掉这些行。
        t.HasCheckConstraint(
                "ck_todo_items_completed_at_consistent",
                $"(status = '{TodoStatuses.Done}' AND completed_at IS NOT NULL) " +
                $"OR (status <> '{TodoStatuses.Done}' AND completed_at IS NULL)");
        });

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue(TodoStatuses.Todo);
        builder.Property(x => x.Priority).IsRequired().HasMaxLength(20).HasDefaultValue(TodoPriorities.Normal);

        // 最主要的查询：某用户某状态的待办
        builder.HasIndex(x => new { x.UserId, x.Status })
            .HasDatabaseName("ix_todo_items_user_id_status");

        // 「今天到期 / 已逾期」类查询
        builder.HasIndex(x => new { x.UserId, x.DueAt })
            .HasDatabaseName("ix_todo_items_user_id_due_at");

        // 进入某个清单后按手动顺序展开
        builder.HasIndex(x => new { x.ListId, x.SortOrder })
            .HasDatabaseName("ix_todo_items_list_id_sort_order");

        // 展开子任务
        builder.HasIndex(x => x.ParentId)
            .HasDatabaseName("ix_todo_items_parent_id");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("fk_todo_items_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);

        // 复合外键 (user_id, list_id) → todo_lists(user_id, id)。
        // 单列外键 list_id → todo_lists(id) 只保证「这个清单存在」，不保证「这个清单是你的」；
        // 一旦服务层漏了归属校验，A 的待办就能挂到 B 的清单下，B 打开清单时看到 A 的待办标题。
        // 复合外键把这条规则交给数据库，应用层写错也插不进去。
        //
        // 代价：复合外键无法用 ON DELETE SET NULL（会把 user_id 也置空，而它是 NOT NULL），
        // 所以「删除清单后把待办退回收件箱」必须在服务层显式完成，见 TodoListService.DeleteAsync。
        // 这里用 Restrict，让意外的硬删除直接报错而不是静默丢数据。
        // list_id 为 NULL 时整条约束不生效（PostgreSQL 默认 MATCH SIMPLE），这正是「未归类待办」能存在的原因。
        builder.HasOne(x => x.List)
            .WithMany()
            .HasForeignKey(x => new { x.UserId, x.ListId })
            .HasPrincipalKey(x => new { x.UserId, x.Id })
            .HasConstraintName("fk_todo_items_todo_lists_user_id_list_id")
            .OnDelete(DeleteBehavior.Restrict);

        // 自引用：子任务。硬删除父任务不会级联删掉子任务 —— 服务层负责先处理子任务。
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .HasConstraintName("fk_todo_items_todo_items_parent_id")
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static string Quote(IReadOnlyList<string> values)
        => string.Join(", ", values.Select(v => $"'{v}'"));
}
