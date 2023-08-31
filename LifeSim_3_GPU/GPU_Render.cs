using ILGPU;
using ILGPU.Runtime;
using ILGPU.Algorithms.Random;
using ILGPU.Runtime.OpenCL;
using System.Collections;
using System.ComponentModel;
using System;
using LifeSim_3_GPU.Game;

namespace LifeSim_3_GPU
{
    public struct Test1
    {
        byte dsvdfv { get; set; }
    }

    public struct Test2
    {
        public Test1 test { get; }
        public byte sdvsv;
        public int dvsvsdv { get; set; }

        //public bool huita;
    }

    public struct SizesForRandomGenes
    {
        public int w;
        public int h;
        public int len;
        public int genLen;
    }

    public struct SizesForKernels
    {
        //(Index2D index, long sourse_len, ArrayView<byte> soursePixels, ArrayView<byte> resPixels, int sourse_w, int sourse_h, int res_w, int pos_x, int pos_y, int scale)
        public long sourse_len;

        public int sourse_w;
        public int sourse_h;

        public int res_w;

        public int pos_x;
        public int pos_y;
    }
    public class GPU_Render : IDisposable
    {
        private static readonly int _numberOfChanels = 3;

        private Bitmap _sourse_img;
        private Context _context;
        private Accelerator _accelerator;
        private RECT _pb1_rect;

        private MemoryBuffer1D<byte, Stride1D.Dense> sourse_img_buffer;
        private MemoryBuffer1D<byte, Stride1D.Dense> View_buffer;
        MemoryBuffer2D<CellData, Stride2D.DenseY> cells_mb;

        private Action<Index2D, ArrayView<byte>, ArrayView<byte>, SizesForKernels, int> SumpleRender;
        private Action<Index2D, ArrayView<byte>, ArrayView<byte>, SizesForKernels, int, int> Render_withScale_down;
        private Action<Index2D, ArrayView<byte>, ArrayView<byte>, SizesForKernels> Render_withScale_4;
        private Action<Index2D, ArrayView<byte>, ArrayView<byte>, SizesForKernels> Render_withScale_8;

        private Action<Index, ArrayView<byte>, ArrayView<byte>, SizesForKernels> Render_withScale_82;

        private Action<Index2D, ArrayView2D<CellData, Stride2D.DenseY>, ArrayView<byte>, SizesForKernels, int, int> RenderThisCellsPls;

        public GPU_Render(Bitmap sourse_img, RECT pb1_rect)
        {
            _sourse_img = sourse_img;
            _pb1_rect = pb1_rect;

            _context = Context.CreateDefault();
            Device device = PickDevice(_context);
            _accelerator = device.CreateAccelerator(_context);

            CreateMemoryByffers();
            //UpdateDynamicMemoryBuffers(_pb1_rect);
            CreateKernels();
        }

        private Device PickDevice(Context context)
        {
            Device device = context.Devices[1];
            Console.WriteLine($"piked device: {device.Name}");
            return device;
        }

        private void CreateMemoryByffers()
        {
            var lockBitmap_res = new LockBitmap(_sourse_img);
            lockBitmap_res.LockBits();

            sourse_img_buffer = _accelerator.Allocate1D<byte>(lockBitmap_res.Pixels.Length);
            sourse_img_buffer.CopyFromCPU(lockBitmap_res.Pixels);

            lockBitmap_res.UnlockBits();

            cells_mb = _accelerator.Allocate2DDenseY(GameScene.Cells);

            //MemoryBuffer2D<Test2, Stride2D.DenseY> cells_mb = _accelerator.Allocate2DDenseY<Test2>(new LongIndex2D(500, 500));

            //MemoryBuffer2D<CellData, Stride2D.DenseY> cells_mb = _accelerator.Allocate2DDenseY<CellData>(new LongIndex2D(500, 500));
        }

