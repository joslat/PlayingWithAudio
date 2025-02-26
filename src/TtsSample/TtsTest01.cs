using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextToAudio;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NAudio.Wave;

namespace TtsSample;

public static class TtsTest01
{
    private const string MainDeploymentName = "tts-hd";
    private const int MaxCharacters = 4096;
    private const int BufferCharacters = 300; // leave room to avoid cutting mid-sentence

    // We'll split at simple sentence-ending characters.
    private static readonly char[] SentenceEndings = new char[] { '.', '!', '?' };

    public async static Task Execute()
    {
        Console.WriteLine("Hello, World!");

        var kernel = Kernel.CreateBuilder()
            .AddAzureOpenAITextToAudio(
                deploymentName: MainDeploymentName,
                endpoint: EnvironmentWellKnown.TTSEndpoint,
                apiKey: EnvironmentWellKnown.TTSApiKey)
            .Build();

        var service = kernel.GetRequiredService<ITextToAudioService>();

        // Execution settings (adjust as needed)
        OpenAITextToAudioExecutionSettings executionSettings = new()
        {
            Voice = "alloy",         // Supported voices: alloy, echo, fable, onyx, nova, shimmer.
            ResponseFormat = "mp3",    // Supported formats: mp3, opus, aac, flac.
            Speed = 0.9f               // Speed from 0.25 to 4.0.
        };

        string sometextDEEN2 = "Hallo Jose!! Es ist immer eine gute Idee, früh aufzustehen und produktiv zu sein.\n" +
                              "Hola Jose!! It is always a good idea to wake up early and be productive.";

        // Build a long text of ~10,000 characters by repeating a base sentence.
        int repeatCount = (10000 / sometextDEEN2.Length) + 1;
        string longText = String.Concat(Enumerable.Repeat(sometextDEEN2, repeatCount));
        longText = longText.Substring(0, 10000); // Trim exactly to 10k characters

        // Split the text into segments.
        List<string> segments = SplitTextIntoSegments(longText, MaxCharacters, BufferCharacters);
        Console.WriteLine($"Split text into {segments.Count} segments.");

        // Process each segment – call TTS and save to temporary files.
        List<string> tempFiles = new();
        int index = 0;
        foreach (var segment in segments)
        {
            string tempFile = $"temp_segment_{index}.mp3";
            Console.WriteLine($"Processing segment {index + 1}/{segments.Count} with {segment.Length} characters.");
            AudioContent audioContent = await service.GetAudioContentAsync(segment, executionSettings);
            await SaveAudioContentToFileAsync(audioContent, tempFile);
            tempFiles.Add(tempFile);
            index++;
        }

        // Merge the temporary MP3 files into one output file.
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

    private static async Task SaveAudioContentToFileAsync(AudioContent audioContent, string filePath)
    {
        if (audioContent == null || audioContent.Data == null)
        {
            throw new ArgumentNullException(nameof(audioContent), "Audio content or data cannot be null.");
        }

        using (var memoryStream = new MemoryStream())
        {
            var data = audioContent.Data.Value;
            await memoryStream.WriteAsync(data);
            byte[] audioBytes = memoryStream.ToArray();
            await System.IO.File.WriteAllBytesAsync(filePath, audioBytes);
        }
    }

    // Splits the text into segments that do not exceed maxCharacters.
    private static List<string> SplitTextIntoSegments(string text, int maxChars, int buffer)
    {
        List<string> segments = new();
        string remainingText = text.Trim();

        while (remainingText.Length > maxChars)
        {
            // Take a tentative segment size with some buffer.
            int tentativeLength = maxChars - buffer;
            string segmentCandidate = remainingText.Substring(0, tentativeLength);

            // Try to split at the last sentence-ending punctuation.
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

    // Merges multiple MP3 files (provided as file paths) into a single MP3 file.
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
