using System.Windows;
using System.Windows.Controls;

namespace RawgLibrary;

/// <summary>
/// Interaction logic for LoginPrompt.xaml
/// </summary>
public partial class LoginPrompt : UserControl
{
    public LoginPrompt(Window window)
    {
        InitializeComponent();
        Window = window;
    }

    public Window Window { get; }
    public string EmailAddress { get => TextBoxEmailAddress.Text; }
    public string Password { get => TextBoxPassword.Password; }

    private void ButtonOK_Click(object sender, RoutedEventArgs e)
    {
        Window.DialogResult = true;
        Window.Close();
    }

    private void ButtonCancel_Click(object sender, RoutedEventArgs e)
    {
        Window.DialogResult = false;
        Window.Close();
    }
}
