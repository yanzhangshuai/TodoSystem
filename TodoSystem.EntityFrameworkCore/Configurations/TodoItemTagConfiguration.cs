using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoSystem.Domain.Entities;

namespace TodoSystem.EntityFrameworkCore.Configurations;

public class TodoItemTagConfiguration : IEntityTypeConfiguration<TodoItemTagEntity>
{
    public void Configure(EntityTypeBuilder<TodoItemTagEntity> builder)
    {
        // 复合主键同时承担三件事：唯一标识一行、阻止同一标签被重复挂上、
        // 提供 (todo_item_id, tag_id) 索引。
        builder.HasKey(x => new { x.TodoItemId, x.TagId });

        builder.HasOne(x => x.TodoItem)
            .WithMany(x => x.ItemTags)
            .HasForeignKey(x => x.TodoItemId)
            .HasConstraintName("fk_todo_item_tags_todo_items_todo_item_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Tag)
            .WithMany(x => x.ItemTags)
            .HasForeignKey(x => x.TagId)
            .HasConstraintName("fk_todo_item_tags_tags_tag_id")
            .OnDelete(DeleteBehavior.Cascade);

        // 主键已经覆盖了 (todo_item_id, tag_id) 前缀查询，
        // 但反查「这个标签被哪些待办用了」用不上它，需要单独给 tag_id 建索引。
        builder.HasIndex(x => x.TagId)
            .HasDatabaseName("ix_todo_item_tags_tag_id");

        // 与 TagEntity / TodoItemEntity 的软删除过滤器配对。两端都有全局过滤器而它们
        // 在这里是「必需的一端」，不配对的话 EF 启动会警告 required end 被过滤，
        // 而且 db.TodoItemTags 会照样返回「标签已被删除」的关联行 —— Include(x => x.Tag)
        // 拿到 null，导航却声明非空。让关联行跟着两端一起被过滤，语义才自洽。
        builder.HasQueryFilter(x => x.Tag!.DeletedAt == null && x.TodoItem!.DeletedAt == null);
    }
}
