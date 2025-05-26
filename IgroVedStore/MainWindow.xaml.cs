using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using IgroVedStore.DataBase;

namespace IgroVedStore
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        OnlineStoreEntities2 db = new OnlineStoreEntities2();
        private int failedAttempts = 0;
        private string currentCaptcha;
        
        private bool isLocked = false;
        public MainWindow()
        {
            InitializeComponent();

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (isLocked)
            {
                MessageBox.Show("Система временно заблокирована. Попробуйте через 10 секунд.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (captchaPanel.Visibility == Visibility.Visible)
            {
                if (string.IsNullOrEmpty(captchaInput.Text) || !captchaInput.Text.Equals(currentCaptcha, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Неверная CAPTCHA!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    await LockSystemFor10Seconds();
                    return;
                }
            }

            try
            {
                var userObj = db.Customers.FirstOrDefault(x => x.Email == txtEmail.Text && x.Password == pwdPassword.Text);
                if (userObj != null)
                {
                    failedAttempts = 0;
                    captchaPanel.Visibility = Visibility.Collapsed;
                    AdminWindow admin = new AdminWindow(userObj.Role);
                    switch (userObj.Role)
                    {
                        case 1:
                            admin.Show();
                            this.Hide();
                            break;
                        case 2:
                            admin.Show();
                            this.Hide();
                            break;
                        case 3:
                            admin.Show();
                            this.Hide();
                            break;
                    }
                    

                }
                else
                {
                    throw new Exception("Неверный логин или пароль");
                }
            }
            catch (Exception ex)
            {
                failedAttempts++;

                if (failedAttempts >= 1)
                {
                    captchaPanel.Visibility = Visibility.Visible;
                    GenerateCaptcha();
                    MessageBox.Show("Неуспешная авторизация. Пожалуйста, введите CAPTCHA.", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    this.Height = 520;
                }

                MessageBox.Show($"{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void RefreshCaptchaButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateCaptcha();
        }
        private async Task LockSystemFor10Seconds()
        {
            isLocked = true;
            DisableControls();

            for (int i = 10; i > 0; i--)
            {
                enterButton.Content = $"ЗАБЛОКИРОВАНО ({i} СЕК)";
                await Task.Delay(1000);
            }

            enterButton.Content = "ВОЙТИ";
            UnlockControls();
            isLocked = false;
            GenerateCaptcha();
        }
        private void exit_MouseDown(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();

        }

        private void GenerateCaptcha()
        {
            captchaCanvas.Children.Clear();
            var random = new Random();

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            currentCaptcha = new string(Enumerable.Repeat(chars, 4)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            for (int i = 0; i < 5; i++)
            {
                var line = new Line
                {
                    X1 = random.Next(0, 150),
                    Y1 = random.Next(0, 40),
                    X2 = random.Next(0, 150),
                    Y2 = random.Next(0, 40),
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1
                };
                captchaCanvas.Children.Add(line);
            }

            for (int i = 0; i < currentCaptcha.Length; i++)
            {
                var textBlock = new TextBlock
                {
                    Text = currentCaptcha[i].ToString(),
                    FontSize = 20 + random.Next(-3, 4),
                    Foreground = Brushes.White,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new TransformGroup
                    {
                        Children = new TransformCollection
                        {
                            new RotateTransform(random.Next(-15, 16)),
                            new TranslateTransform(random.Next(-3, 4), random.Next(-3, 4))
                        }
                    }
                }
            ;

                Canvas.SetLeft(textBlock, 10 + i * 30 + random.Next(-5, 6));
                Canvas.SetTop(textBlock, 10 + random.Next(-5, 6));

                if (random.Next(2) == 0)
                {
                    var line = new Line
                    {
                        X1 = 0,
                        Y1 = textBlock.FontSize / 2,
                        X2 = 20,
                        Y2 = textBlock.FontSize / 2,
                        Stroke = Brushes.Red,
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(line, Canvas.GetLeft(textBlock));
                    Canvas.SetTop(line, Canvas.GetTop(textBlock) + textBlock.FontSize / 2);
                    captchaCanvas.Children.Add(line);
                }

                captchaCanvas.Children.Add(textBlock);
            }
        }

        private void UnlockControls()
        {
            txtEmail.IsEnabled = true;
            pwdPassword.IsEnabled = true;
            captchaInput.IsEnabled = true;
            enterButton.IsEnabled = true;
            refreshCaptchaButton.IsEnabled = true;
        }

        private void DisableControls()
        {
            txtEmail.IsEnabled = false;
            pwdPassword.IsEnabled = false;
            captchaInput.IsEnabled = false;
            enterButton.IsEnabled = false;
            refreshCaptchaButton.IsEnabled = false;
        }


        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            RegisterWindow register = new RegisterWindow();
            register.Show();
            this.Close();
        }
    }
} 
