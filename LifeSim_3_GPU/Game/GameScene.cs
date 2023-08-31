using LifeSim_3_GPU.GPU_Side.KernelsThing;
using System;
using System.Drawing.Imaging;
using System.Security.Cryptography;


namespace LifeSim_3_GPU.Game
{
    public static class GameScene
    {
        public const PixelFormat PIXEL_FORMAT = PixelFormat.Format24bppRgb;
        public static RNGCryptoServiceProvider CprytoRNG = new RNGCryptoServiceProvider();

        public static Bitmap Scene;
        public static CellData[,] Cells;
        public static World World;

        public static void Init(World world)
        {
            World = world;
            Scene = new(world.Setup.Size.W, world.Setup.Size.H, PIXEL_FORMAT);
        }

        private static MyColor GenerateRandomColor()
        {
            double randomHue = RandomIntFromRNG(0, 360);
            double randomSat = RandomDoubleForRandomColor(0.5d, 0.9d);
            double randomVal = RandomDoubleForRandomColor(0.75d, 1d);

            byte[] dsv = ColorConverter.HSVToRGB(randomHue, randomSat, randomVal);
            return new MyColor(dsv[0], dsv[1], dsv[2]);
        }

        public static int RandomIntFromRNG(int min, int max)
        {
            // Generate four random bytes
            byte[] four_bytes = new byte[4];
            CprytoRNG.GetBytes(four_bytes);

            // Convert the bytes to a UInt32
            uint scale = BitConverter.ToUInt32(four_bytes, 0);

            // And use that to pick a random number >= min and < max
            return (int)(min + (max - min) * (scale / (uint.MaxValue + 1.0)));
        }

        public static float RandomDouble0to1()
        {
            int rint1_ = RandomIntFromRNG(0, int.MaxValue);
            float randomf = rint1_ / (float)int.MaxValue;
            return randomf;
        }

        public static double RandomDoubleForRandomColor(double min, double max)
        {
            double r0to1 = RandomDouble0to1();

            return min + ((max - min) * r0to1);
        }
    }
}
