namespace Mappy.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Mappy;
    using Mappy.Data;

    public class UnitCatalogService
    {
        private readonly SortedSet<string> names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string> sideByName =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string> displayNameByUnit =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string> objectNameByUnit =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler NamesChanged;

        public event EventHandler UnitPickerLabelsChanged;

        public string SelectedUnitName { get; set; }

        public void NotifyUnitPickerLabelsChanged()
        {
            this.UnitPickerLabelsChanged?.Invoke(this, EventArgs.Empty);
        }

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

        public void AddCatalogRecords(IEnumerable<UnitCatalogLoadRecord> records)
        {
            var changed = false;
            foreach (var r in records)
            {
                if (string.IsNullOrWhiteSpace(r.Name))
                {
                    continue;
                }

                var name = r.Name.Trim();
                if (this.names.Add(name))
                {
                    changed = true;
                }

                var updateSide = false;
                if (!this.sideByName.TryGetValue(name, out var prev))
                {
                    updateSide = true;
                }
                else if (string.IsNullOrEmpty(prev) && !string.IsNullOrEmpty(r.Side))
                {
                    updateSide = true;
                }

                if (updateSide)
                {
                    this.sideByName[name] = r.Side;
                    changed = true;
                }

                if (!string.IsNullOrWhiteSpace(r.DisplayName))
                {
                    var dn = r.DisplayName.Trim();
                    if (!this.displayNameByUnit.TryGetValue(name, out var prevDn) || string.IsNullOrWhiteSpace(prevDn))
                    {
                        this.displayNameByUnit[name] = dn;
                        changed = true;
                    }
                }

                if (!string.IsNullOrWhiteSpace(r.ObjectName))
                {
                    var on = r.ObjectName.Trim();
                    if (!this.objectNameByUnit.TryGetValue(name, out var prevOn) || string.IsNullOrWhiteSpace(prevOn))
                    {
                        this.objectNameByUnit[name] = on;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                this.NamesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string FormatUnitPickerLabel(string unitInternalName)
        {
            if (string.IsNullOrEmpty(unitInternalName))
            {
                return string.Empty;
            }

            if (this.displayNameByUnit.TryGetValue(unitInternalName, out var dn) && !string.IsNullOrWhiteSpace(dn))
            {
                if (MappySettings.Settings.ShowUnitFriendlyNameFirst)
                {
                    return dn + " (" + unitInternalName + ")";
                }

                return unitInternalName + " (" + dn + ")";
            }

            return unitInternalName;
        }

        public string GetUnitFriendlyDisplayName(string unitInternalName)
        {
            if (string.IsNullOrEmpty(unitInternalName))
            {
                return string.Empty;
            }

            return this.displayNameByUnit.TryGetValue(unitInternalName, out var dn) && !string.IsNullOrWhiteSpace(dn)
                ? dn.Trim()
                : string.Empty;
        }

        public string GetUnitPickerSearchableText(string unitInternalName)
        {
            if (string.IsNullOrEmpty(unitInternalName))
            {
                return string.Empty;
            }

            if (this.displayNameByUnit.TryGetValue(unitInternalName, out var dn) && !string.IsNullOrWhiteSpace(dn))
            {
                return dn.Trim();
            }

            return unitInternalName;
        }

        public string GetPrimaryLabelForMapMarker(string unitInternalName)
        {
            if (string.IsNullOrEmpty(unitInternalName))
            {
                return string.Empty;
            }

            if (MappySettings.Settings.ShowUnitFriendlyNameOnMap
                && this.displayNameByUnit.TryGetValue(unitInternalName, out var dn)
                && !string.IsNullOrWhiteSpace(dn))
            {
                return dn.Trim();
            }

            return unitInternalName;
        }

        public string GetThreeDoBaseName(string unitInternalName)
        {
            if (string.IsNullOrEmpty(unitInternalName))
            {
                return string.Empty;
            }

            if (this.objectNameByUnit.TryGetValue(unitInternalName, out var o) && !string.IsNullOrWhiteSpace(o))
            {
                return o.Trim();
            }

            return unitInternalName.Trim();
        }

        public string GetUnitSide(string unitName)
        {
            if (string.IsNullOrEmpty(unitName))
            {
                return UnitCatalogSide.Unknown;
            }

            return this.sideByName.TryGetValue(unitName, out var s)
                ? UnitCatalogSide.Normalize(s)
                : UnitCatalogSide.Unknown;
        }

        public IReadOnlyList<string> EnumerateDistinctSides()
        {
            var sides = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in this.names)
            {
                sides.Add(this.GetUnitSide(name));
            }

            return sides.OrderBy(s => s, SideTabOrderComparer.Instance).ToList();
        }

        public IReadOnlyList<string> EnumerateSorted() =>
            this.names
                .OrderBy(n => this.GetUnitPickerSearchableText(n), StringComparer.OrdinalIgnoreCase)
                .ThenBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private sealed class SideTabOrderComparer : IComparer<string>
        {
            internal static readonly SideTabOrderComparer Instance = new SideTabOrderComparer();

            public int Compare(string x, string y) => UnitCatalogSide.CompareTabOrder(x, y);
        }
    }
}
