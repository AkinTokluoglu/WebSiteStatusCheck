using MailKit.Net.Smtp;
using MimeKit;
using Telegram.Bot;

namespace WebsiteMonitorApp.Services
{
    public class NotificationService
    {
        private readonly string _telegramBotToken = "Your_Token";
        private readonly string _telegramChatId = "Your_ChatId";

        public async Task SendTelegramNotification(string message)
        {
            var botClient = new TelegramBotClient(_telegramBotToken);
            await botClient.SendTextMessageAsync(_telegramChatId, message);
        }
    }
}
