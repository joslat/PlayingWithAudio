using OpenAI.Chat;
using Azure.AI.OpenAI;
using Azure;

namespace Gpt4oAudioSample;

public static class  Gpt4oAudioTest01
{
    public async static Task Execute()
    {
        AzureOpenAIClient azureClient = new(
            new Uri(EnvironmentWellKnown.Gpt4oAudioEndpoint),
            new AzureKeyCredential(EnvironmentWellKnown.Gpt4oAudioApiKey),
            new AzureOpenAIClientOptions(AzureOpenAIClientOptions.ServiceVersion.V2025_01_01_Preview) 
            );

        ChatClient chatClient = azureClient.GetChatClient(EnvironmentWellKnown.Gpt4oAudioDeploymentName);

        List<ChatMessage> messages =
            [
                new UserChatMessage(ChatMessageContentPart.CreateTextPart("Hi, this is some text to record an audio for. (pause for 3 seconds). And now I have spoken!!.")),
            ];

        // ResponseModalities values and corresponding AudioOptions.
        ChatCompletionOptions options = new()
        {
            ResponseModalities = ChatResponseModalities.Text | ChatResponseModalities.Audio, // we only want audio responses, no text
            AudioOptions = new(
                ChatOutputAudioVoice.Alloy, 
                ChatOutputAudioFormat.Mp3)
        };

        // No speed supported?
        //    Speed = 0.9f               // Speed from 0.25 to 4.0.
        
        ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);

        await PrintAudioContentAsync(completion);
    }

    public async static Task PrintAudioContentAsync(ChatCompletion completion)
    {
        if (completion.OutputAudio is ChatOutputAudio outputAudio)
        {
            Console.WriteLine($"Response audio transcript: {outputAudio.Transcript}");
            string outputFilePath = $"{outputAudio.Id}.mp3";
            
            using (FileStream outputFileStream = File.OpenWrite(outputFilePath))
            {
                await outputFileStream.WriteAsync(outputAudio.AudioBytes);
            }

            Console.WriteLine($"Response audio written to file: {outputFilePath}");
            Console.WriteLine($"Valid on follow up requests until: {outputAudio.ExpiresAt}");
        }
    }
}
