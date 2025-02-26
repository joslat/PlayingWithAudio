using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TtsSample;

public static class EnvironmentWellKnown
{
    private static string? _deploymentName;
    public static string TTSDeploymentName => _deploymentName ??= Environment.GetEnvironmentVariable("AzureOpenAI_Model_tts");

    private static string? _endpoint;
    public static string TTSEndpoint => _endpoint ??= Environment.GetEnvironmentVariable("AzureOpenAItts_Endpoint");

    private static string? _apiKey;
    public static string TTSApiKey => _apiKey ??= Environment.GetEnvironmentVariable("AzureOpenAItts_ApiKey");
}
