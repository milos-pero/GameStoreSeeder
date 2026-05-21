using DBDataGenerator.Schema;

namespace DBDataGenerator.Graph
{
    public static class DependencyGraph
    {
        public static List<List<TableInfo>> BuildWaves(List<TableInfo> tables) { 
            var remaining = tables.ToHashSet();
            var resolved = new HashSet<string>();
            var waves = new List<List<TableInfo>>();

            while (remaining.Count > 0)
            {
                // a table is ready if all its dependencies are resolved
                var wave = remaining
                     .Where(t => t.Dependancies.All(dep => resolved.Contains(dep)))
                     .ToList();

                if (wave.Count == 0)
                {
                    throw new Exception("Circular dependency detected among remaining tables: " + string.Join(", ", remaining.Select(t => t.Name)));
                }

                waves.Add(wave);

                foreach(var table in wave)
                {
                    resolved.Add(table.Name);
                    remaining.Remove(table);
                }
            }

            return waves;
        }
    }
}
