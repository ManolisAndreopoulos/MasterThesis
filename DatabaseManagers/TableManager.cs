using Microsoft.MixedReality.Toolkit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class TableManager : MonoBehaviour
{
    public TextMeshPro ErrorText = null;

    private const string User = "UserX";

    private const string StorageAccountName = "msstorageresource";
    private const string TableTagsName = "PredictedTags" + User;
    private const string TableRuntimesName = "Runtimes"+ User;
    private const string TableMtmHandActionsName = "MtmHandActions" + User;
    private const string TableMtmBodyActionsName = "MtmBodyActionsAllUsers";

    private string storageAccountKey = "IQQpV3U2AY6VJ21FhmkGAaGKyCQeuNN1sldwKAcwYGFbly+zbfgF3OMsBDg5RVKjnmYQoTtKvebe+AStAjdpfg==";

    private string _message = string.Empty;

    void Update()
    {
        ErrorText.text = _message;
    }

    public async void StoreMtmHandActions(List<MtmActionHand> mtmActions)
    {
        foreach (var mtmAction in mtmActions)
        {
            await InsertMtmActionHandAsync(mtmAction);
        }
    }

    public async void StoreMtmAction(MtmAction mtmAction)
    {
        if (mtmAction is MtmActionHand hand)
        {
            await InsertMtmActionHandAsync(hand);
        }
        else if (mtmAction is StepAction step)
        {
            await InsertStep(step);
        }
        else if (mtmAction is BendAndAriseAction bend)
        {
            await InsertBendAndArise(bend);
        }
    }

    private async Task InsertStep(StepAction action)
    {
        var tableUrl = $"https://{StorageAccountName}.table.core.windows.net/{TableMtmBodyActionsName}";

        // Content
        var jsonContent = $"{{" +
                          $"\"TotalCount\":{action.TotalStepCount}," +
                          $"\"LeftCount\":{action.LeftSideStepCount}," +
                          $"\"RightCount\":{action.RightSideStepCount}," +
                          $"\"UnknownCount\":{action.RightSideStepCount}," +
                          $"\"TMUs\":{action.TMU}," +
                          $"\"PartitionKey\":\"{User}\"," +
                          $"\"RowKey\":\"Steps\"" +
                          $"}}";

        var httpRequestMessage = SetUpHttpRequestMessage(tableUrl, jsonContent);

        try
        {
            // Send the request.
            using HttpResponseMessage httpResponseMessage = await new HttpClient().SendAsync(httpRequestMessage, CancellationToken.None);

            // Check if the request was successful
            httpResponseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _message = $"Error: {ex.Message}";
        }
    }

    private async Task InsertBendAndArise(BendAndAriseAction action)
    {
        var tableUrl = $"https://{StorageAccountName}.table.core.windows.net/{TableMtmBodyActionsName}";

        // Content
        var jsonContent = $"{{" +
                          $"\"TotalCount\":\"{action.Total}\"," +
                          $"\"TMUs\":{action.TMU}," +
                          $"\"PartitionKey\":\"{User}\"," +
                          $"\"RowKey\":\"BendAndArise\"" +
                          $"}}";

        var httpRequestMessage = SetUpHttpRequestMessage(tableUrl, jsonContent);

        try
        {
            // Send the request.
            using HttpResponseMessage httpResponseMessage = await new HttpClient().SendAsync(httpRequestMessage, CancellationToken.None);

            // Check if the request was successful
            httpResponseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _message = $"Error: {ex.Message}";
        }
    }

    private async Task InsertMtmActionHandAsync(MtmActionHand mtmActionHand)
    {
        var tableUrl = $"https://{StorageAccountName}.table.core.windows.net/{TableMtmHandActionsName}";


        var handColumn = mtmActionHand.Hand ?? "-";
        // Content
        var jsonContent = $"{{" +
                          $"\"Action\":\"{mtmActionHand.Name}\"," +
                          $"\"Hand\":\"{handColumn}\"," +
                          $"\"TMU\":{mtmActionHand.TMU}," +
                          $"\"Depth\":{mtmActionHand.Depth}," +
                          $"\"WorldX\":{(double)mtmActionHand.WorldPosition.x}," +
                          $"\"WorldY\":{(double)mtmActionHand.WorldPosition.y}," +
                          $"\"WorldZ\":{(double)mtmActionHand.WorldPosition.z}," +
                          $"\"PartitionKey\":\"Transcription\"," +
                          $"\"RowKey\":\"{mtmActionHand.ImageTitle}\"" +
                          $"}}";

        var httpRequestMessage = SetUpHttpRequestMessage(tableUrl, jsonContent);

        try
        {
            // Send the request.
            using HttpResponseMessage httpResponseMessage = await new HttpClient().SendAsync(httpRequestMessage, CancellationToken.None);

            // Check if the request was successful
            httpResponseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _message = $"Error: {ex.Message}";
        }
    }

    public async void StoreRuntimes(WorkflowResultContainer workflowResultContainer, string imageName)
    {
        await InsertRuntimesAsync(workflowResultContainer, imageName);
}

    private async Task InsertRuntimesAsync(WorkflowResultContainer workflowResultContainer, string imageName)
    {
        var tableUrl = $"https://{StorageAccountName}.table.core.windows.net/{TableRuntimesName}";

        // Content
        var jsonContent = $"{{" +
                          $"\"CustomVision\":\"{workflowResultContainer.RuntimeContainer.CustomVision}\"," +
                          $"\"Total\":{workflowResultContainer.RuntimeContainer.Total}," +
                          $"\"PartitionKey\":\"Detection\"," +
                          $"\"RowKey\":\"{imageName}\"" +
                          $"}}";

        var httpRequestMessage = SetUpHttpRequestMessage(tableUrl, jsonContent);

        try
        {
            // Send the request.
            using HttpResponseMessage httpResponseMessage = await new HttpClient().SendAsync(httpRequestMessage, CancellationToken.None);

            // Check if the request was successful
            httpResponseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _message = $"Error: {ex.Message}";
        }
    }

    public async void StoreTags(List<Tag> tags)
    {
        for (var i = 0; i < tags.Count; i++)
        {
            await InsertTagAsync(tags[i], i);
        }
    }

    private async Task InsertTagAsync(Tag tag, int tagIndex)
    {
        string tableUrl = $"https://{StorageAccountName}.table.core.windows.net/{TableTagsName}";

        // Content
        string jsonContent = $"{{" +
                             $"\"Name\":\"{tag.Name.Get()}\"," +
                             $"\"Confidence\":{tag.Probability}," +
                             $"\"Left\":{tag.BoundingBox.Left}," +
                             $"\"Right\":{tag.BoundingBox.Right}," +
                             $"\"Top\":{tag.BoundingBox.Top}," +
                             $"\"Bottom\":{tag.BoundingBox.Bottom}," +
                             $"\"Depth\":{tag.Depth}," +
                             $"\"PartitionKey\":\"{tag.ImageTitle}\"," +
                             $"\"RowKey\":\"{tagIndex}\"" +
                             $"}}";

        var httpRequestMessage = SetUpHttpRequestMessage(tableUrl, jsonContent);

        try
        {
            // Send the request.
            using HttpResponseMessage httpResponseMessage = await new HttpClient().SendAsync(httpRequestMessage, CancellationToken.None);

            // Check if the request was successful
            httpResponseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _message = $"Error: {ex.Message}";
        }
    }

    private HttpRequestMessage SetUpHttpRequestMessage(string tableUrl, string jsonContent)
    {
        // Resources
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, tableUrl);

        // Date
        var date = DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture);

        // Request headers
        httpRequestMessage.Headers.Add("x-ms-date", date);
        httpRequestMessage.Headers.Add("x-ms-version", "2017-04-17");
        httpRequestMessage.Headers.Add("DataServiceVersion", "3.0;NetFx");
        httpRequestMessage.Headers.Add("Accept", "application/json;odata=nometadata");

        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        httpRequestMessage.Content = content;
        httpRequestMessage.Content.Headers.ContentLength = Encoding.UTF8.GetByteCount(jsonContent);
        httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json"); // Set Content-Type header (optional, but recommended)

        // Add the authorization header.
        httpRequestMessage.Headers.Authorization = AzureStorageAuthenticationHelper.GetTableAuthorizationHeader(StorageAccountName, storageAccountKey, httpRequestMessage, date);
        return httpRequestMessage;
    }
}

