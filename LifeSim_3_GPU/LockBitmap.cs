using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LifeSim_3_GPU
{
    public struct RECT
    {
        public int X;
        public int Y;
        public int W;
        public int H;

        public RECT(int x, int y, int w, int h)
        {
            X = x; Y = y; W = w; H = h;
        }

        public override string ToString()
        {
            return $"X: {X} Y: {Y} W: {W} H: {H}";
        }
    }

    public struct MyColor
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public MyColor(byte r, byte g, byte b)
        {
            R = r; G = g; B = b;
        }
        public MyColor(Color color)
        {
            R = color.R; G = color.G; B = color.B;
        }

        public Color ToColor() => Color.FromArgb(0, R, G, B);
        public override string ToString() => $"R: {R} G: {G} B: {B}";

        public void CopyFrom(MyColor color)
        {
            R = color.R;
            G = color.G;
            B = color.B;
        }

        public void HueShif(int shift = 10)
        {
            HSV hsv = ColorConverter.RGBToHSV(this);
            hsv.H += 10;
            if (hsv.H > 360)
                hsv.H -= 360;
            MyColor res = ColorConverter.HSVToRGB(hsv);
            this = new(res.R, res.G, res.B);
        }

        public static MyColor MixColor(MyColor c1, MyColor c2)
        {
            return new((byte)((c1.R + c2.R) / 2), (byte)((c1.G + c2.G) / 2), (byte)((c1.B + c2.B) / 2));
        }
        public static MyColor MixColor(MyColor c1, MyColor c2, MyColor c3)
        {
            return new((byte)((c1.R + c2.R + c3.R) / 3), (byte)((c1.G + c2.G + c3.G) / 3), (byte)((c1.B + c2.B + c3.B) / 3));
        }
        public static MyColor MixColor(MyColor c1, MyColor c2, MyColor c3, MyColor c4)
        {
            // just A1 + A2.. + AN / N 
            return new((byte)((c1.R + c2.R + c3.R + c4.R) / 4), (byte)((c1.G + c2.G + c3.G + c4.G) / 4), (byte)((c1.B + c2.B + c3.B + c4.B) / 4));
        }


    }
    public class LockBitmap
    {
        Bitmap source = null;
        IntPtr Iptr = IntPtr.Zero;
        BitmapData bitmapData = null;

        public byte[] Pixels { get; set; }
        public int Depth { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public LockBitmap(Bitmap source)
        {
            this.source = source;
        }

        public void LockBits()
        {
            try
            {
                Width = source.Width;
                Height = source.Height;

                // get total locked pixels count
                int PixelCount = Width * Height;

                // Create rectangle to lock
                Rectangle rect = new Rectangle(0, 0, Width, Height);

                // get source bitmap pixel format size
                Depth = System.Drawing.Bitmap.GetPixelFormatSize(source.PixelFormat);

                // Check if bpp (Bits Per Pixel) is 8, 24, or 32
                if (Depth != 8 && Depth != 24 && Depth != 32)
                {
                    throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
                }

                // Lock bitmap and return bitmap data
                bitmapData = source.LockBits(rect, ImageLockMode.ReadWrite,
                                             source.PixelFormat);

                // create byte array to copy pixel values
                int step = Depth / 8;
                Pixels = new byte[PixelCount * step];
                Iptr = bitmapData.Scan0;

                // Copy data from pointer to array
                Marshal.Copy(Iptr, Pixels, 0, Pixels.Length);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void UnlockBits()
        {
            try
            {
                // Copy data from byte array to pointer
                Marshal.Copy(Pixels, 0, Iptr, Pixels.Length);

                // Unlock bitmap data
                source.UnlockBits(bitmapData);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        //public MyColor GetPixel(int x, int y)
        //{
        //    int i = ((y * Width) + x) * 3;
        //    if (i > Pixels.Length - 1 || x > Width || y > Height
        //        || x < 0 || y < 0)
        //    {
        //        return ImageProcessing.BorderrlessBGColor;
        //    }

        //    byte b = Pixels[i];
        //    byte g = Pixels[i + 1];
        //    byte r = Pixels[i + 2];

        //    return new MyColor(r, g, b);
        //}

        //public MyColor GetPixel(int i)
        //{
        //    byte b = Pixels[i];
        //    byte g = Pixels[i + 1];
        //    byte r = Pixels[i + 2];

        //    return new MyColor(r, g , b);
        //}

        //public void SetPixel(int x, int y, MyColor color)
        //{
        //    int i = ((y * Width) + x) * 3;
        //    if (i > Pixels.Length - 1)
        //    {
        //        return;
        //    }

        //    Pixels[i] = color.B;
        //    Pixels[i + 1] = color.G;
        //    Pixels[i + 2] = color.R;
        //}

        //public void SetPixel(int x, int y, byte R, byte G, byte B)
        //{
        //    int i = ((y * Width) + x) * 3;

        //    Pixels[i] = B;
        //    Pixels[i + 1] = G;
        //    Pixels[i + 2] = R;
        //}

        //public void SetPixel(int i, byte R, byte G, byte B)
        //{
        //    Pixels[i] = B;
        //    Pixels[i + 1] = G;
        //    Pixels[i + 2] = R;
        //}

        //public void SetPixel(int i, MyColor color)
        //{
        //    Pixels[i] = color.B;
        //    Pixels[i + 1] = color.G;
        //    Pixels[i + 2] = color.R;
        //}
    }
}
