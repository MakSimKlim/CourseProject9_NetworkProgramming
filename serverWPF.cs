// Сервер
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography; // для хэширования
using server; // добавлено пространство имен сервера

public class Server
{
    private readonly serverFirst db = new serverFirst();

    private TcpListener _server;
    private bool _isRunning;
    private Dictionary<TcpClient, string> _clients = new Dictionary<TcpClient, string>(); // список клиентов

    public Server(int port)
    {
        _server = new TcpListener(IPAddress.Any, port);
        _server.Start();

        _isRunning = true;

        Console.WriteLine("(" + DateTime.Now + ") Сервер успешно запущен");

        try
        {
            db = new serverFirst();
            db.Database.Connection.Open(); // попытка открыть соединение с базой данных
            Console.WriteLine("(" + DateTime.Now + ") Успешное подключение к базе данных");
        }
        catch (Exception ex)
        {
            Console.WriteLine("(" + DateTime.Now + ") Ошибка подключения к базе данных: " + ex.Message);
        }

        LoopClients();
    }

    private bool CheckCredentials(string clientName, string password)
    {
        //// Хешируем пароль
        //using (var sha256 = SHA256.Create())
        //{
        //    byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        //    password = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        //}

        var user = db.Authorization.FirstOrDefault(u => u.ClientName == clientName && u.Password == password);
        return user != null;
    }

    private bool RegisterUser(string clientName, string password)
    {
        var existingUser = db.Authorization.FirstOrDefault(u => u.ClientName == clientName);
        if (existingUser == null)
        {
            var newUser = new server.Authorization { ClientName = clientName, Password = password };
            db.Authorization.Add(newUser);
            db.SaveChanges();
            return true;
        }
        return false;
    }

    public void LoopClients()
    {
        while (_isRunning)
        {
            // ждем клиента
            TcpClient newClient = _server.AcceptTcpClient();

            // получаем имя клиента и пароль
            NetworkStream stream = newClient.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string[] credentials = Encoding.UTF8.GetString(buffer, 0, bytesRead).Split(':');
            string clientName = credentials[0];
            string password = credentials[1];

            // проверяем учетные данные в базе данных
            bool isValid = CheckCredentials(clientName, password);
            bool isRegistered = RegisterUser(clientName, password);


            if (isValid || isRegistered)
            {
                // добавляем нового клиента в список
                _clients[newClient] = clientName;
                Console.WriteLine("(" + DateTime.Now + ") Подключение Клиента (" + clientName + ") к серверу выполнено успешно");

                // отправляем сообщение всем клиентам о подключении нового клиента
                BroadcastMessage("Server: Клиент (" + clientName + ") подключился", newClient);

                // создаем новый поток для обслуживания нового клиента
                Thread t = new Thread(new ParameterizedThreadStart(HandleClient));
                t.Start(newClient);

                // отправляем ответ обратно клиенту
                byte[] messageBytes = Encoding.UTF8.GetBytes("OK");
                stream.Write(messageBytes, 0, messageBytes.Length);
            }
            else
            {
                // отправляем сообщение клиенту о неверных учетных данных
                byte[] messageBytes = Encoding.UTF8.GetBytes("Неверные учетные данные");
                stream.Write(messageBytes, 0, messageBytes.Length);
            }
        }
    }

    public void HandleClient(object obj)
    {
        // получаем входящего клиента
        TcpClient client = (TcpClient)obj;

        // получаем поток
        var stream = client.GetStream();

        // даем клиенту возможность отправлять сообщения
        string sData = null;

        while (client.Connected)
        {
            // ждем сообщения от клиента
            byte[] bytes = new byte[256];
            int i = stream.Read(bytes, 0, bytes.Length);

            // если клиент отключился
            if (i == 0)
            {
                Console.WriteLine("(" + DateTime.Now + ") Клиент (" + _clients[client] + ") отключился");
                BroadcastMessage("Server: Клиент (" + _clients[client] + ") отключился");
                _clients.Remove(client);
                break;
            }

            // преобразуем байты в строку
            sData = Encoding.UTF8.GetString(bytes, 0, i);

            // пересылаем сообщение всем клиентам
            BroadcastMessage("Клиент '" + _clients[client] + "': " + sData, client, _clients[client].ToString());

            // выводим сообщение только если оно не приватное
            if (!sData.StartsWith("privat_"))
            {
                Console.WriteLine("(" + DateTime.Now + ") Сообщение от Клиента '" + _clients[client] + "': " + sData);
            }
        }

        // Закрываем соединение
        client.Close();
    }

    public void BroadcastMessage(string message, TcpClient sender = null, string senderName = null)
    {
        string[] messageParts = message.Split(new[] { "privat_" }, StringSplitOptions.None);

        if (messageParts.Length > 1)
        {
            // Это приватное сообщение
            string recipientName = messageParts[1].Split(' ')[0];
            string privateMessage = String.Join(" ", messageParts[1].Split(' ').Skip(1));

            foreach (var client in _clients)
            {
                if (_clients[client.Key].ToString() == recipientName && client.Key != sender)
                {
                    NetworkStream stream = client.Key.GetStream();
                    byte[] messageBytes = Encoding.UTF8.GetBytes("Приватное сообщение от Клиента '" + senderName + "': " + privateMessage);
                    stream.Write(messageBytes, 0, messageBytes.Length);
                    Console.WriteLine("(" + DateTime.Now + ") Приватное сообщение от Клиента '" + senderName + "' для клиента '" + recipientName + "': " + privateMessage);
                }
            }
        }
        else
        {
            // Это публичное сообщение
            foreach (var client in _clients.Keys)
            {
                if (client != sender)
                {
                    NetworkStream stream = client.GetStream();
                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    stream.Write(messageBytes, 0, messageBytes.Length);
                }
            }
        }
    }


    public static void Main(string[] args)
    {
        Server server = new Server(13000);
    }
}
