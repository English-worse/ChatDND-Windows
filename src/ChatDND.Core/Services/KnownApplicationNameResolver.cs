using System.Diagnostics;

namespace ChatDND.Core.Services;

public sealed record ResolvedApplicationName(string DisplayName, bool IsKnown);

public sealed class KnownApplicationNameResolver
{
    private static readonly IReadOnlyDictionary<string, string> KnownApplications =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["WeChat.exe"] = "微信",
            ["Weixin.exe"] = "微信",
            ["QQ.exe"] = "QQ",
            ["DingTalk.exe"] = "钉钉",
            ["DingTalkApp.exe"] = "钉钉",
            ["WXWork.exe"] = "企业微信",
            ["Feishu.exe"] = "飞书",
            ["Lark.exe"] = "飞书",
            ["WeMeet.exe"] = "腾讯会议",
            ["wemeetapp.exe"] = "腾讯会议",
            ["TIM.exe"] = "TIM"
        };

    public ResolvedApplicationName Resolve(string processPath)
    {
        var fileName = Path.GetFileName(processPath.Trim().Trim('"'));
        if (KnownApplications.TryGetValue(fileName, out var knownName))
        {
            return new ResolvedApplicationName(knownName, IsKnown: true);
        }

        var description = TryReadChineseDescription(processPath);
        if (description is not null)
        {
            return new ResolvedApplicationName(description, IsKnown: false);
        }

        return new ResolvedApplicationName(
            $"未识别程序（{fileName}）",
            IsKnown: false);
    }

    private static string? TryReadChineseDescription(string processPath)
    {
        try
        {
            var version = FileVersionInfo.GetVersionInfo(processPath);
            var description = version.FileDescription;
            if (string.IsNullOrWhiteSpace(description)
                || !description.Any(IsChineseCharacter))
            {
                return null;
            }

            return description.Trim();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool IsChineseCharacter(char value)
    {
        return value is >= '\u4e00' and <= '\u9fff';
    }
}
