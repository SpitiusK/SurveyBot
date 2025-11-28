using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurveyBot.Core.Entities;

namespace SurveyBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the User entity.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table name
        builder.ToTable("users");

        // Primary key
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // TelegramId - unique identifier from Telegram
        builder.Property(u => u.TelegramId)
            .HasColumnName("telegram_id")
            .IsRequired();

        builder.HasIndex(u => u.TelegramId)
            .IsUnique()
            .HasDatabaseName("idx_users_telegram_id");

        // Username
        builder.Property(u => u.Username)
            .HasColumnName("username")
            .HasMaxLength(255);

        builder.HasIndex(u => u.Username)
            .HasDatabaseName("idx_users_username")
            .HasFilter("username IS NOT NULL");

        // FirstName
        builder.Property(u => u.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(255);

        // LastName
        builder.Property(u => u.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(255);

        // LastLoginAt
        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at")
            .HasColumnType("timestamp with time zone");

        // CreatedAt
        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // UpdatedAt
        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Configure backing field for Surveys collection
        builder.Navigation(u => u.Surveys)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Relationships
        builder.HasMany(u => u.Surveys)
            .WithOne(s => s.Creator)
            .HasForeignKey(s => s.CreatorId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_surveys_creator");
    }
}
