namespace GameStoreSeeder.Schema
{
    public class TableInfo
    {
        public string Name { get; set; } = string.Empty;
        public List<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();

        //names of tables this table depends on (FK references)
        public List<string> Dependancies { get; set; } = new List<string>();

        public override string ToString()
        {
            var deps = Dependancies.Count > 0 ? $" [Depends on: {string.Join(", ", Dependancies)}]" : "";
            return $"{Name}({Columns.Count} columns){deps}";
        }
    }
}
