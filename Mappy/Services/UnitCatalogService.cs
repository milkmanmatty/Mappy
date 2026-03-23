namespace Mappy.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class UnitCatalogService
    {
        private readonly SortedSet<string> names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler NamesChanged;

        public string SelectedUnitName { get; set; }

        public void AddNames(IEnumerable<string> unitNames)
        {
            var any = false;
            foreach (var n in unitNames)
            {
                if (string.IsNullOrWhiteSpace(n))
                {
                    continue;
                }

                if (this.names.Add(n.Trim()))
                {
                    any = true;
                }
            }

            if (any)
            {
                this.NamesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public IReadOnlyList<string> EnumerateSorted() => this.names.ToList();
    }
}
