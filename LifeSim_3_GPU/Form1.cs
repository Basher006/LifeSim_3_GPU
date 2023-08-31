using LifeSim_3_GPU.Game;
using LifeSim_3_GPU.GPU_Side;

namespace LifeSim_3_GPU
{
    public partial class Form1 : Form
    {
        private const int CAM_SPEED = 1;

        private Bitmap scene;

        public int FPS = 0;
        public int TurnsCounter = 0;
        public int TurnsCounterOld = 0;
        public int TPS = 0;
        private System.Diagnostics.Stopwatch fpsStopWath;
        private long OneSecondCounter = 0;
        private int FPS_counter = 0;

        private string total = "";


        public const System.Drawing.Imaging.PixelFormat PIXEL_FORMAT = System.Drawing.Imaging.PixelFormat.Format24bppRgb;

        private Size PicterBoxSize;

        private int old_x = 0, old_y = 0;

        private Point CameraPos;
        private Point GameField_Center;

        private int scale = 3;
        public static readonly float[] ScalesList = { 0.125f, 0.25f, 0.5f, 1, 2, 4, 8, 16, 32 };
        private readonly int maxScale = ScalesList.Length - 1;


        bool firstTime = true;

        public Form1()
        {
            fpsStopWath = System.Diagnostics.Stopwatch.StartNew();
            InitializeComponent();


            GameField_Center.X = PicterBoxSize.Width / 2;
            GameField_Center.Y = PicterBoxSize.Height / 2;

            CameraPos.X = 0;
            CameraPos.Y = 0;

            pictureBox1.MouseWheel += new MouseEventHandler(pictureBox1_MouseWheel);


            MainLoop.Init(new RECT(CameraPos.X, CameraPos.Y, PicterBoxSize.Width, PicterBoxSize.Height), ScalesList[scale]);
            total = $"({GameScene.World.Setup.Size.W} x {GameScene.World.Setup.Size.H} total: {GameScene.World.Setup.Size.W * GameScene.World.Setup.Size.H})";
            Text = total;

            UpdatePictureBox();
        }

        private void UpdatePictureBox()
        {
            RECT rect = GetGameViewRECT();
            if (rect.W > 0 && rect.H > 0)
            {
                scene = MainLoop.gpu.Cells_Render(rect, ScalesList[scale]);

                DrawTextOnBitmap(ref scene);
                UpdateIMG(scene);
                GC.Collect(3); // 3 == best fps/tps
            }
        }

        private RECT GetGameViewRECT()
        {
            PicterBoxSize = GetPicterBoxSize();
            RECT rect;
            int w = PicterBoxSize.Width - PicterBoxSize.Width % 4; // This is fix for strange bug.
            if (ScalesList[scale] >= 1)
                rect = new RECT(CameraPos.X, CameraPos.Y, (int)(w / ScalesList[scale]), (int)(PicterBoxSize.Height / ScalesList[scale]));
            else
                rect = new RECT(CameraPos.X, CameraPos.Y, w, PicterBoxSize.Height);

            return rect;
        }

        private void UpdateIMG(Bitmap img)
        {
            pictureBox1.Image = img;
            pictureBox1.Update();
            //Console.WriteLine("img Updeted!");

            //fpsStopWath.Stop();
            OneSecondCounter += fpsStopWath.ElapsedMilliseconds;
            FPS_counter++;

            if (OneSecondCounter >= 1000)
            {
                TurnsCounter = MainLoop.turnCounterl;
                TPS = TurnsCounter - TurnsCounterOld;
                TurnsCounterOld = TurnsCounter;


                FPS = FPS_counter;
                UpdateCounters();


                OneSecondCounter = 0;
                FPS_counter = 0;
            }

            fpsStopWath.Restart();
        }

        private Size GetPicterBoxSize()
        {
            return pictureBox1.Size;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            UpdatePictureBox();
        }

        public void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            float old_scale = scale;
            if (e.Delta > 0)
                scale++;
            else
                scale--;

            if (scale < 0)
                scale = 0;
            else if (scale > maxScale)
                scale = maxScale;

            if (old_scale != scale)
            {
                if (scale > old_scale)
                {
                    CameraPos.X += (int)Math.Round(e.X / ScalesList[scale]);
                    CameraPos.Y += (int)Math.Round(e.Y / ScalesList[scale]);
                }
                else
                {
                    CameraPos.X -= (int)Math.Round(e.X / ScalesList[scale + 1]);
                    CameraPos.Y -= (int)Math.Round(e.Y / ScalesList[scale + 1]);
                }

                UpdatePictureBox();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //sourse_LB.UnlockBits();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            int x_diff, y_diff;
            if (e.Button == MouseButtons.Middle)
            {
                if ((e.X != old_x || e.Y != old_y) &&
                    (Math.Abs(old_x - e.X) > ScalesList[scale] || Math.Abs(old_y - e.Y) > ScalesList[scale]))
                {
                    float scale_flip = ScalesList[scale];

                    x_diff = old_x - e.X;
                    y_diff = old_y - e.Y;

                    if (ScalesList[scale] <= 1)
                    {
                        x_diff = (int)(x_diff / ScalesList[scale]) * CAM_SPEED;
                        y_diff = (int)(y_diff / ScalesList[scale]) * CAM_SPEED;
                    }
                    else
                    {
                        double _cam_speed = CAM_SPEED / ScalesList[scale];
                        x_diff = (int)Math.Round(x_diff * _cam_speed);
                        y_diff = (int)Math.Round(y_diff * _cam_speed);
                    }


                    old_x = e.X;
                    old_y = e.Y;

                    CameraPos.Y += y_diff;
                    CameraPos.X += x_diff;

                    UpdatePictureBox();
                    //Console.WriteLine($"diff x: {x_diff}, diff y: {y_diff}");
                }
            }
            else
            {
                old_x = e.X;
                old_y = e.Y;
            }
        }

        private void DrawTextOnBitmap(ref Bitmap img)
        {
            // this is just for test

            string text = "Scale: X" + ScalesList[scale].ToString();

            using var gr = Graphics.FromImage(img);
            FontFamily ff = new FontFamily("Segoe UI");
            Font f = new Font(ff, 12f, FontStyle.Bold);

            float xText = 25f;
            float yText = 25f;

            gr.DrawString(text, f, new SolidBrush(Color.FromArgb(160, 0, 0, 0)), xText + 2f, yText + 2f);
            gr.DrawString(text, f, new SolidBrush(Color.White), xText, yText);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (firstTime)
            {
                firstTime = false;
                MainLoop.startThred();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdatePictureBox();
        }

        private void UpdateCounters()
        {
            fps_lable.Text = $"FPS: {FPS}";
            tps_lable.Text = $"Epoch : {TurnsCounter} ({TPS}/c)";
            //total_lable.Text = total;
        }
    }
}