using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using EquipMaster.Models;
using EquipMaster.ViewModels.Commands;  

namespace EquipMaster.ViewModels
{
    public class UserEditViewModel : INotifyPropertyChanged
    {
        public UserEditViewModel(User user)
        {
            User = user;
            SaveCommand = new RelayCommand<object>(_ => Save());
        }

        public User User { get; }

        public ICommand SaveCommand { get; }

        private void Save()
        {
            try
            {
                using var ctx = new ApplicationDbContext();
                if (User.Id == 0)
                    ctx.Users.Add(User);
                else
                    ctx.Users.Update(User);
                ctx.SaveChanges();

                // Закрываем окно с результатом true
                var wnd = Application.Current.Windows
                             .OfType<Window>()
                             .FirstOrDefault(w => w.DataContext == this);
                if (wnd != null) wnd.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения пользователя:\n{ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        #endregion
    }
}
