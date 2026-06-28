using System;
using System.Windows;
using ProjekatScada.Models;
using ProjekatScada.Services.Interfaces;
using ProjekatScada.ViewModels;

namespace ProjekatScada.Views
{
    public partial class AddWindow : Window
    {
        public AddWindow(
            IDataConcentratorService dataConcentratorService,
            ITagValidationService validationService,
            TagBase tagToEdit = null,
            Alarm alarmToEdit = null)
        {
            InitializeComponent();

            var viewModel = new AddItemViewModel(dataConcentratorService, validationService, tagToEdit, alarmToEdit);
            Title = viewModel.WindowTitle;
            viewModel.RequestClose += (s, e) => Close();
            DataContext = viewModel;
        }

        public bool DialogResultValue
        {
            get
            {
                var vm = DataContext as AddItemViewModel;
                return vm != null && vm.DialogResult;
            }
        }
    }
}
