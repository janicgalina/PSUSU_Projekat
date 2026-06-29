using System.Data.Entity;
using ProjekatScada.Data.Entities;

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

            EnsureTagValueHistoryTable(context);
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
