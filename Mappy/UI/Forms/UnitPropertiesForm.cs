namespace Mappy.UI.Forms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Windows.Forms;

    using Mappy.Data;

    public class UnitPropertiesForm : Form
    {
        private const int TabAreaHeight = 300;
        private const int BottomBarHeight = 50;

        private TabControl tabControl;

        private ComboBox comboSchema;

        private ComboBox comboPlayer;

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
                Size = new Size(360, TabAreaHeight),
            };

            var statsPage = new TabPage("Stats");
            int y = 12;

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
            for (var p = 1; p <= 10; p++)
            {
                this.comboPlayer.Items.Add("Player " + p);
            }

            AddLabeledRow(statsPage, "Player", this.comboPlayer, ref y);

            this.textIdent = new TextBox { Width = 200 };
            AddLabeledRow(statsPage, "Identifier", this.textIdent, ref y);

            this.numericHealth = new NumericUpDown { Minimum = 1, Maximum = 100, Width = 80 };
            AddLabeledRow(statsPage, "Health %", this.numericHealth, ref y);

            this.numericAngle = new NumericUpDown { Minimum = -32768, Maximum = 32767, Width = 80 };
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

            this.textInitialMission = new TextBox { Width = 200 };
            AddLabeledRow(aiPage, "Initial Mission", this.textInitialMission, ref aiY);

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

            this.tabControl.TabPages.Add(statsPage);
            this.tabControl.TabPages.Add(aiPage);
            this.Controls.Add(this.tabControl);

            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(80, TabAreaHeight + 8) };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(180, TabAreaHeight + 8) };
            this.AcceptButton = ok;
            this.CancelButton = cancel;
            this.Controls.Add(ok);
            this.Controls.Add(cancel);
            this.ClientSize = new Size(360, TabAreaHeight + BottomBarHeight);
        }

        public int SelectedSchemaIndex => this.comboSchema.SelectedIndex;

        public void Bind(SchemaUnit u, int schemaIndex, IReadOnlyList<MapSchema> schemas)
        {
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

            var pi = Math.Max(0, Math.Min(u.Player - 1, 9));
            this.comboPlayer.SelectedIndex = pi;

            this.textIdent.Text = u.Ident;
            this.numericHealth.Value = Math.Max(this.numericHealth.Minimum, Math.Min(this.numericHealth.Maximum, u.HealthPercentage));
            this.numericAngle.Value = u.Angle;
            this.numericKills.Value = u.Kills;
            this.numericX.Value = ClampToNumericRange(this.numericX, u.XPos);
            this.numericY.Value = ClampToNumericRange(this.numericY, u.YPos);
            this.numericZ.Value = ClampToNumericRange(this.numericZ, u.ZPos);

            this.textInitialMission.Text = u.InitialMission ?? string.Empty;
            this.numericBuildPriority.Value = ClampToNumericRange(this.numericBuildPriority, u.BuildPriority);
            this.checkAiPriorityTarget.Checked = u.AiPriorityTarget;
            this.checkMissionCriticalUnit.Checked = u.MissionCriticalUnit;
            this.checkAiIgnore.Checked = u.AiIgnore;
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
    }
}
