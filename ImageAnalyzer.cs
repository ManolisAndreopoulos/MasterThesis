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
    [SerializeField] private AdjustedImageProvider _adjustedImageProvider;
    [SerializeField] private float MinimumConfidenceForObjectDetection;

    public TextMeshPro OutputText;

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

    /// <summary>
    /// Trigger re-population of the Batch of most recent images
    /// </summary>
    //public event Action AnalysisWasCompleted;

    private readonly List<string> _wordsIndicateGrasping = new List<string>()
    {
        "Holding",
        "holding",
        "hold",
        "holds",
    };

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void StoreImage()
    {
        StoreImageInternal();
    }

    private async Task StoreImageInternal()
    {
        var image = _adjustedImageProvider.GetCurrentAdjustedABImage();
        OutputText.text = await BlobManager.StoreImageForTraining(image);
    }

    public async void Analyze()
    {
        var prediction = await AnalyzeWithComputerVision(_adjustedImageProvider.NewAdjustedAbImageBatch.First().InBytes);
        OutputText.text = prediction.Result;

        var caption = prediction.Result;

        //Iterate through the list of buzzwords
        if (_wordsIndicateGrasping.Any(word => caption.Contains(word)))
        {
            BlobManager.StoreImage(_adjustedImageProvider.NewAdjustedAbImageBatch.First());
        }

        //AnalysisWasCompleted.Invoke();
    }

    private async Task<GeneralPrediction> AnalyzeWithComputerVision(byte[] image)
    {
        if (_adjustedImageProvider == null)
        {
            var message = $"Error: Assign variable {_adjustedImageProvider.name} in Unity.";
            var faultyPrediction = new GeneralPrediction() { Result = message };

            return faultyPrediction;
        }
        
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

        var prediction = new GeneralPrediction() { Result = result};
        return prediction;
    }

    public async void Detect()
    {
        _adjustedImageProvider.DetectWorkflowIsTriggered = true;

        if (!_adjustedImageProvider.EnoughImagesAreCaptured)
        {
            OutputText.text = "Not enough images are stored, try again in a second.";
            return;
        }

        _adjustedImageProvider.PopulateBatchWithNewImages();

        // Create an array to store the results
        var elementCount = _adjustedImageProvider.NewAdjustedAbImageBatch.Count;
        var taskResults = new Task<WorkflowResultContainer>[elementCount];

        //Profiling total multi-threaded operation
        var totalMultiThreadedStart = Time.time;

        try
        {
            for (var i = 0; i < elementCount; i++)
            {
                var adjustedImage = _adjustedImageProvider.NewAdjustedAbImageBatch[i];
                taskResults[i] = RunWorkflowAsync(adjustedImage);
            }

            // Wait for all tasks to complete
            await Task.WhenAll(taskResults);

            var durationOfMultiThreadedOperation = Math.Round(Time.time - totalMultiThreadedStart, 2);

            //Map results to a List
            List<WorkflowResultContainer> results = new List<WorkflowResultContainer>();
            foreach (var taskResult in taskResults)
            {
                results.Add(taskResult.Result);
            }

            OutputText.text = GetMultiThreadedOutputMessage(results, durationOfMultiThreadedOperation);

            var mtmTranscriber = new MtmTranscriber();
            var mtmActions = mtmTranscriber.GetMTMActionsFromTags(results);

            TableManager.StoreMtmActions(mtmActions);

            //AnalysisWasCompleted.Invoke();
        }
        catch (Exception e)
        {
            OutputText.text = e.Message;
        }
    }

    private string GetMultiThreadedOutputMessage(List<WorkflowResultContainer> results, double durationOfMultiThreadedOperation)
    {
        var message = string.Empty;

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];

            if (!result.TagsWereFound)
            {
                message += $"Image {i}: {result.Message}\n";
                continue;
            }

            message += $"Image {i} Vision Duration: {result.RuntimeContainer.CustomVision}s\n";
        }

        message += $"Total Workflow Duration: {durationOfMultiThreadedOperation}s";

        return message;
    }

    private async Task<WorkflowResultContainer> RunWorkflowAsync(AdjustedImage adjustedImage)
    {
        var totalOperationStartTime = Time.time; // Profiling

        var customVisionResult = await DetectWithCustomVision(adjustedImage.InBytes);

        var customVisionDuration = Math.Round(Time.time - totalOperationStartTime, 2); // Profiling

        var tagManager = new TagsManager();

        var tags = tagManager.GetPredictedTagsFromResult(customVisionResult);

        if (tags.Count == 0)
        {
            var message = "No tags were found";
            return new WorkflowResultContainer(message, false);
        }

        var filteredTags = tagManager.GetTagsWithConfidenceHigherThan(MinimumConfidenceForObjectDetection, tags);

        if (!filteredTags.Any())
        {
            var message = "No tags with high confidence were found";
            return new WorkflowResultContainer(message, false);
        }

        var imageUtilities = new ImageUtilities();

        //var depthImage = new AdjustedImage(
        //    imageUtilities.ConvertToPNG(adjustedImage.DepthMap),
        //    "Depth" + adjustedImage.ImageTitle,
        //    adjustedImage.TimeImageWasCaptured
        //);

        tagManager.AugmentTagsWithImageTitle(filteredTags, adjustedImage.ImageTitle);
        tagManager.AugmentTagsWithForegroundIndices(filteredTags, adjustedImage.Texture, adjustedImage.DepthMap);

        var depthUtilities = new DepthUtilities();
        depthUtilities.AugmentTagsWithFilteredDepth(filteredTags);

        var imageWithBoundingBoxes = new AdjustedImage(
            imageUtilities.AugmentImageWithBoundingBoxesAndDepth(filteredTags, adjustedImage.Texture),
            adjustedImage.ImageTitle,
            adjustedImage.TimeImageWasCaptured
            );

        var blobStartTime = Time.time; // Profiling
        //BlobManager.StoreImage(adjustedImage);
        BlobManager.StoreImage(imageWithBoundingBoxes);
        //BlobManager.StoreImage(depthImage);
        var blobDuration = Math.Round(Time.time - blobStartTime, 2); // Profiling

        var tableStartTime = Time.time; // Profiling
        TableManager.StoreTags(filteredTags, adjustedImage.ImageTitle);
        var tableDuration = Math.Round(Time.time - tableStartTime); // Profiling

        //Profiling
        var totalOperationDuration = Math.Round(Time.time - totalOperationStartTime, 2); // Profiling
        var runtimes = new RuntimeContainer(customVisionDuration, blobDuration, tableDuration, totalOperationDuration);

        var workflowResultContainer = new WorkflowResultContainer(filteredTags, runtimes, true);

        TableManager.StoreRuntimes(workflowResultContainer, adjustedImage.ImageTitle);

        return workflowResultContainer;
    }

    [ItemCanBeNull]
    private async Task<string> DetectWithCustomVision(byte[] image)
    {
        //Connect to resource
        using var client = new HttpClient();
        using var content = new ByteArrayContent(image);

        client.DefaultRequestHeaders.Add("Prediction-Key", CustomVisionPredictionKey);

        var response = await client.PostAsync(CustomVisionPredictionURL, content);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(response.ReasonPhrase);
        }

        var result = await response.Content.ReadAsStringAsync();

        return result;
    }
}