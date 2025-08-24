using System.Windows;
using EquipMaster.Views;
using static EquipMaster.Views.ReportViewerWindow;

namespace EquipMaster.Views.Reports
{
    public partial class ReportsWindow : Window
    {
        public ReportsWindow()
        {
            InitializeComponent();
        }

        private void ShowUsersListReport_Click(object sender, RoutedEventArgs e)
        {
            var viewer = new ReportViewerWindow(ReportType.UsersList);
            viewer.Owner = this;
            viewer.ShowDialog();
        }

        private void ShowEquipmentStatusReport_Click(object sender, RoutedEventArgs e)
        {
            var viewer = new ReportViewerWindow(ReportType.EquipmentStatus);
            viewer.Owner = this;
            viewer.ShowDialog();
        }

        private void ShowMaintenanceHistoryReport_Click(object sender, RoutedEventArgs e)
        {
            var viewer = new ReportViewerWindow(ReportType.MaintenanceHistory);
            viewer.Owner = this;
            viewer.ShowDialog();
        }

        private void ShowAssignmentReport_Click(object sender, RoutedEventArgs e)
        {
            var viewer = new ReportViewerWindow(ReportType.AssignmentReport);
            viewer.Owner = this;
            viewer.ShowDialog();
        }

        private void ShowUpcomingMaintenanceReport_Click(object sender, RoutedEventArgs e)
        {
            var viewer = new ReportViewerWindow(ReportType.UpcomingMaintenance);
            viewer.Owner = this;
            viewer.ShowDialog();
        }

        private void ShowUserActivityLogReport_Click(object sender, RoutedEventArgs e)
        {
            var viewer = new ReportViewerWindow(ReportType.UserActivityLog);
            viewer.Owner = this;
            viewer.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
