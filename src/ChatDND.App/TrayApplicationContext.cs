using ChatDND.Core.Privileges;

namespace ChatDND.App;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _icon;
    private readonly AppController _controller;
    private readonly SingleInstanceGuard _singleInstance;
    private readonly IElevationLauncher _elevationLauncher;
    private readonly bool _isElevated;
    private MainForm? _mainForm;

    public TrayApplicationContext(
        AppController controller,
        SingleInstanceGuard singleInstance,
        IElevationLauncher elevationLauncher,
        bool isElevated)
    {
        _controller = controller;
        _singleInstance = singleInstance;
        _elevationLauncher = elevationLauncher;
        _isElevated = isElevated;
        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = UiStrings.AppTitle,
            Visible = true
        };
        _icon.DoubleClick += (_, _) => ShowMainForm();
        _icon.ContextMenuStrip = BuildMenu();
    }

    public void ShowMainForm()
    {
        if (_mainForm is null || _mainForm.IsDisposed)
        {
            _mainForm = new MainForm(_controller);
        }

        _mainForm.Show();
        _mainForm.Activate();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(UiStrings.EnableDnd, null, (_, _) => Execute(_controller.Enable));
        menu.Items.Add(UiStrings.DisableDnd, null, (_, _) => Execute(_controller.Disable));
        menu.Items.Add("显示主界面", null, (_, _) => ShowMainForm());
        if (!_isElevated)
        {
            menu.Items.Add(UiStrings.RunElevated, null, (_, _) => Elevate());
        }
        menu.Items.Add(UiStrings.ExitApplication, null, (_, _) =>
        {
            _controller.Dispose();
            _icon.Visible = false;
            ExitThread();
        });
        return menu;
    }

    private void Elevate()
    {
        var result = MessageBox.Show(
            UiStrings.ElevationRiskBody,
            UiStrings.ElevationRiskTitle,
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning);
        if (result != DialogResult.OK)
        {
            return;
        }

        var wasEnabled = _controller.PrepareForElevation();
        var resumeDnd = wasEnabled
            && _controller.Settings.Rules.Any(rule => rule.Enabled);
        _singleInstance.Release();
        if (_elevationLauncher.TryRestartElevated(resumeDnd))
        {
            _icon.Visible = false;
            ExitThread();
            return;
        }

        if (_singleInstance.TryAcquire())
        {
            _controller.ResumeAfterFailedElevation(wasEnabled);
        }
        else
        {
            _icon.Visible = false;
            ExitThread();
            return;
        }

        MessageBox.Show(
            "未能启动管理员模式，程序将继续使用普通权限。",
            UiStrings.AppTitle,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void Execute(Action action)
    {
        try
        {
            action();
            _mainForm?.RefreshState();
        }
        catch (InvalidOperationException exception)
        {
            MessageBox.Show(
                exception.Message,
                UiStrings.AppTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
