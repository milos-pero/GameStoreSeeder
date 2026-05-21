using GameStoreSeeder.Config;

Console.WriteLine("Hello, World!");
ConnectionConfig conf = ConnectionConfig.MakeConnectionConfig();
Console.WriteLine(conf.ToString());