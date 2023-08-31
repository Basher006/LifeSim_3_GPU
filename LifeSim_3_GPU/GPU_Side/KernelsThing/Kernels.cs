using ILGPU.Runtime;
using ILGPU;
using System.Drawing;
using System.Security.Policy;
using System;
using ILGPU.Algorithms.Random;
using static ILGPU.Stride1D;

namespace LifeSim_3_GPU.GPU_Side.KernelsThing
{
    public static class Kernels
    {
        public static Action<Index3D, ArrayView1D<long, Stride1D.Dense>, ArrayView3D<byte, Stride3D.DenseXY>, SizesForRandomGenes> FillGenesWithRandom_kernel;
        public static Action<Index2D, ArrayView2D<KernelRandom2, Stride2D.DenseY>, ArrayView3D<byte, Stride3D.DenseXY>, SizesForRandomGenes> FillGenesWithRandom_kernel2;
        public static Action<Index2D, ArrayView2D<KernelRandom2, Stride2D.DenseY>, TurnKernelConstants, uint> InitKernelRandom_kernel;
        public static Action<Index1D, ArrayView2D<CellData, Stride2D.DenseX>, ArrayView2D<KernelRandom2, Stride2D.DenseY>, TurnKernelConstants> InitSpawn_kernel;

        public static void Kompile(Accelerator _accelerator)
        {
            FillGenesWithRandom_kernel = _accelerator.LoadAutoGroupedStreamKernel<Index3D, ArrayView1D<long, Stride1D.Dense>, ArrayView3D<byte, Stride3D.DenseXY>, SizesForRandomGenes>(FillGenesWithRandom);
            FillGenesWithRandom_kernel2 = _accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView2D<KernelRandom2, Stride2D.DenseY>, ArrayView3D<byte, Stride3D.DenseXY>, SizesForRandomGenes>(FillGenesWithRandom2);

            InitKernelRandom_kernel = _accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView2D<KernelRandom2, Stride2D.DenseY>, TurnKernelConstants, uint>(InitKernelRandom);
            InitSpawn_kernel = _accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView2D<CellData, Stride2D.DenseX>, ArrayView2D<KernelRandom2, Stride2D.DenseY>, TurnKernelConstants>(InitSpawn);
        }
        private static void FillGenesWithRandom2(Index2D index, ArrayView2D<KernelRandom2, Stride2D.DenseY> rnd, ArrayView3D<byte, Stride3D.DenseXY> genes, SizesForRandomGenes sizes)
        {
            for (int i = 0; i < sizes.genLen * 3; i++)
            {
                //genes[index.Y, index.X, i] = (byte)(255 / rnd[index.Y, index.X].NextFloat32_0to1());
                genes[index.Y, index.X, i] = rnd[index.Y, index.X].NextByte();
            }
        }
        private static void FillGenesWithRandom(Index3D index, ArrayView1D<long, Stride1D.Dense> randomNumers, ArrayView3D<byte, Stride3D.DenseXY> genes, SizesForRandomGenes sizes)
        {
            //return;
            // 8 == how much byte in long
            //int rndLong_index = index.Y * sizes.h + index.X * sizes.w + index.Z;
            int rndLong_index = (index.Z * sizes.w * sizes.h) + (index.Y * sizes.w) + index.X;
            long random_long = randomNumers[rndLong_index];
            for (int i = 0; i < 8; i++)
            {
                int z = index.Z * 8 + i;
                if (z > sizes.genLen - 1)
                    break;
                genes[index.Y, index.X, z] = (byte)(random_long >> i * 8);
            }
        }

        private static void InitKernelRandom(Index2D index,
            ArrayView2D<KernelRandom2, Stride2D.DenseY> rnd,
            TurnKernelConstants constants,
            uint seed)
        {
            int flatIndex = (index.Y * constants.Size.W) + index.X;

            rnd[index.Y, index.X] = new KernelRandom2((uint)flatIndex);
        }

        private static void InitSpawn(Index1D index,
            ArrayView2D<CellData, Stride2D.DenseX> sourseCells,
            ArrayView2D<KernelRandom2, Stride2D.DenseY> rnd,
            TurnKernelConstants constants)
        {
            int y_index = index / constants.Size.W;
            int x_index = index % constants.Size.W;


            int randX = rnd[y_index, x_index].NextInt32(0, constants.Size.W);
            int randY = rnd[y_index, x_index].NextInt32(0, constants.Size.H);

            sourseCells[randY, randX].IsSpawn = 1;
            sourseCells[randY, randX].Type = 1; // 1 == creature
        }
    }
}
