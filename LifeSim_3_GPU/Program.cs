using LifeSim_3_GPU.GPU_Side.KernelsThing;

namespace LifeSim_3_GPU
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}