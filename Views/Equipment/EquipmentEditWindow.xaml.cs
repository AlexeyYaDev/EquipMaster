using System.Windows;
using EquipMaster.Models;
using EquipMaster.ViewModels;

namespace EquipMaster.Views.Equipment
{
    public partial class EquipmentEditWindow : Window
    {
        public EquipmentEditWindow(EquipMaster.Models.Equipment equipment = null)
        {
            InitializeComponent();
            DataContext = new EquipmentEditViewModel(equipment);
        }
    }
}
