namespace SafeEcu.Infrastructure.Persistence;

public static class SqliteConnectionStringFactory
{
    public static string Create(string dataDirectory, string databaseFileName = "safe-ecu-manager.db")
    {
        Directory.CreateDirectory(dataDirectory);
        var databasePath = Path.Combine(dataDirectory, databaseFileName);

        return $"Data Source={databasePath}";
    }
}
