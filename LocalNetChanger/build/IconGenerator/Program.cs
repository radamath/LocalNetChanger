using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

if (args.Length < 2)
{
    Console.Error.WriteLine("Kullanim: IconGenerator <icon.png> <icon.ico>");
    return 1;
}

var pngPath = args[0];
var icoPath = args[1];

if (!File.Exists(pngPath))
{
    Console.Error.WriteLine($"Dosya bulunamadi: {pngPath}");
    return 1;
}

using var source = new Bitmap(pngPath);
var sizes = new[] { 256, 128, 64, 48, 32, 16 };
var images = sizes
    .Select(size =>
    {
        using var bitmap = RenderBitmap(source, size);
        return (size, Data: BitmapToIconImageData(bitmap));
    })
    .ToArray();

using var stream = File.Create(icoPath);
using var writer = new BinaryWriter(stream);

writer.Write((short)0);
writer.Write((short)1);
writer.Write((short)images.Length);

var offset = 6 + images.Length * 16;
foreach (var (size, data) in images)
{
    writer.Write((byte)(size >= 256 ? 0 : size));
    writer.Write((byte)(size >= 256 ? 0 : size));
    writer.Write((byte)0);
    writer.Write((byte)0);
    writer.Write((short)1);
    writer.Write((short)32);
    writer.Write(data.Length);
    writer.Write(offset);
    offset += data.Length;
}

foreach (var (_, data) in images)
    writer.Write(data);

return 0;

static Bitmap RenderBitmap(Bitmap source, int size)
{
    var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var graphics = Graphics.FromImage(bitmap);
    graphics.Clear(Color.Transparent);
    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
    graphics.SmoothingMode = SmoothingMode.HighQuality;
    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
    graphics.CompositingQuality = CompositingQuality.HighQuality;
    graphics.DrawImage(source, new Rectangle(0, 0, size, size));
    return bitmap;
}

static byte[] BitmapToIconImageData(Bitmap bitmap)
{
    var width = bitmap.Width;
    var height = bitmap.Height;
    var rect = new Rectangle(0, 0, width, height);
    var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

    try
    {
        var xorStride = width * 4;
        var xorSize = xorStride * height;
        var andStride = ((width + 31) / 32) * 4;
        var andSize = andStride * height;

        using var memory = new MemoryStream(40 + xorSize + andSize);
        using var writer = new BinaryWriter(memory);

        writer.Write(40);
        writer.Write(width);
        writer.Write(height * 2);
        writer.Write((short)1);
        writer.Write((short)32);
        writer.Write(0);
        writer.Write(xorSize + andSize);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        for (var y = height - 1; y >= 0; y--)
        {
            for (var x = 0; x < width; x++)
            {
                var color = Color.FromArgb(Marshal.ReadInt32(bmpData.Scan0 + y * bmpData.Stride + x * 4));
                writer.Write(color.B);
                writer.Write(color.G);
                writer.Write(color.R);
                writer.Write(color.A);
            }
        }

        writer.Write(new byte[andSize]);
        return memory.ToArray();
    }
    finally
    {
        bitmap.UnlockBits(bmpData);
    }
}
