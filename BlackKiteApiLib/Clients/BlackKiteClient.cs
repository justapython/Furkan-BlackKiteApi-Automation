using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using RestSharp;
using System.Text.Json;
using BlackKiteApiLib.Models;

namespace BlackKiteApiLib.Clients
{
    public class BlackKiteClient
    {
        private readonly RestClient _client;
        private string _token = string.Empty;

        public BlackKiteClient()
        {
            _client = new RestClient(new RestClientOptions("https://seam.riskscore.cards")
            {
                ThrowOnAnyError = false,
                MaxTimeout = 10000
            });

        }

        public async Task AuthenticateAsync(string clientId, string clientSecret)
        {
            Console.WriteLine($"[DEBUG] Base URL => {_client.Options.BaseUrl}");
            
            var request = new RestRequest("/api/v2/oauth/token", Method.Post);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "client_credentials");
            request.AddParameter("client_id", clientId);
            request.AddParameter("client_secret", clientSecret);

            var response = await _client.ExecuteAsync(request);

            Console.WriteLine($"[DEBUG] Executed URI => {response.ResponseUri}");

            if (!response.IsSuccessful)
            {
                throw new Exception("Authentication failed: " + response.Content);
            }

            var authResponse = JsonSerializer.Deserialize<AuthenticationResponse>(response.Content!);
            _token = authResponse!.access_token;
        }

        public RestRequest AddAuthHeader(RestRequest request)
        {
            request.AddHeader("Authorization", $"Bearer {_token}");
            return request;
        }

        public async Task<RestResponse> ExecuteAsync(RestRequest request)
        {
            return await _client.ExecuteAsync(request);
        }
    }
}
