using JwtAuth.Library.Services;
using Microsoft.Extensions.Configuration;
using Polly.Retry;
using RabbitBase.Library.Contracts;
using Serilog;
using System.Net.Http.Json;

namespace RabbitReader.API
{
    internal class ApiClient : IApiClient
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _httpClient;
        private readonly RetryPolicy<HttpResponseMessage> _httpRequestPolicy;
        private readonly Uri _requestUri;
        private readonly IAuthorizationService _authorizationService;
        private string JWTToken = string.Empty;
        private int TimesRetried = 0;
        private int RetriesLimit = 10;

        public ApiClient(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger logger, IAuthorizationService authorizationService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _authorizationService = authorizationService;

            string targetUrl = config.GetSection("ApplicationSettings")
                .GetSection("TargetApiUrl").Get<string>();
            _requestUri = new Uri(targetUrl);           
            
            _logger.Debug("{0} - instance initialized properly. ", nameof(ApiClient));
        }       

        private async Task AuthorizeHttpClient()
        {
            TimesRetried++;
            JWTToken = await _authorizationService.Authorize();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", JWTToken.ToString());
        }

        public async Task<HttpResponseMessage> PostSensorDataAsync(BmpMeasurementDto measurement)
        {
            var httpContent = JsonContent.Create(measurement);
            _logger.Debug("{0} - http content created properly. ", nameof(PostSensorDataAsync));

            await AuthorizeHttpClient(); 

            HttpResponseMessage response = await _httpClient.PostAsync(_requestUri, httpContent);
            //ToDo inspect logic to avoid endless loops. 
            if (TimesRetried == RetriesLimit)
                throw new Exception("Client got stuck in endless loop. Credentials invalid or server malfunctioning");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                //todo goes in here wihout trying again. so goes here twice in a row
                _logger.Debug("{0} - Token expired. ", nameof(PostSensorDataAsync));
                await AuthorizeHttpClient();
                response = await PostSensorDataAsync(measurement);
            }

            _logger.Debug("{0} - Posted to remote server, received: {1}. ", nameof(PostSensorDataAsync), response.StatusCode);
            return response;
        }       
    }
}
