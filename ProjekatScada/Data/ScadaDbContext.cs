using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using ProjekatScada.Data.Entities;

namespace ProjekatScada.Data
{
    public class ScadaDbContext : DbContext
    {
        public ScadaDbContext()
            : base("name=ScadaDbContext")
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<ScadaDbContext>());
        }

        public DbSet<TagEntity> Tags { get; set; }
        public DbSet<AlarmEntity> Alarms { get; set; }
        public DbSet<ActivatedAlarmEntity> ActivatedAlarms { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TagEntity>().ToTable("Tags");
            modelBuilder.Entity<TagEntity>().Property(t => t.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<AlarmEntity>().ToTable("Alarms");
            modelBuilder.Entity<AlarmEntity>().Property(a => a.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<ActivatedAlarmEntity>().ToTable("ActivatedAlarms");
            modelBuilder.Entity<ActivatedAlarmEntity>().Property(a => a.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            base.OnModelCreating(modelBuilder);
        }
    }
}
