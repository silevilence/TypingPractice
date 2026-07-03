using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using TypingCore.Wpf.Services;
using TypingCore.Wpf.ViewModels;

namespace TypingCore.Wpf.Views;

/// <summary>
/// Handles the WPF event wiring for the stage-eight practice page.
/// </summary>
public partial class TypingPracticeView : UserControl
{
    private HwndSource? hwndSource;
    private Window? hostWindow;

    public TypingPracticeView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        hostWindow = Window.GetWindow(this);
        if (hostWindow is null)
        {
            return;
        }

        hostWindow.PreviewTextInput -= HostWindow_PreviewTextInput;
        hostWindow.PreviewTextInput += HostWindow_PreviewTextInput;

        hwndSource = (HwndSource?)PresentationSource.FromVisual(hostWindow);
        hwndSource?.RemoveHook(WndProc);
        hwndSource?.AddHook(WndProc);

        FocusInputSurface();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (hostWindow is not null)
        {
            hostWindow.PreviewTextInput -= HostWindow_PreviewTextInput;
        }

        hwndSource?.RemoveHook(WndProc);
        hwndSource = null;
        hostWindow = null;
    }

    private void HostWindow_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (DataContext is not TypingPracticeViewModel viewModel || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        viewModel.HandleTextInput(e.Text);
        e.Handled = true;
        FocusInputSurface();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (DataContext is not TypingPracticeViewModel viewModel)
        {
            return IntPtr.Zero;
        }

        bool consumed = viewModel.HandleWindowMessage(msg, wParam);
        if (consumed)
        {
            handled = ShouldHandleWindowMessage(msg, wParam);
            FocusInputSurface();
        }

        return IntPtr.Zero;
    }

    private void InputSurface_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => FocusInputSurface();

    private void FocusInputSurface()
    {
        if (InputSurface.IsVisible)
        {
            Keyboard.Focus(InputSurface);
        }
    }

    private static bool ShouldHandleWindowMessage(int message, nint wParam)
    {
        if (message != WindowMessageInputTranslator.WmKeyDown)
        {
            return false;
        }

        int virtualKey = unchecked((int)wParam);
        return virtualKey is 0x08 or 0x1B or 0x25 or 0x26 or 0x27 or 0x28;
    }
}