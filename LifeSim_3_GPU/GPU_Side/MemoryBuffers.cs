using ILGPU;
using ILGPU.Algorithms.Random;
using ILGPU.Runtime;
using LifeSim_3_GPU.Game;
using LifeSim_3_GPU.GPU_Side.KernelsThing;

namespace LifeSim_3_GPU.GPU_Side
{
    public class MemoryBuffers : IDisposable
    {
        public MemoryBuffer2D<CellData, Stride2D.DenseX> Cells_mb;
        public MemoryBuffer3D<byte, Stride3D.DenseXY> Genes_mb;

        public MemoryBuffer1D<int, Stride1D.Dense> MoveEnergyCost_mb;
        public MemoryBuffer1D<int, Stride1D.Dense> EnergyCost_inWater_mb;
        public MemoryBuffer2D<int, Stride2D.DenseY> Dirs_mb;

        public MemoryBuffer2D<KernelRandom2, Stride2D.DenseY> KernelRandom_mb;


        public MemoryBuffers(Accelerator _accelerator, CellData[,] cells, WorldSetup setup)
        {
            _createMemBuffers(_accelerator, cells, setup);
        }

        private void _createMemBuffers(Accelerator _accelerator, CellData[,] cells, WorldSetup setup)
        {
            _createCellsBuffer(_accelerator, setup);
            _createGenesBuffer(_accelerator, setup);

            // not like this :-(
            _createMoveEnergyCostBuffer(_accelerator, TurnKernel.MoveEnergyCost);
            _createEnergyCost_inWaterBuffer(_accelerator, TurnKernel.MoveEnergyCost_inWater);
            _createMoveDirsBuffer(_accelerator, TurnKernel.Dirs);

            _createRandomBuffer(_accelerator, setup);
        }

        //private void _createCellsBuffer(Accelerator _accelerator, CellData[,] cells)
        //{
        //    Cells_mb = _accelerator.Allocate2DDenseX(cells);
        //}

        private void _createCellsBuffer(Accelerator _accelerator, WorldSetup setup)
        {
            Cells_mb = _accelerator.Allocate2DDenseX<CellData>(new Index2D(setup.Size.H, setup.Size.W));
        }

        private void _createGenesBuffer(Accelerator _accelerator, WorldSetup setup)
        {
            Genes_mb = _accelerator.Allocate3DDenseXY<byte>(new LongIndex3D(setup.Size.H, setup.Size.W, setup.GenLen * 3));
        }

        private void _createMoveEnergyCostBuffer(Accelerator _accelerator, int[] moveEnergyCost)
        {
            MoveEnergyCost_mb = _accelerator.Allocate1D(moveEnergyCost);
        }

        private void _createEnergyCost_inWaterBuffer(Accelerator _accelerator, int[] moveEnergyCost_inWater)
        {
            EnergyCost_inWater_mb = _accelerator.Allocate1D(moveEnergyCost_inWater);
        }

        private void _createMoveDirsBuffer(Accelerator _accelerator, int[,] dirs)
        {
            Dirs_mb = _accelerator.Allocate2DDenseY(dirs);
        }

        private void _createRandomBuffer(Accelerator _accelerator, WorldSetup setup)
        {
            KernelRandom_mb = _accelerator.Allocate2DDenseY<KernelRandom2>(new Index2D(setup.Size.H, setup.Size.W));
        }

        public void Dispose()
        {
            Cells_mb.Dispose();
            Genes_mb.Dispose();

            MoveEnergyCost_mb.Dispose();
            EnergyCost_inWater_mb.Dispose();
            Dirs_mb.Dispose();

            KernelRandom_mb.Dispose();
        }
    }
}
