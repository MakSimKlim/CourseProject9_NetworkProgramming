using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace client4
{
    public partial class SecondWindow : Window
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _thread;
        private string _name; // добавляем поле для имени пользователя

        public SecondWindow(TcpClient client, NetworkStream stream, string name) // добавляем параметр для имени пользователя 
        {
            InitializeComponent();

            _client = client;
            _stream = stream;
            _name = name; // сохраняем имя пользователя

            // отправляем имя на сервер
            byte[] nameBytes = Encoding.UTF8.GetBytes(_name);
            _stream.Write(nameBytes, 0, nameBytes.Length);

            // Запускаем новый поток для чтения входящих сообщений
            _thread = new Thread(new ThreadStart(ReadMessages));
            _thread.Start();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, пустое ли поле ввода или содержит ли оно команду "exit"
            if (string.IsNullOrEmpty(ChatInput.Text) || ChatInput.Text.ToLower() == "exit")
            {
                // Закрываем соединение
                _client.Close();
                _thread.Join(); // Ждем завершения потока чтения сообщений
                this.Close(); // Закрываем окно
                return;
            }

            // Добавляем введенный текст в поле вывода чата
            ChatOutput.Text += ChatInput.Text + Environment.NewLine;

            // Отправляем сообщение на сервер
            SendMessage(ChatInput.Text);

            // Очищаем поле ввода
            ChatInput.Clear();
        }

        // метод чтения сообщений
        public void ReadMessages()
        {
            while (_client.Connected)
            {
                byte[] data = new byte[256];
                int bytes = _stream.Read(data, 0, data.Length);
                string responseData = Encoding.UTF8.GetString(data, 0, bytes);

                // Обновляем поле вывода чата в главном потоке
                Dispatcher.Invoke(() =>
                {
                    ChatOutput.Text += responseData + Environment.NewLine;
                });
            }
        }

        //метод рассылки сообщений
        public void SendMessage(string message)
        {
            if (_stream.CanWrite)
            {
                byte[] clientMessageAsByteArray = Encoding.UTF8.GetBytes(message);
                _stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
            }
        }

        // Закрываем соединение при закрытии окна
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            _client.Close();
            _thread.Join(); // Ждем завершения потока чтения сообщений
        }
    }
}
