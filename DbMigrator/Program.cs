using System;
using System.IO;

namespace DbMigrator
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = "Data Source=.;Initial Catalog=dbmigrator;Integrated Security=True";
            var migrationsPath = Path.Combine(AppContext.BaseDirectory, "Migrations");

            using(var migrator = new DbMigrator(connectionString, migrationsPath))
            {
                migrator.Migrate();
            }
        }
    }
}
