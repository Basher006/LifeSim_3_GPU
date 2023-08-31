namespace LifeSim_3_GPU
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            pictureBox1 = new PictureBox();
            timer1 = new System.Windows.Forms.Timer(components);
            fps_lable = new Label();
            tps_lable = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureBox1.Location = new Point(12, 27);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(776, 411);
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            pictureBox1.MouseMove += pictureBox1_MouseMove;
            // 
            // timer1
            // 
            timer1.Enabled = true;
            timer1.Interval = 1;
            timer1.Tick += timer1_Tick;
            // 
            // fps_lable
            // 
            fps_lable.AutoSize = true;
            fps_lable.Location = new Point(12, 9);
            fps_lable.Name = "fps_lable";
            fps_lable.Size = new Size(50, 15);
            fps_lable.TabIndex = 1;
            fps_lable.Text = "FPS: 000";
            // 
            // tps_lable
            // 
            tps_lable.AutoSize = true;
            tps_lable.Location = new Point(68, 9);
            tps_lable.Name = "tps_lable";
            tps_lable.Size = new Size(98, 15);
            tps_lable.TabIndex = 2;
            tps_lable.Text = "Turns : 0000 (000)";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(tps_lable);
            Controls.Add(fps_lable);
            Controls.Add(pictureBox1);
            Name = "Form1";
            Text = "Form1";
            FormClosing += Form1_FormClosing;
            KeyDown += Form1_KeyDown;
            Resize += Form1_Resize;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pictureBox1;
        private System.Windows.Forms.Timer timer1;
        private Label fps_lable;
        private Label tps_lable;
    }
}