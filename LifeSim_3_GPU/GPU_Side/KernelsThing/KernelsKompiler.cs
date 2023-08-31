using ILGPU.Runtime;

namespace LifeSim_3_GPU.GPU_Side.KernelsThing
{
    public static class KernelsKompiler
    {
        public static void CompileCernels(Accelerator _accelerator)
        {
            Kernels.Kompile(_accelerator);
            CellsRenderKernel.Kompile(_accelerator);
            TurnKernel.Kompile(_accelerator);
            CellsInitKernel.Kompile(_accelerator);
        }
    }
}