        private static void random_kernel2(Index3D index, ArrayView1D<long, Stride1D.Dense> randomNumers, ArrayView3D<byte, Stride3D.DenseXY> genes, SizesForRandomGenes sizes)
        {
            // 8 == how much byte in long
            int rndLong_index = (index.Y * sizes.h) + (index.X * sizes.w) + index.Z;
            long random_long = randomNumers[rndLong_index];
            for (int i = 0; i < 8; i++)
            {
                int z = index.Z * 8 + i;
                if (z > sizes.genLen - 1)
                    break;
                genes[index.X, index.Y, z] = (byte)(random_long >> (i * 8)); 
            }
        }

        private static void random_kernel(Index3D index, RNGView<XorShift64Star> rng, ArrayView3D<byte, Stride3D.DenseXY> genes)
        {
            var r = rng.NextLong();

            //byte[] byffer = new byte[8];
            //for (int i = 0; i < 8; i++)
            //{
            //    byffer[i] = (byte)(r >> (i * 8));
            //}

        }

        public void GenerateRandomGenes3(WorldSetup setup)
        {
            // I don know how to generate random byte or fill byte[] array with random numbers in GPU kernel.
            // So.. im generate random long and split it on 8 bytes. 

            int b_len_z_raw = 3 * setup.GenLen;
            int b_len_z = ((b_len_z_raw / 8) + 1);

            var random = new Random();
            using var rng = RNG.Create<XorShift128Plus>(_accelerator, random);

            int tempSize = setup.Size.W * setup.Size.H * b_len_z;
            using var temp = _accelerator.Allocate1D<long>(tempSize);
            rng.FillUniform(_accelerator.DefaultStream, temp.View);

            var genes = _accelerator.Allocate3DDenseXY<byte>(new LongIndex3D(setup.Size.W, setup.Size.H, setup.GenLen));

            SizesForRandomGenes sizes = new SizesForRandomGenes { w = setup.Size.W, h = setup.Size.H, len = b_len_z_raw, genLen = setup.GenLen };

            var kernel = _accelerator.LoadAutoGroupedStreamKernel<Index3D, ArrayView1D<long, Stride1D.Dense>, ArrayView3D<byte, Stride3D.DenseXY>, SizesForRandomGenes>(random_kernel2);
            kernel((setup.Size.W, setup.Size.H, b_len_z), temp.View, genes.View, sizes);
            _accelerator.Synchronize();
            //var result = genes.GetAsArray3D();
            Console.WriteLine("genes generation complite!");

        }

        public void GenerateRandomGenes2(WorldSetup setup)
        {
            //int i_len;
            int b_len_z = 3 * setup.GenLen;
            int x_add = setup.Size.W % 8;
            int y_add = setup.Size.H % 8;
            //i_len = (setup.Size.W * setup.Size.H) * b_len_z;

            int x_dims = (setup.Size.W + x_add) / 8;
            int y_dims = (setup.Size.H + y_add);
            int random_size = (setup.Size.W * setup.Size.H);

            var random = new Random();
            using var rng = RNG.Create<XorShift64Star>(_accelerator, random, _accelerator.WarpSize);
            var rngView = rng.GetView(random_size);

            var genes_buffer = _accelerator.Allocate3DDenseXY<byte>(new LongIndex3D(setup.Size.W, setup.Size.H, b_len_z));
            var kernel = _accelerator.LoadAutoGroupedStreamKernel<Index3D, RNGView<XorShift64Star>, ArrayView3D<byte, Stride3D.DenseXY>>(random_kernel);
            kernel((x_dims, y_dims, b_len_z), rngView, genes_buffer.View);
            _accelerator.Synchronize();
            Console.WriteLine("genes generation complite!");

        }

