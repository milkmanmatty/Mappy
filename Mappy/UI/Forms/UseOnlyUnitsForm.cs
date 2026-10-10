namespace Mappy.UI.Forms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;

    using Mappy.Data;
    using Mappy.Services;

    public partial class UseOnlyUnitsForm : Form
    {
        private readonly List<string> selected = new List<string>();

        private UnitCatalogService catalog;

        public UseOnlyUnitsForm()
        {
            this.InitializeComponent();
        }

        public UseOnlyUnitsForm(UnitCatalogService catalog, IEnumerable<string> currentNames)
            : this()
        {
            this.catalog = catalog;
            if (currentNames != null)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var name in currentNames)
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var trimmed = name.Trim();
                    if (seen.Add(trimmed))
                    {
                        this.selected.Add(trimmed);
                    }
                }
            }

            this.FillFilters();
            this.RefreshLists();
        }

        public IList<string> SelectedUnitNames { get; private set; }

        private void SearchBoxTextChanged(object sender, EventArgs e)
        {
            this.RefreshLists();
        }

        private void SideComboSelectedIndexChanged(object sender, EventArgs e)
        {
            this.RefreshLists();
        }

        private void TypeComboSelectedIndexChanged(object sender, EventArgs e)
        {
            this.RefreshLists();
        }

        private void AvailableListDoubleClick(object sender, EventArgs e)
        {
            this.AddSelectedAvailable();
        }

        private void SelectedListDoubleClick(object sender, EventArgs e)
        {
            this.RemoveSelectedAllowed();
        }

        private void AddButtonClick(object sender, EventArgs e)
        {
            this.AddSelectedAvailable();
        }

        private void RemoveButtonClick(object sender, EventArgs e)
        {
            this.RemoveSelectedAllowed();
        }

        private void AddMatchingButtonClick(object sender, EventArgs e)
        {
            this.AddAllMatching();
        }

        private void ClearButtonClick(object sender, EventArgs e)
        {
            this.selected.Clear();
            this.RefreshLists();
        }

        private void OkButtonClick(object sender, EventArgs e)
        {
            this.SelectedUnitNames = this.selected.ToList();
        }

        private void UseOnlyUnitsFormShown(object sender, EventArgs e)
        {
            this.searchBox.Focus();
        }

        private void FillFilters()
        {
            this.sideCombo.Items.Add(new FilterChoice(null, "All sides"));
            if (this.catalog != null)
            {
                foreach (var side in this.catalog.EnumerateDistinctSides())
                {
                    this.sideCombo.Items.Add(new FilterChoice(side, UnitCatalogSide.FormatTabLabel(side)));
                }
            }

            this.sideCombo.SelectedIndex = 0;

            this.typeCombo.Items.Add(new FilterChoice(null, "All types"));
            if (this.catalog != null)
            {
                foreach (var tedClass in this.catalog.EnumerateDistinctTedClasses())
                {
                    this.typeCombo.Items.Add(new FilterChoice(tedClass, tedClass));
                }
            }

            this.typeCombo.SelectedIndex = 0;
        }

        private void AddSelectedAvailable()
        {
            this.AddNames(this.SelectedNames(this.availableList));
        }

        private void AddAllMatching()
        {
            this.AddNames(this.availableList.Items.Cast<UnitRow>().Select(row => row.Name));
        }

        private void RemoveSelectedAllowed()
        {
            var removing = new HashSet<string>(this.SelectedNames(this.selectedList), StringComparer.OrdinalIgnoreCase);
            if (removing.Count == 0)
            {
                return;
            }

            this.selected.RemoveAll(name => removing.Contains(name));
            this.RefreshLists();
        }

        private void AddNames(IEnumerable<string> names)
        {
            var seen = new HashSet<string>(this.selected, StringComparer.OrdinalIgnoreCase);
            var any = false;
            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                {
                    continue;
                }

                this.selected.Add(name);
                any = true;
            }

            if (any)
            {
                this.RefreshLists();
            }
        }

        private IEnumerable<string> SelectedNames(ListBox list)
        {
            return list.SelectedItems.Cast<UnitRow>().Select(row => row.Name).ToList();
        }

        private void RefreshLists()
        {
            var query = (this.searchBox.Text ?? string.Empty).Trim();
            var side = (this.sideCombo.SelectedItem as FilterChoice)?.Value;
            var tedClass = (this.typeCombo.SelectedItem as FilterChoice)?.Value;
            var selectedSet = new HashSet<string>(this.selected, StringComparer.OrdinalIgnoreCase);

            this.FillList(this.availableList, this.AvailableNames(query, side, tedClass, selectedSet));
            this.FillList(this.selectedList, this.selected);
            this.availableLabel.Text = "Available (" + this.availableList.Items.Count + ")";
            this.selectedLabel.Text = "Allowed (" + this.selected.Count + ")";
        }

        private IEnumerable<string> AvailableNames(string query, string side, string tedClass, HashSet<string> selectedSet)
        {
            if (this.catalog == null)
            {
                yield break;
            }

            foreach (var name in this.catalog.EnumerateSorted())
            {
                if (selectedSet.Contains(name))
                {
                    continue;
                }

                if (side != null && !string.Equals(this.catalog.GetUnitSide(name), side, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (tedClass != null && !string.Equals(this.catalog.GetUnitTedClass(name), tedClass, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!this.Matches(name, query))
                {
                    continue;
                }

                yield return name;
            }
        }

        private bool Matches(string internalName, string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return true;
            }

            if (internalName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (this.catalog == null)
            {
                return false;
            }

            var searchable = this.catalog.GetUnitPickerSearchableText(internalName);
            if (!string.IsNullOrEmpty(searchable)
                && searchable.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var label = this.catalog.FormatUnitPickerLabel(internalName);
            return !string.IsNullOrEmpty(label)
                && label.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void FillList(ListBox list, IEnumerable<string> names)
        {
            list.BeginUpdate();
            list.Items.Clear();
            foreach (var name in names)
            {
                var label = this.catalog == null ? name : this.catalog.FormatUnitPickerLabel(name);
                list.Items.Add(new UnitRow(name, label));
            }

            list.EndUpdate();
        }

        private sealed class FilterChoice
        {
            public FilterChoice(string value, string label)
            {
                this.Value = value;
                this.Label = label;
            }

            public string Value { get; }

            public string Label { get; }

            public override string ToString()
            {
                return this.Label;
            }
        }

        private sealed class UnitRow
        {
            public UnitRow(string name, string label)
            {
                this.Name = name;
                this.Label = string.IsNullOrEmpty(label) ? name : label;
            }

            public string Name { get; }

            public string Label { get; }

            public override string ToString()
            {
                return this.Label;
            }
        }
    }
}
