using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace thermal_camera_tool
{
    public partial class Form1 : Form
    {
        private const int HIGHLIGHT_COLOR_THERSHOLD = 96;
        private string _current_filename = "";
        private Image _current_img;

        public Form1()
        {
            InitializeComponent();
            pictureBox1.AllowDrop = true;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.FileName = "";
            //ofd.InitialDirectory = @"C:\";
            ofd.Filter = "BMPファイル(*.BMP)|*.BMP";
            ofd.FilterIndex = 1;
            ofd.Title = "開くファイルを選択してください";
            //ofd.RestoreDirectory = true;
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _current_filename = ofd.FileName;
                System.IO.FileStream fs = new System.IO.FileStream( ofd.FileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                System.Drawing.Image img = System.Drawing.Image.FromStream(fs);
                _current_img = img;
                pictureBox1.Image = img;
                fs.Close();

            }
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = _current_filename;
            //はじめに表示されるフォルダを指定する
            sfd.InitialDirectory = @"C:\";
            sfd.Filter = "BMPファイル(*.BMP)|*.BMP";
            sfd.FilterIndex = 1;
            sfd.Title = "保存先のファイルを選択してください";
            sfd.RestoreDirectory = true;
            sfd.OverwritePrompt = true;
            sfd.CheckPathExists = true;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                //OKボタンがクリックされたとき、選択されたファイル名を表示する
                if (_current_filename != "")
                {
                    Bitmap bmp = new Bitmap(_current_img);
                    bmp.Save(sfd.FileName, ImageFormat.Bmp);
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_current_filename != "" && System.IO.File.Exists(_current_filename))
            {
                Bitmap bmp = new Bitmap(_current_img);
                bmp.Save(_current_filename, ImageFormat.Bmp);
            }
        }

        private void highlightText(int top, int left, int width, int height)
        {
            if (_current_filename == "")
            {
                return;
            }
            Bitmap bitmap = new Bitmap(_current_img);
            PixelFormat pixelFormat = PixelFormat.Format32bppArgb;
            int pixelSize = 4;
            BitmapData bmpData = bitmap.LockBits(
              new Rectangle(0, 0, bitmap.Width, bitmap.Height),
              ImageLockMode.ReadWrite,
              pixelFormat
            );
            IntPtr ptr = bmpData.Scan0;
            byte[] pixels = new byte[bmpData.Stride * bitmap.Height];
            System.Runtime.InteropServices.Marshal.Copy(ptr, pixels, 0, pixels.Length);
            // pixcel color replace
            for (int y = top; y < top + height; y++)
            {
                for (int x = left; x < left + width; x++)
                {
                    int pos = y * bmpData.Stride + x * pixelSize;

                    byte b = pixels[pos + 0];
                    byte g = pixels[pos + 1];
                    byte r = pixels[pos + 2];

                    int zerocount = (b == 0 ? 1 : 0) + (g == 0 ? 1 : 0) + (r == 0 ? 1 : 0);
                    bool pure_color = (b > 128 & g == 0 & r == 0) | (b == 0 & g > 128 & r == 0) | (b == 0 & g == 0 & r > 128);

                    if ((b < HIGHLIGHT_COLOR_THERSHOLD &&
                        g < HIGHLIGHT_COLOR_THERSHOLD &&
                        r < HIGHLIGHT_COLOR_THERSHOLD) || (zerocount >= 2 && !pure_color))
                    {
                        b = 255;
                        g = 255;
                        r = 255;
                    }

                    pixels[pos + 0] = b;
                    pixels[pos + 1] = g;
                    pixels[pos + 2] = r;
                }
            }

            // 変更を（した場合）反映
            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, ptr, pixels.Length);
            bitmap.UnlockBits(bmpData);
            _current_img = bitmap;
            pictureBox1.Image = bitmap;
        }


        private void highlightTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            highlightText(0, 0, _current_img.Width, _current_img.Height);
        }

        private void pictureBox1_DragDrop(object sender, DragEventArgs e)
        {
            string[] fileName = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (Path.GetExtension(fileName[0]).ToLower() != ".bmp")
            {
                return;
            }
            _current_filename = fileName[0];
            System.IO.FileStream fs = new System.IO.FileStream(_current_filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            System.Drawing.Image img = System.Drawing.Image.FromStream(fs);
            _current_img = img;
            pictureBox1.Image = img;
            fs.Close();
        }

        private void pictureBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int scale = pictureBox1.Width / _current_img.Width;
            int threshold_vert = 40;
            int threshold_hori = 100;
            int threshold_cent = 40;
            int top_offset = 10;

            int width = pictureBox1.Width / scale;
            int height = pictureBox1.Height / scale;

            int x = e.X / scale;
            int y = e.Y / scale;

            //top bottom
            if (y < threshold_vert)
            {
                if (x < threshold_hori)
                {
                    //top-left
                    highlightText(top_offset, 0, threshold_hori, threshold_vert);
                } 
                else if (width - threshold_hori < x)
                {
                    //top-right
                    highlightText(top_offset, width - threshold_hori, threshold_hori, threshold_vert);
                }
            } 
            else if (height - threshold_vert < y)
            {
                if (x < threshold_hori)
                {
                    //bottom-left
                    highlightText(height - threshold_vert, 0, threshold_hori, threshold_vert);
                }
                else if (width - threshold_hori < x)
                {
                    //bottom-right
                    highlightText(height - threshold_vert, width - threshold_hori, threshold_hori, threshold_vert);
                } 
                else if(width/2-threshold_cent < x && x < width/2+threshold_cent)
                {
                    //bottom-center
                    highlightText(height - threshold_vert, width/2 - threshold_cent, threshold_cent*2, threshold_vert);
                }
            }

        }

        private void resetImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.IO.FileStream fs = new System.IO.FileStream(_current_filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            System.Drawing.Image img = System.Drawing.Image.FromStream(fs);
            _current_img = img;
            pictureBox1.Image = img;
            fs.Close();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
