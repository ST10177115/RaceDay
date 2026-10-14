using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();

        b.Entity<Event>().Property(e => e.EventType).HasConversion<string>();
        b.Entity<Event>().Property(e => e.EventDate).HasColumnType("date");
        b.Entity<Event>().Property(e => e.DistanceKm).HasPrecision(5, 2);
        b.Entity<Category>().Property(c => c.DistanceKm).HasPrecision(5, 2);

        // A participant can only enrol in a given category once
        b.Entity<Enrolment>()
            .HasIndex(e => new { e.ParticipantId, e.CategoryId }).IsUnique();

        // One enrolment has at most one result
        b.Entity<Result>()
            .HasOne(r => r.Enrolment).WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId);

        // SQL Server rejects "multiple cascade paths", so deleting a User
        // must not cascade into Events or Enrolments
        b.Entity<Event>()
            .HasOne(e => e.Organiser).WithMany(u => u.OrganisedEvents)
            .HasForeignKey(e => e.OrganiserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Enrolment>()
            .HasOne(e => e.Participant).WithMany(u => u.Enrolments)
            .HasForeignKey(e => e.ParticipantId).OnDelete(DeleteBehavior.Restrict);

        // Seed the two roles
        b.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Organiser" },
            new Role { RoleId = 2, RoleName = "Participant" });
    }
}