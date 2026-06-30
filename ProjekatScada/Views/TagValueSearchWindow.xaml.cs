using System.Windows;
using ProjekatScada.Services.Interfaces;
using ProjekatScada.ViewModels;

namespace ProjekatScada.Views
{
    public partial class TagValueSearchWindow : Window
    {
        public TagValueSearchWindow(IDataConcentratorService dataConcentratorService)
        {
            InitializeComponent();

            var viewModel = new TagValueSearchViewModel(dataConcentratorService);
            viewModel.RequestClose += (sender, args) => Close();
            DataContext = viewModel;
        }

        public bool DialogResultValue
        {
            get
            {
                var viewModel = DataContext as TagValueSearchViewModel;
                return viewModel != null && viewModel.DialogResult;
            }
        }

        public string GeneratedFilePath
        {
            get
            {
                var viewModel = DataContext as TagValueSearchViewModel;
                return viewModel != null ? viewModel.GeneratedFilePath : null;
            }
        }
    }
}
