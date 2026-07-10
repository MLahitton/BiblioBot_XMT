using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Application.Features.Chat.Common;
using Microsoft.Extensions.Options;

namespace Infrastructure.Chatbot;

public sealed class FastApiChatbotClient : IChatbotClient
{
    private readonly HttpClient _httpClient;
    private readonly ChatbotOptions _options;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public FastApiChatbotClient(
        HttpClient httpClient,
        IOptions<ChatbotOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        if (_httpClient.DefaultRequestHeaders.Accept.All(h => h?.MediaType != "application/json"))
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    public async Task<ChatbotResponseDto> SendMessageAsync(
        ChatbotRequestDto request,
        CancellationToken cancellationToken)
    {
        var endpoint = BuildMessageEndpoint();
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                endpoint,
                request,
                _jsonSerializerOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"FastAPI respondió con estado {(int)response.StatusCode} {response.ReasonPhrase}.");
            }

            var responsePayload = await response.Content.ReadFromJsonAsync<ChatbotResponseDto>(
                _jsonSerializerOptions,
                cancellationToken);

            if (responsePayload is null)
            {
                throw new InvalidOperationException("Respuesta inválida del servicio de chatbot.");
            }

            return responsePayload;
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new InvalidOperationException("Respuesta inválida del servicio de chatbot.", ex);
        }
    }

    private string BuildMessageEndpoint()
    {
        var baseUrl = _options.BaseUrl?.Trim();
        var messagePath = _options.MessagePath?.Trim();

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(messagePath))
        {
            throw new InvalidOperationException("La configuración de chatbot es inválida.");
        }

        var normalizedPath = messagePath.StartsWith('/') ? messagePath : $"/{messagePath}";
        return $"{baseUrl.TrimEnd('/')}{normalizedPath}";
    }
}
