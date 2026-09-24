namespace ChatDND.App;

public static class UiStrings
{
    public const string AppTitle = "应用级免打扰";
    public const string EnableDnd = "开启免打扰";
    public const string DisableDnd = "关闭免打扰";
    public const string AddCurrentApp = "添加当前应用";
    public const string AddExe = "手动选择程序";
    public const string RemoveSelectedRule = "删除选中规则";
    public const string Settings = "设置";
    public const string SettingsTitle = "ChatDND 设置";
    public const string AutoEnableOnLaunch = "启动时自动开启免打扰";
    public const string MinimizeToTrayOnStartup = "启动后最小化到托盘";
    public const string RunAsAdministratorAtStartup = "开机后以管理员身份运行（需要管理员权限）";
    public const string ScanInterval = "音频扫描间隔（毫秒）";
    public const string Save = "保存";
    public const string Cancel = "取消";
    public const string AdminStartupFailed = "无法创建管理员自启任务，请确认当前账户具有任务计划程序权限。";
    public const string RecommendedApps = "推荐聊天应用";
    public const string NoRules = "请先选择至少一个需要静音的应用。";
    public const string NoCandidates = "当前没有发现可添加的应用，请使用“手动选择程序”。";
    public const string RunElevated = "以管理员身份运行（提高兼容性，存在系统权限风险）";
    public const string ExitApplication = "退出程序";
    public const string FirstRunTitle = "第一次使用 ChatDND";
    public const string FirstRunDescription = "请选择需要免打扰的应用，程序只会静音它们的 Windows 音频会话。";
    public const string ElevationRiskTitle = "管理员模式风险说明";
    public const string ElevationRiskBody =
        "管理员权限会扩大程序影响范围；如果程序存在漏洞或被替换，影响可能扩大到系统级操作。\n" +
        "Windows 会弹出 UAC 确认；通过任务计划自动以最高权限启动会降低安全边界。\n" +
        "管理员权限也不能保证控制所有受保护、独占模式或跨会话音频。\n" +
        "管理员模式仍然不会读取或修改联系人、聊天记录、应用配置或账号状态。\n" +
        "如果管理员凭据属于其他 Windows 账户，设置不会与普通用户配置共享。";
    public const string StatusEnabled = "免打扰正在运行";
    public const string StatusDisabled = "免打扰已关闭";
}
