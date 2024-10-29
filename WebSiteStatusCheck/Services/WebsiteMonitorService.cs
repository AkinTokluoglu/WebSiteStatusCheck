using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;

namespace WebsiteMonitorApp.Services
{
    public class WebsiteMonitorService : BackgroundService
    {
        private readonly HttpClient _httpClient;
        private readonly NotificationService _notificationService;
        private readonly ILogger<WebsiteMonitorService> _logger;

        private readonly List<string> _websites = new List<string>
        {
            "https://httpstat.us/526",
            "https://www.netrentacar.de/",
            "https://httpstat.us/500",
            "https://httpstat.us/495",
        };

        public WebsiteMonitorService(HttpClient httpClient, ILogger<WebsiteMonitorService> logger, NotificationService notificationService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _notificationService = notificationService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await MonitorWebsitesAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                await Parallel.ForEachAsync(_websites, stoppingToken, async (website, token) =>
                {
                    await MonitorWebsiteAsync(website);
                });
            }
        }

        private async Task MonitorWebsitesAsync(CancellationToken stoppingToken)
        {
            await Parallel.ForEachAsync(_websites, stoppingToken, async (website, token) =>
            {
                await MonitorWebsiteAsync(website);
            });
        }

        private async Task MonitorWebsiteAsync(string website)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, website);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Web sitesi durum ve yanıt süresi kontrolü
                var response = await _httpClient.SendAsync(request);
                stopwatch.Stop();
                var responseTime = stopwatch.ElapsedMilliseconds;

                // Durum kontrolü
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    var message = $"Website {website} is down! Status Code: {(int)response.StatusCode}";
                    _logger.LogWarning(message);
                    await _notificationService.SendTelegramNotification(message);
                }
                else
                {
                    _logger.LogInformation($"Website {website} is up and running.");
                }

                // Yanıt süresi kontrolü
                if (responseTime > 3000)
                {
                    var slowMessage = $"Website {website} is slow. Response time: {responseTime} ms";
                    _logger.LogWarning(slowMessage);
                    await _notificationService.SendTelegramNotification(slowMessage);
                }
                else
                {
                    _logger.LogInformation($"Website {website} responded in {responseTime} ms.");
                }

                // SSL sertifika geçerlilik kontrolü
                await CheckSslCertificateAsync(website);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError($"Error monitoring website {website}: {ex.Message}");
            }
        }

        private async Task CheckSslCertificateAsync(string url)
        {
            try
            {
                var uri = new Uri(url);
                using var tcpClient = new TcpClient(uri.Host, 443);
                using var sslStream = new SslStream(tcpClient.GetStream(), false, (sender, cert, chain, errors) => true);

                await sslStream.AuthenticateAsClientAsync(uri.Host);

                var certificate = new X509Certificate2(sslStream.RemoteCertificate);
                DateTime expirationDate = DateTime.Parse(certificate.GetExpirationDateString());

                int daysRemaining = (expirationDate - DateTime.Now).Days;
                if (daysRemaining < 30)
                {
                    var sslMessage = $"Website {url} SSL certificate will expire in {daysRemaining} days on {expirationDate:yyyy-MM-dd}.";
                    await _notificationService.SendTelegramNotification(sslMessage);
                    _logger.LogWarning(sslMessage);
                }
                else
                {
                    _logger.LogInformation($"Website {url} SSL certificate is valid. Expiration date: {expirationDate:yyyy-MM-dd}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking SSL certificate for {url}: {ex.Message}");
            }
        }
    }
}
