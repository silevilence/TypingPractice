using System.Windows.Input;

namespace TypingCore.Wpf.Services;

internal static class ShortcutGesture
{
    private static readonly KeyGestureConverter Converter = new();

    public static bool Matches(KeyEventArgs eventArgs, string shortcut)
    {
        ArgumentNullException.ThrowIfNull(eventArgs);

        return Converter.ConvertFromInvariantString(shortcut) is KeyGesture gesture
            && gesture.Matches(null, eventArgs);
    }
}
