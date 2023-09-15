using System.Threading.Tasks;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using TMPro;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Object = System.Object;

public class ImageAnalyzer : MonoBehaviour
{
    [SerializeField] private ProcessedABImage processedAbImage;
    [SerializeField] private float MinimumConfidenceForObjectDetection;

    public TextMeshPro outputText;

    [Header("Computer Vision Resource")]
    [SerializeField] private string ComputerVisionEndpoint;
    [SerializeField] private string ComputerVisionSubscriptionKey;
    
    [Header("Custom Vision Resource")]
    [SerializeField] private string CustomVisionPredictionURL;
    [SerializeField] private string CustomVisionPredictionKey;
    //[SerializeField] private string ProjectTitle;
    //[SerializeField] private string PublishedIteration;

    [Header("Blob Manager")]
    [SerializeField] private BlobManager BlobManager;

    [Header("Table Manager")]
    [SerializeField] private TableManager TableManager;

    private readonly List<string> _wordsIndicateGrasping = new List<string>()
    {
        "Holding",
        "holding",
        "hold",
        "holds",
    };

    private string _errorMessage = string.Empty;
    private string _debugResult = string.Empty;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    public async void Analyze()
    {
        var prediction = await AnalyzeWithComputerVision();
        outputText.text = prediction.Result;

        var caption = prediction.Result;

        //Iterate through the list of buzzwords
        if (_wordsIndicateGrasping.Any(word => caption.Contains(word)))
        {
            BlobManager.StoreImageAfterComputerVision(caption);
        }
    }

    private async Task<Prediction> AnalyzeWithComputerVision()
    {
        if (processedAbImage == null)
        {
            var message = $"Error: Assign variable {processedAbImage.name} in Unity.";
            var faultyPrediction = new Prediction() { Result = message };

            return faultyPrediction;
        }
        
        var image = processedAbImage.CurrentImageInBytes;

        var result = string.Empty;

        //Connect to resource
        try
        {
            using var client = new HttpClient();
            using var content = new ByteArrayContent(image);

            // Request headers.
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ComputerVisionSubscriptionKey);

            // Assemble the URI for the REST API method.
            var uri = $"{ComputerVisionEndpoint}/computervision/imageanalysis:analyze?api-version=2023-04-01-preview&features=caption";

            // This example uses the "application/octet-stream" content type.
            // The other content types you can use are "application/json" and "multipart/form-data".
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            // Asynchronously call the REST API method.
            HttpResponseMessage response = await client.PostAsync(uri, content);
            // Asynchronously get the JSON response.
            result = await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            result = "Error: " + e.Message;
        }

        //Get results

        var prediction = new Prediction() { Result = result};
        return prediction;
    }

    public async void Detect()
    {
        outputText.text = string.Empty;

        var totalOperationStartTime = Time.time;
        
        var tags = await DetectWithCustomVision();

        var customVisionDuration = Math.Round(Time.time - totalOperationStartTime, 2);
        
        if (tags == null)
        {
            outputText.text = _errorMessage;
            return;
        }

        if (tags.Count == 0)
        {
            outputText.text = "No tags were found";
            return;
        }

        var filteredTags = TagsManager.GetTagsWithConfidenceHigherThan(MinimumConfidenceForObjectDetection, tags);

        if (!filteredTags.Any())
        {
            outputText.text = "No tags with high confidence were found";
            return;
        }

        var imageName = "Image" + DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + ".png";

        var blobStartTime = Time.time;
        BlobManager.StoreImageAfterComputerVision(imageName);
        var blobDuration = Math.Round(Time.time - blobStartTime, 2);

        var tableStartTime = Time.time;
        TableManager.StoreTags(filteredTags, imageName);
        var tableDuration = Math.Round(Time.time - tableStartTime);

        //Profiling
        var totalOperationDuration = Math.Round(Time.time - totalOperationStartTime, 2);

        var runtimes = new Runtimes(customVisionDuration, blobDuration, tableDuration, totalOperationDuration);

        TableManager.StoreRuntimes(runtimes, imageName);

        var runtimeProfilingMessage = GetRuntimeProfilingMessage(runtimes);

        outputText.text += runtimeProfilingMessage;
    }

    private static string GetRuntimeProfilingMessage(Runtimes runtimes)
    {
        var customVisionDurationPercentage = $"{Math.Round(100 * runtimes.CustomVision / runtimes.Total)}%";
        var blobDurationPercentage = $"{Math.Round(100 * runtimes.BlobStorage / runtimes.Total)}%";
        var tableDurationPercentage = $"{Math.Round(100 * runtimes.TableStorage / runtimes.Total)}%";

        var customVisionDurationMessage =
            $"\nVision Duration: {runtimes.CustomVision}s ({customVisionDurationPercentage})\n";
        var blobStoreDurationMessage = $"Blob Duration: {runtimes.BlobStorage}s ({blobDurationPercentage})\n";
        var tableStoreDurationMessage = $"Table Duration: {runtimes.TableStorage}s ({tableDurationPercentage})\n";
        var totalDurationMessage = $"Total Duration: {runtimes.Total}s\n";

        var runtimeProfilingMessage = customVisionDurationMessage + blobStoreDurationMessage +
                                      tableStoreDurationMessage + totalDurationMessage;
        return runtimeProfilingMessage;
    }

    [ItemCanBeNull]
    private async Task<List<Tag>> DetectWithCustomVision()
    {
        if (processedAbImage == null)
        {
            _errorMessage = $"Error: Assign variable {processedAbImage.name} in Unity.";
            return null;
        }

        var image = processedAbImage.CurrentImageInBytes;

        var result = string.Empty;

        //Connect to resource
        try
        {
            using var client = new HttpClient();
            using var content = new ByteArrayContent(image);

            client.DefaultRequestHeaders.Add("Prediction-Key", CustomVisionPredictionKey);

            var response = await client.PostAsync(CustomVisionPredictionURL, content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.ReasonPhrase);
            }

            result = await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            _errorMessage = "Error: " + e.Message;
            return null;
        }

        //Get results

        _debugResult = result;

        var predictedTags = TagsManager.GetPredictedTagsFromResult(result);
       
        return predictedTags;
    }
}