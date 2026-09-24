namespace ChatDND.App;

public static class IconFactory
{
    private const string ResourceName = "ChatDND.App.Assets.ChatDND.ico";

    public static Icon LoadAppIcon()
    {
        using var stream = typeof(IconFactory).Assembly
            .GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            return (Icon)SystemIcons.Application.Clone();
        }

        return new Icon(stream);
    }
}
