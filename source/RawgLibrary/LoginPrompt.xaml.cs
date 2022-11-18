using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RawgLibrary
{
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
}
