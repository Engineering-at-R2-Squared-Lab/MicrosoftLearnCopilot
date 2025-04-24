using System.Security.Authentication;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
namespace MicrosoftLearnCopilot.Core;

public class KernelService
{
    private Kernel kernel;
    private string deploymentName;
    private string apiKey;
    private string endpoint;

    private ChatHistory chatHistory;

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

            // implement plugins here ...
            // this.kernel.Plugins.Add();

            var chatCompletionService = this.kernel.GetRequiredService<IChatCompletionService>();
            var invokation = await chatCompletionService.GetChatMessageContentAsync(
                this.chatHistory
            );

            if (invokation == null || invokation.Content == null)
            {
                throw new InvalidOperationException("Chat completion service returned a null response.");
            }

            return invokation.Content;
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
}

