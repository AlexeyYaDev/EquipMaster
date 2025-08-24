using System.Windows;
using EquipMaster.ViewModels;

namespace EquipMaster.Views.Assignment
{
    public partial class AssignmentHistoryWindow : Window
    {
        public AssignmentHistoryWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AssignmentHistoryViewModel vm)
            {
                vm.SearchText = string.Empty;
                vm.AssignedFrom = null;
                vm.AssignedTo = null;
                vm.ReturnedFrom = null;
                vm.ReturnedTo = null;
            }
        }
    }
}
