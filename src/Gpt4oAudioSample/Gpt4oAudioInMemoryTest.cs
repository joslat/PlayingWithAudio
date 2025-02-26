using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using NAudio.Wave;

namespace Gpt4oAudioSample;

public static class Gpt4oAudioInMemoryTest
{
    private const int MaxCharacters = 4096;
    private const int BufferCharacters = 300; // leave room to avoid cutting mid-sentence
    private static readonly char[] SentenceEndings = new char[] { '.', '!', '?' };

    // Main method: processes a long text, obtains audio byte arrays, merges them, and returns the merged audio.
    public async static Task Execute()
    {
        Console.WriteLine("Starting long TTS processing (in-memory)...");

        // Initialize the Azure OpenAI client and ChatClient.
        AzureOpenAIClient azureClient = new(
            new Uri(EnvironmentWellKnown.Gpt4oAudioEndpoint),
            new AzureKeyCredential(EnvironmentWellKnown.Gpt4oAudioApiKey),
            new AzureOpenAIClientOptions(AzureOpenAIClientOptions.ServiceVersion.V2025_01_01_Preview)
        );
        ChatClient chatClient = azureClient.GetChatClient(EnvironmentWellKnown.Gpt4oAudioDeploymentName);

        // Build a long text (~10,000 characters) by repeating a base sentence.
        string baseText = "Hallo Jose!! Es ist immer eine gute Idee, früh aufzustehen und produktiv zu sein.\n" +
                          "Hola Jose!! It is always a good idea to wake up early and be productive. ";
        int repeatCount = (10000 / baseText.Length) + 1;
        string longText = string.Concat(Enumerable.Repeat(baseText, repeatCount));
        longText = longText.Substring(0, 10000); // Exactly 10k characters

        // Split the long text into segments.
        List<string> segments = SplitTextIntoSegments(longText, MaxCharacters, BufferCharacters);
        Console.WriteLine($"Split text into {segments.Count} segments.");

        // Process each segment and store audio bytes in-memory.
        List<byte[]> audioByteSegments = new();
        int index = 0;
        foreach (var segment in segments)
        {
            Console.WriteLine($"Processing segment {index + 1}/{segments.Count} ({segment.Length} characters).");

            List<ChatMessage> messages = new()
            {
                new UserChatMessage(ChatMessageContentPart.CreateTextPart(segment))
            };

            ChatCompletionOptions options = new()
            {
                ResponseModalities = ChatResponseModalities.Text | ChatResponseModalities.Audio,
                AudioOptions = new (
                    ChatOutputAudioVoice.Alloy, 
                    ChatOutputAudioFormat.Mp3)
            };

            ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);
            if (completion.OutputAudio is ChatOutputAudio outputAudio)
            {
                var audioByteArray = outputAudio.AudioBytes.ToArray();
                audioByteSegments.Add(audioByteArray);
                Console.WriteLine($"Segment {index + 1} received: {audioByteArray.Length} bytes.");
            }
            else
            {
                Console.WriteLine($"No audio output for segment {index + 1}.");
            }
            index++;
        }

        // Merge the in-memory MP3 byte arrays into a single byte array.
        byte[] mergedAudio = MergeMp3ByteArrays(audioByteSegments);
        Console.WriteLine($"Merged audio length: {mergedAudio.Length} bytes.");

        await SaveMergedAudioToFileAsync(mergedAudio, "final_output.mp3");
    }

    // Splits the input text into segments that do not exceed maxChars.
    private static List<string> SplitTextIntoSegments(string text, int maxChars, int buffer)
    {
        List<string> segments = new();
        string remainingText = text.Trim();

        while (remainingText.Length > maxChars)
        {
            int tentativeLength = maxChars - buffer;
            string segmentCandidate = remainingText.Substring(0, tentativeLength);
            int lastPunctuation = segmentCandidate.LastIndexOfAny(SentenceEndings);
            int splitIndex = (lastPunctuation > 0) ? lastPunctuation + 1 : tentativeLength;
            string segment = remainingText.Substring(0, splitIndex).Trim();
            segments.Add(segment);
            remainingText = remainingText.Substring(splitIndex).Trim();
        }
        if (!string.IsNullOrEmpty(remainingText))
        {
            segments.Add(remainingText);
        }
        return segments;
    }

    // Merges multiple MP3 byte arrays (in-memory) into a single MP3 byte array.
    private static byte[] MergeMp3ByteArrays(List<byte[]> mp3ByteArrays)
    {
        using (var outputStream = new MemoryStream())
        {
            foreach (var mp3Bytes in mp3ByteArrays)
            {
                using (var ms = new MemoryStream(mp3Bytes))
                using (var reader = new Mp3FileReader(ms))
                {
                    Mp3Frame frame;
                    while ((frame = reader.ReadNextFrame()) != null)
                    {
                        outputStream.Write(frame.RawData, 0, frame.RawData.Length);
                    }
                }
            }
            return outputStream.ToArray();
        }
    }

    // Saves the merged audio byte array to a file.
    public static async Task SaveMergedAudioToFileAsync(byte[] mergedAudio, string filePath)
    {
        if (mergedAudio == null || mergedAudio.Length == 0)
        {
            throw new ArgumentException("Merged audio is empty.", nameof(mergedAudio));
        }
        await File.WriteAllBytesAsync(filePath, mergedAudio);
        Console.WriteLine($"Merged audio saved to file: {filePath}");
    }
}
