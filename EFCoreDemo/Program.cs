using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Update;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Pomelo.EntityFrameworkCore.MySql.Migrations;
using System.Diagnostics.CodeAnalysis;

namespace EFCoreDemo;

internal class Program
{
    private static void Main(string[] args)
    {
        using var ctx = new TestDbContext();
        Article a1 = new() { Title = "t1", Content="c1"};
        List<Comment> comments = [new Comment() { Message = "m11" }, new Comment() { Message = "m12" }];
        a1.Comments = comments;
        ctx.Articles.Add(a1);
        ctx.SaveChanges();
    }
}

internal class TestDbContext : DbContext
{
    public DbSet<Article> Articles {  get; set; }
    public DbSet<Comment> Comments { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string connString = "Server=localhost;Database=efcore_demo;Uid=root;Pwd=123456fj";
        var serverVersion = new MySqlServerVersion(new Version(8, 4, 0));
        optionsBuilder
            .UseMySql(connString, serverVersion)
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IMigrationsSqlGenerator, CustomMySqlMigrationsSqlGenerator>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Remove(typeof(ForeignKeyIndexConvention));
    }
}

internal class Article
{
    public long Id { get; set; }
    public string Title { get; set; }

    public string Content { get; set; }

    public List<Comment> Comments { get; set; }
}

internal class ArticleConfig : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("article");
        builder.Property(a => a.Title).HasMaxLength(255);
    }
}

internal class Comment
{
    public long Id { get; set; }
    public string Message { get; set; }
    public Article Article { get; set; }

    public long ArticleId { get; set; }
}

internal class CommentConfig : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comment");
        builder.HasOne(c => c.Article).WithMany(a => a.Comments).HasForeignKey(c => c.ArticleId);
    }
}

public class CustomMySqlMigrationsSqlGenerator : MySqlMigrationsSqlGenerator
{
    public CustomMySqlMigrationsSqlGenerator(
        [NotNull] MigrationsSqlGeneratorDependencies dependencies,
        [NotNull] ICommandBatchPreparer commandBatchPreparer,
        [NotNull] IMySqlOptions options)
        : base(dependencies, commandBatchPreparer, options)
    {
    }

    protected override void Generate(CreateTableOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        operation.ForeignKeys.Clear();
        base.Generate(operation, model, builder, terminate);
    }

    protected override void Generate(AddForeignKeyOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        return;
    }
}