        public void GenerateRandomGenes(WorldSetup setup)
        {
            // world size its "y" and "x", 3 its numbers of genes deepth, genlen is length of genes [y, x, 3, genlen]
            // so.. 
            // int array len == (y * x * 3 * genlen) =>
            //              sizeOfByteArray =>
            //              sizeOfByteArray_add = sizeOfByteArray % 4  //make it multiple by 4 => 
            //              sizeOfByteArray + sizeOfByteArray_add / 4

            int i_len;
            int b_len = setup.Size.W * setup.Size.H * 3 * setup.GenLen;
            int b_len_add = b_len % 4;
            i_len = (b_len + b_len_add) / 4;

            var random = new Random();

            using var rng = RNG.Create<XorShift128Plus>(_accelerator, random);
            using var buffer = _accelerator.Allocate1D<int>(i_len);
            rng.FillUniform(_accelerator.DefaultStream, buffer.View);

            var randomValues = buffer.GetAsArray1D();
            byte[] result = new byte[randomValues.Length * sizeof(int)];
            Buffer.BlockCopy(randomValues, 0, result, 0, result.Length);
            byte[,,,] bytes = new byte[setup.Size.H, setup.Size.W, 3, setup.GenLen];
            int k = 0;
            for (int y = 0; y < setup.Size.H; y++)
            {
                for (int x = 0; x < setup.Size.W; x++)
                {
                    for (int s = 0; s < 3; s++)
                    {
                        for (int i = 0; i < setup.GenLen; i++)
                        {
                            bytes[y, x, s, i] = result[k];
                            k++;
                        }
                    }
                }
            }
        }

        public Bitmap Cells_Render(RECT rect, float scale)
        {
            int fixed_w, fixed_h;
            int scale_i = (int)scale;
            if (scale >= 1)
            {
                fixed_w = (rect.W * scale_i) - ((rect.W * scale_i) % 4);
                fixed_h = rect.H * scale_i;
            }
            else
            {
                fixed_w = rect.W;
                fixed_h = rect.H;
            }

            Bitmap outImg;
            if (fixed_w <= 0 || fixed_h <= 0)
            {
                outImg = new Bitmap(1, 1, Form1.PIXEL_FORMAT);
                return outImg;
            }
            else
            {
                outImg = new Bitmap(fixed_w, fixed_h, Form1.PIXEL_FORMAT);
            }
            var lockBitmap_res = new LockBitmap(outImg);
            lockBitmap_res.LockBits();

            using var view_buffer = _accelerator.Allocate1D<byte>(lockBitmap_res.Pixels.Length);

            SizesForKernels sizes = new();
            sizes.sourse_len = sourse_img_buffer.Length;
            sizes.sourse_w = GameScene.World.Setup.Size.W;
            sizes.sourse_h = GameScene.World.Setup.Size.H;
            sizes.res_w = fixed_w;
            sizes.pos_x = rect.X;
            sizes.pos_y = rect.Y;

            int get_scale, set_scale;
            if (scale > 1)
            {
                set_scale = (int)scale;
                get_scale = 1;
            }
            else
            {
                set_scale = 1;
                get_scale = (int)(1 / scale);
            }

            //RenderThisCellsPls
            RenderThisCellsPls(new Index2D(rect.W, rect.H), cells_mb.View, view_buffer.View, sizes, get_scale, set_scale);
            _accelerator.Synchronize();


            lockBitmap_res.Pixels = view_buffer.GetAsArray1D();

            lockBitmap_res.UnlockBits();

            return outImg;
        }


