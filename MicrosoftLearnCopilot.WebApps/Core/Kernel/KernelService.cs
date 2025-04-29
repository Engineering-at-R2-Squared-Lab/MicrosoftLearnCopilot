using System.Security.Authentication;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MicrosoftLearnCopilot.Core.Function;
namespace MicrosoftLearnCopilot.Core;

public class KernelService
{
    private Kernel kernel;
    private string deploymentName;
    private string apiKey;
    private string endpoint;

    private ChatHistory chatHistory;

    public void setChatHistory(ChatHistory history)
    {
        this.chatHistory = history;
    }

    public KernelService(string deploymentName, string apiKey, string endpoint)
    {
        this.deploymentName = deploymentName;
        this.apiKey = apiKey;
        this.endpoint = endpoint;
        this.kernel = InitializeKernel();
        this.chatHistory = new ChatHistory();
    }

    private Kernel InitializeKernel()
    {
        try
        {
            if (string.IsNullOrEmpty(deploymentName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentException("Deployment name, API key, and endpoint must be provided.");
            }

            var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(deploymentName: this.deploymentName, apiKey: this.apiKey, endpoint: this.endpoint);
            builder.Plugins.AddFromType<MicrosoftLearnAPI>("MicrosoftLearnAPI");
            return builder.Build();
        }
        catch (Exception ex)
        {
            throw new AuthenticationException("Failed to initialize kernel, Please check your credentials.", ex);
        }
    }

    /// <summary>
    /// Sends a user prompt to the chat completion service and retrieves the generated response.
    /// </summary>
    /// <param name="prompt">The user input or prompt to be sent to the chat completion service.</param>
    /// <returns>
    /// A <see cref="Task{String}"/> representing the asynchronous operation. 
    /// The task result contains the generated response from the chat completion service.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the chat completion service returns a null response or fails to process the request.
    /// </exception>
    /// <exception cref="UriFormatException">
    /// Thrown when the URI format is invalid, typically due to an incorrect endpoint configuration.
    /// </exception>
    public async Task<string> chatCompletion(string prompt)
    {
        try
        {
            chatHistory.AddUserMessage(prompt);

            OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = 1000,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var chatCompletionService = this.kernel.GetRequiredService<IChatCompletionService>();
            var invokation = await chatCompletionService.GetChatMessageContentAsync(
                this.chatHistory,
                executionSettings: settings,
                kernel: this.kernel
            );

            if (invokation == null || invokation.Content == null)
            {
                throw new InvalidOperationException("Chat completion service returned a null response.");
            }

            return invokation.Content.ToString();
        }
        catch (Exception ex)
        {
            if (ex is UriFormatException)
            {
                throw new UriFormatException("Invalid URI format. Please check your endpoint. If you use Azure endpoint from Azure AI ensure the endpoint is inference endpoint.", ex);
            }
            throw new InvalidOperationException("Failed to get chat completion.", ex);
        }
    }

    /// <summary>
    /// Streams chat completion response chunks as they are generated.
    /// </summary>
    /// <param name="prompt">The user input or prompt to be sent to the chat completion service.</param>
    /// <returns>
    /// An <see cref="IAsyncEnumerable{String}"/> yielding response chunks as they are generated.
    /// </returns>
    public async IAsyncEnumerable<string> chatCompletionStream(string prompt)
    {
        chatHistory.AddUserMessage(prompt);

        OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 1000,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var chatCompletionService = this.kernel.GetRequiredService<IChatCompletionService>();

        // Use Semantic Kernel's streaming API
        await foreach (var message in chatCompletionService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            executionSettings: settings,
            kernel: this.kernel))
        {
            if (message?.Content != null)
            {
                yield return message.Content;
            }
        }
    }

    /// <summary>
    /// Streams a natural, context-aware answer using the chat history.
    /// </summary>
    public async IAsyncEnumerable<string> SummarizeOrAnswerStream(string userQuestion)
    {
        // Compose a prompt that includes chat history context
        string prompt = $@"
You are a helpful assistant. Continue the conversation naturally, using the following chat history and the user's latest question.

Chat history:
{this.chatHistory}

User question:
{userQuestion}

Answer:
";
        // Add the latest user message to the chat history
        chatHistory.AddUserMessage(userQuestion);

        OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 1000,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var chatCompletionService = this.kernel.GetRequiredService<IChatCompletionService>();

        await foreach (var message in chatCompletionService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            executionSettings: settings,
            kernel: this.kernel))
        {
            if (message?.Content != null)
            {
                yield return message.Content;
            }
        }
    }

    /// <summary>
    /// Streams a conversational, context-aware answer using chat history and Microsoft Learn knowledge.
    /// </summary>
    public async IAsyncEnumerable<string> SummarizeOrAnswerWithKnowledgeStream(string userQuestion, string knowledge)
    {
        // Optionally, strip HTML tags for a cleaner prompt
        string plainText = string.IsNullOrWhiteSpace(knowledge)
            ? ""
            : System.Text.RegularExpressions.Regex.Replace(knowledge, "<.*?>", string.Empty);

        string prompt = $@"
You are a helpful assistant. Use the following Microsoft Learn content and the chat history to answer the user's question in a conversational style.

Chat history:
{this.chatHistory}

User question:
{userQuestion}

Microsoft Learn content:
{plainText}

Answer:
";
        chatHistory.AddUserMessage(userQuestion);

        OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 1000,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var chatCompletionService = this.kernel.GetRequiredService<IChatCompletionService>();

        await foreach (var message in chatCompletionService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            executionSettings: settings,
            kernel: this.kernel))
        {
            if (message?.Content != null)
            {
                yield return message.Content;
            }
        }
    }

    /// <summary>
    /// Uses the LLM to generate a chat room name/title from the user's first prompt.
    /// </summary>
    public async Task<string> GenerateRoomNameAsync(string prompt)
    {
        string systemPrompt = @"Generate a short, descriptive chat room name (max 6 words, no punctuation) for the following user query. 
Return only the name, no quotes or extra text.

User query:
" + prompt;

        var result = await chatCompletion(systemPrompt);
        // Optionally, clean up the result

        return result?.Trim().Replace("\n", " ") ?? "New Chat";
    }
}

