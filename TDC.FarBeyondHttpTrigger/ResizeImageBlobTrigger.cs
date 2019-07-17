using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace TDC.FarBeyondHttpTrigger
{
    public static class ResizeImageBlobTrigger
    {
        [FunctionName("ResizeImageBlobTrigger")]
        public static void Run(
            [BlobTrigger("uploadedimages/{name}.{extension}", Connection = "AzureBlobTrigger")] Stream inImage,
            [Blob("resizedimages/{name}-resized.png", FileAccess.Write, Connection = "AzureBlobTrigger")] Stream outImage,
            string name, 
            string extension,
            ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name}.{extension} \n Size: {inImage.Length} Bytes");

            using (Image<Rgba32> image = Image.Load(inImage))
            {
                image.Mutate(x => x
                    .Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.BoxPad,
                        Size = new Size(100, 100)
                    }));

                image.SaveAsPng(outImage);
            }
        }
    }
}
