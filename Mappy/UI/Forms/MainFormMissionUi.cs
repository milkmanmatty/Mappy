namespace Mappy.UI.Forms
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows.Forms;

    using Mappy;
    using Mappy.Data;
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

        private TabControl missionUnitPickerTabs;

        private TreeView missionArmUnitsTree;

        private TreeView missionCoreUnitsTree;

        private TreeView missionOtherUnitsTree;

        private TreeView missionPlacedUnitsTree;

        private MapAttributes missionPlacedUnitsAttributesSubscription;

        private Button missionAddSchemaButton;

        private Button missionRemoveSchemaButton;

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
            this.OnMissionMapChanged();
        }

        private void OnMissionMapChanged()
        {
            this.DetachMissionPlacedUnitsListener();
            this.RefreshMissionSchemaCombo();
            this.AttachMissionPlacedUnitsListener();
            var hasMap = this.missionCoreModel != null && this.missionCoreModel.Map.IsSome;
            if (this.missionOtaToolStrip != null)
            {
                this.missionOtaToolStrip.Visible = hasMap;
            }

            if (this.missionAddSchemaButton != null)
            {
                this.missionAddSchemaButton.Enabled = hasMap;
            }

            if (this.missionRemoveSchemaButton != null)
            {
                this.missionRemoveSchemaButton.Enabled = hasMap;
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
                RowCount = 7,
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

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
            this.missionAddSchemaButton = new Button { Text = "Add schema", AutoSize = true, Enabled = false };
            this.missionAddSchemaButton.Click += (_, __) =>
                {
                    this.missionDispatcher?.AddMapSchema();
                    this.RefreshMissionSchemaCombo();
                };
            this.missionRemoveSchemaButton = new Button { Text = "Remove schema", AutoSize = true, Enabled = false };
            this.missionRemoveSchemaButton.Click += (_, __) =>
                {
                    this.missionDispatcher?.RemoveActiveMapSchema();
                    this.RefreshMissionSchemaCombo();
                };
            schBtns.Controls.Add(this.missionAddSchemaButton);
            schBtns.Controls.Add(this.missionRemoveSchemaButton);
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

            this.missionUnitPickerTabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 0),
            };

            var tabAll = new TabPage("All");
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
            tabAll.Controls.Add(this.missionUnitsList);

            this.missionArmUnitsTree = this.CreateMissionSideUnitTree();
            this.missionCoreUnitsTree = this.CreateMissionSideUnitTree();
            this.missionOtherUnitsTree = this.CreateMissionSideUnitTree();

            this.missionUnitPickerTabs.TabPages.Add(tabAll);
            var tabArm = new TabPage("ARM");
            tabArm.Controls.Add(this.missionArmUnitsTree);
            this.missionUnitPickerTabs.TabPages.Add(tabArm);
            var tabCore = new TabPage("CORE");
            tabCore.Controls.Add(this.missionCoreUnitsTree);
            this.missionUnitPickerTabs.TabPages.Add(tabCore);
            var tabOther = new TabPage("Other");
            tabOther.Controls.Add(this.missionOtherUnitsTree);
            this.missionUnitPickerTabs.TabPages.Add(tabOther);

            root.Controls.Add(this.missionUnitPickerTabs, 0, 4);

            root.Controls.Add(
                new Label
                    {
                        Text = "Placed units",
                        AutoSize = true,
                        Margin = new Padding(0, 4, 0, 4),
                    },
                0,
                5);

            this.missionPlacedUnitsTree = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false,
                Margin = new Padding(0, 0, 0, 0),
            };
            this.missionPlacedUnitsTree.NodeMouseDoubleClick += this.MissionPlacedUnitsTree_NodeMouseDoubleClick;
            root.Controls.Add(this.missionPlacedUnitsTree, 0, 6);

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

            this.RefreshMissionPlacedUnitsTree();
        }

        private void AttachMissionPlacedUnitsListener()
        {
            this.missionCoreModel?.Map.IfSome(
                m =>
                    {
                        this.missionPlacedUnitsAttributesSubscription = m.Attributes;
                        this.missionPlacedUnitsAttributesSubscription.SchemaUnitsChanged += this.MissionPlacedUnits_SchemaUnitsChanged;
                    });
        }

        private void DetachMissionPlacedUnitsListener()
        {
            if (this.missionPlacedUnitsAttributesSubscription != null)
            {
                this.missionPlacedUnitsAttributesSubscription.SchemaUnitsChanged -= this.MissionPlacedUnits_SchemaUnitsChanged;
                this.missionPlacedUnitsAttributesSubscription = null;
            }
        }

        private void MissionPlacedUnits_SchemaUnitsChanged(object sender, SchemaUnitsChangedEventArgs e)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(this.RefreshMissionPlacedUnitsTree));
            }
            else
            {
                this.RefreshMissionPlacedUnitsTree();
            }
        }

        private void RefreshMissionPlacedUnitsTree()
        {
            if (this.missionPlacedUnitsTree == null || this.missionPlacedUnitsTree.IsDisposed)
            {
                return;
            }

            this.missionPlacedUnitsTree.BeginUpdate();
            try
            {
                this.missionPlacedUnitsTree.Nodes.Clear();
                this.missionCoreModel?.Map.IfSome(
                    m =>
                        {
                            for (var s = 0; s < m.Attributes.Schemas.Count; s++)
                            {
                                var sch = m.Attributes.Schemas[s];
                                var parentText = $"Schema {s}: {sch.SchemaType}";
                                var parent = new TreeNode(parentText)
                                {
                                    Tag = new MissionPlacedUnitTreeTag(s, null),
                                };
                                foreach (var u in sch.Units.OrderBy(x => x.Unitname, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Player).ThenBy(x => x.Id))
                                {
                                    var label = string.IsNullOrEmpty(u.Ident)
                                        ? $"{u.Unitname} [P{u.Player}]"
                                        : $"{u.Unitname} [P{u.Player}] — {u.Ident}";
                                    parent.Nodes.Add(
                                        new TreeNode(label)
                                        {
                                            Tag = new MissionPlacedUnitTreeTag(s, u.Id),
                                        });
                                }

                                parent.Expand();
                                this.missionPlacedUnitsTree.Nodes.Add(parent);
                            }
                        });
            }
            finally
            {
                this.missionPlacedUnitsTree.EndUpdate();
            }
        }

        private void MissionPlacedUnitsTree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag is MissionPlacedUnitTreeTag tag && tag.UnitId.HasValue)
            {
                this.missionDispatcher?.CenterViewOnSchemaUnit(tag.SchemaIndex, tag.UnitId.Value);
            }
        }

        private readonly struct MissionPlacedUnitTreeTag
        {
            public MissionPlacedUnitTreeTag(int schemaIndex, Guid? unitId)
            {
                this.SchemaIndex = schemaIndex;
                this.UnitId = unitId;
            }

            public int SchemaIndex { get; }

            public Guid? UnitId { get; }
        }

        private TreeView CreateMissionSideUnitTree()
        {
            var tv = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false,
                ShowLines = true,
                ShowRootLines = false,
            };
            tv.AfterSelect += this.MissionSideUnitTree_AfterSelect;
            tv.ItemDrag += this.MissionSideUnitTree_ItemDrag;
            return tv;
        }

        private void MissionSideUnitTree_AfterSelect(object sender, EventArgs e)
        {
            if (sender is TreeView tv && tv.SelectedNode?.Tag is string s && !string.IsNullOrEmpty(s))
            {
                this.missionUnitCatalog.SelectedUnitName = s;
            }
        }

        private void MissionSideUnitTree_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (!(sender is TreeView tv))
            {
                return;
            }

            if (!(e.Item is TreeNode n) || !(n.Tag is string name) || string.IsNullOrEmpty(name))
            {
                return;
            }

            tv.SelectedNode = n;
            this.missionUnitCatalog.SelectedUnitName = name;
            tv.DoDragDrop("MAPPYUNIT|" + name, DragDropEffects.Copy);
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

            this.RefreshMissionSideUnitTrees(sel);
        }

        private void RefreshMissionSideUnitTrees(string preferredSelection)
        {
            this.FillSideUnitTree(this.missionArmUnitsTree, UnitSideCategory.Arm, preferredSelection);
            this.FillSideUnitTree(this.missionCoreUnitsTree, UnitSideCategory.Core, preferredSelection);
            this.FillSideUnitTree(this.missionOtherUnitsTree, UnitSideCategory.Other, preferredSelection);
        }

        private void FillSideUnitTree(TreeView tv, UnitSideCategory side, string preferredSelection)
        {
            if (tv == null || tv.IsDisposed || this.missionUnitCatalog == null)
            {
                return;
            }

            tv.BeginUpdate();
            try
            {
                tv.Nodes.Clear();
                foreach (var name in this.missionUnitCatalog.EnumerateSorted())
                {
                    if (this.missionUnitCatalog.GetUnitSide(name) != side)
                    {
                        continue;
                    }

                    tv.Nodes.Add(new TreeNode(name) { Tag = name });
                }

                if (!string.IsNullOrEmpty(preferredSelection)
                    && this.missionUnitCatalog.GetUnitSide(preferredSelection) == side)
                {
                    foreach (TreeNode n in tv.Nodes)
                    {
                        if (n.Tag is string t && string.Equals(t, preferredSelection, StringComparison.OrdinalIgnoreCase))
                        {
                            tv.SelectedNode = n;
                            break;
                        }
                    }
                }
            }
            finally
            {
                tv.EndUpdate();
            }
        }
    }
}