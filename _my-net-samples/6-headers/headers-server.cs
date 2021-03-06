﻿    class Program
    {
        static void Main(string[] args)
        {
            var queueProcessor = new RabbitConsumer(){Enabled = true};
            queueProcessor.Start();
            Console.ReadLine();
        }
    }

     public class RabbitConsumer : IDisposable
    {
        private const string HostName = "localhost";
        private const string UserName = "guest";
        private const string Password = "guest";
        
        private const string QueueName = "queue1"; // server 1
        //private const string QueueName = "queue2"; // server 2
        //private const string QueueName = "queue3"; // server 3

        private const bool IsDurable = true; //<-

        private const string VirtualHost = "";
        private int Port = 0;

        public delegate void OnReceiveMessage(string message);

        public bool Enabled { get; set; }
    
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _model;
        private Subscription _subscription;

        public RabbitConsumer()
        {
            _connectionFactory = new ConnectionFactory
            {
                HostName = HostName,
                UserName = UserName,
                Password = Password
            };

            if (string.IsNullOrEmpty(VirtualHost) == false)
                _connectionFactory.VirtualHost = VirtualHost;
            if (Port > 0)
                _connectionFactory.Port = Port;

            _connection = _connectionFactory.CreateConnection();
            _model = _connection.CreateModel();
            _model.BasicQos(0, 1, false);
        }

        public void Start()
        {
            _subscription = new Subscription(_model, QueueName, false);

            var consumer = new ConsumeDelegate(Poll);
            consumer.Invoke();
        }

        private delegate void ConsumeDelegate();

        private void Poll()
        {
            while (Enabled)
            {
                //Получение сообщения
                var deliveryArgs = _subscription.Next();

                //Десериализация
                var message = Encoding.Default.GetString(deliveryArgs.Body);

                //Обработка
                Console.WriteLine("Message Recieved - {0}", message);
                foreach (var headerKey in deliveryArgs.BasicProperties.Headers.Keys)
                {
                    var headerValue = deliveryArgs.BasicProperties.Headers[headerKey];
                    var val = Encoding.Default.GetString((byte[])headerValue);
                    Console.WriteLine("Header - Key: {0}, Value: {1}", headerKey, val);
                }
                Console.WriteLine();
                
                //Подтверждение
                _subscription.Ack(deliveryArgs);
            }
        }

        public void Dispose()
        {
            if (_model != null)
                _model.Dispose();
            if (_connection != null)
                _connection.Dispose();

            _connectionFactory = null;

            GC.SuppressFinalize(this);
        }
    }
