using System.Windows;
using System.Windows.Input;

namespace PlayniteExtensions.Common;

/// <summary>
/// Taken from https://stackoverflow.com/a/44000109/1622598
/// </summary>
public static class IgnoreScrollBehaviour
{
    public static readonly DependencyProperty IgnoreScrollProperty = DependencyProperty.RegisterAttached("IgnoreScroll", typeof(bool), typeof(IgnoreScrollBehaviour), new PropertyMetadata(OnIgnoreScollChanged));

    public static void SetIgnoreScroll(DependencyObject o, string value)
    {
        o.SetValue(IgnoreScrollProperty, value);
    }

    public static string GetIgnoreScroll(DependencyObject o)
    {
        return (string)o.GetValue(IgnoreScrollProperty);
    }

    private static void OnIgnoreScollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        bool ignoreScoll = (bool)e.NewValue;

        if (!(d is UIElement element))
            return;

        if (ignoreScoll)
            element.PreviewMouseWheel += Element_PreviewMouseWheel;
        else
            element.PreviewMouseWheel -= Element_PreviewMouseWheel;
    }

    private static void Element_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!(sender is UIElement element))
            return;

        e.Handled = true;
        var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) { RoutedEvent = UIElement.MouseWheelEvent };
        element.RaiseEvent(e2);
    }
}
