using Microsoft.EntityFrameworkCore;
using Midnight.EC.Plant.WEB.Models.Entities;

namespace Midnight.EC.Plant.WEB.Models.Data;

public class PlantDbContext : DbContext
{
    public PlantDbContext(DbContextOptions<PlantDbContext> options) : base(options)
    {
    }

    public DbSet<Entities.Plant> Plants => Set<Entities.Plant>();
    public DbSet<PlantSpecies> PlantSpecies => Set<PlantSpecies>();
    public DbSet<PlantKnowledge> PlantKnowledge => Set<PlantKnowledge>();
    public DbSet<PlantDiary> PlantDiaries => Set<PlantDiary>();
    public DbSet<PlantImage> PlantImages => Set<PlantImage>();
    public DbSet<PlantAnalysis> PlantAnalyses => Set<PlantAnalysis>();
    public DbSet<PlantSource> PlantSources => Set<PlantSource>();
    public DbSet<PlantSourceSpecies> PlantSourceSpecies => Set<PlantSourceSpecies>();
    public DbSet<PlantSourceContent> PlantSourceContents => Set<PlantSourceContent>();
    public DbSet<PlantAnalysisJob> PlantAnalysisJobs => Set<PlantAnalysisJob>();
    public DbSet<PlantCareRecord> PlantCareRecords => Set<PlantCareRecord>();
    public DbSet<PlantProfile> PlantProfiles => Set<PlantProfile>();
    public DbSet<PlantReminder> PlantReminders => Set<PlantReminder>();
    public DbSet<PlantKnowledgeSyncLog> PlantKnowledgeSyncLogs => Set<PlantKnowledgeSyncLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlantSpecies>(entity =>
        {
            entity.ToTable("PlantSpecies");
            entity.HasIndex(e => e.ScientificName);
            entity.HasIndex(e => e.CommonName);
            entity.HasIndex(e => e.ChineseName);
            entity.HasIndex(e => e.TaxonId);
            entity.Property(e => e.ScientificName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CommonName).HasMaxLength(256);
            entity.Property(e => e.ChineseName).HasMaxLength(256);
            entity.Property(e => e.Genus).HasMaxLength(128);
            entity.Property(e => e.Family).HasMaxLength(128);
            entity.Property(e => e.TaxonId).HasMaxLength(128);
            entity.Property(e => e.ImageUrl).HasMaxLength(1024);
            entity.Property(e => e.SourceType).HasMaxLength(64);
            entity.Property(e => e.SourceId).HasMaxLength(128);
        });

