namespace Mappy.UI.Forms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Windows.Forms;

    using Mappy.Data;
    using Mappy.Services;

    public class UnitPropertiesForm : Form
    {
        private const int FormClientWidth = 360;
        private const int MissionPanelWidth = 540;
        private const int TabAreaHeight = 340;
        private const int BottomBarHeight = 50;

        private TabControl tabControl;

        private ComboBox comboSchema;

        private ComboBox comboPlayer;

        private TextBox textUnitInternalId;

        private TextBox textUnitFriendlyName;

        private TextBox textIdent;
        private NumericUpDown numericHealth;
        private NumericUpDown numericAngle;
        private NumericUpDown numericKills;
        private NumericUpDown numericX;
        private NumericUpDown numericY;
        private NumericUpDown numericZ;

        private TextBox textInitialMission;
        private NumericUpDown numericBuildPriority;
        private CheckBox checkAiPriorityTarget;
        private CheckBox checkMissionCriticalUnit;
        private CheckBox checkAiIgnore;
        private CheckBox checkImmunity;

        private Panel missionHost;

        private MissionEditorPanel missionEditor;

        private bool missionExpanded;

        private bool syncingMissionText;

        private UnitCatalogService missionCatalog;

        private Action<Action<int, int>, Action, bool> beginMissionPick;

        private Action<Action<int, int>> cancelMissionPick;

        public UnitPropertiesForm()
        {
            this.Text = "Unit properties";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            const int labelW = 100;
            const int fieldX = 120;

            void AddLabeledRow(Control parent, string label, Control c, ref int rowY)
            {
                parent.Controls.Add(new Label { Text = label, Location = new Point(12, rowY + 3), Width = labelW });
                c.Location = new Point(fieldX, rowY);
                parent.Controls.Add(c);
                rowY += 28;
            }

            this.tabControl = new TabControl
            {
                Location = new Point(0, 0),
                Size = new Size(FormClientWidth, TabAreaHeight),
            };

            var statsPage = new TabPage("Stats");
            int y = 12;

            this.textUnitInternalId = new TextBox
            {
                Width = 220,
                ReadOnly = true,
                TabStop = false,
                BackColor = SystemColors.Control,
            };
            AddLabeledRow(statsPage, "Unit ID", this.textUnitInternalId, ref y);

            this.textUnitFriendlyName = new TextBox
            {
                Width = 220,
                ReadOnly = true,
                TabStop = false,
                BackColor = SystemColors.Control,
            };
            AddLabeledRow(statsPage, "Friendly name", this.textUnitFriendlyName, ref y);

            this.comboSchema = new ComboBox
            {
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            AddLabeledRow(statsPage, "Schema", this.comboSchema, ref y);

            this.comboPlayer = new ComboBox
            {
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            for (var p = 1; p <= 11; p++)
            {
                this.comboPlayer.Items.Add("Player " + p);
            }

            AddLabeledRow(statsPage, "Player", this.comboPlayer, ref y);

            this.textIdent = new TextBox { Width = 200 };
            AddLabeledRow(statsPage, "Identifier", this.textIdent, ref y);

            this.numericHealth = new NumericUpDown { Minimum = 1, Maximum = 100, Width = 80 };
            AddLabeledRow(statsPage, "Health %", this.numericHealth, ref y);

            this.numericAngle = new NumericUpDown { Minimum = 0, Maximum = 65535, Width = 80 };
            AddLabeledRow(statsPage, "Angle", this.numericAngle, ref y);

            this.numericKills = new NumericUpDown { Minimum = 0, Maximum = 100000, Width = 80 };
            AddLabeledRow(statsPage, "Kills", this.numericKills, ref y);

            this.numericX = new NumericUpDown { Minimum = -10000000, Maximum = 10000000, Width = 100 };
            AddLabeledRow(statsPage, "XPos", this.numericX, ref y);

            this.numericY = new NumericUpDown { Minimum = -10000000, Maximum = 10000000, Width = 100 };
            AddLabeledRow(statsPage, "YPos (Height)", this.numericY, ref y);

            this.numericZ = new NumericUpDown { Minimum = -10000000, Maximum = 10000000, Width = 100 };
            AddLabeledRow(statsPage, "ZPos", this.numericZ, ref y);

            var aiPage = new TabPage("AI");
            int aiY = 12;

            aiPage.Controls.Add(new Label
            {
                Text = "Initial Mission",
                Location = new Point(12, aiY + 3),
                Width = labelW,
            });
            this.textInitialMission = new TextBox
            {
                Width = 140,
                Location = new Point(fieldX, aiY),
            };
            var setMission = new Button
            {
                Text = ">>",
                Location = new Point(fieldX + 146, aiY - 1),
                Width = 54,
            };
            setMission.Click += (s, e) => this.SetMissionExpanded(true);
            this.textInitialMission.TextChanged += (s, e) => this.MissionTextBoxChanged();
            aiPage.Controls.Add(this.textInitialMission);
            aiPage.Controls.Add(setMission);
            aiY += 28;

            this.numericBuildPriority = new NumericUpDown { Minimum = -1000000, Maximum = 1000000, Width = 100 };
            AddLabeledRow(aiPage, "Build Priority", this.numericBuildPriority, ref aiY);

            this.checkAiPriorityTarget = new CheckBox
            {
                Text = "Priority Target Of AI",
                AutoSize = true,
                Location = new Point(12, aiY),
            };
            aiPage.Controls.Add(this.checkAiPriorityTarget);
            aiY += 28;

            this.checkMissionCriticalUnit = new CheckBox
            {
                Text = "Mission Critical Unit",
                AutoSize = true,
                Location = new Point(12, aiY),
            };
            aiPage.Controls.Add(this.checkMissionCriticalUnit);
            aiY += 28;

            this.checkAiIgnore = new CheckBox
            {
                Text = "AI Should Ignore Unit",
                AutoSize = true,
                Location = new Point(12, aiY),
            };
            aiPage.Controls.Add(this.checkAiIgnore);
            aiY += 28;

            this.checkImmunity = new CheckBox
            {
                Text = "Enemy Does Not Target (Immunity)",
                AutoSize = true,
                Location = new Point(12, aiY),
            };
            aiPage.Controls.Add(this.checkImmunity);

            this.tabControl.TabPages.Add(statsPage);
            this.tabControl.TabPages.Add(aiPage);
            this.Controls.Add(this.tabControl);

            var show = new Button { Text = "Show", Location = new Point(12, TabAreaHeight + 8), Width = 75, Height = 23 };
            show.Click += (s, e) => this.ShowRequested?.Invoke(this, EventArgs.Empty);

            var ok = new Button { Text = "OK", Location = new Point(180, TabAreaHeight + 8), Width = 75, Height = 23 };
            ok.Click += (s, e) =>
                {
                    this.ApplyMissionEditorToText();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };

            var cancel = new Button { Text = "Cancel", Location = new Point(265, TabAreaHeight + 8), Width = 75, Height = 23 };
            cancel.Click += (s, e) =>
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                };

            this.AcceptButton = ok;
            this.CancelButton = cancel;
            this.missionHost = new Panel
            {
                Location = new Point(FormClientWidth, 0),
                Size = new Size(MissionPanelWidth, TabAreaHeight + BottomBarHeight),
                Visible = false,
            };
            this.Controls.Add(show);
            this.Controls.Add(ok);
            this.Controls.Add(cancel);
            this.Controls.Add(this.missionHost);
            this.ClientSize = new Size(FormClientWidth, TabAreaHeight + BottomBarHeight);
        }

        public event EventHandler ShowRequested;

        public string InitialMissionText
        {
            get
            {
                return this.textInitialMission.Text ?? string.Empty;
            }

            set
            {
                this.textInitialMission.Text = value ?? string.Empty;
            }
        }

        public int SelectedSchemaIndex => this.comboSchema.SelectedIndex;

        public void ConfigureMissionEditing(
            UnitCatalogService catalog,
            Action<Action<int, int>, Action, bool> beginPick,
            Action<Action<int, int>> cancelPick)
        {
            this.missionCatalog = catalog;
            this.beginMissionPick = beginPick;
            this.cancelMissionPick = cancelPick;
        }

        public void Bind(SchemaUnit u, int schemaIndex, IReadOnlyList<MapSchema> schemas, UnitCatalogService catalog)
        {
            var internalName = u.Unitname ?? string.Empty;
            this.textUnitInternalId.Text = internalName;
            this.textUnitFriendlyName.Text = catalog != null
                ? catalog.GetUnitFriendlyDisplayName(internalName)
                : string.Empty;

            this.comboSchema.Items.Clear();
            for (var i = 0; i < schemas.Count; i++)
            {
                this.comboSchema.Items.Add("Schema " + i + ": " + schemas[i].SchemaType);
            }

            if (schemas.Count > 0)
            {
                var si = Math.Max(0, Math.Min(schemaIndex, schemas.Count - 1));
                this.comboSchema.SelectedIndex = si;
            }

            var pi = Math.Max(0, Math.Min(u.Player - 1, 10));
            this.comboPlayer.SelectedIndex = pi;

            this.textIdent.Text = u.Ident;
            this.numericHealth.Value = ClampToNumericRange(this.numericHealth, u.HealthPercentage);
            this.numericAngle.Value = ClampToNumericRange(this.numericAngle, u.Angle);
            this.numericKills.Value = ClampToNumericRange(this.numericKills, u.Kills);
            this.numericX.Value = ClampToNumericRange(this.numericX, u.XPos);
            this.numericY.Value = ClampToNumericRange(this.numericY, u.YPos);
            this.numericZ.Value = ClampToNumericRange(this.numericZ, u.ZPos);

            this.textInitialMission.Text = u.InitialMission ?? string.Empty;
            this.numericBuildPriority.Value = ClampToNumericRange(this.numericBuildPriority, u.BuildPriority);
            this.checkAiPriorityTarget.Checked = u.AiPriorityTarget;
            this.checkMissionCriticalUnit.Checked = u.MissionCriticalUnit;
            this.checkAiIgnore.Checked = u.AiIgnore;
            this.checkImmunity.Checked = u.Immunity;
        }

        public void ApplyTo(SchemaUnit u)
        {
            u.Player = this.comboPlayer.SelectedIndex >= 0 ? this.comboPlayer.SelectedIndex + 1 : 1;
            u.Ident = this.textIdent.Text ?? string.Empty;
            u.HealthPercentage = (int)this.numericHealth.Value;
            u.Angle = (int)this.numericAngle.Value;
            u.Kills = (int)this.numericKills.Value;
            u.XPos = (int)this.numericX.Value;
            u.YPos = (int)this.numericY.Value;
            u.ZPos = (int)this.numericZ.Value;

            u.InitialMission = this.textInitialMission.Text ?? string.Empty;
            u.BuildPriority = (int)this.numericBuildPriority.Value;
            u.AiPriorityTarget = this.checkAiPriorityTarget.Checked;
            u.MissionCriticalUnit = this.checkMissionCriticalUnit.Checked;
            u.AiIgnore = this.checkAiIgnore.Checked;
            u.Immunity = this.checkImmunity.Checked;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (this.missionExpanded && this.missionEditor != null && this.missionEditor.TryHandleCommandKey(keyData))
            {
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape && this.missionEditor != null && this.missionEditor.TryCancelPick())
            {
                return true;
            }

            return base.ProcessDialogKey(keyData);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            this.missionEditor?.TryCancelPick();
            base.OnFormClosed(e);
        }

        private static decimal ClampToNumericRange(NumericUpDown n, int v)
        {
            var d = (decimal)v;
            if (d < n.Minimum)
            {
                return n.Minimum;
            }

            if (d > n.Maximum)
            {
                return n.Maximum;
            }

            return d;
        }

        private void SetMissionExpanded(bool expanded)
        {
            if (expanded == this.missionExpanded)
            {
                return;
            }

            if (expanded)
            {
                this.EnsureMissionEditor();
                this.missionEditor.LoadMission(this.InitialMissionText);
                this.missionHost.Visible = true;
                this.KeepExpandedFormOnScreen();
                this.ClientSize = new Size(FormClientWidth + MissionPanelWidth, this.ClientSize.Height);
                this.missionExpanded = true;
                return;
            }

            this.ApplyMissionEditorToText();
            this.missionEditor?.TryCancelPick();
            this.missionHost.Visible = false;
            this.ClientSize = new Size(FormClientWidth, this.ClientSize.Height);
            this.missionExpanded = false;
        }

        private void EnsureMissionEditor()
        {
            if (this.missionEditor != null)
            {
                return;
            }

            this.missionEditor = new MissionEditorPanel(this.missionCatalog, this.beginMissionPick, this.cancelMissionPick)
            {
                Dock = DockStyle.Fill,
            };
            this.missionEditor.CollapseRequested += (s, e) => this.SetMissionExpanded(false);
            this.missionEditor.MissionChanged += (s, e) => this.ApplyMissionEditorToText();
            this.missionHost.Controls.Add(this.missionEditor);
        }

        private void ApplyMissionEditorToText()
        {
            if (this.missionEditor == null)
            {
                return;
            }

            this.missionEditor.CommitEdit();
            this.syncingMissionText = true;
            this.textInitialMission.Text = this.missionEditor.MissionText;
            this.syncingMissionText = false;
        }

        private void MissionTextBoxChanged()
        {
            if (this.syncingMissionText || !this.missionExpanded || this.missionEditor == null)
            {
                return;
            }

            this.missionEditor.LoadMission(this.textInitialMission.Text);
        }

        private void KeepExpandedFormOnScreen()
        {
            var borderWidth = this.Width - this.ClientSize.Width;
            var targetRight = this.Left + FormClientWidth + MissionPanelWidth + borderWidth;
            var screen = Screen.FromControl(this).WorkingArea;
            if (targetRight > screen.Right)
            {
                this.Left = Math.Max(screen.Left, this.Left - (targetRight - screen.Right));
            }
        }
    }
}
