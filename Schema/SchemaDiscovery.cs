using DBDataGenerator.Config;
using DBDataGenerator.Db;
using Microsoft.EntityFrameworkCore;

namespace DBDataGenerator.Schema;

public static class SchemaDiscovery
{
    public static async Task<List<TableInfo>> DiscoverAsync(DynamicDbContext db, string databaseName, DatabaseType dbType)
    {
        // 1. get all tables
        List<string> tables;
        if (dbType == DatabaseType.MySQL)
        {
            tables = await db.Database
                .SqlQueryRaw<string>("""
                    SELECT TABLE_NAME
                    FROM information_schema.TABLES
                    WHERE TABLE_SCHEMA = {0}
                      AND TABLE_TYPE = 'BASE TABLE'
                    ORDER BY TABLE_NAME
                    """, databaseName)
                .ToListAsync();
        }
        else
        {
            tables = await db.Database
                .SqlQueryRaw<string>("""
                    SELECT TABLE_NAME
                    FROM information_schema.TABLES
                    WHERE TABLE_CATALOG = {0}
                      AND TABLE_SCHEMA  = 'public'
                      AND TABLE_TYPE    = 'BASE TABLE'
                    ORDER BY TABLE_NAME
                    """, databaseName)
                .ToListAsync();
        }

        // 2. get all columns
        List<ColumnRow> columns;
        if (dbType == DatabaseType.MySQL)
        {
            columns = await db.Database
                .SqlQueryRaw<ColumnRow>("""
                    SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE,
                           CHARACTER_MAXIMUM_LENGTH
                    FROM information_schema.COLUMNS
                    WHERE TABLE_SCHEMA = {0}
                    ORDER BY TABLE_NAME, ORDINAL_POSITION
                    """, databaseName)
                .ToListAsync();
        }
        else
        {
            columns = await db.Database
                .SqlQueryRaw<ColumnRow>("""
                    SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE,
                           CHARACTER_MAXIMUM_LENGTH
                    FROM information_schema.COLUMNS
                    WHERE TABLE_CATALOG = {0}
                      AND TABLE_SCHEMA  = 'public'
                    ORDER BY TABLE_NAME, ORDINAL_POSITION
                    """, databaseName)
                .ToListAsync();
        }

        // 3. get primary key columns
        List<PkRow> pkRows;
        if (dbType == DatabaseType.MySQL)
        {
            pkRows = await db.Database
                .SqlQueryRaw<PkRow>("""
                    SELECT kcu.TABLE_NAME, kcu.COLUMN_NAME
                    FROM information_schema.TABLE_CONSTRAINTS tc
                    JOIN information_schema.KEY_COLUMN_USAGE kcu
                      ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                     AND tc.TABLE_SCHEMA    = kcu.TABLE_SCHEMA
                    WHERE tc.TABLE_SCHEMA    = {0}
                      AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    """, databaseName)
                .ToListAsync();
        }
        else
        {
            pkRows = await db.Database
                .SqlQueryRaw<PkRow>("""
                    SELECT kcu.TABLE_NAME, kcu.COLUMN_NAME
                    FROM information_schema.TABLE_CONSTRAINTS tc
                    JOIN information_schema.KEY_COLUMN_USAGE kcu
                      ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                     AND tc.TABLE_SCHEMA    = kcu.TABLE_SCHEMA
                    WHERE tc.TABLE_CATALOG   = {0}
                      AND tc.TABLE_SCHEMA    = 'public'
                      AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    """, databaseName)
                .ToListAsync();
        }

        // 4. get FK relationships
        List<FkRow> fks;
        if (dbType == DatabaseType.MySQL)
        {
            fks = await db.Database
                .SqlQueryRaw<FkRow>("""
                    SELECT kcu.TABLE_NAME, kcu.COLUMN_NAME,
                           kcu.REFERENCED_TABLE_NAME, kcu.REFERENCED_COLUMN_NAME
                    FROM information_schema.KEY_COLUMN_USAGE kcu
                    JOIN information_schema.TABLE_CONSTRAINTS tc
                      ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                     AND tc.TABLE_SCHEMA    = kcu.TABLE_SCHEMA
                    WHERE kcu.TABLE_SCHEMA             = {0}
                      AND tc.CONSTRAINT_TYPE            = 'FOREIGN KEY'
                      AND kcu.REFERENCED_TABLE_NAME IS NOT NULL
                    """, databaseName)
                .ToListAsync();
        }
        else
        {
            fks = await db.Database
                .SqlQueryRaw<FkRow>("""
                    SELECT
                        kcu.TABLE_NAME,
                        kcu.COLUMN_NAME,
                        ccu.TABLE_NAME  AS REFERENCED_TABLE_NAME,
                        ccu.COLUMN_NAME AS REFERENCED_COLUMN_NAME
                    FROM information_schema.TABLE_CONSTRAINTS tc
                    JOIN information_schema.KEY_COLUMN_USAGE kcu
                      ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                     AND tc.TABLE_SCHEMA    = kcu.TABLE_SCHEMA
                    JOIN information_schema.REFERENTIAL_CONSTRAINTS rc
                      ON rc.CONSTRAINT_NAME   = tc.CONSTRAINT_NAME
                     AND rc.CONSTRAINT_SCHEMA = tc.TABLE_SCHEMA
                    JOIN information_schema.CONSTRAINT_COLUMN_USAGE ccu
                      ON ccu.CONSTRAINT_NAME = rc.UNIQUE_CONSTRAINT_NAME
                     AND ccu.TABLE_SCHEMA    = rc.UNIQUE_CONSTRAINT_SCHEMA
                    WHERE tc.TABLE_CATALOG   = {0}
                      AND tc.TABLE_SCHEMA    = 'public'
                      AND tc.CONSTRAINT_TYPE = 'FOREIGN KEY'
                    """, databaseName)
                .ToListAsync();
        }

        // 5. assemble TableInfo objects
        var fkLookup = fks.ToLookup(f => f.TABLE_NAME);
        var pkLookup = pkRows.ToLookup(p => p.TABLE_NAME);

        var columnsByTable = columns
            .GroupBy(c => c.TABLE_NAME)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<TableInfo>();

        foreach (var tableName in tables)
        {
            var tableInfo = new TableInfo { Name = tableName };
            var tableFks = fkLookup[tableName].ToList();
            var tablePks = pkLookup[tableName].Select(p => p.COLUMN_NAME).ToHashSet();
            var fkByColumn = tableFks.ToDictionary(f => f.COLUMN_NAME);

            if (columnsByTable.TryGetValue(tableName, out var cols))
            {
                foreach (var col in cols)
                {
                    var isFk = fkByColumn.TryGetValue(col.COLUMN_NAME, out var fkRow);
                    tableInfo.Columns.Add(new ColumnInfo
                    {
                        Name = col.COLUMN_NAME,
                        DataType = col.DATA_TYPE,
                        IsNullable = col.IS_NULLABLE == "YES",
                        IsPrimaryKey = tablePks.Contains(col.COLUMN_NAME),
                        IsForeignKey = isFk,
                        MaxLength = col.CHARACTER_MAXIMUM_LENGTH,
                        ReferencedTable = isFk ? fkRow!.REFERENCED_TABLE_NAME : null,
                        ReferencedColumn = isFk ? fkRow!.REFERENCED_COLUMN_NAME : null,
                    });
                }
            }

            tableInfo.Dependancies = tableFks
                .Select(f => f.REFERENCED_TABLE_NAME)
                .Distinct()
                .ToList();

            result.Add(tableInfo);
        }

        return result;
    }

    private class ColumnRow
    {
        public string TABLE_NAME { get; set; } = string.Empty;
        public string COLUMN_NAME { get; set; } = string.Empty;
        public string DATA_TYPE { get; set; } = string.Empty;
        public string IS_NULLABLE { get; set; } = string.Empty;
        public int? CHARACTER_MAXIMUM_LENGTH { get; set; }
    }

    private class PkRow
    {
        public string TABLE_NAME { get; set; } = string.Empty;
        public string COLUMN_NAME { get; set; } = string.Empty;
    }

    private class FkRow
    {
        public string TABLE_NAME { get; set; } = string.Empty;
        public string COLUMN_NAME { get; set; } = string.Empty;
        public string REFERENCED_TABLE_NAME { get; set; } = string.Empty;
        public string REFERENCED_COLUMN_NAME { get; set; } = string.Empty;
    }
}