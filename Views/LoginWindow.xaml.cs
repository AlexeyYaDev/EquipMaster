using System.Windows;

namespace EquipMaster.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void OnLoginClick(object sender, RoutedEventArgs e)
        {
            // Получаем введённые данные
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            // Проверяем логин и пароль
            if (IsValidUser(username, password))
            {
                MessageBox.Show("С возвращением, администратор!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Открываем главное окно
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                // Закрываем окно авторизации
                this.Close();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsValidUser(string username, string password)
        {
            // Проверка на логин 
            return username == "admin" && password == "1234";
        }
    }
}
