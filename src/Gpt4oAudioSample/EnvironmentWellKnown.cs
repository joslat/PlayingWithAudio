using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gpt4oAudioSample;

public static class EnvironmentWellKnown
{
    private static string? _deploymentName;
    public static string Gpt4oAudioDeploymentName => _deploymentName ??= Environment.GetEnvironmentVariable("AzureOpenAI_Model_Gpt4oAudio");

    private static string? _endpoint;
    public static string Gpt4oAudioEndpoint => _endpoint ??= Environment.GetEnvironmentVariable("AzureOpenAI_Endpoint_Gpt4oAudio");

    private static string? _apiKey;
    public static string Gpt4oAudioApiKey => _apiKey ??= Environment.GetEnvironmentVariable("AzureOpenAI_ApiKey_Gpt4oAudio");
}
