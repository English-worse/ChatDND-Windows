using ChatDND.Core.Models;

namespace ChatDND.App;

public sealed class SettingsForm : Form
{
    private readonly ListBox _candidates;

    public SettingsForm(IReadOnlyList<CandidateApp> candidates)
    {
        Text = UiStrings.FirstRunTitle;
        StartPosition = FormStartPosition.CenterParent;
        Width = 560;
        Height = 360;
        _candidates = new ListBox
        {
            Dock = DockStyle.Fill,
            DisplayMember = nameof(CandidateApp.DisplayName)
        };
        _candidates.Items.AddRange(candidates.ToArray());

        var description = new Label
        {
            Text = UiStrings.FirstRunDescription,
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8)
        };
        var confirm = new Button
        {
            Text = "确定",
            DialogResult = DialogResult.OK,
            AutoSize = true
        };
        var cancel = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            AutoSize = true
        };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft
        };
        buttons.Controls.AddRange([confirm, cancel]);
        Controls.Add(_candidates);
        Controls.Add(buttons);
        Controls.Add(description);
        AcceptButton = confirm;
        CancelButton = cancel;
    }

    public CandidateApp? SelectedCandidate => _candidates.SelectedItem as CandidateApp;
}
