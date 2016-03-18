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
using ImageJoiner.CustomExceptions;
using System.Text.RegularExpressions;

namespace ImageJoiner
{
    public partial class Form1 : Form
    {
        // sprawdzic czy nie powalilem rows z columns
        bool validData;
        int smallImageWidth = 0;
        int smallImageHeight = 0;
        int currentRow = 0;
        int currentColumn = 0;
        int maxWidth = 0;
        int maxHeight = 0;
        int bufferColumns = 0;
        int bufferRows = 0;
        Point startingPoint = Point.Empty;
        Bitmap bufferImage;
        Point movingPoint = Point.Empty;
        Point positionInWholeImage = Point.Empty;
        bool panning = false;
        public Form1()
        {
            InitializeComponent();
        }

        // Zdarzenie, które ma miejsce gdy użytkownik przeciągnię jeden lub więcej plików na listę, ale jeszcze nie upuści ich.
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
                    if (fi.Extension != ".jpg" && fi.Extension != ".png" && fi.Extension != ".bmp" && fi.Extension != ".TIF")
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

        // Zdarzenie, które ma miejsce, gdy użytkownik upuści jeden lub więcej plików na listę
        private void listViewImages_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (validData)
                {
                    try
                    {
                        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                        Array.Sort(files);
                        int filesCount = files.Count(s => s != null);
                        ListViewItem[] items = new ListViewItem[filesCount];
                        Image[] images = new Image[filesCount];
                        int currentListIndex = listViewImages.Items.Count;
                        if (listViewImages.Items.Count > 0)
                        {
                            var tempImage = Image.FromFile(listViewImages.Items[0].Text);
                            smallImageWidth = tempImage.Width;
                            smallImageHeight = tempImage.Height;
                        }
                        else
                        {
                            var tempImage = Image.FromFile(files[0]);
                            smallImageWidth = tempImage.Width;
                            smallImageHeight = tempImage.Height;
                        }
                        progressBar.Visible = true;
                        progressBar.Minimum = 1;
                        progressBar.Maximum = filesCount;
                        progressBar.Value = 1;
                        progressBar.Step = 1;
                        int tempMaxRowNumber = 0;
                        int tempMaxColumnNumber = 0;
                        int tempRowNumber = 0;
                        int tempColumnNumber = 0;
                        for (int i = 0; i < filesCount; ++i)
                        {
                            FileInfo fi = new FileInfo(files[i]);
                            if(!validateFileName(Path.GetFileNameWithoutExtension(files[i])))
                            {
                                throw new WrongFileNameException(String.Format("File \"{0}\" does not match the following expression:\n YnnnnXnnnn.extension (for example Y0000X0000.png)", Path.GetFileName(files[i])));
                            }
                            images[i] = Image.FromFile(files[i]);
                            if((images[i].Height != smallImageHeight) ||(images[i].Width != smallImageWidth))
                            {
                                throw new ImagesDifferentSizeExcpetion("Images are not the same size");
                            }
                            tempRowNumber = this.GetRowFromFileName(Path.GetFileNameWithoutExtension(files[i]));
                            tempColumnNumber = this.GetColumnFromFileName(Path.GetFileNameWithoutExtension(files[i]));
                            if (tempMaxRowNumber < tempRowNumber)
                            {
                                tempMaxRowNumber = tempRowNumber;
                            }
                            if(tempMaxColumnNumber < tempColumnNumber)
                            {
                                tempMaxColumnNumber = tempColumnNumber;
                            }
                            ListViewItem item = new ListViewItem();
                            item.Text = files[i];
                            item.ImageIndex = currentListIndex;
                            ++currentListIndex;
                            items[i] = item;
                            progressBar.PerformStep();
                        }
                        maxWidth = (tempMaxRowNumber + 1) * smallImageWidth;
                        maxHeight = (tempMaxColumnNumber + 1) * smallImageHeight;
                        listViewImages.Items.AddRange(items);
                        imagesList.Images.AddRange(images);
                        progressBar.Visible = false;
                    }
                    catch(WrongFileNameException wrongFileNameException)
                    {
                        MessageBox.Show(wrongFileNameException.Message);
                    }
                    catch(ImagesDifferentSizeExcpetion imagesDifferentSizeExcpetion)
                    {
                        MessageBox.Show(imagesDifferentSizeExcpetion.Message);
                    }
                    catch(Exception exception)
                    {
                        MessageBox.Show(exception.Message);
                    }
                    finally
                    {
                        progressBar.Visible = false;
                    }
                }
            }
        }

        // Zdarzenie, które ma miejsce po kliknięciu przycisku "Join Images". Jeżeli istnieją obrazy w liście, to wywołuje funkcję łączącą je.
        private void buttonJoinImages_Click(object sender, EventArgs e)
        {
            int imagesCount = listViewImages.Items.Count;
            if (imagesCount > 0)
            {
                //files = listViewImages.Items.Cast<ListViewItem>().Select(item => item.Text).ToArray();
                pictureBoxFinallImage.Image = joinImages(0,0);
            }
            else
            {
                MessageBox.Show("There are no images to join.");
            }
        }
        // Funkcja łącząca obrazki w 1.
        private Bitmap joinImages(int startRowNumber, int startColumnNumber)
        {
            // Images contain images, rectangles contain image positions
            List<Tuple<Image, Rectangle>> imagesWithRectangles = new List<Tuple<Image, Rectangle>>();
            CalculateImagesToExceedWidthAndHeight();
            List<string> files = GetBufferImages();
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
                    row = GetRowFromFileName(fileNameWithoutExtension);
                    column = GetColumnFromFileName(fileNameWithoutExtension);
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
                bufferImage = new Bitmap(width, height);

                //get a graphics object from the image so we can draw on it
                using (Graphics g = Graphics.FromImage(bufferImage))
                {
                    //set background color
                    g.Clear(Color.Black);

                    //go through each image and draw it on the final image
                    foreach (var imageWithRectangle in imagesWithRectangles)
                    {
                        g.DrawImage(imageWithRectangle.Item1, imageWithRectangle.Item2);
                    }
                }

                return bufferImage;
            }
            catch (Exception ex)
            {
                if (bufferImage != null)
                    bufferImage.Dispose();

                throw ex;
            }
            finally
            {
                for (int i = 0; i < imagesWithRectangles.Count; ++i)
                {
                    var tempTuple = imagesWithRectangles.ElementAt(i);
                    tempTuple.Item1.Dispose();
                    tempTuple = null;
                }
            }
        }

        // Włącza tryb przeciągania (panning = true), który umożliwia przeglądanie zdjęcia
        private void pictureBoxFinallImage_MouseDown(object sender, MouseEventArgs e)
        {
            panning = true;
            startingPoint = e.Location;
        }

        // Wylicza którą część obrazu wyświetlić i wywołuje metodę odpowiedzialną za rysowanie obrazu
        private void pictureBoxFinallImage_MouseMove(object sender, MouseEventArgs e)
        {
            // Wziąć pod uwagę maxWidht i maxHeight, odświeżyć bufor
            if (panning && (bufferImage != null))
            {
                CalulatePositionInWholeImage(e.Location);
                pictureBoxFinallImage.Invalidate(); // Wywołuje zdarzenie pictureBoxFinallImage_Paint
                int tempRow = currentRow;
                int tempColumn = currentColumn;
                CalculateRowAndColumnBasedOnPosition();
                if(tempRow != currentRow || tempColumn != currentColumn)
                {
                    pictureBoxFinallImage.Image = joinImages(currentRow, currentColumn);
                    pictureBoxFinallImage.Invalidate();
                }
            }
        }

        // Oblicza pozycję lewego górnego rogu małego obrazka w odniesieniu do całego obrazka
        public void CalulatePositionInWholeImage(Point currentPosition)
        {
            int newX = movingPoint.X + currentPosition.X - startingPoint.X;
            int newY = movingPoint.Y + currentPosition.Y - startingPoint.Y;
            if (newX < (-bufferImage.Width + pictureBoxFinallImage.Width)) newX = (-bufferImage.Width + pictureBoxFinallImage.Width);
            else if (newX > 0) newX = 0;
            if (newY < (-bufferImage.Height + pictureBoxFinallImage.Height)) newY = (-bufferImage.Height + pictureBoxFinallImage.Height);
            else if (newY > 0) newY = 0;
            positionInWholeImage = new Point(newX, newY);
        }

        // Po puszczeniu przycisku myszy obraz nie ma być dalej przesuwany
        private void pictureBoxFinallImage_MouseUp(object sender, MouseEventArgs e)
        {
            panning = false;
        }

        // Rysuje wyświetlaną część obrazu
        private void pictureBoxFinallImage_Paint(object sender, PaintEventArgs e)
        {
            if (bufferImage != null)
            {
                e.Graphics.Clear(Color.Black);
                e.Graphics.DrawImage(bufferImage, positionInWholeImage);
            }
        }

        // Spradza, czy podany plik ma nazwę w następującym formacie: YnnnnXnnnn, np Y0001X0001
        public bool validateFileName(string filename)
        {
            string pattern = @"Y\d{4}X\d{4}$";
            Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(filename);
            if (matches.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Zwraca numer kolumny na podstawie nazwy pliku
        public int GetColumnFromFileName(string fileName)
        {
            return int.Parse(fileName.Substring(1, 4)); ;
        }

        // Zwraca numer wiersza na podstawie nazwy pliku
        public int GetRowFromFileName(string fileName)
        {
            return int.Parse(fileName.Substring(6, 4)); ;
        }

        // Returns locations of images used in buffer
        public List<string> GetBufferImages()
        {
            List<string> fileNames = new List<string>();
            string fileNameToFind = "";
            string fileNameFound = "";
            ListViewItem listViewItem;
            for(int i = currentRow; i<= (currentRow + bufferColumns); ++i)
            {
                for(int j = currentColumn; j<=(currentColumn +bufferRows); ++j)
                {
                    fileNameFound = "";
                    fileNameToFind = "Y" + j.ToString().PadLeft(4, '0') + "X" + i.ToString().PadLeft(4, '0');
                    listViewItem =  listViewImages.Items.Cast<ListViewItem>().FirstOrDefault(item => Path.GetFileNameWithoutExtension(item.Text) == fileNameToFind);
                    if(listViewItem != null)
                    {
                        fileNames.Add(listViewItem.Text);
                    }
                    listViewItem = null;
                }
            }
            return fileNames;
        }

        // Oblicza wiersz i kolumnę na podstawie pozycji lewego górnego rogu wyświetlanej części obrazu (movingPoint)
        private void CalculateRowAndColumnBasedOnPosition()
        {
            if(positionInWholeImage.X <= smallImageWidth)
            {
                if(positionInWholeImage.Y <= smallImageHeight)
                {
                    currentRow = 0;
                    currentColumn = 0;
                }
                else
                {
                    currentRow = 0;
                    currentColumn = positionInWholeImage.Y / smallImageHeight + 1;
                }
            }
            else
            {
                if(positionInWholeImage.Y <= smallImageHeight)
                {
                    currentRow = positionInWholeImage.X / smallImageWidth + 1;
                    currentColumn = 0;
                }
                else
                {
                    currentRow = positionInWholeImage.X / smallImageWidth + 1;
                    currentColumn = positionInWholeImage.Y / smallImageHeight + 1;
                }
            }
        }
        // Liczy ile obrazów jest potrzebnych, aby były dłuższe w osi x i y od obszaru wyświetlania. Przdatne przy wyliczaniu bufora na obrazy
        public void CalculateImagesToExceedWidthAndHeight()
        {
            // dodac sprawdzanie dzielenie przez 0
            if(smallImageWidth > pictureBoxFinallImage.Width)
            {
                if(smallImageHeight > pictureBoxFinallImage.Height)
                {
                    bufferColumns = 2;
                    bufferRows = 2;
                }
                else
                {
                    bufferColumns = 2;
                    bufferRows = pictureBoxFinallImage.Height / smallImageHeight + 2;
                }
            }
            else
            {
                if (smallImageHeight > pictureBoxFinallImage.Height)
                {
                    bufferColumns = pictureBoxFinallImage.Width / smallImageWidth + 2;
                    bufferRows = 2;
                }
                else
                {
                    bufferColumns = pictureBoxFinallImage.Width / smallImageWidth + 2;
                    bufferRows = pictureBoxFinallImage.Height / smallImageHeight + 2;
                }
            }
        }

        // Wywołuje metodę, która czyści listę obrazów, wyświetlany obraz i zmienne pomocnicze
        private void buttonRemoveImages_Click(object sender, EventArgs e)
        {
            RemoveAllImages();
        }

        // Czyści listę obrazów, wyświetlany obraz i zmienne pomocnicze
        private void RemoveAllImages()
        {
            if (listViewImages.Items.Count != 0)
            {
                listViewImages.Dispose();
                imagesList.Dispose();
                if (bufferImage != null)
                {
                    bufferImage.Dispose();
                }
                if (pictureBoxFinallImage.Image != null)
                {
                    pictureBoxFinallImage.Image.Dispose();
                    pictureBoxFinallImage.Invalidate(); // Odświeża obraz
                }
                startingPoint = Point.Empty;
                movingPoint = Point.Empty;
                positionInWholeImage = Point.Empty;
                smallImageWidth = 0;
                smallImageHeight = 0;
                bufferColumns = 0;
                bufferRows = 0;
                currentRow = 0;
                currentColumn = 0;
                maxWidth = 0;
                maxHeight = 0;
            }
            else
            {
                MessageBox.Show("There are no images to remove");
            }
        }
    }
}
