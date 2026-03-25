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

        private readonly Dictionary<string, UnitSideCategory> sideByName =
            new Dictionary<string, UnitSideCategory>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string> displayNameByUnit =
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
                else if (prev == UnitSideCategory.Other && r.Side != UnitSideCategory.Other)
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

        public UnitSideCategory GetUnitSide(string unitName)
        {
            if (string.IsNullOrEmpty(unitName))
            {
                return UnitSideCategory.Other;
            }

            return this.sideByName.TryGetValue(unitName, out var s) ? s : UnitSideCategory.Other;
        }

        public IReadOnlyList<string> EnumerateSorted() => this.names.ToList();
    }
}
