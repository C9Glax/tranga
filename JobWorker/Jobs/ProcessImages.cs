using API.Schema.Jobs;
using Microsoft.VisualBasic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Binarization;

namespace JobWorker.Jobs;

public class ProcessImages(ProcessImagesJob data) : Job<ProcessImagesJob>(data)
{
    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal(ProcessImagesJob data)
    {
        string path = data.Path;
        string[] imagePaths = File.GetAttributes(path).HasFlag(FileAttribute.Directory)
            ? Directory.GetFiles(path)
            : [path];
        bool bwImages = data.Bw;
        int compression = data.Compression;
        if (!bwImages && compression == 100)
            return [];

        DateTime start = DateTime.Now;
        foreach (string imagePath in imagePaths)
        {
            using Image image = Image.Load(imagePath);
            if(bwImages) 
                image.Mutate(i => i.ApplyProcessor(new AdaptiveThresholdProcessor()));
            File.Delete(imagePath);
            image.SaveAsJpeg(imagePath, new JpegEncoder()
            {
                Quality = compression
            });
        }
        Log.Info($"Image processing took {DateTime.Now.Subtract(start):s\\.fff} B/W:{bwImages} Compression: {compression}");
        return [];
    }
}