        modelBuilder.Entity<PlantKnowledge>(entity =>
        {
            entity.ToTable("PlantKnowledge");
            entity.HasIndex(e => e.SpeciesId).IsUnique();
            entity.Property(e => e.TemperatureMin).HasPrecision(5, 2);
            entity.Property(e => e.TemperatureMax).HasPrecision(5, 2);
            entity.HasOne(e => e.Species)
                .WithOne(e => e.Knowledge)
                .HasForeignKey<PlantKnowledge>(e => e.SpeciesId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Entities.Plant>(entity =>
        {
            entity.ToTable("Plants");
            entity.HasIndex(e => e.SpeciesId);
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.NickName).HasMaxLength(256);
            entity.Property(e => e.Location).HasMaxLength(256);
            entity.HasOne(e => e.Species)
                .WithMany(e => e.Plants)
                .HasForeignKey(e => e.SpeciesId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlantDiary>(entity =>
        {
            entity.ToTable("PlantDiaries");
            entity.HasIndex(e => new { e.PlantId, e.DiaryDate });
            entity.Property(e => e.Title).HasMaxLength(256);
            entity.HasOne(e => e.Plant)
                .WithMany(e => e.Diaries)
                .HasForeignKey(e => e.PlantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlantImage>(entity =>
        {
            entity.ToTable("PlantImages");
            entity.HasIndex(e => e.PlantId);
            entity.HasIndex(e => e.DiaryId);
            entity.HasIndex(e => new { e.PlantId, e.IsCover });
            entity.Property(e => e.Note).HasMaxLength(2000);
            entity.Property(e => e.FileName).HasMaxLength(512).IsRequired();
            entity.Property(e => e.StoragePath).HasMaxLength(1024).IsRequired();
            entity.Property(e => e.ThumbnailPath).HasMaxLength(1024);
            entity.Property(e => e.OriginalFileName).HasMaxLength(512);
            entity.Property(e => e.ContentType).HasMaxLength(128);
            entity.Property(e => e.Sha256).HasMaxLength(64);
            entity.HasOne(e => e.Plant)
                .WithMany(e => e.Images)
                .HasForeignKey(e => e.PlantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Diary)
                .WithMany(e => e.Images)
                .HasForeignKey(e => e.DiaryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PlantAnalysis>(entity =>
        {
            entity.ToTable("PlantAnalyses");
            entity.HasIndex(e => new { e.PlantId, e.CreatedAt });
            entity.Property(e => e.ModelName).HasMaxLength(128);
            entity.Property(e => e.PromptVersion).HasMaxLength(64);
            entity.Property(e => e.Summary).HasMaxLength(2000);
            entity.Property(e => e.Confidence).HasPrecision(5, 4);
            entity.HasOne(e => e.Plant)
                .WithMany(e => e.Analyses)
                .HasForeignKey(e => e.PlantId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Diary)
                .WithMany(e => e.Analyses)
                .HasForeignKey(e => e.DiaryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PlantSource>(entity =>
        {
            entity.ToTable("PlantSources");
            entity.HasIndex(e => e.SpeciesId);
            entity.HasIndex(e => e.ContentHash);
            entity.HasIndex(e => e.Url);
            entity.Property(e => e.Title).HasMaxLength(512);
            entity.Property(e => e.Url).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.Domain).HasMaxLength(256);
            entity.Property(e => e.Author).HasMaxLength(256);
            entity.Property(e => e.Language).HasMaxLength(16);
            entity.Property(e => e.ContentHash).HasMaxLength(64);
            entity.HasOne(e => e.Species)
                .WithMany(e => e.Sources)
                .HasForeignKey(e => e.SpeciesId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlantSourceSpecies>(entity =>
        {
            entity.ToTable("PlantSourceSpecies");
            entity.HasKey(e => new { e.SourceId, e.SpeciesId });
            entity.HasIndex(e => e.SpeciesId);
            entity.HasOne(e => e.Source)
                .WithMany(e => e.LinkedSpecies)
                .HasForeignKey(e => e.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Species)
                .WithMany(e => e.SourceLinks)
                .HasForeignKey(e => e.SpeciesId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PlantSourceContent>(entity =>
        {
            entity.ToTable("PlantSourceContents");
            entity.HasIndex(e => e.SourceId);
            entity.HasIndex(e => e.ContentHash);
            entity.Property(e => e.ParserType).HasMaxLength(128);
            entity.Property(e => e.ParserVersion).HasMaxLength(64);
            entity.Property(e => e.ContentHash).HasMaxLength(64);
            entity.HasOne(e => e.Source)
                .WithMany(e => e.Contents)
                .HasForeignKey(e => e.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlantAnalysisJob>(entity =>
        {
            entity.ToTable("PlantAnalysisJobs");
            entity.HasIndex(e => e.PlantId);
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.Plant)
                .WithMany(e => e.AnalysisJobs)
                .HasForeignKey(e => e.PlantId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Diary)
                .WithMany(e => e.AnalysisJobs)
                .HasForeignKey(e => e.DiaryId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<PlantImage>()
                .WithMany()
                .HasForeignKey(e => e.ImageId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Analysis)
                .WithOne(e => e.Job)
                .HasForeignKey<PlantAnalysisJob>(e => e.AnalysisId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PlantKnowledgeSyncLog>(entity =>
        {
            entity.ToTable("PlantKnowledgeSyncLogs");
            entity.HasIndex(e => e.SpeciesId);
            entity.HasIndex(e => e.Provider);
            entity.Property(e => e.RequestUrl).HasMaxLength(2048);
            entity.Property(e => e.ResponseHash).HasMaxLength(64);
            entity.HasOne(e => e.Species)
                .WithMany(e => e.SyncLogs)
                .HasForeignKey(e => e.SpeciesId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlantCareRecord>(entity =>
        {
            entity.ToTable("PlantCareRecords");
            entity.HasIndex(e => new { e.PlantId, e.RecordDate });
            entity.HasIndex(e => e.CareType);
            entity.Property(e => e.Unit).HasMaxLength(16);
            entity.Property(e => e.NumericValue).HasPrecision(10, 2);
            entity.HasOne(e => e.Plant)
                .WithMany(e => e.CareRecords)
                .HasForeignKey(e => e.PlantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlantProfile>(entity =>
        {
            entity.ToTable("PlantProfiles");
            entity.HasIndex(e => e.PlantId).IsUnique();
            entity.Property(e => e.TargetHumidityMin).HasPrecision(5, 2);
            entity.Property(e => e.TargetHumidityMax).HasPrecision(5, 2);
            entity.Property(e => e.TargetTemperatureMin).HasPrecision(5, 2);
            entity.Property(e => e.TargetTemperatureMax).HasPrecision(5, 2);
            entity.Property(e => e.PersonalCareNotes).HasMaxLength(2000);
            entity.HasOne(e => e.Plant)
                .WithOne(e => e.Profile)
                .HasForeignKey<PlantProfile>(e => e.PlantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlantReminder>(entity =>
        {
            entity.ToTable("PlantReminders");
            entity.HasIndex(e => new { e.PlantId, e.Status });
            entity.HasIndex(e => new { e.PlantId, e.SourceKey }).IsUnique();
            entity.HasIndex(e => e.DueDate);
            entity.Property(e => e.Title).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.SourceKey).HasMaxLength(128).IsRequired();
            entity.HasOne(e => e.Plant)
                .WithMany(e => e.Reminders)
                .HasForeignKey(e => e.PlantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
