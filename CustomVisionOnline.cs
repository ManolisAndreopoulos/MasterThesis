using System.Threading.Tasks;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net.Http;
using TMPro;
using System.Linq;
using JetBrains.Annotations;
using System.Diagnostics;

public class CustomVisionOnline : MonoBehaviour
{
    public TextMeshPro OutputText;
    public GetPutDetector MtmTranscriber = null;

    [SerializeField] private AbImageSamplerOnline _abImageSamplerOnline;
    [SerializeField] private float MinimumConfidenceForObjectDetection;

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

    private bool _startImageDetection;
    private bool _previousDetectFinished;

    //For automatically invoking the detection 
    private Stopwatch _stopwatch;
    private const long DetectTimeThresholdInMilliseconds = 1500;


    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (_startImageDetection && _previousDetectFinished)
        {
            _previousDetectFinished = false;
            Detect();
        }
    }

    public void StartImageDetection()
    {
        _startImageDetection = true;
        _previousDetectFinished = true;
    }

    public void StopImageDetection()
    {
        _startImageDetection = false;
        _previousDetectFinished = false;
    }

    public class TemporaryDataContainer
    {
        public string OperationMessage { get; set; }
        public bool Success { get; set; }
        public List<WorkflowResultContainer> Results { get; set; }
    }

    private async void Detect()
    {
        try
        {
            _abImageSamplerOnline.DetectWorkflowIsTriggered = true;
            var dataContainer = await Task.Run(DetectInternal);

            OutputText.text = dataContainer.OperationMessage;

            //todo: Add the following line after debugging
            //if (!dataContainer.Success || !dataContainer.Results.Any(r => r.TagsWereFound))
            //{
            //    return;
            //}

            // Create and store images with BBoxes
            var imageUtilities = new ImageUtilities();

            foreach (var result in dataContainer.Results)
            {
                //var depthImageContainer = new ImageContainer(
                //    imageUtilities.ConvertDepthMapToPNG(imageContainer.DepthMap),
                //    "Depth" + imageContainer.ImageTitle,
                //    imageContainer.TimeImageWasCaptured
                //);
                var filteredTags = result.Tags;

                if (result.ImageContainer == null) continue;

                var adjustedImage = result.ImageContainer;
                var imageWithBoundingBoxes = new ImageContainer(
                    imageUtilities.AugmentImageWithBoundingBoxesAndDepth(filteredTags, adjustedImage),
                    adjustedImage.ImageTitle,
                    adjustedImage.TimeImageWasCaptured);

                //BlobManager.StoreImage(imageContainer);
                BlobManager.StoreImage(imageWithBoundingBoxes);
                //BlobManager.StoreImage(depthImageContainer);
            }

            //MTM Get/Put transcription on main thread
            if (dataContainer.Success)
            {
                var mtmActions = MtmTranscriber.GetMTMActionsFromTags(dataContainer.Results);
                if (mtmActions.Count > 0)
                {
                    TableManager.StoreMtmHandActions(mtmActions);
                }
            }
        }
        catch (Exception e)
        {
            OutputText.text = e.Message;
        }
        finally
        {
            _previousDetectFinished = true; //To trigger the next invocation
        }
    }

    private async Task<TemporaryDataContainer> DetectInternal()
    {
        string operationMessage;
        TemporaryDataContainer dataContainer;
        List<WorkflowResultContainer> results = new List<WorkflowResultContainer>();

        if (!_abImageSamplerOnline.EnoughImagesAreCaptured)
        {
            dataContainer = new TemporaryDataContainer()
            {
                OperationMessage = "Not enough images are stored, try again in a second.",
                Success = false,
                Results = new List<WorkflowResultContainer>()
            };
            return dataContainer;
        }

        _abImageSamplerOnline.PopulateBatchWithNewImages();

        // Create an array to store the results
        var elementCount = _abImageSamplerOnline.NewAdjustedAbImageBatch.Count;
        var taskResults = new Task<WorkflowResultContainer>[elementCount];

        //Profiling total multi-threaded operation
        var totalMultiThreadedStopwatch = Stopwatch.StartNew();

        try
        {
            for (var i = 0; i < elementCount; i++)
            {
                var adjustedImage = _abImageSamplerOnline.NewAdjustedAbImageBatch[i];
                taskResults[i] = RunWorkflowAsync(adjustedImage);
                //await Task.Delay(100);
            }

            // Wait for all tasks to complete
            await Task.WhenAll(taskResults);

            totalMultiThreadedStopwatch.Stop();

            var durationOfMultiThreadedOperationInSeconds = Math.Round(totalMultiThreadedStopwatch.ElapsedMilliseconds / 1000.0, 2);

            //Map results to a List
            foreach (var taskResult in taskResults)
            {
                results.Add(taskResult.Result);
            }

            operationMessage = GetMultiThreadedOutputMessage(results, durationOfMultiThreadedOperationInSeconds);

            dataContainer = new TemporaryDataContainer()
            {
                OperationMessage = operationMessage,
                Success = true,
                Results = results
            };
        }
        catch (Exception e)
        {
            operationMessage = e.Message;
            dataContainer = new TemporaryDataContainer()
            {
                OperationMessage = operationMessage,
                Success = false,
                Results = new List<WorkflowResultContainer>()
            };
        }

        return dataContainer;
    }

    private string GetMultiThreadedOutputMessage(List<WorkflowResultContainer> results, double durationOfMultiThreadedOperationInSeconds)
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

        message += $"Total Workflow Duration: {durationOfMultiThreadedOperationInSeconds}s\n\n";

        return message;
    }

    private async Task<WorkflowResultContainer> RunWorkflowAsync(ImageContainer imageContainer)
    {
        var totalOperationStopwatch = Stopwatch.StartNew(); // Profiling
        var customVisionStopwatch = Stopwatch.StartNew(); // Profiling

        var customVisionResult = await DetectWithCustomVision(imageContainer.InBytes);

        customVisionStopwatch.Stop();
        var customVisionDuration = Math.Round(customVisionStopwatch.ElapsedMilliseconds / 1000.0, 2); // Profiling

        var tagManager = new TagsManager();
        var depthUtilities = new DepthUtilities();

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

        tagManager.AugmentTagsWithImageTitle(filteredTags, imageContainer.ImageTitle);
        tagManager.AugmentTagsWithForegroundIndices(filteredTags, imageContainer.Pixels, imageContainer.DepthMap);

        depthUtilities.AugmentTagsWithFilteredDepth(filteredTags);

        // Store tags in table
        TableManager.StoreTags(filteredTags);

        //Profiling
        totalOperationStopwatch.Stop();
        var totalOperationDuration = Math.Round(totalOperationStopwatch.ElapsedMilliseconds / 1000.0, 2); // Profiling

        var runtimes = new RuntimeContainer(customVisionDuration, totalOperationDuration); //blob and table storing durations are zero because they are async and we don't await for their execution

        var workflowResultContainer = new WorkflowResultContainer(filteredTags, runtimes, true, imageContainer);

        TableManager.StoreRuntimes(workflowResultContainer, imageContainer.ImageTitle);

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

    public async void StoreImage()
    {
        var image = _abImageSamplerOnline.GetCurrentAdjustedABImage();
        var operationMessage = await BlobManager.StoreImageForTraining(image);
        OutputText.text = operationMessage;
    }

    #region Stopwatch Methods

    private bool CheckTimerHasPassed()
    {
        if (_stopwatch == null) return true;

        if (_stopwatch.ElapsedMilliseconds < DetectTimeThresholdInMilliseconds)
        {
            return false;
        }

        _stopwatch = null;
        return true;
    }

    private void ResetTimer()
    {
        _stopwatch = Stopwatch.StartNew();
    }

    #endregion

    /*
    private readonly List<string> _wordsIndicateGrasping = new List<string>()
    {
        "Holding",
        "holding",
        "hold",
        "holds",
    };

    public async void Analyze()
    {
        var prediction = await AnalyzeWithComputerVision(_abImageSamplerOnline.NewAdjustedAbImageBatch.First().InBytes);
        OutputText.text = prediction.Result;

        var caption = prediction.Result;

        //Iterate through the list of buzzwords
        if (_wordsIndicateGrasping.Any(word => caption.Contains(word)))
        {
            BlobManager.StoreImage(_abImageSamplerOnline.NewAdjustedAbImageBatch.First());
        }
    }

    private async Task<GeneralPrediction> AnalyzeWithComputerVision(byte[] image)
    {
        if (_abImageSamplerOnline == null)
        {
            var message = $"Error: Assign variable {_abImageSamplerOnline.name} in Unity.";
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

    */

}