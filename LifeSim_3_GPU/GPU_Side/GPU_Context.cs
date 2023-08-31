using ILGPU.Runtime;
using ILGPU;
using ILGPU.Algorithms.Random;
using LifeSim_3_GPU.Game;
using LifeSim_3_GPU.GPU_Side.KernelsThing;
using ILGPU.Runtime.OpenCL;

namespace LifeSim_3_GPU.GPU_Side
{
    public class GPU_Context : IDisposable
    {
        public MemoryBuffers MemBuffers;

        private WorldSetup _wSetup;

        private Context _context;
        private Accelerator _accelerator;

        int turns = 0;

        public GPU_Context(CellData[,] cells, WorldSetup setup)
        {
            // 1. Create context.
            // 2. Pick device.
            // 3. Compile kernels and create device memory buffers.
            // 4. Now we can run kernels. 

            _wSetup = setup;

            _context = Context.CreateDefault();
            Device device = PickDevice(_context);
            _accelerator = device.CreateAccelerator(_context);

            KernelsKompiler.CompileCernels(_accelerator);
            MemBuffers = new MemoryBuffers(_accelerator, cells, setup);

            //_fillGenesWithRandom();
            TurnKernelConstants constants = MainLoop.createConstantsForTurn(); // not like this
            _initKernelRandom(constants);
            _fillGenesWithRandom();
            _initCellsOnGPU(constants);
            _initSpawnCreatures(constants);
        }


        public void Turn(TurnKernelConstants constants)
        {
            SpawnThingCounter counter = new();
            for (int i = 0; i < 1; i++) // how much turns done before Synchronize(). 1 work fine fine.
            {
                TurnKernel.Turn_kernel(new Index2D(_wSetup.Size.W, _wSetup.Size.H),
                                MemBuffers.Cells_mb.View,
                                MemBuffers.Genes_mb.View,
                                MemBuffers.KernelRandom_mb.View,
                                MemBuffers.Dirs_mb.View,
                                MemBuffers.MoveEnergyCost_mb.View,
                                MemBuffers.EnergyCost_inWater_mb.View,
                                constants);

                //TurnKernel.SimpleTurn_kernel(new Index2D(_wSetup.Size.W, _wSetup.Size.H),
                //                MemBuffers.Cells_mb.View);

                counter = new();
                TurnKernel.AfterTurnDone_kernel(new Index2D(_wSetup.Size.W, _wSetup.Size.H),
                    MemBuffers.Cells_mb.View,
                    counter
                    );
                turns++;
                //Console.WriteLine(counter.Creatures);
            }

            _accelerator.Synchronize();
        }

        public Bitmap Cells_Render(RECT rect, float scale)
        {
            SizesForRenderKernel sizes = CreateSizesForRenderKernel(rect, scale);

            Bitmap outImg = CreateBitmapForRender(sizes);
            if (outImg.Width <= 1 || outImg.Height <= 1)
                return outImg;
            LockBitmap lockBitmap_res = new (outImg);
            lockBitmap_res.LockBits();

            using var view_buffer = _accelerator.Allocate1D<byte>(lockBitmap_res.Pixels.Length);

            CellsRenderKernel.RenderCells_Kernel(new Index2D(rect.W, rect.H), MemBuffers.Cells_mb.View, view_buffer.View, GameScene.World.Setup.Colors, sizes);
            //_accelerator.Synchronize();

            lockBitmap_res.Pixels = view_buffer.GetAsArray1D();
            lockBitmap_res.UnlockBits();

            return outImg;
        }

        private void _fillGenesWithRandom()
        {
            SizesForRandomGenes sizes = new SizesForRandomGenes {  genLen = _wSetup.GenLen };

            Kernels.FillGenesWithRandom_kernel2((_wSetup.Size.W, _wSetup.Size.H), MemBuffers.KernelRandom_mb.View, MemBuffers.Genes_mb.View, sizes);
            _accelerator.Synchronize();
        }

        public void Dispose()
        {
            _context.Dispose();
            _accelerator.Dispose();
        }

        private void _initKernelRandom(TurnKernelConstants constants)
        {
            Kernels.InitKernelRandom_kernel(new Index2D(_wSetup.Size.W, _wSetup.Size.H), MemBuffers.KernelRandom_mb.View, constants, (uint)DateTime.Now.ToBinary());
            _accelerator.Synchronize();
        }

        private void _initCellsOnGPU(TurnKernelConstants constants)
        {
            CellsInitKernel.InitCells_kernel(new Index2D(_wSetup.Size.W, _wSetup.Size.H), MemBuffers.Cells_mb.View, MemBuffers.Genes_mb.View, MemBuffers.KernelRandom_mb.View, constants);
            _accelerator.Synchronize();
        }

        private void _initSpawnCreatures(TurnKernelConstants constants)
        {
            int index;
            int max_cells = _wSetup.Size.W * _wSetup.Size.H;
            if (constants.InitCreatureCount < max_cells)
                index = constants.InitCreatureCount;
            else
                index = max_cells;

            Kernels.InitSpawn_kernel(new Index1D(index - 1), MemBuffers.Cells_mb, MemBuffers.KernelRandom_mb, constants);
            _accelerator.Synchronize();
        }

        private Bitmap CreateBitmapForRender(SizesForRenderKernel sizes)
        {
            if (sizes.res_w <= 0 || sizes.res_h <= 0)
                return new Bitmap(1, 1, Form1.PIXEL_FORMAT);
            else
                return new Bitmap(sizes.res_w, sizes.res_h, Form1.PIXEL_FORMAT);
        }

        private SizesForRenderKernel CreateSizesForRenderKernel(RECT rect, float scale)
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


            SizesForRenderKernel sizes = new();

            sizes.scale_i = scale_i;
            sizes.set_scale = set_scale;
            sizes.get_scale = get_scale;

            sizes.sourse_w = _wSetup.Size.W;
            sizes.sourse_h = _wSetup.Size.H;

            sizes.res_w = fixed_w;
            sizes.res_h = fixed_h;

            sizes.pos_x = rect.X;
            sizes.pos_y = rect.Y;


            return sizes;
        }


        private Device PickDevice(Context context)
        {
            // This is need implement to form1, atm just hardcode.
            Device device = context.Devices[1];
            Console.WriteLine($"piked device: {device.Name}");
            return device;
        }
    }
}
