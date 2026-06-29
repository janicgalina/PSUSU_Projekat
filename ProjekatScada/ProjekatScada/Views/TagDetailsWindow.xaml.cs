using System.Windows;
using ProjekatScada.Models;

namespace ProjekatScada.Views
{
    public partial class TagDetailsWindow : Window
    {
        public TagDetailsWindow(AnalogInputTag analogInputTag)
        {
            InitializeComponent();
            DataContext = analogInputTag;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
