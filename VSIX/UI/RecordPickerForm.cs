using KnowledgeIncidentAnalysisAI.Domain.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace KnowledgeIncidentAnalysisAI.UI
{
    internal sealed class RecordPickerForm : Form
    {
        private readonly TextBox _schemaTextBox;
        private readonly TextBox _tableTextBox;
        private readonly TextBox _keyColumnTextBox;
        private readonly TextBox _keyValueTextBox;

        public RecordReference SelectedRecord { get; private set; }

        public RecordPickerForm()
        {
            Text = "Select Record";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 220);

            var instructions = new Label
            {
                AutoSize = false,
                Location = new Point(12, 12),
                Size = new Size(396, 40),
                Text = "Identify the parent record to analyze. Related updates and timeline events will be scoped to this record."
            };

            var schemaLabel = new Label
            {
                AutoSize = true,
                Location = new Point(12, 58),
                Text = "Table schema:"
            };

            _schemaTextBox = new TextBox
            {
                Location = new Point(120, 54),
                Size = new Size(276, 23),
                Text = "dbo"
            };

            var tableLabel = new Label
            {
                AutoSize = true,
                Location = new Point(12, 88),
                Text = "Table name:"
            };

            _tableTextBox = new TextBox
            {
                Location = new Point(120, 84),
                Size = new Size(276, 23)
            };

            var keyLabel = new Label
            {
                AutoSize = true,
                Location = new Point(12, 118),
                Text = "Key column:"
            };

            _keyColumnTextBox = new TextBox
            {
                Location = new Point(120, 114),
                Size = new Size(276, 23),
                Text = "Id"
            };

            var valueLabel = new Label
            {
                AutoSize = true,
                Location = new Point(12, 148),
                Text = "Key value:"
            };

            _keyValueTextBox = new TextBox
            {
                Location = new Point(120, 144),
                Size = new Size(276, 23)
            };

            var okButton = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(216, 178),
                Size = new Size(88, 27),
                Text = "OK"
            };
            okButton.Click += OnOkClick;

            var cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(308, 178),
                Size = new Size(88, 27),
                Text = "Cancel"
            };

            Controls.Add(instructions);
            Controls.Add(schemaLabel);
            Controls.Add(_schemaTextBox);
            Controls.Add(tableLabel);
            Controls.Add(_tableTextBox);
            Controls.Add(keyLabel);
            Controls.Add(_keyColumnTextBox);
            Controls.Add(valueLabel);
            Controls.Add(_keyValueTextBox);
            Controls.Add(okButton);
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_tableTextBox.Text) ||
                string.IsNullOrWhiteSpace(_keyColumnTextBox.Text) ||
                string.IsNullOrWhiteSpace(_keyValueTextBox.Text))
            {
                MessageBox.Show(
                    "Enter table name, key column, and key value.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            SelectedRecord = new RecordReference
            {
                TableSchema = _schemaTextBox.Text.Trim(),
                TableName = _tableTextBox.Text.Trim(),
                RecordKey = _keyColumnTextBox.Text.Trim(),
                RecordKeyValue = _keyValueTextBox.Text.Trim()
            };
        }
    }
}
