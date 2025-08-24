using System.Windows;

namespace EquipMaster.Views.Assignment
{
    public partial class ReturnEquipmentWindow : Window
    {
        public ReturnEquipmentWindow()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
