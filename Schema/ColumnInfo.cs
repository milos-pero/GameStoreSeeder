namespace GameStoreSeeder.Schema
{
    public class ColumnInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsForeignKey { get; set; }

        //Reference points incase its a FK
        public string? ReferencedTable { get; set; }
        public string? ReferencedColumn { get; set; }

        public override string ToString()
        {

            var flags = new List<string>();

            if (IsPrimaryKey) flags.Add("PK");

            if (IsForeignKey) flags.Add($"FK -> {ReferencedTable}.{ReferencedColumn}");

            if (IsNullable) flags.Add("nullable");

            var flagStr = flags.Count > 0 ? $" ({string.Join(", ", flags)})" : "";

            return $"{Name} ({DataType}){flagStr}";
        }
    }
}