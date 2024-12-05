using Amazon.S3;
using Amazon.XRay.Recorder.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add AWS SDK for S3
builder.Services.AddSingleton<IAmazonS3>(serviceProvider =>
{
    var clientConfig = new AmazonS3Config
    {
        RegionEndpoint = Amazon.RegionEndpoint.USWest2
    };
    return new AmazonS3Client(clientConfig);
});

// Add services to the container
builder.Services.AddControllers();

var app = builder.Build();

// Add X-Ray middleware
app.UseXRay("MyApp");

app.MapGet("/api/s3/list-buckets", async (IAmazonS3 s3Client) =>
{
    AWSXRayRecorder.Instance.BeginSubsegment("ListS3Buckets");

    try
    {
        // List the S3 buckets
        var response = await s3Client.ListBucketsAsync();

        var buckets = response.Buckets.Select(bucket => new
        {
            name = bucket.BucketName,
            creation_date = bucket.CreationDate
        }).ToList();

        // Add metadata to the subsegment
        AWSXRayRecorder.Instance.AddAnnotation("bucket_count", buckets.Count);

        return Results.Ok(buckets);
    }
    catch (Exception ex)
    {
        AWSXRayRecorder.Instance.AddException(ex);
        return Results.Problem(ex.Message);
    }
    finally
    {
        AWSXRayRecorder.Instance.EndSubsegment();
    }
});

app.Run();
