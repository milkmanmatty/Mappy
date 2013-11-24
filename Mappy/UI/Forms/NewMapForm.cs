﻿namespace Mappy.UI.Forms
{
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;

    public partial class NewMapForm : Form
    {
        public NewMapForm()
        {
            this.InitializeComponent();
        }

        public int MapWidth
        {
            get; private set;
        }

        public int MapHeight
        {
            get; private set;
        }

        private void NewMapForm_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                int w = Convert.ToInt32(this.textBox1.Text);
                int h = Convert.ToInt32(this.textBox2.Text);

                if (w < 1 || h < 1)
                {
                    e.Cancel = true;
                }
                else
                {
                    this.MapWidth = w;
                    this.MapHeight = h;
                }
            }
            catch (FormatException)
            {
                e.Cancel = true;
            }
        }

        private bool ValidateFields()
        {
            try
            {
                int w = Convert.ToInt32(this.textBox1.Text);
                int h = Convert.ToInt32(this.textBox2.Text);

                if (w < 1 || h < 1)
                {
                    return false;
                }
                else
                {
                    this.MapWidth = w;
                    this.MapHeight = h;
                }
            }
            catch (FormatException)
            {
                return false;
            }

            return true;
        }

        private void Button1Click(object sender, EventArgs e)
        {
            if (this.ValidateFields())
            {
                this.DialogResult = DialogResult.OK;
            }
        }
    }
}
