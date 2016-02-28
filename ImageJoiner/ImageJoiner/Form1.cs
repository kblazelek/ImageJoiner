using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageJoiner
{
    public partial class Form1 : Form
    {
        protected bool validData;
        int currentListIndex = 0;
        Point startingPoint = Point.Empty;
        Bitmap finalImage;
        Point finallImagePosition = Point.Empty;
        Point movingPoint = Point.Empty;
        bool panning = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void listViewImages_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length != 0)
                {
                    validData = true;
                }
                else
                {
                    validData = false;
                }
                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    if (fi.Extension != ".jpg" && fi.Extension != ".png" && fi.Extension != ".bmp")
                    {
                        validData = false;
                    }

                    if (validData)
                    {
                        e.Effect = DragDropEffects.Copy;
                    }
                    else
                        e.Effect = DragDropEffects.None;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void listViewImages_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (validData)
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    foreach (string file in files)
                    {
                        FileInfo fi = new FileInfo(file);
                        imagesList.Images.Add(Image.FromFile(file));
                        ListViewItem item = new ListViewItem();
                        item.Text = file;
                        item.ImageIndex = currentListIndex;
                        ++currentListIndex;
                        listViewImages.Items.Add(item);
                    }
                }
            }
        }

        private void buttonJoinImages_Click(object sender, EventArgs e)
        {
            int imagesCount = listViewImages.Items.Count;
            string[] files;
            if (imagesCount > 0)
            {
                files = new string[imagesCount];
                files = listViewImages.Items.Cast<ListViewItem>().Select(item => item.Text).ToArray();
                Array.Sort(files);
                pictureBoxFinallImage.Image = joinImages(files); // dodac sprawdzanie czy files wypelnione
            }
            else
            {
                MessageBox.Show("There are no images to join.");
            }
        }
        // Dodać sprawdzanie nazw i rozmiarów obrazu
        private Bitmap joinImages(string[] files)
        {
            // Images contain images, rectangles contain image positions
            List<Tuple<Image, Rectangle>> imagesWithRectangles = new List<Tuple<Image, Rectangle>>();
            string fileNameWithoutExtension;
            int row, column;
            int xPosition = 0;
            int yPosition = 0;

            try
            {
                int width = 0;
                int height = 0;

                foreach (string image in files)
                {
                    fileNameWithoutExtension = Path.GetFileNameWithoutExtension(image);
                    row = Int32.Parse(fileNameWithoutExtension.Split('_')[0]);
                    column = Int32.Parse(fileNameWithoutExtension.Split('_')[1]);
                    //create a Bitmap from the file and add it to the list
                    Bitmap bitmap = new Bitmap(image);

                    //update the size of the final bitmap
                    xPosition = column * bitmap.Width;
                    yPosition = row * bitmap.Height;
                    if (xPosition + bitmap.Width > width) width = xPosition + bitmap.Width;
                    if (yPosition + bitmap.Height > height) height = yPosition + bitmap.Height;
                    Tuple<Image, Rectangle> imageWithRectangle = new Tuple<Image, Rectangle>(bitmap, new Rectangle(xPosition, yPosition, bitmap.Width, bitmap.Height));
                    imagesWithRectangles.Add(imageWithRectangle);
                }

                //create a bitmap to hold the combined image
                finalImage = new Bitmap(width, height);

                //get a graphics object from the image so we can draw on it
                using (Graphics g = Graphics.FromImage(finalImage))
                {
                    //set background color
                    g.Clear(Color.Black);

                    //go through each image and draw it on the final image
                    foreach (var imageWithRectangle in imagesWithRectangles)
                    {
                        g.DrawImage(imageWithRectangle.Item1, imageWithRectangle.Item2);
                    }
                }

                return finalImage;
            }
            catch (Exception ex)
            {
                if (finalImage != null)
                    finalImage.Dispose();

                throw ex;
            }
            finally
            {
                //clean up memory
                for (int i = 0; i < imagesWithRectangles.Count; ++i)
                {
                    var tempTuple = imagesWithRectangles.ElementAt(i);
                    tempTuple.Item1.Dispose();
                    tempTuple = null;
                }
            }
        }

        private void pictureBoxFinallImage_MouseDown(object sender, MouseEventArgs e)
        {
            panning = true;
            startingPoint = e.Location;
        }

        private void pictureBoxFinallImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (panning && (finalImage != null))
            {
                movingPoint = new Point(finallImagePosition.X + e.Location.X - startingPoint.X,
                                        finallImagePosition.Y + e.Location.Y - startingPoint.Y);
                finallImagePosition = new Point(finallImagePosition.X + e.Location.X - startingPoint.X, finallImagePosition.Y + e.Location.Y - startingPoint.Y);
                pictureBoxFinallImage.Invalidate();
            }
        }

        private void pictureBoxFinallImage_MouseUp(object sender, MouseEventArgs e)
        {
            panning = false;
        }

        private void pictureBoxFinallImage_Paint(object sender, PaintEventArgs e)
        {
            if (finalImage != null)
            {
                e.Graphics.Clear(Color.Black);
                e.Graphics.DrawImage(finalImage, movingPoint);
            }
        }
    }
}
