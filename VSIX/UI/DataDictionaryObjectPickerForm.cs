using KnowledgeIncidentAnalysisAI.Domain.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace KnowledgeIncidentAnalysisAI.UI
{
    internal sealed class DataDictionaryObjectPickerForm : Form
    {
        private readonly ComboBox _objectTypeCombo;
        private readonly TextBox _schemaTextBox;
        private readonly TextBox _nameTextBox;

        public DatabaseObjectReference SelectedObject { get; private set; }

        public DataDictionaryObjectPickerForm()
        {
            Text = "Data Dictionary Generator";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 200);

            var instructions = new Label
            {
                AutoSize = false,
                Location = new Point(12, 12),
                Size = new Size(396, 40),
                Text = "Select a database object to explain in English (table schema, relationships, or Sproc/Fnx/View purpose)."
            };

            var typeLabel = new Label
            {
                AutoSize = true,
                Location = new Point(12, 58),
                Text = "Object type:"
            };

            _objectTypeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(120, 54),
                Size = new Size(276, 23)
            };
            _objectTypeCombo.Items.AddRange(new object[]
            {
                "Table",
                "View",
                "Stored Procedure",
                "Function"
            });
            _objectTypeCombo.SelectedIndex = 0;

            var schemaLabel = new Label
            {
                AutoSize = true,
                Location = new Point(12, 92),
                Text = "Schema:"
            };

            _schemaTextBox = new TextBox
            {
                Location = new Point(120, 88),
                Size = new Size(276, 23),
                Text = "dbo"
            };

            var nameLabel = new Label
            {
                AutoSize = true,
                Location = new Point(12, 122),
                Text = "Object name:"
            };

            _nameTextBox = new TextBox
            {
                Location = new Point(120, 118),
                Size = new Size(276, 23)
            };

            var okButton = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(216, 158),
                Size = new Size(88, 27),
                Text = "Explain"
            };
            okButton.Click += OnOkClick;

            var cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(308, 158),
                Size = new Size(88, 27),
                Text = "Cancel"
            };

            Controls.Add(instructions);
            Controls.Add(typeLabel);
            Controls.Add(_objectTypeCombo);
            Controls.Add(schemaLabel);
            Controls.Add(_schemaTextBox);
            Controls.Add(nameLabel);
            Controls.Add(_nameTextBox);
            Controls.Add(okButton);
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show(
                    "Enter an object name.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            SelectedObject = new DatabaseObjectReference
            {
                ObjectType = _objectTypeCombo.SelectedItem?.ToString(),
                Schema = _schemaTextBox.Text.Trim(),
                Name = _nameTextBox.Text.Trim()
            };
        }
    }
}
