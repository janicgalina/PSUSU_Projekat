using System;
using System.Data.Entity;
using System.Linq;
using ProjekatScada.Data.Entities;
using ProjekatScada.Models.Enums;
using ProjekatScada.Services;

namespace ProjekatScada.Data
{
    public class ScadaDatabaseInitializer : CreateDatabaseIfNotExists<ScadaDbContext>
    {
        public override void InitializeDatabase(ScadaDbContext context)
        {
            if (!context.Database.Exists())
            {
                context.Database.Create();
            }

            EnsureUsersTable(context);
            SeedDefaultUsers(context);
            EnsureTagValueHistoryTable(context);
        }

        private static void EnsureUsersTable(ScadaDbContext context)
        {
            context.Database.ExecuteSqlCommand(@"
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
BEGIN
    CREATE TABLE [dbo].[Users](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Username] [nvarchar](128) NOT NULL,
        [PasswordHash] [nvarchar](512) NOT NULL,
        [Role] [int] NOT NULL
    )
    CREATE UNIQUE INDEX IX_Users_Username ON [dbo].[Users]([Username])
END");
        }

        private static void SeedDefaultUsers(ScadaDbContext context)
        {
            if (context.Users.Any())
            {
                return;
            }

            context.Users.Add(new UserEntity
            {
                Username = "admin",
                PasswordHash = PasswordHasher.HashPassword("AdminPassword123!"),
                Role = UserRole.Admin
            });

            context.Users.Add(new UserEntity
            {
                Username = "operater",
                PasswordHash = PasswordHasher.HashPassword("OperatorPass123!"),
                Role = UserRole.Operator
            });

            context.Users.Add(new UserEntity
            {
                Username = "student",
                PasswordHash = PasswordHasher.HashPassword("StudentPass1234!"),
                Role = UserRole.Student
            });

            context.Users.Add(new UserEntity
            {
                Username = "teacher",
                PasswordHash = PasswordHasher.HashPassword("TeacherPass1234!"),
                Role = UserRole.Teacher
            });

            context.SaveChanges();
        }

        private static void EnsureTagValueHistoryTable(ScadaDbContext context)
        {
            context.Database.ExecuteSqlCommand(@"
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TagValueHistory')
BEGIN
    CREATE TABLE [dbo].[TagValueHistory](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [TagId] [int] NOT NULL,
        [TagName] [nvarchar](128) NOT NULL,
        [Value] [float] NOT NULL,
        [RecordedAt] [datetime] NOT NULL
    )
END");
        }
    }
}
