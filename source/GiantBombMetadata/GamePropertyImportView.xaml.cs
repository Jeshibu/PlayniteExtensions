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

namespace GiantBombMetadata
{
    /// <summary>
    /// Interaction logic for GamePropertyImportView.xaml
    /// </summary>
    public partial class GamePropertyImportView : UserControl
    {
        public GamePropertyImportView(Window window)
        {
            InitializeComponent();
            Window = window;
        }

        public Window Window { get; }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Window.DialogResult = true;
            Window.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Window.DialogResult = false;
            Window.Close();
        }
    }
}
