using System;


namespace LifeSim_3_GPU
{
    public struct CellData
    {
        // cell resoursrs
        public int PhotoSintezValue { get; set; }
        public int MineralsInWaterValue { get; set; }

        //stats
        public int Energy { get; set; }
        public int Minerals { get; set; }
        public byte Type { get; set; } // 0 - empty, 1 - creature, 2 - corpse, 3 - mineral
        public long BotnTurn { get; set; }

        // genes
        //public byte[] Com_genes { get; set; }
        //public byte[] CndIsActive { get; set; }
        //public byte[] Cnd_genes { get; set; }
        public int Pointer { get; set; }
        public byte Ret { get; set; }
        public int ActionCounter { get; set; }

        // render things
        public int X { get; set; }
        public int Y { get; set; }
        public MyColor Color { get; set; }
        public byte IsNeedToRender { get; set; }
        public int Dir { get; set; }

        public byte IsSpawn { get; set; }

        public CellData(int x, int y, MyColor color, int dir, int photoSintezValue, int mineralsInWaterValue, int initEnergy, int initMinerals)
        {
            X = x; Y = y;

            Color = color;
            PhotoSintezValue = photoSintezValue;
            MineralsInWaterValue = mineralsInWaterValue;
            Energy = initEnergy;
            Minerals = initMinerals;

            Type = 0;
            BotnTurn = 0;
            Pointer = 0;
            Ret = 0;
            ActionCounter = 0;
            Dir = dir;

            IsNeedToRender = 1;
            IsSpawn = 0;
        }

        public void CopyFrom(ref CellData cell)
        {
            PhotoSintezValue = cell.PhotoSintezValue;
            MineralsInWaterValue = cell.MineralsInWaterValue;

            Energy = cell.Energy;
            Minerals = cell.Minerals;
            Type = cell.Type;
            BotnTurn = cell.BotnTurn;

            Pointer = cell.Pointer;
            Ret = cell.Ret;
            ActionCounter = cell.ActionCounter;

            X = cell.X;
            Y = cell.Y;
            Color = cell.Color;
            Dir = cell.Dir;
            Ret = cell.Ret;
            IsNeedToRender = cell.IsNeedToRender;

            //IsSpawn = cell.IsSpawn;
            IsSpawn = 1;
        }

        public void Despawn()
        {
            IsSpawn = 0;
            Type = 0;
            Ret = 1;
        }

        public void SpawnAsCreature()
        {
            IsSpawn = 1;
            Type = 1;
        }

        public void SpawnAsCreature(ref CellData cell)
        {
            CopyFrom(ref cell);
            IsSpawn = 1;
            Type = 1;
        }
    }

    public enum CellType
    { // !!!! creature need to be last!!! very impotant!!!! becouse look func use CellType.toInt
      // but if (type == creature) => chek isFamaly ? CellType.toInt : CellType.toInt + 1
      // so, creature have 2 variant, but its work only if creature last in this enum.
      //
      // also need numbers start from 1, not 0. 

        Empty = 1,
        Food,
        Corps,
        Wall,
        Mineral,
        Creature
    }
}
