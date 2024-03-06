using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PipeHttp.Api;

public class CallParallelService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CallParallelService> _logger;
    private readonly List<PipelineResult> responses = new List<PipelineResult>();

    public CallParallelService(IHttpClientFactory httpClientFactory, ILogger<CallParallelService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<PipelineResult>> Execute()
    {

        List<Pipeline> pipeline = await GetPipelineConfig();

        try
        {
            foreach (var pipe in pipeline)
            {
                try
                {
                    if (!pipe.Config.Sequential)
                    {
                        await ParallelBackend(pipe.Backends);
                    }
                    else
                    {
                        await SequentialBackend(pipe.Backends);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogInformation("backend with error {e}", e.Message);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogInformation("pipeline with error {e}", e.Message);
        }

        return responses;
    }

    private async Task ParallelBackend(List<Backend> backends)
    {
        // Configuração básica do HttpClient
        using HttpClient client = _httpClientFactory.CreateClient();

        // Lista para armazenar as tarefas de solicitação
        var requestTasks = new List<Task<HttpResponseMessage>>();

        foreach (var backend in backends)
        {
            // Adicionando as tarefas de solicitação à lista
            var request = RequestFactory(backend);
            requestTasks.Add(client.SendAsync(request));
        }

        // Aguardar todas as solicitações serem concluídas
        await Task.WhenAll(requestTasks);

        // Processar as respostas
        foreach (var requestTask in requestTasks)
        {
            HttpResponseMessage response = await requestTask;
            var contentResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            responses.Add(new PipelineResult()
            {
                Content = contentResult,
                HttpResponse = response
            });
            Console.WriteLine($"Status da resposta: {response.StatusCode}");
            // Adicione qualquer outro processamento necessário aqui
        }

    }

    private async Task SequentialBackend(List<Backend> backends)
    {
        // Configuração básica do HttpClient
        using HttpClient client = _httpClientFactory.CreateClient();

        foreach (var backend in backends)
        {
            var request = RequestFactory(backend);
            var response = await client.SendAsync(request);
            var contentResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            responses.Add(new PipelineResult()
            {
                Content = contentResult,
                HttpResponse = response
            });
            Console.WriteLine($"Status da resposta: {response.StatusCode}");
            Console.WriteLine($"Contant da resposta: {contentResult}");
        }
    }

    private async Task<List<Pipeline>> GetPipelineConfig()
    {
        string json = await File.ReadAllTextAsync("config-pipeline.json");

        List<Pipeline> pipeline = JsonSerializer.Deserialize<List<Pipeline>>(json);

        return pipeline;
    }

    private HttpRequestMessage RequestFactory(Backend backend)
    {
        var request = new HttpRequestMessage(new HttpMethod(backend.Method), backend.Url);
        if (backend.Body != null)
        {
            request.Content = new StringContent(backend.Body);
        }

        foreach (var header in backend.Headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

        return request;
    }

    public class Pipeline
    {
        [JsonPropertyName("config")]
        public required Config Config { get; set; }
        [JsonPropertyName("backends")]
        public required List<Backend> Backends { get; set; }
    }

    public class Config
    {
        [JsonPropertyName("sequential")]
        public bool Sequential { get; set; }
    }

    public class Backend
    {
        [JsonPropertyName("method")]
        public required string Method { get; set; }
        [JsonPropertyName("url")]
        public required string Url { get; set; }
        [JsonPropertyName("body")]
        public string? Body { get; set; }
        [JsonPropertyName("headers")]
        public Dictionary<string, string> Headers = [];
    }

    public class PipelineResult
    {
        public HttpResponseMessage? HttpResponse { get; set; }
        public string? Content { get; set; } = "";
    }

}