        public Bitmap Render(RECT rect, float scale)
        {
            int fixed_w, fixed_h;
            int scale_i = (int)scale;
            if (scale >= 1)
            {
                fixed_w = (rect.W * scale_i) - ((rect.W * scale_i) % 4);
                fixed_h = rect.H * scale_i;
            }
            else
            {
                fixed_w = rect.W;
                fixed_h = rect.H;
            }

            Bitmap outImg;
            if (fixed_w <= 0 || fixed_h <= 0)
            {
                // This is for fix program crush when picture box size 0 or less. That happen when form not yet render.
                outImg = new Bitmap(1, 1, Form1.PIXEL_FORMAT);
                return outImg;
            }
            else
            {
                outImg = new Bitmap(fixed_w, fixed_h, Form1.PIXEL_FORMAT);
            }
            var lockBitmap_res = new LockBitmap(outImg);
            lockBitmap_res.LockBits();

            using var view_buffer = _accelerator.Allocate1D<byte>(lockBitmap_res.Pixels.Length);

            SizesForKernels sizes = new();
            sizes.sourse_len = sourse_img_buffer.Length;
            sizes.sourse_w = _sourse_img.Width;
            sizes.sourse_h = _sourse_img.Height;
            sizes.res_w = fixed_w;
            sizes.pos_x = rect.X;
            sizes.pos_y = rect.Y;

            int get_scale, set_scale;
            if (scale > 1)
            {
                set_scale = (int)scale;
                get_scale = 1;
            }
            else
            {
                set_scale = 1;
                get_scale = (int)(1 / scale);
            }

            //SumpleRender(new Index2D(fixed_w, fixed_h), sourse_img_buffer.View, view_buffer.View, sizes, 1);
            if (scale_i == 4)
                Render_withScale_4(new Index2D(rect.W, rect.H), sourse_img_buffer.View, view_buffer.View, sizes);
            else if (scale_i ==8 )
                Render_withScale_8(new Index2D(rect.W, rect.H), sourse_img_buffer.View, view_buffer.View, sizes);
            else
                Render_withScale_down(new Index2D(rect.W, rect.H), sourse_img_buffer.View, view_buffer.View, sizes, get_scale, set_scale);
            _accelerator.Synchronize();



            lockBitmap_res.Pixels = view_buffer.GetAsArray1D();

            lockBitmap_res.UnlockBits();

            return outImg;
        }

        public void Dispose()
        {
            _sourse_img.Dispose();

            _context.Dispose();
            _accelerator.Dispose();

            sourse_img_buffer.Dispose();
            View_buffer.Dispose();
        }

        # region kernels

        private void CreateKernels()
        {
            SumpleRender = _accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<byte>, ArrayView<byte>, SizesForKernels, int>(SimpleRenderKErnel);
            //Render_withScale_down = _accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<byte>, ArrayView<byte>, SizesForKernels, int, int>(RenderKernel_scale_down);
            Render_withScale_down = _accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<byte>, ArrayView<byte>, SizesForKernels, int, int>(RenderKernel_scale_up);
            Render_withScale_4 = _accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<byte>, ArrayView<byte>, SizesForKernels>(RenderKernel_scale4);
            Render_withScale_8 = _accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<byte>, ArrayView<byte>, SizesForKernels>(RenderKernel_scale8);

            RenderThisCellsPls = _accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView2D<CellData, Stride2D.DenseY>, ArrayView<byte>, SizesForKernels, int, int>(SimpleRenderKErnel_cells);
        }

        private static void RenderKernel_scale4(Index2D index, ArrayView<byte> soursePixels, ArrayView<byte> resPixels, SizesForKernels sizes)
        {
            int get_x = (index.X * 1) + sizes.pos_x;
            int get_y = (index.Y * 1) + sizes.pos_y;
            int flat_index_sourse = ((get_y * sizes.sourse_w) + get_x) * 3;

            // render 1-4x1-4
            for (int x = 1; x < 4; x++)
            {
                for (int y = 1; y < 4; y++)
                {
                    SetPixel(index, x, y, soursePixels, resPixels, sizes, get_x, get_y, flat_index_sourse, 4);
                }
            }
            //render 0-0x0-4
            for (int x = 1; x < 4; x++)
            {
                SetPixel_with_addValue(index, x, 0, soursePixels, resPixels, sizes, get_x, get_y, flat_index_sourse, 4);
            }
            //render 1-4x0-0
            for (int y = 0; y < 4; y++)
            {
                SetPixel_with_addValue(index, 0, y, soursePixels, resPixels, sizes, get_x, get_y, flat_index_sourse, 4);
            }
            // render first
            {
                SetPixel_with_addValue(index, 0, 0, soursePixels, resPixels, sizes, get_x, get_y, flat_index_sourse, 4);
            }
        }

