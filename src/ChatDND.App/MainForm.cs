using ChatDND.Core.Models;

namespace ChatDND.App;

public sealed class MainForm : Form
{
    private readonly AppController _controller;
    private readonly CheckBox _toggle;
    private readonly ListBox _rules;
    private readonly Label _status;
    private bool _updating;

    public MainForm(AppController controller)
    {
        _controller = controller;
        Text = UiStrings.AppTitle;
        StartPosition = FormStartPosition.CenterScreen;
        Width = 720;
        Height = 460;
        MinimumSize = new Size(620, 400);

        _toggle = new CheckBox
        {
            Text = controller.IsEnabled ? UiStrings.DisableDnd : UiStrings.EnableDnd,
            Checked = controller.IsEnabled,
            AutoSize = true
        };
        _rules = new ListBox
        {
            Dock = DockStyle.Fill,
            DisplayMember = nameof(AppRule.DisplayName)
        };
        _status = new Label
        {
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 8)
        };
        RefreshRules();

        var addCurrent = new Button
        {
            Text = UiStrings.AddCurrentApp,
            AutoSize = true
        };
        addCurrent.Click += (_, _) =>
        {
            var discovered = _controller.DiscoverCandidates();
            if (discovered.Count == 0)
            {
                MessageBox.Show(
                    UiStrings.NoCandidates,
                    UiStrings.AppTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var picker = new SettingsForm(discovered);
            if (picker.ShowDialog(this) == DialogResult.OK
                && picker.SelectedCandidate is not null)
            {
                _controller.AddRule(picker.SelectedCandidate);
                RefreshRules();
            }
        };

        var addExe = new Button
        {
            Text = UiStrings.AddExe,
            AutoSize = true
        };
        addExe.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "可执行文件 (*.exe)|*.exe",
                CheckFileExists = true
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _controller.AddRule(
                    dialog.FileName,
                    Path.GetFileNameWithoutExtension(dialog.FileName));
                RefreshRules();
            }
        };

        var remove = new Button
        {
            Text = UiStrings.RemoveSelectedRule,
            AutoSize = true
        };
        remove.Click += (_, _) =>
        {
            if (_rules.SelectedItem is AppRule rule)
            {
                _controller.RemoveRule(rule);
                RefreshRules();
            }
        };

        _toggle.CheckedChanged += (_, _) =>
        {
            if (_updating)
            {
                return;
            }

            try
            {
                if (_toggle.Checked)
                {
                    _controller.Enable();
                    _toggle.Text = UiStrings.DisableDnd;
                }
                else
                {
                    _controller.Disable();
                    _toggle.Text = UiStrings.EnableDnd;
                }

                RefreshStatus();
            }
            catch (InvalidOperationException exception)
            {
                _toggle.Checked = false;
                MessageBox.Show(
                    exception.Message,
                    UiStrings.AppTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                RefreshStatus();
            }
        };

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        top.Controls.AddRange([_toggle, _status]);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        buttons.Controls.AddRange([addExe, addCurrent, remove]);

        Controls.Add(_rules);
        Controls.Add(buttons);
        Controls.Add(top);

        FormClosing += (_, args) =>
        {
            if (args.CloseReason == CloseReason.UserClosing)
            {
                args.Cancel = true;
                Hide();
            }
        };
    }

    public void RefreshState()
    {
        _updating = true;
        _toggle.Checked = _controller.IsEnabled;
        _toggle.Text = _controller.IsEnabled
            ? UiStrings.DisableDnd
            : UiStrings.EnableDnd;
        _updating = false;
        RefreshRules();
    }

    private void RefreshRules()
    {
        _rules.Items.Clear();
        _rules.Items.AddRange(_controller.Settings.Rules.ToArray());
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        _status.Text = _controller.IsEnabled
            ? UiStrings.StatusEnabled
            : UiStrings.StatusDisabled;
    }
}
