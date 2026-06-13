using System;
using System.Drawing;
using System.Windows.Forms;

namespace KnowledgeIncidentAnalysisAI.UI
{
    internal sealed class AnalysisResultForm : Form
    {
        private AnalysisResultForm(string title, string body)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(720, 480);

            var textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Text = body,
                WordWrap = true
            };

            var closeButton = new Button
            {
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
                Height = 32,
                Text = "Close"
            };

            Controls.Add(textBox);
            Controls.Add(closeButton);
            AcceptButton = closeButton;
        }

        public static void ShowResult(string title, string body)
        {
            using (var form = new AnalysisResultForm(title, body))
            {
                form.ShowDialog();
            }
        }
    }
}
