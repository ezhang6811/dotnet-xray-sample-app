using Amazon.S3;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Trace all AWS SDK calls
AWSSDKHandler.RegisterXRayForAllServices();

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

app.MapGet("/aws-sdk-call", async (IAmazonS3 s3Client) =>
{
    return await ListBuckets(s3Client);
});

app.Run();

async Task<IResult> ListBuckets(IAmazonS3 s3Client)
{
    try
    {
        // List the S3 buckets
        var response = await s3Client.ListBucketsAsync();

        var buckets = response.Buckets.Select(bucket => new
        {
            name = bucket.BucketName,
            creation_date = bucket.CreationDate
        }).ToList();

        return Results.Ok(buckets);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
}
