using ILGPU;
using ILGPU.Runtime;
using LifeSim_3_GPU.Game;
using Microsoft.VisualBasic;
using System;

namespace LifeSim_3_GPU.GPU_Side.KernelsThing
{
    public static class TurnKernel
    {
        public static readonly int[,] Dirs = new int[8, 2] {
            { -1, -1 }, { 0, -1 }, { 1, -1 },
            { -1,  0 },            { 1,  0  },
            { -1,  1 }, { 0,  1 }, { 1,  1  }};

        public static readonly int[] MoveEnergyCost = new int[]{
            5, 4, 5,
            2,    2,
            2, 1, 2
        };
        public static readonly int[] MoveEnergyCost_inWater = new int[]{
            1, 1, 1,
            1,    1,
            1, 1, 1
        };

        public static Action<Index2D,
            ArrayView2D<CellData, Stride2D.DenseX>,
            ArrayView3D<byte, Stride3D.DenseXY>,
            ArrayView2D<KernelRandom2, Stride2D.DenseY>,
            ArrayView2D<int, Stride2D.DenseY>,
            ArrayView1D<int, Stride1D.Dense>,
            ArrayView1D<int, Stride1D.Dense>,
            TurnKernelConstants> Turn_kernel;

        public static Action<Index2D,
            ArrayView2D<CellData, Stride2D.DenseX>> SimpleTurn_kernel;

        public static Action<Index2D, ArrayView2D<CellData, Stride2D.DenseX>, SpawnThingCounter> AfterTurnDone_kernel;

        public static void Kompile(Accelerator _accelerator)
        {
            Turn_kernel = _accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<CellData, Stride2D.DenseX>,
                ArrayView3D<byte, Stride3D.DenseXY>,
                ArrayView2D<KernelRandom2, Stride2D.DenseY>,
                ArrayView2D<int, Stride2D.DenseY>,
                ArrayView1D<int, Stride1D.Dense>,
                ArrayView1D<int, Stride1D.Dense>,
                TurnKernelConstants>(Turn);

            SimpleTurn_kernel = _accelerator.LoadAutoGroupedStreamKernel<Index2D,
            ArrayView2D<CellData, Stride2D.DenseX>>(simpleTurn);

            AfterTurnDone_kernel = _accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView2D<CellData, Stride2D.DenseX>, SpawnThingCounter>(AfterTurnDone);
        }

        private static void AfterTurnDone(Index2D index, ArrayView2D<CellData, Stride2D.DenseX> sourseCells, SpawnThingCounter counter)
        {
            sourseCells[index.Y, index.X].Ret = 0;
            if (sourseCells[index.Y, index.X].IsSpawn != 0)
            {
                sourseCells[index.Y, index.X].Ret = 0;

                // this is not work :C 
                //if (sourseCells[index.Y, index.X].Type == 1) //type == 1 is creature
                //{
                //    counter.Creatures++;
                //}
            }
        }

        private static void simpleTurn(Index2D index,
            ArrayView2D<CellData, Stride2D.DenseX> sourseCells)
        {
            if (sourseCells[index.Y, index.X].IsSpawn != 0 && sourseCells[index.Y, index.X].Ret == 0)
            {
            }
        }

