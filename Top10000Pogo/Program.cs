using ImageMagick;
using System.Text.RegularExpressions;
using Xabe.FFmpeg;

const string baseDir = "C:\\Users\\kekpr\\Downloads\\PogoScraped";
const string imgDirName = "all_images";
const int imgNumMax = 10000;

string fileTypePattern = @"[^.]{3,5}$";
Regex regex = new Regex(fileTypePattern);

var imgSize = new MagickGeometry("480x360!");
imgSize.IgnoreAspectRatio = true;

var color = new MagickColor("#114080");

var cardSettings = new MagickReadSettings
{
    Font = "Calibri",
    TextGravity = Gravity.Center,
    BackgroundColor = color,
    FillColor = MagickColors.White,
    Width = 480,
    FontPointsize = 32,
    Height = 360,
};


string[] ignoreFolders = ["avatars", "emojis", "roles", imgDirName];
List<string> folders = Directory.GetDirectories(baseDir).ToList();
List<string> imagesAll = new();

string imagesDir = Directory.CreateDirectory(baseDir + "\\" + imgDirName).FullName;

async Task mainAsync()
{
    removeInvalidDirectories();
    collectImages();
    shuffleList();
    generateImages();
    await generateVideo();
}

await mainAsync();

void removeInvalidDirectories()
{
    for (int i = folders.Count() - 1; i >= 0; i--)
    {
        string folderName = folders[i].Substring(baseDir.Length + 1);
        if (!isValidDirectory(folderName))
        {
            folders.Remove(folders[i]);
            continue;
        }
        for(int dir = 0; dir < Directory.GetDirectories(folders[i]).Length; dir++)
        {
            folders.Add(Directory.GetDirectories(folders[i])[dir]);
        }

        folders.Remove(folders[i]);
    }
    Console.WriteLine("Directories removed");
}
bool isValidDirectory(string dir)
{
    if (dir != null && !ignoreFolders.Contains(dir))
    {
        return true;
    }
    return false;
}

void collectImages()
{
    for (int folder = 0; folder < folders.Count; folder++)
    {
        string dir = folders[folder];
        string[] images = Directory.GetFiles(dir);

        for (int img = 0; img < images.Length; img++)
        {
            if (Path.GetExtension(images[img]).Equals(".gif", StringComparison.OrdinalIgnoreCase)) 
            {
                continue;
            }
            if (imagesAll.Count >= imgNumMax)
            {
                return;
            }
            imagesAll.Add(images[img]);
        }
    }
    Console.WriteLine("Images collected");
}

void shuffleList()
{
    Random rng = new Random();

    for (int i = imagesAll.Count() - 1; i > 0; i--)
    {
        int swapIndex = rng.Next(i + 1);
        (imagesAll[i], imagesAll[swapIndex]) = (imagesAll[swapIndex], imagesAll[i]); 
    }
    Console.WriteLine("List shuffled");
}

void generateImages()
{
    if (imagesAll == null || imagesAll.Count == 0)
    {
        Console.Error.WriteLine("imagesAll null or 0");
        return;
    }

    int totalImages = imagesAll.Count;
    int outputIndex = 0;

    using (var introCard = new MagickImage($"caption:welcome to top {imgNumMax} pogo", cardSettings))
    {
        introCard.Format = MagickFormat.Png;
        introCard.Write(Path.Combine(imagesDir, $"img_{outputIndex:D5}.png"));
        outputIndex++;
    }

    for (int img = 0; img < totalImages; img++)
        {
        if (imagesAll[img] == null)
        {
            Console.Error.WriteLine($"Image {img} invalid");
            continue;
        }

        using (var card = new MagickImage($"caption:number {totalImages - img}", cardSettings))
        {
            card.Format = MagickFormat.Png;
            card.Write(Path.Combine(imagesDir, $"img_{outputIndex:D5}.png"));
            outputIndex++;
        }

        using (var image = new MagickImage(imagesAll[img]))
        {
            image.Format = MagickFormat.Png;
            image.Resize(imgSize);
            image.Write(Path.Combine(imagesDir, $"img_{outputIndex:D5}.png"));
            outputIndex++;
        }

        Console.WriteLine($"{outputIndex} | {img}");
    }

    using (var outroCard = new MagickImage("caption:thanks for watching", cardSettings))
    {
        outroCard.Format = MagickFormat.Png;
        outroCard.Write(Path.Combine(imagesDir, $"img_{outputIndex:D5}.png"));
    }
    Console.WriteLine("All images generated successfully.");
}


async Task generateVideo()
{
    Console.WriteLine("Generating video..");
    await FFmpeg.Conversions.New()
        .AddParameter($"-framerate 0.5 -i {imagesDir}\\img_%05d.png -stream_loop -1 -i C:\\Users\\kekpr\\Downloads\\audio.mp3")
        .AddParameter("-c:v libx264 -crf 40 -preset ultrafast -pix_fmt yuv420p -c:a aac -b:a 96k -shortest")
        .SetOutput("video.mp4")
        .Start();

    Console.WriteLine("video.mp4 done");

    //await FFmpeg.Conversions.New()
    //    .AddParameter("-i temp.mp4 -vf reverse")
    //    .SetOutput("test.mp4")
    //    .Start();
};