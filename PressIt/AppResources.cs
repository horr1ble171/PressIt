namespace PressIt;

internal static class AppResources
{
    private static Image? _logo;
    public static Image Logo
    {
        get
        {
            if (_logo == null)
            {
                using var stream = typeof(AppResources).Assembly.GetManifestResourceStream("PressIt.Resources.logo.png");
                _logo = Image.FromStream(stream!);
            }
            return _logo;
        }
    }
}
