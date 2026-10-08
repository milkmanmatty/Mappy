namespace Mappy.UI.Forms
{
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.Windows.Forms;

    using Mappy.Services;
    using Mappy.Util;

    public sealed class MissionEditorPanel : UserControl
    {
        private const string IdleStatus = "Keys: M move, P patrol. Esc cancels map picking.";

        private readonly UnitCatalogService catalog;

        private readonly Action<Action<int, int>, Action, bool> beginPick;

        private readonly Action<Action<int, int>> cancelPickIfOwner;

        private readonly DataGridView grid;

        private readonly Label statusLabel;

        private readonly Button doneButton;

        private Action<int, int> pickHandler;

        private bool loadingMission;

        public MissionEditorPanel(
            UnitCatalogService catalog,
            Action<Action<int, int>, Action, bool> beginPick,
            Action<Action<int, int>> cancelPickIfOwner)
        {
            this.catalog = catalog;
            this.beginPick = beginPick;
            this.cancelPickIfOwner = cancelPickIfOwner;

            this.grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
                BorderStyle = BorderStyle.Fixed3D,
                BackgroundColor = SystemColors.Window,
            };
            this.grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Command",
                HeaderText = "Command",
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
            this.grid.KeyDown += this.GridKeyDown;
            this.grid.CellEndEdit += (s, e) => this.NotifyMissionChanged();

            var tools = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(6, 8, 0, 0),
            };
            tools.Controls.Add(this.MakeTool("Move", (s, e) => this.BeginMovePick()));
            tools.Controls.Add(this.MakeTool("Patrol", (s, e) => this.BeginPatrolPick()));
            tools.Controls.Add(this.MakeTool("Wait", (s, e) => this.AddWait()));
            tools.Controls.Add(this.MakeTool("Orders", (s, e) => this.AddOrders()));
            tools.Controls.Add(this.MakeTool("Build", (s, e) => this.AddBuild()));
            tools.Controls.Add(this.MakeTool("Wait attacked", (s, e) => this.AddWaitAttacked()));
            tools.Controls.Add(this.MakeTool("Attack", (s, e) => this.AddAttack()));
            tools.Controls.Add(this.MakeTool("Guard", (s, e) => this.AddGuard()));
            tools.Controls.Add(this.MakeTool("Unload", (s, e) => this.BeginUnloadPick()));
            tools.Controls.Add(this.MakeTool("Silo", (s, e) => this.AddSilo()));
            tools.Controls.Add(this.MakeTool("Selectable", (s, e) => this.AppendCommand("s")));
            tools.Controls.Add(this.MakeTool("Self-destruct", (s, e) => this.AppendCommand("d")));

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168f));
            body.Controls.Add(this.grid, 0, 0);
            body.Controls.Add(tools, 1, 0);

            this.statusLabel = new Label
            {
                Text = IdleStatus,
                AutoSize = false,
                Dock = DockStyle.Bottom,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            this.doneButton = new Button
            {
                Text = "Done",
                Width = 75,
                Height = 23,
                Enabled = false,
            };
            this.doneButton.Click += (s, e) => this.CancelOwnPick();

            var up = new Button { Text = "Up", Width = 75, Height = 23 };
            up.Click += (s, e) => this.MoveSelected(-1);
            var down = new Button { Text = "Down", Width = 75, Height = 23 };
            down.Click += (s, e) => this.MoveSelected(1);
            var add = new Button { Text = "Add", Width = 75, Height = 23 };
            add.Click += (s, e) => this.AddBlankCommand();
            var delete = new Button { Text = "Delete", Width = 75, Height = 23 };
            delete.Click += (s, e) => this.DeleteSelected();

            var commandButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
            };
            var commandX = 8;
            foreach (var commandButton in new[] { up, down, add, delete, this.doneButton })
            {
                commandButton.Location = new Point(commandX, 0);
                commandButtons.Controls.Add(commandButton);
                commandX += commandButton.Width + 6;
            }

            var collapse = new Button
            {
                Text = "<<",
                Width = 40,
                Height = 24,
                Location = new Point(8, 4),
            };
            collapse.Click += (s, e) => this.CollapseRequested?.Invoke(this, EventArgs.Empty);
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
            };
            header.Controls.Add(collapse);

            this.Controls.Add(body);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(commandButtons);
            this.Controls.Add(header);
        }

        public event EventHandler CollapseRequested;

        public event EventHandler MissionChanged;

        private enum BuildPlacement
        {
            Cancel,
            AtPoint,
            Lab,
        }

        private enum AttackKind
        {
            Cancel,
            Unit,
            Point,
        }

        public string MissionText
        {
            get
            {
                var commands = new System.Collections.Generic.List<string>();
                foreach (DataGridViewRow row in this.grid.Rows)
                {
                    if (row.IsNewRow)
                    {
                        continue;
                    }

                    commands.Add(Convert.ToString(row.Cells[0].Value, CultureInfo.InvariantCulture));
                }

                return InitialMissionCommands.Join(commands);
            }
        }

        public void LoadMission(string initialMission)
        {
            this.loadingMission = true;
            this.grid.Rows.Clear();
            foreach (var command in InitialMissionCommands.Split(initialMission))
            {
                this.grid.Rows.Add(command);
            }

            if (this.grid.Rows.Count > 0)
            {
                this.grid.Rows[0].Selected = true;
            }

            this.loadingMission = false;
        }

        public void CommitEdit()
        {
            if (this.grid.IsCurrentCellInEditMode)
            {
                this.grid.EndEdit();
            }
        }

        public bool TryCancelPick()
        {
            if (this.pickHandler == null)
            {
                return false;
            }

            this.CancelOwnPick();
            return true;
        }

        public bool TryHandleCommandKey(Keys keyData)
        {
            if (this.grid.IsCurrentCellInEditMode)
            {
                return false;
            }

            if (keyData == Keys.M)
            {
                this.BeginMovePick();
                return true;
            }

            if (keyData == Keys.P)
            {
                this.BeginPatrolPick();
                return true;
            }

            return false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (this.TryHandleCommandKey(keyData))
            {
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape && this.TryCancelPick())
            {
                return true;
            }

            if (keyData == Keys.Escape && this.grid.IsCurrentCellInEditMode)
            {
                this.grid.CancelEdit();
                return true;
            }

            return base.ProcessDialogKey(keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.CancelOwnPick();
            }

            base.Dispose(disposing);
        }

        private Button MakeTool(string text, EventHandler click)
        {
            var button = new Button
            {
                Text = text,
                Width = 120,
                Height = 26,
                Margin = new Padding(0, 0, 0, 4),
            };
            button.Click += click;
            return button;
        }

        private void GridKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && !this.grid.IsCurrentCellInEditMode)
            {
                this.DeleteSelected();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void BeginMovePick()
        {
            this.BeginMapPick(
                (x, z) => "m " + x.ToString(CultureInfo.InvariantCulture) + " " + z.ToString(CultureInfo.InvariantCulture),
                "Move: click the map to add points. Esc or Done cancels.",
                true);
        }

        private void BeginPatrolPick()
        {
            this.BeginMapPick(
                (x, z) => "p " + x.ToString(CultureInfo.InvariantCulture) + " " + z.ToString(CultureInfo.InvariantCulture),
                "Patrol: click the map to add points. Esc or Done cancels.",
                true);
        }

        private void BeginUnloadPick()
        {
            this.BeginMapPick(
                (x, z) => "u " + x.ToString(CultureInfo.InvariantCulture) + " " + z.ToString(CultureInfo.InvariantCulture),
                "Unload: click the map. Esc or Done cancels.");
        }

        private void BeginBuildAtPointPick(string unitName)
        {
            this.BeginMapPick(
                (x, z) => "b " + unitName + " 1 " + x.ToString(CultureInfo.InvariantCulture) + " " + z.ToString(CultureInfo.InvariantCulture),
                "Build " + unitName + ": click the map. Esc or Done cancels.");
        }

        private void BeginAttackPointPick()
        {
            this.BeginMapPick(
                (x, z) => "a " + x.ToString(CultureInfo.InvariantCulture) + " " + z.ToString(CultureInfo.InvariantCulture),
                "Attack: click the map. Esc or Done cancels.");
        }

        private void BeginMapPick(Func<int, int, string> format, string status, bool precisionCursor = false)
        {
            if (this.beginPick == null)
            {
                return;
            }

            Action<int, int> handler = null;
            handler = (x, z) =>
            {
                if (this.IsDisposed || !ReferenceEquals(this.pickHandler, handler))
                {
                    return;
                }

                this.AppendCommand(format(x, z));
            };

            Action cancelled = () =>
            {
                if (!ReferenceEquals(this.pickHandler, handler))
                {
                    return;
                }

                this.pickHandler = null;
                if (this.IsDisposed)
                {
                    return;
                }

                this.doneButton.Enabled = false;
                this.statusLabel.Text = IdleStatus;
            };

            this.pickHandler = handler;
            this.doneButton.Enabled = true;
            this.statusLabel.Text = status;
            this.beginPick(handler, cancelled, precisionCursor);
        }

        private void NotifyMissionChanged()
        {
            if (this.loadingMission)
            {
                return;
            }

            this.MissionChanged?.Invoke(this, EventArgs.Empty);
        }

        private IWin32Window GetDialogOwner()
        {
            var form = this.FindForm();
            if (form != null)
            {
                return form;
            }

            return this;
        }

        private void CancelOwnPick()
        {
            var handler = this.pickHandler;
            if (handler == null || this.cancelPickIfOwner == null)
            {
                return;
            }

            this.cancelPickIfOwner(handler);
        }

        private void AddWait()
        {
            var seconds = this.PromptInt(this.GetDialogOwner(), "Wait", "Seconds", 60, 0, 1000000);
            if (!seconds.HasValue)
            {
                return;
            }

            this.AppendCommand("w " + seconds.Value.ToString(CultureInfo.InvariantCulture));
        }

        private void AddOrders()
        {
            if (!this.PromptOrders(this.GetDialogOwner(), out var fire, out var move))
            {
                return;
            }

            this.AppendCommand(
                "o " + fire.ToString(CultureInfo.InvariantCulture) + " " + move.ToString(CultureInfo.InvariantCulture));
        }

        private void AddBuild()
        {
            if (this.catalog == null)
            {
                return;
            }

            string unitName;
            using (var picker = new UnitPickerForm(this.catalog))
            {
                if (picker.ShowDialog(this) != DialogResult.OK || string.IsNullOrEmpty(picker.SelectedInternalName))
                {
                    return;
                }

                unitName = picker.SelectedInternalName;
            }

            var placement = this.PromptBuildPlacement(this.GetDialogOwner(), unitName);
            if (placement == BuildPlacement.AtPoint)
            {
                this.BeginBuildAtPointPick(unitName);
                return;
            }

            if (placement != BuildPlacement.Lab)
            {
                return;
            }

            var count = this.PromptInt(this.GetDialogOwner(), "Lab queue", "How many " + unitName + " to build", 1, 1, 100000);
            if (!count.HasValue)
            {
                return;
            }

            this.AppendCommand("b " + unitName + " " + count.Value.ToString(CultureInfo.InvariantCulture));
        }

        private void AddWaitAttacked()
        {
            var ident = this.PromptText(this.GetDialogOwner(), "Wait until attacked", "Identifier (optional)", string.Empty);
            if (ident == null)
            {
                return;
            }

            ident = ident.Trim();
            this.AppendCommand(ident.Length == 0 ? "wa" : "wa " + ident);
        }

        private void AddAttack()
        {
            var kind = this.PromptAttackKind(this.GetDialogOwner());
            if (kind == AttackKind.Point)
            {
                this.BeginAttackPointPick();
                return;
            }

            if (kind != AttackKind.Unit)
            {
                return;
            }

            var target = this.PromptText(this.GetDialogOwner(), "Attack", "Unit name or identifier", string.Empty);
            if (string.IsNullOrWhiteSpace(target))
            {
                return;
            }

            this.AppendCommand("a " + target.Trim());
        }

        private void AddGuard()
        {
            var ident = this.PromptText(this.GetDialogOwner(), "Guard", "Identifier", string.Empty);
            if (string.IsNullOrWhiteSpace(ident))
            {
                return;
            }

            this.AppendCommand("g " + ident.Trim());
        }

        private void AddSilo()
        {
            var count = this.PromptInt(this.GetDialogOwner(), "Silo", "Weapons to stockpile", 1, 1, 100000);
            if (!count.HasValue)
            {
                return;
            }

            this.AppendCommand("bw " + count.Value.ToString(CultureInfo.InvariantCulture));
        }

        private void AddBlankCommand()
        {
            var index = this.InsertIndex();
            this.grid.Rows.Insert(index, string.Empty);
            this.grid.CurrentCell = this.grid.Rows[index].Cells[0];
            this.grid.BeginEdit(true);
            this.NotifyMissionChanged();
        }

        private void AppendCommand(string text)
        {
            if (this.grid.IsCurrentCellInEditMode)
            {
                this.grid.EndEdit();
            }

            var index = this.grid.Rows.Add(text);
            this.grid.ClearSelection();
            this.grid.Rows[index].Selected = true;
            this.grid.CurrentCell = this.grid.Rows[index].Cells[0];
            if (this.grid.IsHandleCreated && index >= 0 && index < this.grid.RowCount)
            {
                this.grid.FirstDisplayedScrollingRowIndex = index;
            }

            this.NotifyMissionChanged();
        }

        private void DeleteSelected()
        {
            if (this.grid.CurrentRow == null || this.grid.CurrentRow.IsNewRow)
            {
                return;
            }

            var index = this.grid.CurrentRow.Index;
            this.grid.Rows.RemoveAt(index);
            if (this.grid.Rows.Count > 0)
            {
                var next = Math.Min(index, this.grid.Rows.Count - 1);
                this.grid.CurrentCell = this.grid.Rows[next].Cells[0];
            }

            this.NotifyMissionChanged();
        }

        private void MoveSelected(int delta)
        {
            if (this.grid.IsCurrentCellInEditMode)
            {
                this.grid.EndEdit();
            }

            if (this.grid.CurrentRow == null)
            {
                return;
            }

            var index = this.grid.CurrentRow.Index;
            var target = index + delta;
            if (target < 0 || target >= this.grid.Rows.Count)
            {
                return;
            }

            var current = this.grid.Rows[index].Cells[0].Value;
            this.grid.Rows[index].Cells[0].Value = this.grid.Rows[target].Cells[0].Value;
            this.grid.Rows[target].Cells[0].Value = current;
            this.grid.CurrentCell = this.grid.Rows[target].Cells[0];
            this.NotifyMissionChanged();
        }

        private int InsertIndex()
        {
            if (this.grid.CurrentRow == null)
            {
                return this.grid.Rows.Count;
            }

            return this.grid.CurrentRow.Index + 1;
        }

        private string PromptText(IWin32Window owner, string title, string label, string defaultValue)
        {
            using (var form = new Form())
            {
                form.Text = title;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ShowInTaskbar = false;
                form.ClientSize = new Size(320, 104);

                var caption = new Label
                {
                    Text = label,
                    AutoSize = true,
                    Location = new Point(12, 12),
                };
                var box = new TextBox
                {
                    Text = defaultValue ?? string.Empty,
                    Location = new Point(12, 36),
                    Width = 296,
                };
                var ok = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(152, 68),
                    Width = 75,
                };
                var cancel = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(233, 68),
                    Width = 75,
                };
                form.Controls.Add(caption);
                form.Controls.Add(box);
                form.Controls.Add(ok);
                form.Controls.Add(cancel);
                form.AcceptButton = ok;
                form.CancelButton = cancel;
                if (form.ShowDialog(owner) != DialogResult.OK)
                {
                    return null;
                }

                return box.Text ?? string.Empty;
            }
        }

        private int? PromptInt(IWin32Window owner, string title, string label, int defaultValue, int min, int max)
        {
            var text = this.PromptText(owner, title, label, defaultValue.ToString(CultureInfo.InvariantCulture));
            if (text == null)
            {
                return null;
            }

            if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                || value < min
                || value > max)
            {
                MessageBox.Show(
                    owner,
                    "Enter a whole number from " + min + " to " + max + ".",
                    title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return null;
            }

            return value;
        }

        private bool PromptOrders(IWin32Window owner, out int fire, out int move)
        {
            fire = 2;
            move = 1;
            using (var form = new Form())
            {
                form.Text = "Orders";
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ShowInTaskbar = false;
                form.ClientSize = new Size(280, 140);

                var fireLabel = new Label { Text = "Fire", AutoSize = true, Location = new Point(12, 16) };
                var fireBox = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(90, 12),
                    Width = 170,
                };
                fireBox.Items.AddRange(new object[] { "Hold Fire", "Return Fire", "Fire at Will" });
                fireBox.SelectedIndex = 2;

                var moveLabel = new Label { Text = "Movement", AutoSize = true, Location = new Point(12, 48) };
                var moveBox = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(90, 44),
                    Width = 170,
                };
                moveBox.Items.AddRange(new object[] { "Hold Position", "Maneuver", "Roam" });
                moveBox.SelectedIndex = 1;

                var ok = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(112, 100),
                    Width = 75,
                };
                var cancel = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(193, 100),
                    Width = 75,
                };
                form.Controls.Add(fireLabel);
                form.Controls.Add(fireBox);
                form.Controls.Add(moveLabel);
                form.Controls.Add(moveBox);
                form.Controls.Add(ok);
                form.Controls.Add(cancel);
                form.AcceptButton = ok;
                form.CancelButton = cancel;
                if (form.ShowDialog(owner) != DialogResult.OK)
                {
                    return false;
                }

                fire = fireBox.SelectedIndex;
                move = moveBox.SelectedIndex;
                return true;
            }
        }

        private BuildPlacement PromptBuildPlacement(IWin32Window owner, string unitName)
        {
            var result = BuildPlacement.Cancel;
            using (var form = new Form())
            {
                form.Text = "Build " + unitName;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ShowInTaskbar = false;
                form.ClientSize = new Size(280, 148);

                var caption = new Label
                {
                    Text = "Where should " + unitName + " be built?",
                    AutoSize = true,
                    Location = new Point(12, 12),
                };
                var atPoint = new Button
                {
                    Text = "At map point",
                    Location = new Point(12, 48),
                    Width = 120,
                };
                atPoint.Click += (s, e) =>
                {
                    result = BuildPlacement.AtPoint;
                    form.Close();
                };
                var lab = new Button
                {
                    Text = "Lab queue",
                    Location = new Point(148, 48),
                    Width = 120,
                };
                lab.Click += (s, e) =>
                {
                    result = BuildPlacement.Lab;
                    form.Close();
                };
                var cancel = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(193, 108),
                    Width = 75,
                };
                form.Controls.Add(caption);
                form.Controls.Add(atPoint);
                form.Controls.Add(lab);
                form.Controls.Add(cancel);
                form.CancelButton = cancel;
                form.ShowDialog(owner);
            }

            return result;
        }

        private AttackKind PromptAttackKind(IWin32Window owner)
        {
            var result = AttackKind.Cancel;
            using (var form = new Form())
            {
                form.Text = "Attack";
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ShowInTaskbar = false;
                form.ClientSize = new Size(280, 148);

                var caption = new Label
                {
                    Text = "Attack a unit, or a point on the map?",
                    AutoSize = true,
                    Location = new Point(12, 12),
                };
                var unit = new Button
                {
                    Text = "Unit / ident",
                    Location = new Point(12, 48),
                    Width = 120,
                };
                unit.Click += (s, e) =>
                {
                    result = AttackKind.Unit;
                    form.Close();
                };
                var point = new Button
                {
                    Text = "Map point",
                    Location = new Point(148, 48),
                    Width = 120,
                };
                point.Click += (s, e) =>
                {
                    result = AttackKind.Point;
                    form.Close();
                };
                var cancel = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(193, 108),
                    Width = 75,
                };
                form.Controls.Add(caption);
                form.Controls.Add(unit);
                form.Controls.Add(point);
                form.Controls.Add(cancel);
                form.CancelButton = cancel;
                form.ShowDialog(owner);
            }

            return result;
        }
    }
}
