using ILGPU.Runtime;
using ILGPU;

namespace LifeSim_3_GPU.GPU_Side.KernelsThing
{
    public static class CellsRenderKernel
    {
        public static Action<Index2D, ArrayView2D<CellData, Stride2D.DenseX>, ArrayView<byte>, ColorsForKernel, SizesForRenderKernel> RenderCells_Kernel;

        public static void Kompile(Accelerator _accelerator)
        {
            RenderCells_Kernel = _accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView2D<CellData, Stride2D.DenseX>, ArrayView<byte>, ColorsForKernel, SizesForRenderKernel>(RenderCells_with_ColorMix);
        }

        private static void RenderCells_with_ColorMix(Index2D index, ArrayView2D<CellData, Stride2D.DenseX> sourseCells, ArrayView<byte> resPixels, ColorsForKernel colors, SizesForRenderKernel sizes)
        {
            int get_x = index.X * sizes.get_scale + sizes.pos_x;
            int get_y = index.Y * sizes.get_scale + sizes.pos_y;

            FlagsForKernel flags = SetFlags(get_x, get_y, sourseCells, sizes);

            var Mixed_colors = GetColors_for_ColorMixing(get_x, get_y, sourseCells, colors, flags, sizes);
            // [0] = sourse
            // [1] = top
            // [2] = left
            // [3] = leftTop


            DrawSpawnedCreature(index, Mixed_colors, resPixels, flags, sizes);
        }

        private static void DrawSpawnedCreature(Index2D index, MyColor[] Mixed_colors, ArrayView<byte> resPixels, FlagsForKernel flags, SizesForRenderKernel sizes)
        {
            if (sizes.set_scale >= 4)
            {
                int side_fatness = sizes.set_scale / 4;
                if (side_fatness == 0)
                    side_fatness = 1;

                // draw 3x3 cube with coord 1:1 (for set_scale == 4 as an example)
                MainCubetDraw(index, Mixed_colors[0], resPixels, sizes, side_fatness);

                // draw top side 1-4 (for set_scale == 4 as an example)
                bool isNeedDecriseVal_on_top_side = flags.isNeedDecriseVal_on_top_side || (!flags.topCell_is_spawned && flags.top_is_cell && !flags.this_is_cell);
                TopSideDraw(index, Mixed_colors, resPixels, sizes, side_fatness, isNeedDecriseVal_on_top_side);

                // draw left side 1-4 (for set_scale == 4 as an example)
                bool isNeedDecriseVal_on_left_side = flags.isNeedDecriseVal_on_left_side || (!flags.leftCell_is_spawned && flags.left_is_cell && !flags.this_is_cell);
                LeftSideDraw(index, Mixed_colors, resPixels, sizes, side_fatness, isNeedDecriseVal_on_left_side);

                // draw 0:0 coord pixel (for set_scale == 4 as an example)
                bool isNeedDecriseVal_on_leftTop_side = flags.isNeedDecriseVal_on_leftTop_side || 
                    ((!flags.leftTopCell_is_spawned && !flags.topCell_is_spawned && !flags.leftCell_is_spawned) && 
                    (flags.leftTop_is_cell || flags.top_is_cell || flags.left_is_cell) 
                    & !flags.this_is_cell);
                LeftTopSideDraw(index, Mixed_colors, resPixels, sizes, side_fatness, isNeedDecriseVal_on_leftTop_side);
            }
            else
            {
                DefaultDraw(index, Mixed_colors[0], resPixels, sizes);
            }
        }


        private static void MainCubetDraw(Index2D index, MyColor color, ArrayView<byte> resPixels, SizesForRenderKernel sizes, int side_fatness)
        {
            for (int x = side_fatness; x < sizes.set_scale; x++)
            {
                for (int y = side_fatness; y < sizes.set_scale; y++)
                {
                    SetPixel(index, x, y, color, resPixels, sizes);
                }
            }
        }

