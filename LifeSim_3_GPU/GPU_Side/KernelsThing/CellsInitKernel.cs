using ILGPU;
using ILGPU.Runtime;

namespace LifeSim_3_GPU.GPU_Side.KernelsThing
{
    public static class CellsInitKernel
    {

        public static Action<Index2D,
            ArrayView2D<CellData, Stride2D.DenseX>,
            ArrayView3D<byte, Stride3D.DenseXY>,
            ArrayView2D<KernelRandom2, Stride2D.DenseY>,
            TurnKernelConstants> InitCells_kernel;

        public static void Kompile(Accelerator _accelerator)
        {
            InitCells_kernel = _accelerator.LoadAutoGroupedStreamKernel<Index2D,
                            ArrayView2D<CellData, Stride2D.DenseX>,
                            ArrayView3D<byte, Stride3D.DenseXY>,
                            ArrayView2D<KernelRandom2, Stride2D.DenseY>,
                            TurnKernelConstants>(InitCells);
        }

        private static void InitCells(Index2D index,
            ArrayView2D<CellData, Stride2D.DenseX> sourseCells,
            ArrayView3D<byte, Stride3D.DenseXY> genes,
            ArrayView2D<KernelRandom2, Stride2D.DenseY> rnd,
            TurnKernelConstants constants)
        {
            sourseCells[index.Y, index.X] = new CellData(index.X, index.Y,
                GenerateRandomColor(index, genes, rnd, constants),
                GenerateRandomDir(index, rnd),
                GetPhotosuthesValue(index, constants),
                constants.CollectMineralsFromWaterValue,
                constants.InitEnergy,
                constants.InitMinerals);

            sourseCells[index.Y, index.X].Type = 0; // 0 == empty
            sourseCells[index.Y, index.X].IsSpawn = 0;
        }

        private static MyColor GenerateRandomColor(Index2D index, ArrayView3D<byte, Stride3D.DenseXY> genes, ArrayView2D<KernelRandom2, Stride2D.DenseY> rnd, TurnKernelConstants constants)
        {
            // 255 / (sum(1/3 of gens) / (float)len(1/gens)) == 0..1
            double hueSum = 0, satSum = 0, valSum = 0;
            for (int i = 0; i < constants.GenLen * 3; i+=3)
            {
                hueSum += genes[index.Y, index.X, i];
                satSum += genes[index.Y, index.X, i + 1];
                valSum += genes[index.Y, index.X, i  +2];
            }

            double hue = rnd[index.Y, index.X].NextFloat32_0to1(0, 360, (uint)hueSum);

            byte[] rgb = ColorConverter.HSVToRGB(hue, TurnKernelConstants.CREATURE_SAT, TurnKernelConstants.CRATURE_VAL);

            return new MyColor(rgb[0], rgb[1], rgb[2]);
        }

        private static int GenerateRandomDir(Index2D index, ArrayView2D<KernelRandom2, Stride2D.DenseY> rnd)
        {
            double scale = rnd[index.Y, index.X].NextFloat32_0to1();
            return (int)(1 + 7 * scale);
        }

        private static int GetPhotosuthesValue(Index2D index, TurnKernelConstants constants)
        {
            // (!!!!)
            return 10;
        }

        public static byte[] HSVToRGB(double H, double S, double V)
        {
            double r, g, b;

            if (S == 0)
            {
                r = V;
                g = V;
                b = V;
            }
            else
            {
                int i;
                double f, p, q, t;

                if (H == 360)
                    H = 0;
                else
                    H = H / 60;

                i = (int)H;
                f = H - i;

                p = V * (1.0 - S);
                q = V * (1.0 - (S * f));
                t = V * (1.0 - (S * (1.0 - f)));

                switch (i)
                {
                    case 0:
                        r = V;
                        g = t;
                        b = p;
                        break;

                    case 1:
                        r = q;
                        g = V;
                        b = p;
                        break;

                    case 2:
                        r = p;
                        g = V;
                        b = t;
                        break;

                    case 3:
                        r = p;
                        g = q;
                        b = V;
                        break;

                    case 4:
                        r = t;
                        g = p;
                        b = V;
                        break;

                    default:
                        r = V;
                        g = p;
                        b = q;
                        break;
                }
            }
            return new byte[] { (byte)(r * 255), (byte)(g * 255), (byte)(b * 255) };
        }
    }
}
