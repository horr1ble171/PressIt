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
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.png");
                _logo = Image.FromFile(path);
            }
            return _logo;
        }
    }
}
