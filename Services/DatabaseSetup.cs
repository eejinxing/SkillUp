using Newtonsoft.Json.Linq;
using Npgsql;

namespace SkillUp.Services;

/// <summary>
/// Runs once when the app starts: creates the database if it is missing, creates the tables from Database/schema.sql if the
/// database is empty, and (when SkillUp:SeedDemoData is true) loads Database/demo-seed.json into a database with no accounts.
/// </summary>
public static class DatabaseSetup
{
    public static void Run(NpgsqlDataSource source, string connectionString, string contentRoot)
    {
        CreateDatabaseIfMissing(connectionString);
        using var db = new Db(source);
        if (db.Scalar("SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'role'") is not int)
            db.Exec(File.ReadAllText(Path.Combine(contentRoot, "Database", "schema.sql")));
        if (Settings.SeedDemoData && (int)db.Scalar("SELECT COUNT(*) FROM app_user") == 0)
            DemoSeed.Import(db, JObject.Parse(File.ReadAllText(Path.Combine(contentRoot, "Database", "demo-seed.json"))));
    }

    /// <summary>Connects to the built-in "postgres" database and creates the app's database there when it does not exist yet.</summary>
    static void CreateDatabaseIfMissing(string connectionString)
    {
        var target = new NpgsqlConnectionStringBuilder(connectionString);
        var name = target.Database;
        target.Database = "postgres";
        target.Pooling = false;
        using var connection = new NpgsqlConnection(target.ConnectionString);
        connection.Open();
        using var exists = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        exists.Parameters.AddWithValue("name", name);
        if (exists.ExecuteScalar() != null) return;
        // CREATE DATABASE takes no parameters; the name is quoted as an identifier.
        using var create = new NpgsqlCommand("CREATE DATABASE \"" + name.Replace("\"", "\"\"") + "\"", connection);
        create.ExecuteNonQuery();
    }
}
