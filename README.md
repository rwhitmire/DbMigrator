# DbMigrator

The purpose of this library is to provide migration functionality
for C# and SQL Server. SQL scripts are added to a directory and
DbMigrator decides what to execute.

### Installation

Copy [DbMigrator.cs](https://raw.githubusercontent.com/rwhitmire/DbMigrator/master/DbMigrator/DbMigrator.cs) into your project.

### Adding Migrations

Migration scripts should be named numerically and created in the
order you want them executed (0001.sql, 0002.sql, etc). See the 
Migrations directory for an example. Set the scripts to copy to 
output directory or they will not be deployed.

### Executing Migrations

```csharp
var connectionString = "...";
var migrationsPath = Path.Combine(AppContext.BaseDirectory, "Migrations");

using(var migrator = new DbMigrator(connectionString, migrationsPath))
{
    migrator.Migrate();
}
```
