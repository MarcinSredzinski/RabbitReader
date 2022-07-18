using JwtAuth.Library.Services;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;
using RabbitBase.Library.Contracts;
using System.Net.Http.Json;
using ILogger = Serilog.ILogger;

namespace RabbitReader.API
{
    internal class AuthorizedApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IAuthorizationService _authorizationService;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _httpRetryPolicy;
        private const int RetriesLimit = 10;
        private DateTime _refreshToken;

        public AuthorizedApiClient(HttpClient client, IConfiguration config, ILogger logger, IAuthorizationService authorizationService)
        {
            _httpClient = client;
            _logger = logger;
            _authorizationService = authorizationService;

            string targetUrl = config.GetSection("ApplicationSettings")
               .GetSection("TargetApiUrl").Get<string>();
            var requestUri = new Uri(targetUrl);
            _httpClient.BaseAddress = requestUri;
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpRetryPolicy = Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                 .WaitAndRetryAsync(RetriesLimit, retryAttempt => TimeSpan.FromSeconds(retryAttempt), onRetry: (response, timespan) =>
                 {
                     if (response.Result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                     {
                         ObtainAuthorizationToken();
                     }
                 });
        }

        public async Task<HttpResponseMessage> PostSensorDataAsync(BmpMeasurementDto measurement)
        {
            ObtainAuthorizationToken();
            var httpContent = JsonContent.Create(measurement);
            _logger.Debug("{0} - http content created properly. ", nameof(PostSensorDataAsync));

            var response = await _httpRetryPolicy.ExecuteAsync(async () => await _httpClient.PostAsync("", httpContent));

            _logger.Debug("{0} - Posted to remote server, received: {1}. ", nameof(PostSensorDataAsync), response.StatusCode);
            return response;
        }

        private void ObtainAuthorizationToken()
        {
            if (_httpClient.DefaultRequestHeaders.Authorization == null)
            {
                if (_refreshToken < DateTime.Now)
                {
                    _refreshToken = DateTime.Now.AddMinutes(5);
                    _httpClient.DefaultRequestHeaders.Accept.Clear();
                    var token = Task.Run(async () => await _authorizationService.Authorize());
                    token.Wait();
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Result.ToString());
                    _logger.Debug("{0} - obtained new token", nameof(ObtainAuthorizationToken));
                }
                else
                {
                    _logger.Debug("{0} - token still valid", nameof(ObtainAuthorizationToken));
                }
            }
        }
    }
}