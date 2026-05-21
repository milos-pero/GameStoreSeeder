namespace DBDataGenerator.Schema;

public class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public int? MaxLength { get; set; }
    public string? ReferencedTable { get; set; }
    public string? ReferencedColumn { get; set; }

    public override string ToString()
    {
        var flags = new List<string>();

        if (IsPrimaryKey) flags.Add("PK");

        if (IsForeignKey) flags.Add($"FK -> {ReferencedTable}.{ReferencedColumn}");

        if (IsNullable) flags.Add("nullable");

        if (MaxLength.HasValue) flags.Add($"max:{MaxLength}");

        var flagStr = flags.Count > 0 ? $" [{string.Join(", ", flags)}]" : "";
        return $"{Name} ({DataType}){flagStr}";
    }
}