        private static void Turn(
            Index2D index,
            ArrayView2D<CellData, Stride2D.DenseX> sourseCells,
            ArrayView3D<byte, Stride3D.DenseXY> genes,
            ArrayView2D<KernelRandom2, Stride2D.DenseY> rnd,
            ArrayView2D<int, Stride2D.DenseY> dirs,
            ArrayView1D<int, Stride1D.Dense> MoveEnergyCost,
            ArrayView1D<int, Stride1D.Dense> MoveEnergyCost_inWater,
            TurnKernelConstants constans)
        {

            if (sourseCells[index.Y, index.X].IsSpawn != 0 && sourseCells[index.Y, index.X].Ret == 0)
            {
                //type == 1 is creature
                if (sourseCells[index.Y, index.X].Type == 1)
                {
                    int actionCounter = 0;
                    while (sourseCells[index.Y, index.X].Ret == 0 && actionCounter < constans.MaxActionPerTurn)
                    {
                        actionCounter++;
                        if (sourseCells[index.Y, index.X].Energy > 0 &&
                            constans.currentTurn - sourseCells[index.Y, index.X].BotnTurn < constans.CreatureLifeTime)
                        {
                            byte CurCmdByte = genes[index.Y, index.X, sourseCells[index.Y, index.X].Pointer * 3]; // 3 is genes deepth
                            int curCmdNum = CurCmdByte / constans.CommandDiv;

                            // We cant use array of methods or any ref of methods in kernel.. so.. 
                            // One way its just switch or if/else block.
                            // commands => 0 = Move, 1 = Reproduce, 2 = RotateLeft, 3 = RotateRight, 4 = Photosynthes, 5 = EnergyToMinerals, 6 = MineralsToEnergy;

                            if (curCmdNum == 0) // move
                            {
                                if (sourseCells[index.Y, index.X].Energy > constans.MinimumEnergyForMove)
                                    Move(index, sourseCells, dirs, MoveEnergyCost, MoveEnergyCost_inWater, constans);
                            }
                            else if (curCmdNum == 1) // reproduce
                            {
                                if (sourseCells[index.Y, index.X].Energy > constans.MinimumEnergyForReprosuce)
                                    Reproduce(index, sourseCells, genes, rnd, dirs, MoveEnergyCost, MoveEnergyCost_inWater, constans);
                            }
                            else if (curCmdNum == 2) // rotate left
                            {
                                RotateLeft(index, sourseCells);
                            }
                            else if (curCmdNum == 3) // rotate right
                            {
                                RotateRight(index, sourseCells);
                            }
                            else if (curCmdNum == 4) // photosynthes
                            {
                                Photosynthes(index, sourseCells, constans);
                            }
                            else if (curCmdNum == 5) // attack
                            {
                                if (sourseCells[index.Y, index.X].Energy > constans.MinimumEnergyForAttack)
                                    if (sourseCells[index.Y, index.X].Minerals > constans.TryAttackCostMinerals)
                                    {
                                        sourseCells[index.Y, index.X].Minerals -= constans.TryAttackCostMinerals;
                                        Attack(index, sourseCells, genes, rnd, dirs, constans);
                                    }
                            }
                            else if (curCmdNum == 6) // minerals to energy
                            {
                                MineralsToEnergy(index, sourseCells, constans);
                            }
                            else if (curCmdNum == 7) // energy to minerals
                            {
                                EnergyToMinerals(index, sourseCells, constans);
                                //Attack(index, sourseCells, genes, rnd, dirs, constans);
                            }
                            else if (curCmdNum == 8) // if all work right this block is never run.
                            {
                                Photosynthes(index, sourseCells, constans);
                            }

                            PointerIncrement(index, sourseCells, constans);
                            sourseCells[index.Y, index.X].Energy -= 1;
                        }
                        else
                        {
                            sourseCells[index.Y, index.X].Despawn();
                        }
                    }
                    sourseCells[index.Y, index.X].Ret = 1;
                }
            }
        }

        #region =Comands(7)= !!! When change number of commands (when add or remove command in if/else block) you must change "CommandDiv" in constans by hands. CommandDiv == (byte.max / comands_count) + 1 !!!
        private static void Move(Index2D index,
            ArrayView2D<CellData, Stride2D.DenseX> sourseCells,
            ArrayView2D<int, Stride2D.DenseY> dirs,
            ArrayView1D<int, Stride1D.Dense> MoveEnergyCost,
            ArrayView1D<int, Stride1D.Dense> MoveEnergyCost_inWater,
            TurnKernelConstants constans)
        {
            if (TryGetCoordinatsFromDir(index, sourseCells, dirs, out int target_x, out int target_y, constans))
            {
                if (sourseCells[target_y, target_x].IsSpawn != 0) // 0 == empty
                    return;

                // if !in_water
                sourseCells[index.Y, index.X].Energy -= MoveEnergyCost[sourseCells[index.Y, index.X].Dir];

                if (sourseCells[index.Y, index.X].Energy > 0)
                {
                    sourseCells[index.Y, index.X].Ret = 1;
                    sourseCells[target_y, target_x].SpawnAsCreature(ref sourseCells[index.Y, index.X]);
                    sourseCells[index.Y, index.X].Despawn();
                }
            }
        }

