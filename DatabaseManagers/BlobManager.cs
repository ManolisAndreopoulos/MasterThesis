using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks; 
using UnityEngine;


public class BlobManager : MonoBehaviour
{
    // Replace with your Azure Storage account name and access key or connection string
    private const string StorageAccountName = "msstorageresource";
    private const string StorageAccountKey = "IQQpV3U2AY6VJ21FhmkGAaGKyCQeuNN1sldwKAcwYGFbly+zbfgF3OMsBDg5RVKjnmYQoTtKvebe+AStAjdpfg==";

    private string defaultContainerName = "ab-images";
    private string trainingImagesContainerName = "training-images";

    private string _message = string.Empty;

    public async Task<string> StoreImage(AdjustedImage adjustedImage)
    {
        _message = adjustedImage.ImageTitle + " : " + await PutBlobAsync(adjustedImage, defaultContainerName);
        return _message;
    }

    public async Task<string> StoreImageForTraining(AdjustedImage adjustedImage)
    {
        _message = adjustedImage.ImageTitle + " : " + await PutBlobAsync(adjustedImage, trainingImagesContainerName);
        return _message;
    }

    private async Task<string> PutBlobAsync(AdjustedImage adjustedImage, string containerName)
    {
        var imageInBytes = adjustedImage.InBytes;

        var blobName = adjustedImage.ImageTitle;
        var blobUrl = $"https://{StorageAccountName}.blob.core.windows.net/{containerName}/{blobName}";


        // Resources
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, blobUrl);

        // Request headers
        httpRequestMessage.Headers.Add("x-ms-date", adjustedImage.TimeImageWasCaptured.ToString("R", CultureInfo.InvariantCulture));
        httpRequestMessage.Headers.Add("x-ms-version", "2017-04-17");
        httpRequestMessage.Headers.Add("x-ms-blob-type", "BlockBlob");

        // Content
        httpRequestMessage.Content = new ByteArrayContent(imageInBytes);
        httpRequestMessage.Content.Headers.ContentLength = imageInBytes.Length;
        httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png"); // Set Content-Type header (optional, but recommended)

        // If you need any additional headers, add them here before creating
        //   the authorization header. 

        // Add the authorization header.
        httpRequestMessage.Headers.Authorization = AzureStorageAuthenticationHelper.GetBlobAuthorizationHeader(StorageAccountName, StorageAccountKey, httpRequestMessage);

        try
        {
            // Send the request.
            using HttpResponseMessage httpResponseMessage = await new HttpClient().SendAsync(httpRequestMessage, CancellationToken.None);

            // Check if the request was successful
            httpResponseMessage.EnsureSuccessStatusCode();

            return "Uploaded successfully.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}