        private static void RenderKernel_scale8(Index2D index, ArrayView<byte> soursePixels, ArrayView<byte> resPixels, SizesForKernels sizes)
        {
            int get_x = (index.X * 1) + sizes.pos_x;
            int get_y = (index.Y * 1) + sizes.pos_y;
            int flat_index_sourse = ((get_y * sizes.sourse_w) + get_x) * 3;

            // render 1-4x1-4
            for (int x = 1; x < 8; x++)
            {
                for (int y = 1; y < 8; y++)
                {
                    SetPixel(index, x, y, soursePixels, resPixels, sizes, get_x, get_y, flat_index_sourse, 8);
                }
            }
            //render 0-0x0-4
            for (int x = 1; x < 8; x++)
            {
                SetPixel_with_addValue(index, x, 0, soursePixels, resPixels, sizes, get_x, get_y, flat_index_sourse, 8);
                SetPixel_with_addValue(index, x, 1, soursePixels, resPixels, sizes, get_x, get_y, flat_index_sourse, 8);
            }
            //render 1-4x0-0
            for (int y = 0; y < 8; y++)
            {
                SetPixel_with_addValue(index, 0, y, soursePixels, resPixels, sizes, get_x, get_y, flat_index_sourse, 8);
                SetPixel_with_addValue(index, 1, y, soursePixels, resPixels, sizes, get_x, get_y, flat_index_sourse, 8);
            }
            // render first
            {
                SetPixel_with_addValue(index, 0, 0, soursePixels, resPixels, sizes, get_x, get_y, flat_index_sourse, 8);
                SetPixel_with_addValue(index, 0, 1, soursePixels, resPixels, sizes, get_x, get_y, flat_index_sourse, 8);
                SetPixel_with_addValue(index, 1, 1, soursePixels, resPixels, sizes, get_x, get_y, flat_index_sourse, 8);
                SetPixel_with_addValue(index, 1, 0, soursePixels, resPixels, sizes, get_x, get_y, flat_index_sourse, 8);
            }
        }

        private static void SetPixel_with_addValue(Index2D index, int x, int y, ArrayView<byte> soursePixels, ArrayView<byte> resPixels, SizesForKernels sizes, int get_x, int get_y, int flat_index_sourse, int scale)
        {
            int set_x = (index.X * scale) + x;
            int set_y = (index.Y * scale) + y;
            int flat_index_res = ((set_y * sizes.res_w) + set_x) * 3;

            if (flat_index_sourse > sizes.sourse_len - 1 ||
            get_x > sizes.sourse_w ||
                    get_y > sizes.sourse_h ||
                    get_x < 0 ||
                    get_y < 0)
            {
                resPixels[flat_index_res] = 80;
                resPixels[flat_index_res + 1] = 80;
                resPixels[flat_index_res + 2] = 80;
            }
            else
            {
                resPixels[flat_index_res] =     (byte)(soursePixels[flat_index_sourse]     - (soursePixels[flat_index_sourse]     / 3));
                resPixels[flat_index_res + 1] = (byte)(soursePixels[flat_index_sourse + 1] - (soursePixels[flat_index_sourse + 1] / 3));
                resPixels[flat_index_res + 2] = (byte)(soursePixels[flat_index_sourse + 2] - (soursePixels[flat_index_sourse + 2] / 3));
            }
        }

