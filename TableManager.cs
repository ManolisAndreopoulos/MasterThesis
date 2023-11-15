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
    [Header("Response")]
    [SerializeField]
    private TextMeshPro response = null;


    private string storageAccountName = "msstorageresource";
    private string tableTagsName = "PredictedTags";
    private string tableRuntimesName = "OperationRuntimes";
    private string tableMtmActionsName = "MtmActions";

    private string storageAccountKey = "IQQpV3U2AY6VJ21FhmkGAaGKyCQeuNN1sldwKAcwYGFbly+zbfgF3OMsBDg5RVKjnmYQoTtKvebe+AStAjdpfg==";

    private string _message = string.Empty;

    public async void StoreMtmActions(List<MtmAction> mtmActions)
    {
        foreach (var mtmAction in mtmActions)
        {
            if (mtmAction is MtmActionHand hand)
            {
                await InsertMtmActionHandAsync(hand);
            }
            
        }
        
    }

    private async Task InsertMtmActionHandAsync(MtmActionHand mtmActionHand)
    {
        string tableUrl = $"https://{storageAccountName}.table.core.windows.net/{tableMtmActionsName}";


        var handColumn = mtmActionHand.Hand ?? "-";
        // Content
        string jsonContent = $"{{" +
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
            //return $"Error {ex.Message}";
        }
    }

    public async void StoreRuntimes(WorkflowResultContainer workflowResultContainer, string imageName)
    {
        await InsertRuntimesAsync(workflowResultContainer, imageName);
}

    private async Task InsertRuntimesAsync(WorkflowResultContainer workflowResultContainer, string imageName)
    {
        string tableUrl = $"https://{storageAccountName}.table.core.windows.net/{tableRuntimesName}";

        // Content
        string jsonContent = $"{{" +
                             $"\"CustomVision\":\"{workflowResultContainer.RuntimeContainer.CustomVision}\"," +
                             $"\"BlobStorage\":{workflowResultContainer.RuntimeContainer.BlobStorage}," +
                             $"\"TableStorage\":{workflowResultContainer.RuntimeContainer.TableStorage}," +
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
            //return $"Error {ex.Message}";
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
        string tableUrl = $"https://{storageAccountName}.table.core.windows.net/{tableTagsName}";

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
            //return $"Error {ex.Message}";
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
        httpRequestMessage.Headers.Authorization = AzureStorageAuthenticationHelper.GetTableAuthorizationHeader(storageAccountName, storageAccountKey, httpRequestMessage, date);
        return httpRequestMessage;
    }
}

