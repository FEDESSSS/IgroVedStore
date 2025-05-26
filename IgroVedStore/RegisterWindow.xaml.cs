using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using IgroVedStore.DataBase;

namespace IgroVedStore
{
    public partial class RegisterWindow : Window
    {
        private readonly OnlineStoreEntities2 _db;

        public RegisterWindow()
        {
            InitializeComponent();
            _db = new OnlineStoreEntities2();
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация данных
                if (string.IsNullOrWhiteSpace(txtFio.Text))
                {
                    ShowError("Введите ФИО");
                    return;
                }

                if (!IsValidEmail(txtEmail.Text))
                {
                    ShowError("Введите корректный email");
                    return;
                }

                if (!IsValidPhone(txtPhone.Text))
                {
                    ShowError("Введите корректный телефон");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtCity.Text))
                {
                    ShowError("Введите город");
                    return;
                }

                if (pwdPassword.Password.Length < 6)
                {
                    ShowError("Пароль должен содержать минимум 6 символов");
                    return;
                }

                if (pwdPassword.Password != pwdConfirmPassword.Password)
                {
                    ShowError("Пароли не совпадают");
                    return;
                }

                // Проверка, существует ли пользователь с таким email
                if (_db.Customers.Any(c => c.Email == txtEmail.Text))
                {
                    ShowError("Пользователь с таким email уже зарегистрирован");
                    return;
                }

                // Создание нового пользователя
                var newCustomer = new Customers
                {
                    Name = txtFio.Text,
                    Email = txtEmail.Text,
                    Phone = txtPhone.Text,
                    City = txtCity.Text,
                    Password = pwdPassword.Password, 
                    RegistrationDate = DateTime.Now,
                    Role = 3 
                };

                _db.Customers.Add(newCustomer);
                _db.SaveChanges();

                MessageBox.Show("Регистрация прошла успешно!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                var authWindow = new MainWindow();
                authWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            return Regex.IsMatch(phone, @"^\+?[0-9]{10,15}$");
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка ввода",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Auth_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var authWindow = new MainWindow();
            authWindow.Show();
            this.Close();
        }

        private void Exit_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}