namespace ChatDND.App;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _icon;
    private readonly AppController _controller;
    private MainForm? _mainForm;

    public TrayApplicationContext(AppController controller)
    {
        _controller = controller;
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
            _mainForm = new MainForm(
                _controller,
                _controller.DiscoverCandidates());
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
        menu.Items.Add(UiStrings.ExitApplication, null, (_, _) =>
        {
            _controller.Dispose();
            _icon.Visible = false;
            ExitThread();
        });
        return menu;
    }

    private static void Execute(Action action)
    {
        try
        {
            action();
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
