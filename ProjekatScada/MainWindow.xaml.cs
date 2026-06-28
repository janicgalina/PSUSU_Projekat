using System;
using System.Windows;
using ProjekatScada.ViewModels;

namespace ProjekatScada
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
