using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace DbMigrator
{
    public class DbMigrator : IDisposable
    {
        private const string tableName = "__Migrations";
        private string _path;
        private SqlConnection _connection;

        /// <summary>
        /// DbMigrator constructor
        /// </summary>
        /// <param name="connectionString">Connection string for the database being migrated.</param>
        /// <param name="migrationsPath">Path to your migrations directory. All SQL files in the directory
        /// should be numbered sequentially (0001.sql, 0002.sql, etc) and the files should be set to copy
        /// to output directory.</param>
        public DbMigrator(string connectionString, string migrationsPath)
        {
            _path = migrationsPath;
            _connection = new SqlConnection(connectionString);
            _connection.Open();
        }

        /// <summary>
        /// Reads all files in the provided migrationsPath and executes all scripts that have not
        /// been previously executed.
        /// </summary>
        public void Migrate()
        {
            CreateMigrationsTable();
            var latest = GetLatestMigration();
            var scripts = GetUnexecutedScripts(latest);
            ExecuteScripts(scripts);
        }

        private void CreateMigrationsTable()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $@"
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'{tableName}')
                    BEGIN
                        CREATE TABLE [dbo].[{tableName}](
                            [Id] [int] IDENTITY(1,1) NOT NULL,
                            [Version] [int] NOT NULL,
                            [CreatedAt] [datetimeoffset](7) NOT NULL,
                         CONSTRAINT [PK{tableName}] PRIMARY KEY CLUSTERED 
                        (
                            [Id] ASC
                        ))
                    END
                ";

                command.ExecuteNonQuery();
            }
        }

        private int GetLatestMigration()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $@"
                    SELECT TOP(1) [Version]
                    FROM {tableName}
                    ORDER BY [Version] DESC
                ";

                var version = command.ExecuteScalar();

                if (version == null)
                {
                    return 0;
                }

                return Convert.ToInt32(version);
            }
        }

        private IEnumerable<Tuple<string, string>> GetUnexecutedScripts(int latest)
        {
            var scripts = new List<Tuple<string, string>>();

            foreach (var filePath in Directory.GetFiles(_path))
            {
                var filename = Path.GetFileNameWithoutExtension(filePath);
                var num = Convert.ToInt32(filename);

                if (num > latest)
                {
                    scripts.Add(Tuple.Create(filename, File.ReadAllText(filePath)));
                }
            }

            return scripts;
        }

        private void ExecuteScripts(IEnumerable<Tuple<string, string>> scripts)
        {
            foreach (var script in scripts)
            {
                if (string.IsNullOrWhiteSpace(script.Item2))
                {
                    continue;
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = script.Item2;
                    command.ExecuteNonQuery();
                }

                SetLatestMigration(Convert.ToInt32(script.Item1));
            }
        }

        private void SetLatestMigration(int version)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $@"
                    INSERT INTO {tableName}
                    VALUES (@Version, @CreatedAt)
                ";

                command.Parameters.AddWithValue("@Version", version);
                command.Parameters.AddWithValue("@CreatedAt", DateTimeOffset.UtcNow);
                command.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
