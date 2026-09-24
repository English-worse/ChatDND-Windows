using ChatDND.Core.Models;

namespace ChatDND.App;

public sealed class AppSettingsForm : Form
{
    private readonly AppController _controller;
    private readonly CheckBox _autoEnable;
    private readonly CheckBox _minimizeToTray;
    private readonly CheckBox _runAsAdministrator;
    private readonly NumericUpDown _scanInterval;

    public AppSettingsForm(AppController controller)
    {
        _controller = controller;
        Text = UiStrings.SettingsTitle;
        Icon = IconFactory.LoadAppIcon();
        StartPosition = FormStartPosition.CenterParent;
        Width = 560;
        Height = 360;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var settings = controller.Settings;
        _autoEnable = new CheckBox
        {
            Text = UiStrings.AutoEnableOnLaunch,
            Checked = settings.AutoEnableOnLaunch,
            AutoSize = true
        };
        _minimizeToTray = new CheckBox
        {
            Text = UiStrings.MinimizeToTrayOnStartup,
            Checked = settings.MinimizeToTrayOnStartup,
            AutoSize = true
        };
        _runAsAdministrator = new CheckBox
        {
            Text = UiStrings.RunAsAdministratorAtStartup,
            Checked = settings.RunAsAdministratorAtStartup,
            AutoSize = true
        };
        _scanInterval = new NumericUpDown
        {
            Minimum = 100,
            Maximum = 5000,
            Increment = 100,
            Value = Math.Clamp(settings.ScanIntervalMs, 100, 5000),
            Width = 120
        };

        var intervalRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        intervalRow.Controls.Add(new Label
        {
            Text = UiStrings.ScanInterval,
            AutoSize = true,
            Padding = new Padding(0, 6, 8, 0)
        });
        intervalRow.Controls.Add(_scanInterval);

        var fields = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(16)
        };
        fields.Controls.AddRange([
            _autoEnable,
            _minimizeToTray,
            _runAsAdministrator,
            intervalRow
        ]);

        var save = new Button
        {
            Text = UiStrings.Save,
            DialogResult = DialogResult.None,
            AutoSize = true
        };
        save.Click += (_, _) => Save();
        var cancel = new Button
        {
            Text = UiStrings.Cancel,
            DialogResult = DialogResult.Cancel,
            AutoSize = true
        };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8)
        };
        buttons.Controls.AddRange([save, cancel]);

        Controls.Add(fields);
        Controls.Add(buttons);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void Save()
    {
        try
        {
            _controller.SaveGeneralSettings(
                _autoEnable.Checked,
                _minimizeToTray.Checked,
                _runAsAdministrator.Checked,
                (int)_scanInterval.Value);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (InvalidOperationException exception)
        {
            MessageBox.Show(
                exception.Message,
                UiStrings.SettingsTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
