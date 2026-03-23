namespace Mappy.UI.Forms
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows.Forms;

    using Mappy;
    using Mappy.Models;
    using Mappy.Services;

    public partial class MainForm
    {
        private TabPage missionTab;

        private CoreModel missionCoreModel;

        private Dispatcher missionDispatcher;

        private UnitCatalogService missionUnitCatalog;

        private ComboBox missionSchemaCombo;

        private ToolStripComboBox missionToolbarSchemaCombo;

        private ToolStrip missionOtaToolStrip;

        private ListBox missionUnitsList;

        private bool missionSchemaComboProgrammaticChange;

        public void SetMissionServices(CoreModel core, Dispatcher dispatcher, UnitCatalogService catalog)
        {
            this.missionCoreModel = core;
            this.missionDispatcher = dispatcher;
            this.missionUnitCatalog = catalog;
            this.BuildMissionOtaToolStrip();
            this.BuildMissionTabContent();
            var mapStream = core.PropertyAsObservable(x => x.Map, nameof(core.Map));
            mapStream.Subscribe(_ => this.OnMissionMapChanged());
            catalog.NamesChanged += (_, __) => this.RefreshMissionUnitsList();
            this.RefreshMissionUnitsList();
        }

        private void OnMissionMapChanged()
        {
            this.RefreshMissionSchemaCombo();
            var hasMap = this.missionCoreModel != null && this.missionCoreModel.Map.IsSome;
            if (this.missionOtaToolStrip != null)
            {
                this.missionOtaToolStrip.Visible = hasMap;
            }
        }

        private void BuildMissionOtaToolStrip()
        {
            if (this.missionOtaToolStrip != null)
            {
                return;
            }

            this.missionToolbarSchemaCombo = new ToolStripComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 220,
            };
            this.missionToolbarSchemaCombo.SelectedIndexChanged += this.MissionToolbarSchemaCombo_SelectedIndexChanged;

            this.missionOtaToolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                Stretch = false,
                Visible = false,
                Padding = new Padding(6, 2, 6, 2),
            };
            this.missionOtaToolStrip.Items.Add(new ToolStripLabel("OTA schema:"));
            this.missionOtaToolStrip.Items.Add(this.missionToolbarSchemaCombo);

            this.Controls.Add(this.missionOtaToolStrip);
            var menuIdx = this.Controls.IndexOf(this.topMenu);
            if (menuIdx >= 0)
            {
                this.Controls.SetChildIndex(this.missionOtaToolStrip, menuIdx + 1);
            }
        }

        private void BuildMissionTabContent()
        {
            this.missionTab = this.otaMissionTab;
            if (this.missionTab.Controls.Count > 0)
            {
                return;
            }

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            root.Controls.Add(new Label { Text = "Schema", AutoSize = true, Margin = new Padding(0, 0, 0, 4) }, 0, 0);

            this.missionSchemaCombo = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 0, 0, 8),
            };
            this.missionSchemaCombo.SelectedIndexChanged += this.MissionSchemaCombo_SelectedIndexChanged;
            root.Controls.Add(this.missionSchemaCombo, 0, 1);

            var schBtns = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 0, 0, 8) };
            var addSch = new Button { Text = "Add schema", AutoSize = true };
            addSch.Click += (_, __) =>
                {
                    this.missionDispatcher?.AddMapSchema();
                    this.RefreshMissionSchemaCombo();
                };
            var remSch = new Button { Text = "Remove schema", AutoSize = true };
            remSch.Click += (_, __) =>
                {
                    this.missionDispatcher?.RemoveActiveMapSchema();
                    this.RefreshMissionSchemaCombo();
                };
            schBtns.Controls.Add(addSch);
            schBtns.Controls.Add(remSch);
            root.Controls.Add(schBtns, 0, 2);

            root.Controls.Add(
                new Label
                    {
                        Text = "Unit types",
                        AutoSize = true,
                        Margin = new Padding(0, 4, 0, 4),
                    },
                0,
                3);

            this.missionUnitsList = new ListBox
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false,
            };
            this.missionUnitsList.SelectedIndexChanged += (_, __) =>
                {
                    if (this.missionUnitsList.SelectedItem is string s)
                    {
                        this.missionUnitCatalog.SelectedUnitName = s;
                    }
                };
            this.missionUnitsList.MouseDown += this.MissionUnitsList_MouseDown;
            root.Controls.Add(this.missionUnitsList, 0, 4);

            this.missionTab.Controls.Add(root);
        }

        private void MissionToolbarSchemaCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.missionSchemaComboProgrammaticChange)
            {
                return;
            }

            if (this.missionToolbarSchemaCombo.SelectedIndex < 0)
            {
                return;
            }

            this.ApplySchemaIndexFromUi(this.missionToolbarSchemaCombo.SelectedIndex);
        }

        private void MissionSchemaCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.missionSchemaComboProgrammaticChange)
            {
                return;
            }

            if (this.missionSchemaCombo.SelectedIndex < 0)
            {
                return;
            }

            this.ApplySchemaIndexFromUi(this.missionSchemaCombo.SelectedIndex);
        }

        private void ApplySchemaIndexFromUi(int index)
        {
            this.missionDispatcher?.SetActiveSchemaIndex(index);
            this.SyncSchemaCombosToIndex(index);
        }

        private void SyncSchemaCombosToIndex(int index)
        {
            this.missionSchemaComboProgrammaticChange = true;
            try
            {
                if (this.missionSchemaCombo != null
                    && index >= 0
                    && index < this.missionSchemaCombo.Items.Count)
                {
                    this.missionSchemaCombo.SelectedIndex = index;
                }

                if (this.missionToolbarSchemaCombo != null
                    && index >= 0
                    && index < this.missionToolbarSchemaCombo.Items.Count)
                {
                    this.missionToolbarSchemaCombo.SelectedIndex = index;
                }
            }
            finally
            {
                this.missionSchemaComboProgrammaticChange = false;
            }
        }

        private void RefreshMissionSchemaCombo()
        {
            if (this.missionSchemaCombo == null)
            {
                return;
            }

            this.missionSchemaComboProgrammaticChange = true;
            try
            {
                this.missionSchemaCombo.Items.Clear();
                if (this.missionToolbarSchemaCombo != null)
                {
                    this.missionToolbarSchemaCombo.Items.Clear();
                }

                this.missionCoreModel?.Map.IfSome(
                    m =>
                        {
                            for (var i = 0; i < m.Attributes.Schemas.Count; i++)
                            {
                                var sch = m.Attributes.Schemas[i];
                                var label = $"Schema {i}: {sch.SchemaType}";
                                this.missionSchemaCombo.Items.Add(label);
                                this.missionToolbarSchemaCombo?.Items.Add(label);
                            }

                            var idx = Math.Min(Math.Max(0, m.ActiveSchemaIndex), Math.Max(0, m.Attributes.Schemas.Count - 1));
                            if (this.missionSchemaCombo.Items.Count > 0)
                            {
                                this.missionSchemaCombo.SelectedIndex = idx;
                            }

                            if (this.missionToolbarSchemaCombo != null && this.missionToolbarSchemaCombo.Items.Count > 0)
                            {
                                this.missionToolbarSchemaCombo.SelectedIndex = idx;
                            }
                        });
            }
            finally
            {
                this.missionSchemaComboProgrammaticChange = false;
            }
        }

        private void MissionUnitsList_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            var i = this.missionUnitsList.IndexFromPoint(e.Location);
            if (i < 0 || !(this.missionUnitsList.Items[i] is string name) || string.IsNullOrEmpty(name))
            {
                return;
            }

            this.missionUnitsList.SelectedIndex = i;
            this.missionUnitCatalog.SelectedUnitName = name;
            this.missionUnitsList.DoDragDrop("MAPPYUNIT|" + name, DragDropEffects.Copy);
        }

        private void RefreshMissionUnitsList()
        {
            if (this.missionUnitsList == null || this.missionUnitCatalog == null)
            {
                return;
            }

            var sel = this.missionUnitCatalog.SelectedUnitName;
            this.missionUnitsList.Items.Clear();
            foreach (var n in this.missionUnitCatalog.EnumerateSorted())
            {
                this.missionUnitsList.Items.Add(n);
            }

            if (!string.IsNullOrEmpty(sel))
            {
                var i = this.missionUnitsList.Items.Cast<string>().ToList().FindIndex(x => string.Equals(x, sel, StringComparison.OrdinalIgnoreCase));
                if (i >= 0)
                {
                    this.missionUnitsList.SelectedIndex = i;
                }
            }
        }
    }
}