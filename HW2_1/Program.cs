using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HW2
{
    public class NotificationService
    {
        private readonly INotificationSender _notificationSender;
        private readonly ILogger _logger;

        public NotificationService(INotificationSender notificationSender, ILogger logger)
        {
            _notificationSender = notificationSender;
            _logger = logger;
        }

        public void SendNotification(string message, string recipient)
        {
            string formattedMessage = $"Уведомление: {message}";
            _notificationSender.SendNotification(recipient, formattedMessage);
            _logger.Log($"Отправлено уведомление для {recipient}");
        }
    }

    public interface ILogger
    {
        void Log(string message);
    }

    public class FileLogger : ILogger
    {
        public void Log(string message)
        {
            File.WriteAllText("log.txt", message);
        }
    }

    public interface INotificationSender
    {
        void SendNotification(string recipient, string message);
    }

    public class EmailSender : INotificationSender
    {
        public void SendNotification(string recipient, string message)
        {
            Console.WriteLine($"Email для {recipient}: {message}");
        }
    }

    public class SmsSender : INotificationSender
    {
        public void SendNotification(string recipient, string message)
        {
            Console.WriteLine($"SMS для {recipient}: {message}");
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            ServiceCollection services = new ServiceCollection();

            Console.WriteLine("Выберите тип уведомления (1 - Email, 2 - SMS):");
            string choice = Console.ReadLine();

            if (choice == "1")
            {
                services.AddSingleton<INotificationSender, EmailSender>();
            }
            else
            {
                services.AddSingleton<INotificationSender, SmsSender>();
            }

            services.AddSingleton<ILogger, FileLogger>();
            services.AddSingleton<NotificationService>();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetRequiredService<NotificationService>();
            service.SendNotification("Ваш заказ готов", "user@example.com");
        }
    }


}