        private static void TopSideDraw(Index2D index, MyColor[] colors, ArrayView<byte> resPixels, SizesForRenderKernel sizes, int side_fatness, bool needDecrice)
        {
            for (int x = side_fatness; x < sizes.set_scale; x++)
            {
                for (int y = 0; y < side_fatness; y++)
                {
                    if (needDecrice)
                        SetPixel_with_decriseValue(index, x, y, colors[1], resPixels, sizes);
                    else
                        SetPixel(index, x, y, colors[0], resPixels, sizes);
                }
            }
        }

        private static void LeftSideDraw(Index2D index, MyColor[] colors, ArrayView<byte> resPixels, SizesForRenderKernel sizes, int side_fatness, bool needDecrice)
        {
            for (int y = side_fatness; y < sizes.set_scale; y++)
            {
                for (int x = 0; x < side_fatness; x++)
                {
                    if (needDecrice)
                        SetPixel_with_decriseValue(index, x, y, colors[2], resPixels, sizes);
                    else
                        SetPixel(index, x, y, colors[0], resPixels, sizes);
                }
            }
        }

        private static void LeftTopSideDraw(Index2D index, MyColor[] colors, ArrayView<byte> resPixels, SizesForRenderKernel sizes, int side_fatness, bool needDecrice)
        {
            for (int x = 0; x < side_fatness; x++)
            {
                for (int y = 0; y < side_fatness; y++)
                {
                    if (needDecrice)
                        SetPixel_with_decriseValue(index, x, y, colors[3], resPixels, sizes);
                    else
                        SetPixel(index, x, y, colors[0], resPixels, sizes);
                }
            }
        }

        private static void DefaultDraw(Index2D index, MyColor color, ArrayView<byte> resPixels, SizesForRenderKernel sizes)
        {
            for (int x = 0; x < sizes.set_scale; x++)
            {
                for (int y = 0; y < sizes.set_scale; y++)
                {
                    SetPixel(index, x, y, color, resPixels, sizes);
                }
            }
        }

        private static void SetPixel_with_decriseValue(Index2D index, int x, int y, MyColor color, ArrayView<byte> resPixels, SizesForRenderKernel sizes)
        {
            int set_x = index.X * sizes.set_scale + x;
            int set_y = index.Y * sizes.set_scale + y;
            int flat_index_res = (set_y * sizes.res_w + set_x) * 3;

            resPixels[flat_index_res] = (byte)(color.R * 0.6f);
            resPixels[flat_index_res + 1] = (byte)(color.G * 0.6f);
            resPixels[flat_index_res + 2] = (byte)(color.B * 0.6f);
        }

        private static void SetPixel(Index2D index, int x, int y, MyColor color, ArrayView<byte> resPixels, SizesForRenderKernel sizes)
        {
            int set_x = index.X * sizes.set_scale + x;
            int set_y = index.Y * sizes.set_scale + y;
            int flat_index_res = (set_y * sizes.res_w + set_x) * 3;

            resPixels[flat_index_res] = color.R;
            resPixels[flat_index_res + 1] = color.G;
            resPixels[flat_index_res + 2] = color.B;
        }

        private static FlagsForKernel SetFlags(int get_x, int get_y, ArrayView2D<CellData, Stride2D.DenseX> sourseCells, SizesForRenderKernel sizes)
        {
            FlagsForKernel flags = new()
            {
                this_is_cell = !ChekCoord(get_x, get_y, sizes),
                top_is_cell = !ChekTopCoord(get_x, get_y, sizes),
                left_is_cell = !ChekLeftCoord(get_x, get_y, sizes),
                leftTop_is_cell = !ChekLeftTopCoord(get_x, get_y, sizes)
            };

            if (flags.this_is_cell)
                flags.is_spawn = sourseCells[get_y, get_x].IsSpawn != 0;
            else
                flags.is_spawn = false;

            if (flags.top_is_cell)
                flags.topCell_is_spawned = sourseCells[get_y - 1, get_x].IsSpawn != 0;
            else
                flags.topCell_is_spawned = false;

            if (flags.left_is_cell)
                flags.leftCell_is_spawned = sourseCells[get_y, get_x - 1].IsSpawn != 0;
            else
                flags.leftCell_is_spawned = false;

            if (flags.leftTop_is_cell)
                flags.leftTopCell_is_spawned = sourseCells[get_y - 1, get_x - 1].IsSpawn != 0;
            else
                flags.leftTopCell_is_spawned = false;


            if (flags.is_spawn)
            {
                flags.isNeedDecriseVal_on_top_side = true;
                flags.isNeedDecriseVal_on_left_side = true;
                flags.isNeedDecriseVal_on_leftTop_side = true;
            }
            else
            {
                if (flags.topCell_is_spawned)
                    flags.isNeedDecriseVal_on_top_side = true;
                else
                    flags.isNeedDecriseVal_on_top_side = false;

                if (flags.leftCell_is_spawned)
                    flags.isNeedDecriseVal_on_left_side = true;
                else
                    flags.isNeedDecriseVal_on_left_side = false;

                if (flags.leftTopCell_is_spawned || flags.topCell_is_spawned || flags.leftCell_is_spawned)
                    flags.isNeedDecriseVal_on_leftTop_side = true;
                else
                    flags.isNeedDecriseVal_on_leftTop_side = false;
            }

            return flags;
        }

