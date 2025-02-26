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

public static class Gpt4oAudioLongTest
{
    private const int MaxCharacters = 4096;
    private const int BufferCharacters = 300; // leave room to avoid cutting mid-sentence
    private static readonly char[] SentenceEndings = new char[] { '.', '!', '?' };

    public async static Task Execute()
    {
        Console.WriteLine("Starting long TTS processing...");

        // Initialize the Azure OpenAI client and ChatClient using your environment settings.
        AzureOpenAIClient azureClient = new(
            new Uri(EnvironmentWellKnown.Gpt4oAudioEndpoint),
            new AzureKeyCredential(EnvironmentWellKnown.Gpt4oAudioApiKey),
            new AzureOpenAIClientOptions(AzureOpenAIClientOptions.ServiceVersion.V2025_01_01_Preview)
        );

        ChatClient chatClient = azureClient.GetChatClient(EnvironmentWellKnown.Gpt4oAudioDeploymentName);

        // Build a long text (~10,000 characters) by repeating a base string.
        string baseText = "Hallo Jose!! Es ist immer eine gute Idee, früh aufzustehen und produktiv zu sein.\n" +
                          "Hola Jose!! It is always a good idea to wake up early and be productive. ";
        int repeatCount = (10000 / baseText.Length) + 1;
        string longText = String.Concat(Enumerable.Repeat(baseText, repeatCount));
        longText = longText.Substring(0, 10000); // Trim exactly to 10k characters

        // Split the long text into segments.
        List<string> segments = SplitTextIntoSegments(longText, MaxCharacters, BufferCharacters);
        Console.WriteLine($"Split text into {segments.Count} segments.");

        // For each segment, generate audio and save to a temporary file.
        List<string> tempFiles = new();
        int index = 0;
        foreach (var segment in segments)
        {
            Console.WriteLine($"Processing segment {index + 1}/{segments.Count} ({segment.Length} characters).");

            // Build a message with the current segment.
            List<ChatMessage> messages = new()
            {
                new UserChatMessage(ChatMessageContentPart.CreateTextPart(segment))
            };

            // Set up chat options for audio only. (Note: additional options like speed might not be supported yet.)
            ChatCompletionOptions options = new()
            {
                ResponseModalities = ChatResponseModalities.Text | ChatResponseModalities.Audio,
                AudioOptions = new(
                    ChatOutputAudioVoice.Alloy, 
                    ChatOutputAudioFormat.Mp3)
            };

            ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);

            if (completion.OutputAudio is ChatOutputAudio outputAudio)
            {
                string tempFile = $"temp_segment_{index}.mp3";
                await SaveAudioBytesToFileAsync(outputAudio.AudioBytes.ToArray(), tempFile);
                tempFiles.Add(tempFile);
                Console.WriteLine($"Segment {index + 1} audio saved to {tempFile}");
            }
            else
            {
                Console.WriteLine($"No audio output for segment {index + 1}");
            }
            index++;
        }

        // Merge the temporary MP3 files into one final file.
        string finalOutput = "final_output.mp3";
        MergeMp3Files(tempFiles, finalOutput);
        Console.WriteLine($"Merged {tempFiles.Count} segments into {finalOutput}");

        // Clean up temporary files.
        foreach (var file in tempFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
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

    // Saves the provided audio bytes to a file.
    private static async Task SaveAudioBytesToFileAsync(byte[] audioBytes, string filePath)
    {
        if (audioBytes == null || audioBytes.Length == 0)
        {
            throw new ArgumentNullException(nameof(audioBytes), "Audio bytes cannot be null or empty.");
        }
        await File.WriteAllBytesAsync(filePath, audioBytes);
    }

    // Merges multiple MP3 files (given by file paths) into a single MP3 file.
    private static void MergeMp3Files(List<string> inputFiles, string outputFile)
    {
        using (var outputStream = File.Create(outputFile))
        {
            foreach (var file in inputFiles)
            {
                using (var reader = new Mp3FileReader(file))
                {
                    Mp3Frame frame;
                    while ((frame = reader.ReadNextFrame()) != null)
                    {
                        outputStream.Write(frame.RawData, 0, frame.RawData.Length);
                    }
                }
            }
        }
    }
}