        private static void SetPixel(Index2D index, int x, int y, ArrayView<byte> soursePixels, ArrayView<byte> resPixels, SizesForKernels sizes, int get_x, int get_y, int flat_index_sourse, int scale)
        {
            int set_x = (index.X * scale) + x;
            int set_y = (index.Y * scale) + y;
            int flat_index_res = ((set_y * sizes.res_w) + set_x) * 3;

            if (flat_index_sourse > sizes.sourse_len - 1 ||
            get_x > sizes.sourse_w ||
                    get_y > sizes.sourse_h ||
                    get_x < 0 ||
                    get_y < 0)
            {
                resPixels[flat_index_res] = 80;
                resPixels[flat_index_res + 1] = 80;
                resPixels[flat_index_res + 2] = 80;
            }
            else
            {
                resPixels[flat_index_res] =     soursePixels[flat_index_sourse];
                resPixels[flat_index_res + 1] = soursePixels[flat_index_sourse + 1];
                resPixels[flat_index_res + 2] = soursePixels[flat_index_sourse + 2];
            }
        }

        private static void RenderKernel_scale_up(Index2D index, ArrayView<byte> soursePixels, ArrayView<byte> resPixels, SizesForKernels sizes, int get_scale, int set_scale)
        {
            int get_x = (index.X * get_scale) + sizes.pos_x;
            int get_y = (index.Y * get_scale) + sizes.pos_y;
            int flat_index_sourse = ((get_y * sizes.sourse_w) + get_x) * 3;

            for (int x = 0; x < set_scale; x++)
            {
                for (int y = 0; y < set_scale; y++)
                {
                    int set_x = (index.X * set_scale) + x;
                    int set_y = (index.Y * set_scale) + y;
                    int flat_index_res = ((set_y * sizes.res_w) + set_x) * 3;

                    if (flat_index_sourse > sizes.sourse_len - 1 ||
                        get_x > sizes.sourse_w ||
                        get_y > sizes.sourse_h ||
                        get_x < 0 ||
                        get_y < 0)
                    {
                        resPixels[flat_index_res] =     80;
                        resPixels[flat_index_res + 1] = 80;
                        resPixels[flat_index_res + 2] = 80;
                    }
                    else
                    {
                        resPixels[flat_index_res] =     soursePixels[flat_index_sourse];
                        resPixels[flat_index_res + 1] = soursePixels[flat_index_sourse + 1];
                        resPixels[flat_index_res + 2] = soursePixels[flat_index_sourse + 2];
                    }
                }
            }
        }

        private static void RenderKernel_scale_down(Index2D index, ArrayView<byte> soursePixels, ArrayView<byte> resPixels, SizesForKernels sizes, int get_scale, int set_scale)
        {
            int get_x = (index.X * get_scale) + sizes.pos_x;
            int get_y = (index.Y * get_scale) + sizes.pos_y;
            int flat_index_sourse = ((get_y * sizes.sourse_w) + get_x) * 3;

            int set_x = (index.X * set_scale);
            int set_y = (index.Y * set_scale);
            int flat_index_res = ((set_y * sizes.res_w) + set_x) * 3;

            if (flat_index_sourse > sizes.sourse_len - 1 ||
                get_x > sizes.sourse_w ||
                get_y > sizes.sourse_h ||
                get_x < 0 ||
                get_y < 0)
            {
                resPixels[flat_index_res] = 80;
                resPixels[flat_index_res + 1] = 80;
                resPixels[flat_index_res + 2] = 80;
            }
            else
            {
                resPixels[flat_index_res] = soursePixels[flat_index_sourse];
                resPixels[flat_index_res + 1] = soursePixels[flat_index_sourse + 1];
                resPixels[flat_index_res + 2] = soursePixels[flat_index_sourse + 2];
            }
        }