        private static MyColor[] GetColors_for_ColorMixing(int get_x, int get_y, ArrayView2D<CellData, Stride2D.DenseX> sourseCells, ColorsForKernel colors, FlagsForKernel flags, SizesForRenderKernel sizes)
        {
            MyColor[] res = new MyColor[4];

            // main color
            MyColor sourse = new();
            if (flags.this_is_cell)
            {
                if (flags.is_spawn)
                    sourse.CopyFrom(sourseCells[get_y, get_x].Color);
                else
                    sourse.CopyFrom(colors.Game_BgColor);
            }
            else
                sourse.CopyFrom(colors.BgColor);

            // top cell color
            MyColor pick_top = new();
            if (flags.top_is_cell)
            {
                if (flags.topCell_is_spawned)
                    pick_top.CopyFrom(sourseCells[get_y - 1, get_x].Color);
                else
                    pick_top.CopyFrom(colors.Game_BgColor);
            }
            else
                pick_top.CopyFrom(sourse);

            // left cell color
            MyColor pick_left = new();
            if (flags.left_is_cell)
            {
                if (flags.leftCell_is_spawned)
                    pick_left.CopyFrom(sourseCells[get_y, get_x - 1].Color);
                else
                    pick_left.CopyFrom(colors.Game_BgColor);
            }
            else
                pick_left.CopyFrom(sourse);

            // leftTop cell color
            MyColor pick_leftTop = new();
            if (flags.leftTop_is_cell)
            {
                if (flags.leftTopCell_is_spawned)
                    pick_leftTop.CopyFrom(sourseCells[get_y - 1, get_x - 1].Color);
                else
                    pick_leftTop.CopyFrom(colors.Game_BgColor);
            }
            else
                pick_leftTop.CopyFrom(sourse);

            res[0] = sourse;
            res[1] = MyColor.MixColor(sourse, pick_top);
            res[2] = MyColor.MixColor(sourse, pick_left);
            res[3] = MyColor.MixColor(sourse, pick_left, pick_top, pick_leftTop);

            return res;
        }

        private static bool ChekCoord(int get_x, int get_y, SizesForRenderKernel sizes)
        {
            return get_y > sizes.sourse_h - 1 || get_x > sizes.sourse_w - 1 ||
                    get_y < 0 || get_x < 0;
        }

        private static bool ChekTopCoord(int get_x, int get_y, SizesForRenderKernel sizes)
        {
            return get_y - 1 > sizes.sourse_h - 1 || get_x > sizes.sourse_w - 1 ||
                    get_y - 1 < 0 || get_x < 0;
        }

        private static bool ChekLeftCoord(int get_x, int get_y, SizesForRenderKernel sizes)
        {
            return get_y > sizes.sourse_h - 1 || get_x - 1 > sizes.sourse_w - 1 ||
                    get_y < 0 || get_x - 1 < 0;
        }

        private static bool ChekLeftTopCoord(int get_x, int get_y, SizesForRenderKernel sizes)
        {
            return get_y - 1 > sizes.sourse_h - 1 || get_x - 1 > sizes.sourse_w - 1 ||
                    get_y - 1 < 0 || get_x - 1 < 0;
        }
    }
}
