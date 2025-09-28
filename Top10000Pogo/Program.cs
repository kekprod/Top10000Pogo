using ImageMagick;
using System.Text.RegularExpressions;
using Xabe.FFmpeg;

const string baseDir = "C:\\Users\\kekpr\\Downloads\\PogoScraped";
const string imgDirName = "all_images";

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
    FontPointsize = 30,
    Height = 360,
};


string[] ignoreFolders = ["avatars", "emojis", "roles", imgDirName];
List<string> folders = Directory.GetDirectories(baseDir).ToList();
List<string> imagesAll = new();

int imgNumber = 0;

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
        folders.Add(Directory.GetDirectories(folders[i])[0]);
        folders.Remove(folders[i]);
    }
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
            if (regex.Match(images[img]).Value.Contains("gif")) {
                continue;
            }
            imagesAll.Add(images[img]);
        }
    }
}

void shuffleList()
{
    Random rng = new Random();

    for (int i = imagesAll.Count() - 1; i > 0; i--)
    {
        int swapIndex = rng.Next(i + 1);
        (imagesAll[i], imagesAll[swapIndex]) = (imagesAll[swapIndex], imagesAll[i]); 
    }
}

void generateImages()
{
    if (imagesAll == null || imagesAll.Count() == 0)
    {
        Console.Error.WriteLine("imagesAll null or 0");
        return;
    }
    using var outroCard = new MagickImage("caption:thanks for watching", cardSettings);
    outroCard.Format = MagickFormat.Png;
    outroCard.Write(imagesDir + $"\\img_{imgNumber:D5}.png");
    imgNumber++;

    for (int img = 0; img < imagesAll.Count(); img++) // get all images
    {
        string name = $"img_{imgNumber:D5}";

        using var image = new MagickImage(imagesAll[img]);
        image.Format = MagickFormat.Png;
        image.Resize(imgSize);
        image.Write(imagesDir + $"\\{name}.png");
        imgNumber++;

        using var card = new MagickImage($"caption:number {(imgNumber + 1)/2}", cardSettings);
        card.Format = MagickFormat.Png;
        card.Write(imagesDir + $"\\img_{imgNumber:D5}.png");
        imgNumber++;

        Console.WriteLine($"{imgNumber} | {img}");
    }
    using var introCard = new MagickImage("caption:welcome to top 10000 pogo", cardSettings);
    introCard.Format = MagickFormat.Png;
    introCard.Write(imagesDir + $"\\img_{imgNumber:D5}.png");
}

async Task generateVideo()
{
    Console.WriteLine("Generating video..");
    await FFmpeg.Conversions.New()
        .AddParameter($"-framerate 0.5 -i {imagesDir}\\img_%05d.png -i C:\\Users\\kekpr\\Downloads\\audio.mp3")
        .AddParameter("-c:v libx264 -crf 40 -preset ultrafast -pix_fmt yuv420p -c:a aac -b:a 96k -shortest")
        .SetOutput("temp.mp4")
        .Start();

    await FFmpeg.Conversions.New()
        .AddParameter("-i temp.mp4 -vf reverse")
        .SetOutput("video.mp4")
        .Start();
};