#r "Microsoft.WindowsAzure.Storage"
#r "Microsoft.Cognitive.CustomVison"

using Microsoft.Cognitive.CustomVison.Prediction;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.Net.Http.Headers;
using System.Configuration;
using System.IO;
using System;

public async static Task Run(Stream inputBlob, string name, ILogger log)
{
    log.LogInformation($"Analyzing uploaded image {name} for custom vision content...");

    var byteData = await ToByteArrayAsync(inputBlob);
    var result = await AnalyzeImageAsync(byteData, log);
    var response = new CustomVison();

    foreach (var prediction in result.Predictions)
    {
        log.LogInformation("Tag Id:" + prediction.TagId);
        log.LogInformation("Tag Name:" + prediction.TagName);
        log.LogInformation("Tag Name:" + prediction.Probability.ToString());

        if (prediction.Probability > response.Probability)
        {
            response = new CustomVison
            {
                TagId = prediction.TagId,
                TagName = prediction.TagName,
                Probability = prediction.Probability
            };
        }
    }

    // Copy blob to the "rejected or accepted" container
    if ((response.Probability * 100) < 75)
        StoreBlobWithMetadata(inputBlob, "rejected", name, result, log);
    else
        StoreBlobWithMetadata(inputBlob, "accepted", name, result, log);
}

private async static Task<ImageAnalysisInfo> AnalyzeImageAsync(byte[] byteData, ILogger log)
{
    string endpoint = ConfigurationManager.AppSettings["VisionEndpoint"];
    string predictionKey =  ConfigurationManager.AppSettings["PredictionKey"];

    HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Add("Prediction-Key", predictionKey);

    using (var content = ByteArrayContent(byteData))
    {
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var response = await client.PostAsync(endpoint, content);

        return await response.Content.ReadAsAsync<ImageAnalysisInfo>();
    }
}

// Writes a blob to a specified container and stores metadata with it
private static void StoreBlobWithMetadata(Stream image, string containerName, string blobName, CustomVison info, ILogger log)
{
    log.LogInformation($"Writing blob and metadata to {containerName} container...");  

    try
    {
        var connection = ConfigurationManager.AppSettings["AzureWebJobsStorage"].ToString();
        var account = CloudStorageAccount.Parse(connection);

        log.LogInformation($"Connection: {connection}");

        CloudBlobClient client = account.CreateCloudBlobClient();
        CloudBlobContainer container = client.GetContainerReference(containerName); 
        BlobContainerPermissions permissions = new BlobContainerPermissions();
        CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

        if (blob != null)
        {
            using (StreamReader sr = new StreamReader(image))
            {
                // Write the blob metadata
                blob.Metadata["TagId"] = info.TagId; 
                blob.Metadata["TagName"] = info.TagName; 
                blob.Metadata["Probability"] = info.Probability.ToString();

                // Upload the blob
                blob.UploadFromStream(image);
            }
        }
    }
    catch (Exception ex)
    {
        log.LogError(ex.Message);
    }
}

// Converts a stream to a byte array
private async static Task<byte[]> ToByteArrayAsync(Stream stream)
{
    Int32 length = stream.Length > Int32.MaxValue ? Int32.MaxValue : Convert.ToInt32(stream.Length);
    byte[] buffer = new Byte[length];
    await stream.ReadAsync(buffer, 0, length);
    stream.Position = 0;
    return buffer;
}

public class ImageAnalysisInfo
{
    public string Id { get; set; }
    public string Project { get; set; }
    public string Iteration { get; set; }
    public DateTime Created { get; set; }
    public CustomVison[] Predictions { get; set; } 
}

public class CustomVison
{
    public string TagId { get; set; }
    public string TagName { get; set; }
    public double Probability { get; set; }
}