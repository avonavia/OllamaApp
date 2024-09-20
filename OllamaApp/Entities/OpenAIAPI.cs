using System.Text.Json.Serialization;
using OllamaSharp.Models.Chat;

namespace OllamaApp.Entities;

public class OpenAIAPI
{
    public class ResponseData
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("object")] public string Object { get; set; } = "";
        [JsonPropertyName("created")] public ulong Created { get; set; }
        [JsonPropertyName("choices")] public List<Choice> Choices { get; set; } = new();
        [JsonPropertyName("usage")] public Usage Usage { get; set; } = new();
    }

    public class Usage
    {
        [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("index")] public int Index { get; set; }
        [JsonPropertyName("message")] public Message Message { get; set; } = new();
        [JsonPropertyName("finish_reason")] public string FinishReason { get; set; } = "";
    }

    public class Request
    {
        [JsonPropertyName("model")] public string ModelId { get; set; } = "";
        [JsonPropertyName("messages")] public List<Message> Messages { get; set; } = new();
    }
}