        private static void Reproduce(Index2D index,
            ArrayView2D<CellData, Stride2D.DenseX> sourseCells,
            ArrayView3D<byte, Stride3D.DenseXY> genes,
            ArrayView2D<KernelRandom2, Stride2D.DenseY> rnd,
            ArrayView2D<int, Stride2D.DenseY> dirs,
            ArrayView1D<int, Stride1D.Dense> MoveEnergyCost,
            ArrayView1D<int, Stride1D.Dense> MoveEnergyCost_inWater,
            TurnKernelConstants constans)
        {
            //bool isValidDir = TryGetCoordinatsFromDir(sourseCells, cell, dirs, out int target_x, out int target_y, constans);
            if (TryGetCoordinatsFromDir(index, sourseCells, dirs, out int target_x, out int target_y, constans))
            {
                if (sourseCells[target_y, target_x].IsSpawn != 0) // 0 == empty
                    return;

                sourseCells[index.Y, index.X].Energy -= constans.ReproduceCost_Energy;
                sourseCells[index.Y, index.X].Minerals -= constans.ReproduceCost_Minerals;

                // if !in_water
                sourseCells[index.Y, index.X].Energy -= MoveEnergyCost[sourseCells[index.Y, index.X].Dir];

                if (sourseCells[index.Y, index.X].Energy > 2 && sourseCells[index.Y, index.X].Minerals > 2)
                {
                    sourseCells[index.Y, index.X].Ret = 1;
                    int halfEnergy = sourseCells[index.Y, index.X].Energy / 2;
                    int halfMinerals = sourseCells[index.Y, index.X].Minerals / 2;
                    // minus energy per reprosuce
                    sourseCells[index.Y, index.X].Energy = halfEnergy;
                    sourseCells[index.Y, index.X].Minerals = halfMinerals;
                    sourseCells[target_y, target_x].SpawnAsCreature(ref sourseCells[index.Y, index.X]);
                    sourseCells[target_y, target_x].BotnTurn = constans.currentTurn;
                    sourseCells[target_y, target_x].Pointer = 0;

                    GenesDeepCopy(genes, index.X, index.Y, target_x, target_y, constans);

                    double mutateRoll = rnd[target_y, target_x].NextFloat32_0to1();
                    if (mutateRoll < constans.MutateChance)
                        Mutate(sourseCells, genes, rnd, target_x, target_y, constans);
                    else if (constans.StrongMutateEnable != 0)
                    {
                        double strongMutateRoll = rnd[target_y, target_x].NextFloat32_0to1();
                        if (strongMutateRoll < constans.StrongMutateChance)
                            StrongMutate(sourseCells, genes, rnd, target_x, target_y, constans);
                    }
                }
            }
        }

        private static void Attack(Index2D index,
            ArrayView2D<CellData, Stride2D.DenseX> sourseCells,
            ArrayView3D<byte, Stride3D.DenseXY> genes,
            ArrayView2D<KernelRandom2, Stride2D.DenseY> rnd,
            ArrayView2D<int, Stride2D.DenseY> dirs,
            TurnKernelConstants constans)
        {
            // minerals -= minerals per try attack

            if (TryGetCoordinatsFromDir(index, sourseCells, dirs, out int target_x, out int target_y, constans))
            {
                if (sourseCells[target_y, target_x].IsSpawn == 0 || sourseCells[target_y, target_x].Type != 1) // 1 == creature
                    return;

                if (ChekFamaly(index.X, index.Y, target_x, target_y, genes, constans) < constans.GensDifferenceForFamaly)
                    return;

                sourseCells[index.Y, index.X].Energy -= constans.AttackCostEnergy;

                if (constans.EnableCounterAttack != 0)
                {
                    int x1, y1, x2, y2;
                    if (sourseCells[index.Y, index.X].Energy > sourseCells[target_y, target_x].Energy)
                    {
                        x1 = index.X; y1 = index.Y; x2 = target_x; y2 = target_y;
                    }
                    else
                    {
                        x1 = target_x; y1 = target_y; x2 = index.X; y2 = index.Y;
                    }
                    sourseCells[y1, x1].Energy += sourseCells[y2, x2].Energy;
                    sourseCells[y1, x1].Minerals += sourseCells[y2, x2].Minerals;

                    sourseCells[y1, x1].Ret = 1;

                    sourseCells[y2, x2].Despawn();
                }
                else
                {
                    sourseCells[index.Y, index.X].Energy += sourseCells[target_y, target_x].Energy;
                    sourseCells[index.Y, index.X].Minerals += sourseCells[target_y, target_x].Minerals;

                    sourseCells[index.Y, index.X].Ret = 1;

                    sourseCells[target_y, target_x].Despawn();
                }

            }
        }

