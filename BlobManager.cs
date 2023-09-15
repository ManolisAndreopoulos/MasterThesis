using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;


public class BlobManager : MonoBehaviour
{
    [Header("Azure Storage")]
    [SerializeField]
    string containerName = default; // Replace with your container name where you want to upload the blob
    
    [Header("Image Resource")]
    [SerializeField]
    private ProcessedABImage processedAbImage = null;

    [Header("Response")] 
    [SerializeField] 
    private TextMeshPro response = null;

    // Replace with your Azure Storage account name and access key or connection string
    private const string StorageAccountName = "msstorageresource";
    private const string StorageAccountKey = "IQQpV3U2AY6VJ21FhmkGAaGKyCQeuNN1sldwKAcwYGFbly+zbfgF3OMsBDg5RVKjnmYQoTtKvebe+AStAjdpfg==";
    private string _blobName; // Replace with your blob name (the name you want to give to the uploaded PNG AbRawImage)
    
    private string _message = string.Empty;

    public async void StoreImageAfterComputerVision(string imageName)
    {
        processedAbImage.StoreOnly = false;
        _blobName = imageName;
        _message = _blobName + " : " + await PutBlobAsync();
        //UpdateMessage(!processedAbImage.StoreOnly);
    }

    public async void StoreImage()
    {
        processedAbImage.StoreOnly = true;
        _blobName = "AbImage" + DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + ".png";
        _message = _blobName + " : " + await PutBlobAsync();
        UpdateMessage(!processedAbImage.StoreOnly);
    }

    private void UpdateMessage(bool toAppend)
    {
        if (toAppend)
        {
            response.text += _message;
        }
        else
        {
            response.text = _message;
        }
    }

    private async Task<string> PutBlobAsync()
    {
       
        string blobUrl = $"https://{StorageAccountName}.blob.core.windows.net/{containerName}/{_blobName}";

        // Transforming from Alpha8 to RGB
        //var conversionStart = Time.time;
        var image = processedAbImage.AnalyzedImageInBytes;
        //var conversionDurationMessage = $"\nImage Conversion took: {Time.time - conversionStart} s\n";

        // Resources
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, blobUrl);

        // Request headers
        httpRequestMessage.Headers.Add("x-ms-date", DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture));
        httpRequestMessage.Headers.Add("x-ms-version", "2017-04-17");
        httpRequestMessage.Headers.Add("x-ms-blob-type", "BlockBlob");

        // Content
        httpRequestMessage.Content = new ByteArrayContent(image);
        httpRequestMessage.Content.Headers.ContentLength = image.Length;
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

