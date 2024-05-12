using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography; // для хэширования
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

namespace client4
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private TcpClient _client;
        private NetworkStream _stream;

        public LoginWindow()
        {
            InitializeComponent();

            string server = "127.0.0.1";
            int port = 13000;

            _client = new TcpClient(server, port);
            _stream = _client.GetStream();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            // Получите данные из полей ввода
            string clientName = ClientNameEmailPhoneInput.Text;
            string password = PasswordInput.Password;

            //// Хешируйте пароль
            //using (var sha256 = SHA256.Create())
            //{
            //    byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            //    password = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            //}

            // Отправьте имя пользователя и пароль на сервер
            byte[] credentialsBytes = Encoding.UTF8.GetBytes(clientName + ":" + password);
            _stream.Write(credentialsBytes, 0, credentialsBytes.Length);

            // Ждем ответа от сервера в отдельном потоке
            string response = await Task.Run(() =>
            {
                byte[] buffer = new byte[1024];
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            });

            if (response == "OK")
            {
                var secondWindow = new SecondWindow(_client, _stream, clientName); // передаем имя пользователя
                secondWindow.Show();

                // Закройте окно авторизации
                this.Close();
            }
            else
            {
                // Пользователь не существует, покажите сообщение об ошибке
                MessageBox.Show("Неверное имя пользователя или пароль.");
            }
        }

        private async void SignUp_Click(object sender, RoutedEventArgs e)
        {
            // Получите данные из полей ввода
            string clientName = ClientNameEmailPhoneInput.Text;
            string password = PasswordInput.Password;

            //// Хешируйте пароль
            //using (var sha256 = SHA256.Create())
            //{
            //    byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            //    password = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            //}

            // Отправьте имя пользователя и пароль на сервер
            byte[] credentialsBytes = Encoding.UTF8.GetBytes(clientName + ":" + password);
            _stream.Write(credentialsBytes, 0, credentialsBytes.Length);

            // Ждем ответа от сервера в отдельном потоке
            string response = await Task.Run(() =>
            {
                byte[] buffer = new byte[1024];
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            });

            if (response == "OK")
            {
                // Пользователь успешно зарегистрирован, откройте новое окно
                var secondWindow = new SecondWindow(_client, _stream, clientName); // передаем имя пользователя
                secondWindow.Show();

                // Закройте окно регистрации
                this.Close();
            }
            else
            {
                // Пользователь уже существует, покажите сообщение об ошибке
                MessageBox.Show("Пользователь с таким именем уже существует.");
            }
        }

        private void ShowPasswordCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            VisiblePasswordInput.Text = PasswordInput.Password;
            PasswordInput.Visibility = Visibility.Collapsed;
            VisiblePasswordInput.Visibility = Visibility.Visible;
        }

        private void ShowPasswordCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            PasswordInput.Password = VisiblePasswordInput.Text;
            PasswordInput.Visibility = Visibility.Visible;
            VisiblePasswordInput.Visibility = Visibility.Collapsed;
        }
    }
}