        private static void RotateLeft(Index2D index, ArrayView2D<CellData, Stride2D.DenseX> sourseCells)
        {
            sourseCells[index.Y, index.X].Dir += 1;
            if (sourseCells[index.Y, index.X].Dir > 7)
                sourseCells[index.Y, index.X].Dir = 0;
        }

        private static void RotateRight(Index2D index, ArrayView2D<CellData, Stride2D.DenseX> sourseCells)
        {
            sourseCells[index.Y, index.X].Dir -= 1;
            if (sourseCells[index.Y, index.X].Dir < 0)
                sourseCells[index.Y, index.X].Dir = 7;
        }

        private static void Photosynthes(Index2D index, ArrayView2D<CellData, Stride2D.DenseX> sourseCells, TurnKernelConstants constans)
        {
            sourseCells[index.Y, index.X].Energy += sourseCells[index.Y, index.X].PhotoSintezValue;
        }

        private static void EnergyToMinerals(Index2D index, ArrayView2D<CellData, Stride2D.DenseX> sourseCells, TurnKernelConstants constans)
        {
            if (sourseCells[index.Y, index.X].Energy > constans.EnergyToMinerals_EnergyValue)
            {
                sourseCells[index.Y, index.X].Energy -= constans.EnergyToMinerals_EnergyValue;
                sourseCells[index.Y, index.X].Minerals += constans.EnergyToMinerals_MineralsValue;
            }
        }

        private static void MineralsToEnergy(Index2D index, ArrayView2D<CellData, Stride2D.DenseX> sourseCells, TurnKernelConstants constans)
        {
            if (sourseCells[index.Y, index.X].Minerals > constans.MineralsToEnergy_MineralsValue)
            {
                sourseCells[index.Y, index.X].Energy += constans.MineralsToEnergy_EnergyValue;
                sourseCells[index.Y, index.X].Minerals -= constans.MineralsToEnergy_MineralsValue;
            }
        }

        #endregion

        #region other defs
        private static void GenesDeepCopy(ArrayView3D<byte, Stride3D.DenseXY> genes, int sourse_index_X, int sourse_index_Y, int target_index_X, int target_index_Y, TurnKernelConstants constans)
        {
            for (int i = 0; i < constans.GenLen * 3; i++)
            {
                genes[target_index_Y, target_index_X, i] = genes[sourse_index_Y, sourse_index_X, i];
            }
        }

        private static float ChekFamaly(int x1, int y1, int x2, int y2, ArrayView3D<byte, Stride3D.DenseXY> genes, TurnKernelConstants constans)
        {
            int sameGenCounter = 0;
            for (int i = 0; i < constans.GenLen * 3; i++)
            {
                if (genes[y1, x1, i] == genes[y2, x2, i])
                    sameGenCounter++;
            }
            return sameGenCounter > 0 ? (constans.GenLen * 3) / (float)sameGenCounter : 0f;
        }

        private static void Mutate(ArrayView2D<CellData, Stride2D.DenseX> sourseCells, ArrayView3D<byte, Stride3D.DenseXY> genes, ArrayView2D<KernelRandom2, Stride2D.DenseY> rnd, int target_index_X, int target_index_Y, TurnKernelConstants constans)
        {
            int genMutateIndex = rnd[target_index_Y, target_index_X].NextInt32(0, constans.GenLen * 3 - 1);
            genes[target_index_Y, target_index_X, genMutateIndex] = rnd[target_index_Y, target_index_X].NextByte();
            HueShiftOnMutate(target_index_X, target_index_Y, sourseCells);
            //HueShiftOnMutate2(target_index_X, target_index_Y, sourseCells, genes, rnd, constans);
        }

