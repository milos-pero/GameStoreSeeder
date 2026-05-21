using Bogus;
using DBDataGenerator.Schema;

namespace DBDataGenerator.Generation;

public class DataGenerator
{
    private readonly Faker _faker = new();
    private readonly Dictionary<string, List<int>> _idPool;

    public DataGenerator(Dictionary<string, List<int>> idPool)
    {
        _idPool = idPool;
    }

    public object? GenerateValue(ColumnInfo col, int rowIndex)
    {
        // FK column — pick a random ID from the pool
        if (col.IsForeignKey && col.ReferencedTable != null)
        {
            if (_idPool.TryGetValue(col.ReferencedTable, out var ids) && ids.Count > 0)
                return ids[rowIndex % ids.Count];
            throw new Exception($"No IDs in pool for referenced table '{col.ReferencedTable}'");
        }

        var name = col.Name.ToLower();
        var type = col.DataType.ToLower();

        object? value = null;

        // name-based hints take priority over type
        if (name.Contains("email"))
            value = $"{rowIndex}_{_faker.Internet.Email()}";
        else if (name.Contains("username"))
            value = $"{_faker.Internet.UserName()}_{rowIndex}";
        else if (name.Contains("password"))
            value = _faker.Random.Hash();
        else if (name.Contains("website") || name.Contains("url"))
            value = _faker.Internet.Url();
        else if (name.Contains("description") || name.Contains("comment"))
            value = _faker.Lorem.Paragraph();
        else if (name.Contains("title"))
            value = $"{_faker.Commerce.ProductName()} {rowIndex}";
        else if (name.Contains("name"))
            value = $"{_faker.Company.CompanyName()} {rowIndex}";
        else if (name.Contains("code"))
        {
            int idx = rowIndex % 676;
            value = $"{(char)('A' + idx / 26)}{(char)('A' + idx % 26)}";
        }
        else
        {
            value = type switch
            {
                "int" => (object)_faker.Random.Int(1, int.MaxValue),
                "smallint" when name.Contains("year") => _faker.Random.Int(1970, 2024),
                "smallint" when name.Contains("point") => _faker.Random.Int(10, 500),
                "smallint" => _faker.Random.Int(1, 32767),

                "tinyint" when name.Contains("rating") => _faker.Random.Int(1, 10),
                "tinyint" when name.Contains("discount") => _faker.Random.Int(1, 100),
                "tinyint" => _faker.Random.Int(0, 127),
                "decimal" or "numeric" => (object)Math.Round(_faker.Random.Decimal(0.99m, 99.99m), 2),
                "real" or "double precision" or "float" => (object)Math.Round(_faker.Random.Double(0.99, 99.99), 2),
                "datetime" or "timestamp" or
                "timestamp without time zone" or
                "timestamp with time zone"
                    when name.Contains("start") => DateTime.SpecifyKind(_faker.Date.Between(DateTime.UtcNow.AddYears(-2), DateTime.UtcNow.AddYears(-1)), DateTimeKind.Utc),
                "datetime" or "timestamp" or
                "timestamp without time zone" or
                "timestamp with time zone"
                    when name.Contains("end") => DateTime.SpecifyKind(_faker.Date.Between(DateTime.UtcNow, DateTime.UtcNow.AddYears(1)), DateTimeKind.Utc),
                "datetime" or "timestamp" or
                "timestamp without time zone" or
                "timestamp with time zone" => (object)DateTime.SpecifyKind(_faker.Date.Past(3), DateTimeKind.Utc),
                "date" => (object)_faker.Date.Past(3).Date,
                "boolean" or "bool" => (object)_faker.Random.Bool(),
                "varchar" or "character varying" => _faker.Lorem.Word(),
                "char" or "character" => _faker.Lorem.Letter(),
                "text" => _faker.Lorem.Paragraph(),
                "uuid" => (object)Guid.NewGuid(),
                _ => _faker.Lorem.Word()
            };
        }

        // truncate strings that exceed the column's max length
        if (value is string strVal && col.MaxLength.HasValue && strVal.Length > col.MaxLength.Value)
            value = strVal[..col.MaxLength.Value];

        // convert any remaining DateTime to UTC for PostgreSQL compatibility
        if (value is DateTime dt && dt.Kind != DateTimeKind.Utc)
            value = dt.ToUniversalTime();

        // last resort — if column is not nullable and value is still null, use a safe default
        if (value == null && !col.IsNullable)
            value = type switch
            {
                "int" or "smallint" or "tinyint" => (object)1,
                "decimal" or "numeric" or "real" or "float" => (object)0.99m,
                "datetime" or "timestamp" or
                "timestamp without time zone" or
                "timestamp with time zone" => DateTime.UtcNow,
                "boolean" or "bool" => (object)false,
                "uuid" => (object)Guid.NewGuid(),
                _ => "default"
            };

        return value;
    }
}