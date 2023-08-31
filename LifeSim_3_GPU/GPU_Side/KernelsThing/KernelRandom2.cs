using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace LifeSim_3_GPU.GPU_Side.KernelsThing
{
    public struct KernelRandom2
    {
        private const int a1 = 123;
        private const int a2 = 456;
        private const int a3 = 789;

        private uint w;

        private const long m = uint.MaxValue;

        private uint PreviosValue;

        public KernelRandom2(uint seed)
        {
            PreviosValue = seed;
            w = a3;
            var a = (NextUInt32() * 3);
            PreviosValue = a;
            a = (NextUInt32() + NextUInt32()) / 3;
            PreviosValue = a;
        }

        public uint NextUInt32()
        {
            return _next();
        }

        private uint _next(uint seed)
        {
            uint t = seed ^ (seed << 11);
            w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));
            return w;
        }

        private uint _next()
        {
            uint t = PreviosValue ^ (PreviosValue << 11);
            w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));
            PreviosValue = w;
            return w;
        }

        public int NextInt32(int min, int max)
        {
            double scale = NextFloat32_0to1();
            return (int)(min + (max - min) * scale);
        }

        public double NextFloat32_0to1()
        {
            _next();
            return (double)PreviosValue / m;
        }

        public double NextFloat32_0to1(uint seed)
        {
            _next(seed);
            return (double)PreviosValue / m;
        }

        public float NextFloat32_0to1(double min, double max)
        {
            double scale = NextFloat32_0to1();

            return (float)(min + ((max - min) * scale));
        }

        public float NextFloat32_0to1(double min, double max, uint seed)
        {
            double scale = NextFloat32_0to1(seed);

            return (float)(min + ((max - min) * scale));
        }

        public byte NextByte()
        {
            double scale = NextFloat32_0to1();
            return (byte)(256 * scale);

        }
    }
}
