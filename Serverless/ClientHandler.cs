using System;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Serverless
{
    public class ClientHandler
    {
        private readonly HubConnection _connection;

        public ClientHandler(string connectionString, string hubName, string userId)
        {
            var serviceUtils = new ServiceUtils(connectionString);

            var url = GetClientUrl(serviceUtils.Endpoint, hubName);

            _connection = new HubConnectionBuilder()
                .WithUrl(url, option =>
                {
                    option.AccessTokenProvider = () =>
                    {
                        return Task.FromResult(serviceUtils.GenerateAccessToken(url, userId));
                    };
                }).Build();

            _connection.On<string, string>("SendMessage",
                (string server, string message) =>
                {
                    Console.WriteLine($"[{DateTime.Now.ToString()}] Received message from server {server}: {message}");
                });
        }
        
        public async Task StartAsync()
        {
            await _connection.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _connection.DisposeAsync();
        }

        private string GetClientUrl(string endpoint, string hubName)
        {
            return $"{endpoint}/client/?hub={hubName}";
        }
    }
}