        private static void StrongMutate(ArrayView2D<CellData, Stride2D.DenseX> sourseCells, ArrayView3D<byte, Stride3D.DenseXY> genes, ArrayView2D<KernelRandom2, Stride2D.DenseY> rnd, int target_index_X, int target_index_Y, TurnKernelConstants constans)
        {
            for (int i = 0; i < constans.HowMuchGenesMutateOnStrongMutate; i++)
            {
                Mutate(sourseCells, genes, rnd, target_index_X, target_index_Y, constans);
            }
        }

        private static bool TryGetCoordinatsFromDir(Index2D index, ArrayView2D<CellData, Stride2D.DenseX> sourseCells, ArrayView2D<int, Stride2D.DenseY> dirs, out int x, out int y, TurnKernelConstants constans)
        {
            x = index.X + dirs[sourseCells[index.Y, index.X].Dir, 0];
            y = index.Y + dirs[sourseCells[index.Y, index.X].Dir, 1];

            if (constans.CycleWorld_x != 0)
            {
                if (x < 0)
                    x = constans.Size.W - 1;
                if (x > constans.Size.W - 1)
                    x = 0;
            }
            else if (x < 0 || x > constans.Size.W - 1)
                return false;

            if (constans.CycleWorld_y != 0)
            {
                if (y < 0)
                    y = constans.Size.H - 1;
                if (y > constans.Size.H - 1)
                    y = 0;
            }
            else if (y < 0 || y > constans.Size.H - 1)
                return false;

            return true;
        }

        private static void PointerIncrement(Index2D index, ArrayView2D<CellData, Stride2D.DenseX> sourseCells, TurnKernelConstants constans, int increment = 1)
        {
            sourseCells[index.Y, index.X].Pointer += increment * 3;
            if (sourseCells[index.Y, index.X].Pointer * 3 + 2 >= constans.GenLen)
            {
                sourseCells[index.Y, index.X].Pointer = 0;
            }
        }

        private static void HueShiftOnMutate2(int target_x, int target_y, ArrayView2D<CellData, Stride2D.DenseX> sourseCells, ArrayView3D<byte, Stride3D.DenseXY> genes, ArrayView2D<KernelRandom2, Stride2D.DenseY> rnd, TurnKernelConstants constans)
        {
            double hueSum = 0, satSum = 0, valSum = 0;
            for (int i = 0; i < constans.GenLen * 3; i += 3)
            {
                hueSum += genes[target_y, target_x, i];
                satSum += genes[target_y, target_x, i + 1];
                valSum += genes[target_y, target_x, i + 2];
            }

            double hue = rnd[target_y, target_x].NextFloat32_0to1(0, 360, (uint)hueSum);
            double sat = rnd[target_y, target_x].NextFloat32_0to1(0.5d, 0.9d, (uint)satSum);
            double val = rnd[target_y, target_x].NextFloat32_0to1(0.75d, 1d, (uint)valSum);

            byte[] rgb = ColorConverter.HSVToRGB(hue, sat, val);

            sourseCells[target_y, target_x].Color = new(rgb[0], rgb[1], rgb[2]);
        }

        private static void HueShiftOnMutate(int target_x, int target_y, ArrayView2D<CellData, Stride2D.DenseX> sourseCells, int shift=10)
        {
            //byte R = sourseCells[target_y, target_x].Color.R;
            //byte G = sourseCells[target_y, target_x].Color.G;
            //byte B = sourseCells[target_y, target_x].Color.B;

            //double[] hsv = ColorConverter.RGBToHSV(R, G, B);
            //hsv[0] += shift;
            //if (hsv[0] > 360)
            //    hsv[0] -= 360;
            //byte[] rgb = ColorConverter.HSVToRGB(hsv[0], hsv[2], hsv[2]);

            //MyColor c = new(rgb[0], rgb[1], rgb[2]);
            //sourseCells[target_y, target_x].Color.CopyFrom(c);

            HSV hsv = ColorConverter.RGBToHSV(sourseCells[target_y, target_x].Color);
            hsv.H += shift;
            if (hsv.H > 360)
                hsv.H -= 360;
            hsv.V = TurnKernelConstants.CRATURE_VAL;
            hsv.S = TurnKernelConstants.CREATURE_SAT;
            MyColor shiftedColor = ColorConverter.HSVToRGB(hsv);
            sourseCells[target_y, target_x].Color = shiftedColor;

            //sourseCells[target_y, target_x].Color.HueShif(shift);

        }

        #endregion
    }
}
