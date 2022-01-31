using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageProcessor
{
    internal static class Crapifier
    {
        internal static async Task<WriteableBitmap> GetProcessedImageAsync(Image source)
        {
            var translation = ZoomBorder.GetTranslateTransform(source);
            var scale = 4.0 / ZoomBorder.GetScaleTransform(source).ScaleX;
            var topEmptySpace = source.ActualHeight * scale < 300 ? (int)(source.ActualHeight * scale / 2 - 150) : 0;
            return await GetProcessedImageAsync((BitmapImage)source.Source, -(int)translation.X / 4, -(int)translation.Y / 4 + topEmptySpace, scale);
        }
        internal static WriteableBitmap GetProcessedImage(Image source)
        {
            //TODO:  Determine Part of original to use
            var translation = ZoomBorder.GetTranslateTransform(source);
            var scale = 4.0 / ZoomBorder.GetScaleTransform(source).ScaleX;
            var topEmptySpace = source.ActualHeight * scale < 300 ? (int)(source.ActualHeight * scale / 2 - 150) : 0;
            var leftEmptySpace = source.ActualWidth * scale < 300 ? (int)(source.ActualWidth * scale / 2 - 200) : 0;
            return GetProcessedImage((BitmapImage)source.Source, -(int)(translation.X /3 ) + leftEmptySpace, -(int)translation.Y / 4 + topEmptySpace, scale);
        }
        internal static async Task<WriteableBitmap> GetProcessedImageAsync(BitmapImage sourceImage, int x = 0, int y = 0, double scale = 1) {
            WriteableBitmap processedImage;
            var processTask = Task.Factory.StartNew(async () => processedImage = GetProcessedImage(sourceImage, x, y, scale));
            return await processTask.Result;
        }
        internal static WriteableBitmap GetProcessedImage(BitmapImage sourceImage, int x = 0, int y = 0, double scale = 1)
        {
            Debug.Assert(sourceImage != null, "Source image must be a BitmapImage");
            //var processed = new WriteableBitmap(100, 75, 4, 4, PixelFormats.Bgr24, null);
            //var sourceImage = (WriteableBitmap)source.Source;
            var sourceWidth = (int)sourceImage.Width;
            var sourceHeight = (int)sourceImage.Height;
            var sourceBpp = (sourceImage.Format.BitsPerPixel + 7) / 8;
            var sourceStride = sourceBpp * sourceWidth;
            var pixelBuffer = new Byte[sourceStride * sourceHeight];
            sourceImage.CopyPixels(pixelBuffer, sourceStride, 0);
            var destInfo = new BitmapInfo { Width = 100, Height = 75, PixelFormat = PixelFormats.Bgr24, Dpi = 24 };

            var colors = new Color[destInfo.Width, destInfo.Height];
            var averageDivisor = Math.Pow(Math.Max(1, Math.Floor(scale)), 2);
            //Loop through original to get average colors
            for (int i = 0; i < destInfo.Width; i++)
                for (int j = 0; j < destInfo.Height; j++)
                {
                    float r = 0, g = 0, b = 0;
                    var minX = (int)Math.Floor(x + i * scale * .99);
                    var minY = (int)Math.Floor(y + j * scale * 74 / 75);
                    if (minX < 0 || minY < 0 || minX + Math.Max(scale, 1) > sourceWidth || minY + Math.Max(scale, 1) > sourceHeight)
                    {
                        colors[i, j] = Colors.Black;
                        continue;
                    }
                    for (int k = minX; k < minX + Math.Max(scale, 1); k++)
                        for (int l = minY; l < minY + Math.Max(scale, 1); l++)
                        {
                            var pixel = GetPixel(pixelBuffer, sourceStride, k, l, sourceBpp);
                            r += pixel.R;
                            g += pixel.G;
                            b += pixel.B;
                        }
                    var rAvg = r / averageDivisor;
                    var gAvg = g / averageDivisor;
                    var bAvg = b / averageDivisor;
                    Debug.Assert(rAvg < 256 && gAvg < 256 && bAvg < 256, "RGB values are less than 256");
                    colors[i, j] = (new Color() { R = (byte)rAvg, G = (byte)gAvg, B = (byte)bAvg }).To6bitColor();
                }

            //Format into rawData
            var rawData = new byte[destInfo.RawStride * destInfo.Height];
            for (int i = 0; i < destInfo.Width; i++)
                for (int j = 0; j < destInfo.Height; j++)
                {
                    var baseIndex = j * destInfo.RawStride + i * destInfo.PixelFormat.BitsPerPixel / 8;
                    rawData[baseIndex] = colors[i, j].B;
                    rawData[baseIndex + 1] = colors[i, j].G;
                    rawData[baseIndex + 2] = colors[i, j].R;
                }
            var processed = BitmapSource.Create(destInfo.Width, destInfo.Height, destInfo.Dpi, destInfo.Dpi, destInfo.PixelFormat, null, rawData, destInfo.RawStride);
            return new WriteableBitmap(processed);
        }

        internal static Color To6bitColor(this Color orig)
        {
            byte r = orig.R;
            byte g = orig.G;
            byte b = orig.B;
            //TODO:  Translate to usable colors
            r = (byte)(Math.Floor(r / 64d) * 64);
            g = (byte)(Math.Floor(g / 64d) * 64);
            b = (byte)(Math.Floor(b / 64d) * 64);
            return new Color() { R = (byte)r, G = (byte)g, B = (Byte)b };
        }

        internal static RGBAPixel GetPixel(byte[] buffer, int stride, int x, int y, int bpp = 4)
        {
            var pixel = new RGBAPixel
            {
                B = buffer[stride * y + x * bpp],
                G = buffer[stride * y + x * bpp + 1],
                R = buffer[stride * y + x * bpp + 2],
                A = bpp > 3 ? buffer[stride * y + x * bpp + 3] : (byte)0xff
            };
            return pixel;
        }

        static Crapifier()
        {
            InitTestImage(_createdInfo);
        }

        private static void InitTestImage(BitmapInfo info)
        {
            byte[] rawImage = new byte[info.RawStride * info.Height];
            for (var i = 0; i < info.Width; i++)
                for (var j = 0; j < info.Height; j++)
                {
                    var baseIndex = j * info.RawStride + i * info.PixelFormat.BitsPerPixel / 8;
                    rawImage[baseIndex] = (byte)(i * 255 / 20);         //B
                    rawImage[baseIndex + 1] = 0;                        //G
                    rawImage[baseIndex + 2] = (byte)(j * 255 / 15);     //R
                }
            var bmpSource = BitmapSource.Create(info.Width, info.Height, info.Dpi, info.Dpi, info.PixelFormat, null, rawImage, info.RawStride);
            TestImage = new WriteableBitmap(bmpSource);
        }
        private static void InitTestImage2()
        {
            TestImage = new WriteableBitmap(4, 4, 4, 4, PixelFormats.Rgb24, BitmapPalettes.Halftone64);
            try
            {
                TestImage.Lock();
                for (var i = 0; i < 4; i++)
                    for (var j = 0; j < 4; j++)
                    {
                        var rect = new Int32Rect(i, j, 1, 1);
                        var colorValue = (byte)(i * 16 + j * 2);
                        int[] colorData = { colorValue, (colorValue + 1), (colorValue + 2), 255 };
                        //TestImage.WritePixels(rect, colorData, 4, 0);
                        unsafe
                        {
                            IntPtr pBackBuffer = TestImage.BackBuffer;
                            pBackBuffer += i * TestImage.BackBufferStride;
                            pBackBuffer += j * 3;
                            //int color = colorData[0] << 48;
                            //color |= colorData[1] << 32;
                            //color |= colorData[2] << 16;
                            *((int*)pBackBuffer) = colorData[0] << 16;
                            *((int*)pBackBuffer + 1) = colorData[1] << 8;
                            *((int*)pBackBuffer + 2) = colorData[3];
                        }
                    }
                TestImage.AddDirtyRect(new Int32Rect(0, 0, 4, 4));
            }
            finally
            {
                TestImage.Unlock();
            }
        }
        // Define parameters used to create the test BitmapSource.
        static BitmapInfo _createdInfo = new BitmapInfo { Width = 20, Height = 15, Dpi = 16 };

        internal static WriteableBitmap TestImage { get; private set; }

        internal static bool TestRead()
        {
            var pixels = new byte[_createdInfo.RawStride * _createdInfo.Height];
            TestImage.CopyPixels(pixels, _createdInfo.RawStride, 0);
            var bpp = (_createdInfo.PixelFormat.BitsPerPixel + 7) / 8;
            var passed = TestImage.PixelWidth == _createdInfo.Width && TestImage.PixelHeight == _createdInfo.Height;
            for (var i = 0; passed && i < _createdInfo.Width; i++)
                for (var j = 0; passed && j < _createdInfo.Height; j++)
                {
                    var baseIndex = j * _createdInfo.RawStride + i * bpp;
                    passed = passed && (pixels[baseIndex] == (byte)(i * 255 / 20));
                    passed = passed && (pixels[baseIndex + 1] == 0);
                    passed = passed && (pixels[baseIndex + 2] == (byte)(j * 255 / 15));
                }
            return passed;
        }

        public static string GetPixelString(WriteableBitmap bmp, BitmapInfo info)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Created bitmap is {bmp.PixelWidth} X {bmp.PixelHeight}");
            sb.AppendLine("   Column:  " + string.Join("   ",
                Enumerable.Range(0, info.Width).Select(i => i.ToString("D2"))));

            //Get raw pixel data:
            var rawData = new byte[info.RawStride * info.Height];
            bmp.CopyPixels(rawData, info.RawStride, 0);
            var bpp = (info.PixelFormat.BitsPerPixel + 7) / 8;
            Debug.WriteLine("Max index should be " + ((info.Height - 1) * info.RawStride + (info.Width) * 3 + 2) + " bytes per pixel = " + bpp);
            for (var i = 0; i < info.Height; i++)
            {
                //Grab row data
                Debug.WriteLine("Row " + i + "Max index = " + Enumerable.Range(0, info.Width - 1).Last());
                var baseIndicies = Enumerable.Range(0, info.Width).Select(index =>
                    i * info.RawStride + index * bpp).ToArray();
                var rowRData = baseIndicies.Select(ii => rawData[ii + 2]);
                var rowGData = baseIndicies.Select(ii => rawData[ii + 1]);
                var rowBData = baseIndicies.Select(ii => rawData[ii]);
                sb.AppendLine();
                sb.AppendLine("Row " + i.ToString("D2") + ": R: " + string.Join("  ", rowRData.Select(val => ((int)val).ToString("D3"))));
                sb.AppendLine("        G: " + string.Join("  ", rowGData.Select(val => ((int)val).ToString("D3"))));
                sb.AppendLine("        B: " + string.Join("  ", rowBData.Select(val => ((int)val).ToString("D3"))));
            }
            return sb.ToString();
        }
    }
    public struct RGBAPixel
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;
    }


    public class BitmapInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int RawStride => (Width * PixelFormat.BitsPerPixel + 7) / 8;
        public int Dpi { get; set; } = 96;
        public PixelFormat PixelFormat { get; set; } = PixelFormats.Bgr24;
    }
}
