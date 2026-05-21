namespace GameStoreSeeder.Config
{
    public enum DatabaseType
    {
        MySQL,
        PostgreSQL
    }
    public class ConnectionConfig
    {
        public DatabaseType DbType { get; set; }
        public string Host { get; set; } = "localhost";
        public int Port { get; set; }
        public string Database { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string BuildConnectionString()
        {
            return DbType switch
            {
                DatabaseType.MySQL => $"Server={Host};Port={Port};Database={Database};User={Username};Password={Password};AllowLoadLocalInfile=true;",
                DatabaseType.PostgreSQL => $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}",
                _ => throw new NotSupportedException($"Unsupported database type: {DbType}")
            };
        }

        public static ConnectionConfig MakeConnectionConfig()
        {
            Console.WriteLine("Database Connection:");

            //type
            Console.Write("Database type (1 = MySQL, 2 = PostgreSQL):");

            var dbType = Console.ReadLine()?.Trim() == "2" ? DatabaseType.PostgreSQL : DatabaseType.MySQL;

            int defaultPort = dbType == DatabaseType.MySQL ? 3306 : 5432;

            //host
            Console.Write($"Host [localhost]: ");
            var host = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(host)) host = "localhost";

            //port
            Console.Write($"Port [{defaultPort}]: ");
            var PortInput = Console.ReadLine()?.Trim();
            var port = string.IsNullOrEmpty(PortInput) ? defaultPort : int.Parse(PortInput);

            Console.Write("Database name: ");
            var database = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.Write("username: ");
            var username = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.Write("password: ");
            var password = ReadPassword();

            Console.WriteLine();

            return new ConnectionConfig
            {
                DbType = dbType,
                Host = host,
                Port = port,
                Database = database,
                Username = username,
                Password = password
            };
        }

        //Read password without echoing characters to the console
        private static string ReadPassword()
        {
            var password = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;
                if (key == ConsoleKey.Backspace && password.Length > 0)
                {
                    Console.Write("\b \b");
                    password = password[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    password += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);
            Console.WriteLine();
            return password;
        }
        public override string ToString()
        {
            return $"[{DbType}] {Username}@{Host}:{Port}/{Database}";
        }
    }
}