        private static void SimpleRenderKErnel_cells(Index2D index, ArrayView2D<CellData, Stride2D.DenseY> sourseCells, ArrayView<byte> resPixels, SizesForKernels sizes, int get_scale, int set_scale)
        {
            int get_x = (index.X * get_scale) + sizes.pos_x;
            int get_y = (index.Y * get_scale) + sizes.pos_y;

            for (int x = 0; x < set_scale; x++)
            {
                for (int y = 0; y < set_scale; y++)
                {
                    int set_x = (index.X * set_scale) + x;
                    int set_y = (index.Y * set_scale) + y;
                    int flat_index_res = ((set_y * sizes.res_w) + set_x) * 3;

                    if (get_x > sizes.sourse_w - 1 ||
                        get_y > sizes.sourse_h - 1 ||
                        get_x < 0 ||
                        get_y < 0)
                    {
                        resPixels[flat_index_res] = 80;
                        resPixels[flat_index_res + 1] = 80;
                        resPixels[flat_index_res + 2] = 80;
                    }
                    else
                    {
                        resPixels[flat_index_res] = sourseCells[get_y, get_x].Color.R;
                        resPixels[flat_index_res + 1] = sourseCells[get_y, get_x].Color.G;
                        resPixels[flat_index_res + 2] = sourseCells[get_y, get_x].Color.B;
                    }
                }
            }






            //int flat_index_sourse = XY_to_flat(index.X, index.Y, sourse_w); // ((y * Width) + x) * 3;
            //int flat_index_res = XY_to_flat(index.X, index.Y, res_w);

            //int flat_index_sourse = (((index.Y + sizes.pos_y) * sizes.sourse_w) + (index.X + sizes.pos_x)) * 3;
            //int index_sourse_y = index.Y + sizes.pos_y;
            //int index_sourse_x = index.X + sizes.pos_x;


            //int flat_index_res = ((index.Y * sizes.res_w) + index.X) * 3;

            //if (flat_index_sourse > sizes.sourse_len - 1 || index.X + sizes.pos_x > sizes.sourse_w || index.Y + sizes.pos_y > sizes.sourse_h
            //    || index.X + sizes.pos_x < 0 || index.Y + sizes.pos_y < 0)
            //if (index_sourse_y > sizes.sourse_h || index_sourse_x > sizes.sourse_w ||
            //        index_sourse_y < 0 || index_sourse_x < 0)
            //{
            //    resPixels[flat_index_res] = 80;
            //    resPixels[flat_index_res + 1] = 80;
            //    resPixels[flat_index_res + 2] = 80;
            //}
            //else
            //{
            //    resPixels[flat_index_res] = sourseCells[index_sourse_y, index_sourse_x].Color.R;
            //    resPixels[flat_index_res + 1] = sourseCells[index_sourse_y, index_sourse_x].Color.G;
            //    resPixels[flat_index_res + 2] = sourseCells[index_sourse_y, index_sourse_x].Color.B;
            //}
        }

        private static void SimpleRenderKErnel(Index2D index, ArrayView<byte> soursePixels, ArrayView<byte> resPixels, SizesForKernels sizes, int scale)
        {
            //int flat_index_sourse = XY_to_flat(index.X, index.Y, sourse_w); // ((y * Width) + x) * 3;
            //int flat_index_res = XY_to_flat(index.X, index.Y, res_w);

            int flat_index_sourse = (((index.Y + sizes.pos_y) * sizes.sourse_w) + (index.X + sizes.pos_x)) * 3;
            int flat_index_res = ((index.Y * sizes.res_w) + index.X) * 3;
            
            if (flat_index_sourse > sizes.sourse_len - 1 || index.X + sizes.pos_x > sizes.sourse_w || index.Y + sizes.pos_y > sizes.sourse_h
                || index.X + sizes.pos_x < 0 || index.Y + sizes.pos_y < 0)
            {
                resPixels[flat_index_res]     = 80;
                resPixels[flat_index_res + 1] = 80;
                resPixels[flat_index_res + 2] = 80;
            }
            else
            {
                resPixels[flat_index_res] =     soursePixels[flat_index_sourse];
                resPixels[flat_index_res + 1] = soursePixels[flat_index_sourse + 1];
                resPixels[flat_index_res + 2] = soursePixels[flat_index_sourse + 2];
            }
        }

        #endregion


        private static int XY_to_flat(int x, int y, int Width)
        {
            return ((y * Width) + x) * 3;
        }
    }
}
