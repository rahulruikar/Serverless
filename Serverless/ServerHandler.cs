using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Serverless
{
    public class ServerHandler
    {
        private static readonly HttpClient Client = new HttpClient();

        private readonly string _serverName;

        private readonly ServiceUtils _serviceUtils;

        private readonly string _hubName;

        private readonly string _endpoint;

        private readonly PayloadMessage _defaultPayloadMessage;

        public ServerHandler(string connectionString, string hubName)
        {
            _serverName = GenerateServerName();
            _serviceUtils = new ServiceUtils(connectionString);
            _hubName = hubName;
            _endpoint = _serviceUtils.Endpoint;

            _defaultPayloadMessage = new PayloadMessage
            {
                Target = "SendMessage",
                Arguments = new[]
                {
                    _serverName,
                    "Hello from server",
                }
            };
        }

        public async Task Start()
        {
            ShowHelp();
            while (true)
            {
                var argLine = Console.ReadLine();
                if (argLine == null)
                {
                    continue;
                }
                var args = argLine.Split(' ');

                if (args.Length == 3 && args[0].Equals("send"))
                {
                    await SendRequest(args[1], _hubName, args[2]);
                }
                else
                {
                    Console.WriteLine($"Can't recognize command {argLine}");
                }
            }
        }

        public async Task SendRequest(string command, string hubName, string arg = null)
        {
            string url = null;
            HttpMethod method = HttpMethod.Post;
            switch (command)
            {
                case "addusertogroup":
                    url = AddUserToGroup(hubName, arg);
                    method = HttpMethod.Put;
                    break;
                case "removeuserfromgroup":
                    url = RemoveUserFromGroup(hubName, arg);
                    method = HttpMethod.Delete;
                    break;

                case "user":
                    url = GetSendToUserUrl(hubName, arg);
                    break;
                case "group":
                    url = GetSendToGroupUrl(hubName, arg);
                    break;
                default:
                    Console.WriteLine($"Can't recognize command {command}");
                    break;
            }

            if (!string.IsNullOrEmpty(url))
            {
                var request = BuildRequest(url, method);
                Console.WriteLine(request.ToString());
                var response = await Client.SendAsync(request);
                Console.WriteLine(response.StatusCode);
                if (response.StatusCode != HttpStatusCode.Accepted)
                {
                    Console.WriteLine($"Sent error: {response.StatusCode}");
                }
            }
        }

        private Uri GetUrl(string baseUrl)
        {
            return new UriBuilder(baseUrl).Uri;
        }

        private string AddUserToGroup(string hubName, string userId)
        {
            return $"{GetBaseUrl(hubName)}/groups/TestGroup/users/{userId}";
        }

        private string RemoveUserFromGroup(string hubName, string userId)
        {
            return $"{GetBaseUrl(hubName)}/groups/TestGroup/users/{userId}";
        }

        private string GetSendToUserUrl(string hubName, string userId)
        {
            return $"{GetBaseUrl(hubName)}/users/{userId}";
        }


        private string GetSendToGroupUrl(string hubName, string group)
        {
            return $"{GetBaseUrl(hubName)}/groups/{group}";
        }

        private string GetBaseUrl(string hubName)
        {
            return $"{_endpoint}/api/v1/hubs/{hubName.ToLower()}";
        }

        private string GenerateServerName()
        {
            return $"{Environment.MachineName}_{Guid.NewGuid():N}";
        }

        private HttpRequestMessage BuildRequest(string url, HttpMethod method)
        {
            var request = new HttpRequestMessage(method, GetUrl(url));

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _serviceUtils.GenerateAccessToken(url, _serverName));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (method == HttpMethod.Post) 
                request.Content = new StringContent(JsonConvert.SerializeObject(_defaultPayloadMessage), Encoding.UTF8, "application/json");

            return request;
        }

        private void ShowHelp()
        {
            Console.WriteLine("*********Usage*********\n" +
                              "send user <User Id>\n" +
                              "send group <Group Name>\n" +
                              "send addusertogroup <User Id>\n" +
                              "send removeuserfromgroup <User Id>\n" +
                              "***********************");
        }
    }

    public class PayloadMessage
    {
        public string Target { get; set; }

        public object[] Arguments { get; set; }
    }
}
