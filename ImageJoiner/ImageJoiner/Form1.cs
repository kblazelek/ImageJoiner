using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ImageJoiner.CustomExceptions;
using System.Text.RegularExpressions;
using System.Threading;

namespace ImageJoiner
{
    public enum RowAndColumnNumeration
    {
        XrightYdown,
        XrightYup,
        XleftYdown,
        XleftYup
    }
    public partial class Form1 : Form
    {
        private bool validData;
        private int smallImageWidth = 0;
        private int smallImageHeight = 0;
        private int currentRow = 0;
        private int currentColumn = 0;
        private int maxWidth = 0;
        private int maxHeight = 0;
        private int bufferColumns = 0;
        private int bufferRows = 0;
        private int maxRowNumber = 0;
        private int maxColumnNumber = 0;
        private Point panningStartingPoint = Point.Empty;
        private Point pictureBoxPositionRelatedToWholePicture = Point.Empty;
        private Point pictureBoxPositionRelatedToBuffer = Point.Empty;
        private Bitmap bufferImage;
        private Bitmap previewImage;
        private bool panning = false;
        RowAndColumnNumeration rowAndColumnNumeration;
        FormAskForRowAndColumnNumeration formAskForRowAndColumnNumeration;
        Object progressBarLock = new Object();
        Object finallImageVariablesLock = new Object();
        Object imagesLock = new Object();
        Object itemsLock = new Object();
        List<Image> tempImages;
        List<ListViewItem> tempItems;
        Boolean droppedImagesSuccessfulllyLoaded = true;
        int tempMaxColumnNumber = 0;
        int tempMaxRowNumber = 0;
        int tempMaxHeight = 0;
        int tempMaxWidth = 0;
        #region Accessors

        public int MaxWidth
        {
            get { return maxWidth; }
            set
            {
                maxWidth = value;
                labelWidth.Text = "Width: " + MaxWidth.ToString();
            }
        }

        public int MaxHeight
        {
            get { return maxHeight; }
            set
            {
                maxHeight = value;
                labelHeight.Text = "Height: " + MaxHeight.ToString();
            }
        }

        public int MaxRowNumber
        {
            get { return maxRowNumber; }
            set
            {
                maxRowNumber = value;
                if (listViewImages.Items.Count > 0)
                {
                    labelRows.Text = "Rows: " + (MaxRowNumber + 1).ToString();
                }
                else
                {
                    labelRows.Text = "Rows: 0";
                }
            }
        }

        public int MaxColumnNumber
        {
            get { return maxColumnNumber; }
            set
            {
                maxColumnNumber = value;
                if (listViewImages.Items.Count > 0)
                {
                    labelColumns.Text = "Columns: " + (MaxColumnNumber + 1).ToString();
                }
                else
                {
                    labelColumns.Text = "Columns: 0";
                }
            }
        }

        #endregion
        #region Constructor
        public Form1()
        {
            InitializeComponent();
        }
        #endregion
        #region Event handlers
        /// <summary>
        /// Zdarzenie, które ma miejsce gdy użytkownik przeciągnię jeden lub więcej plików na listę, ale jeszcze nie upuści ich.
        /// Wyświetla ikonkę informującą o tym, czy pliki można upuścić lub nie.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Zdarzenie, które ma miejsce, gdy użytkownik upuści jeden lub więcej obrazów na listę. Sprawdza, czy obrazy mają takie same wymiary.
        /// Gdy wszystkie obrazy są poprawne, to ścieżki do nich wraz z minuaturkami obrazów są dodawane do list.
        /// Dodatkowo pyta się o rodzaj numeracji wierszy i kolumn
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewImages_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (validData)
                {
                    if (listViewImages.Items.Count == 0)
                    {
                        formAskForRowAndColumnNumeration = new FormAskForRowAndColumnNumeration();
                        formAskForRowAndColumnNumeration.StartPosition = FormStartPosition.Manual;
                        formAskForRowAndColumnNumeration.Location = this.PointToScreen(Point.Empty);
                        var result = formAskForRowAndColumnNumeration.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            this.rowAndColumnNumeration = formAskForRowAndColumnNumeration.rowAndColumnNumeration;
                        }
                        else
                        {
                            this.rowAndColumnNumeration = RowAndColumnNumeration.XrightYdown;
                        }
                        formAskForRowAndColumnNumeration.Close();
                    }
                    string[] filesArray = (string[])e.Data.GetData(DataFormats.FileDrop);
                    List<string> files = filesArray.ToList<string>();
                    int filesCount = files.Count(s => s != null);
                    try
                    {
                        int currentListIndex = listViewImages.Items.Count;
                        if (listViewImages.Items.Count > 0)
                        {
                            var tempImage = Image.FromFile(listViewImages.Items[0].Text);
                            smallImageWidth = tempImage.Width;
                            smallImageHeight = tempImage.Height;
                            tempImage.Dispose();
                            tempImage = null;
                        }
                        else
                        {
                            var tempImage = Image.FromFile(files[0]);
                            smallImageWidth = tempImage.Width;
                            smallImageHeight = tempImage.Height;
                            tempImage.Dispose();
                            tempImage = null;
                        }
                        progressBar.Visible = true;
                        progressBar.Minimum = 1;
                        progressBar.Maximum = filesCount;
                        progressBar.Value = 1;
                        progressBar.Step = 1;
                        int neededProcessors = 0;
                        int numberOfImagesPerCore = 0;
                        int remainedImages = 0;
                        tempMaxColumnNumber = MaxColumnNumber;
                        tempMaxRowNumber = MaxRowNumber;
                        tempMaxWidth = MaxWidth;
                        tempMaxHeight = MaxHeight;
                        tempImages = new List<Image>();
                        tempItems = new List<ListViewItem>();
                        // Obsłużyć wyjątki
                        // Gdy liczba obrazow do zaladowania jest mala wykonaj wszystko na 1 rdzeniu, w przeciwnym wypadku użyj wszystkich rdzeni
                        if (filesCount <= Environment.ProcessorCount)
                        {
                            neededProcessors = 1;
                            numberOfImagesPerCore = filesCount;
                            remainedImages = 0;
                        }
                        else
                        {
                            neededProcessors = Environment.ProcessorCount;
                            numberOfImagesPerCore = filesCount / Environment.ProcessorCount;
                            remainedImages = filesCount % Environment.ProcessorCount != 0 ? filesCount % Environment.ProcessorCount : 0;
                        }
                        List<Thread> threads = new List<Thread>();
                        int startingIndexOnList = 0;
                        KeyValuePair<List<string>, int> pair;
                        droppedImagesSuccessfulllyLoaded = true;
                        for (int i = 0; i < neededProcessors; ++i)
                        {
                            startingIndexOnList = i * numberOfImagesPerCore + currentListIndex;
                            if (i < neededProcessors - 1)
                            {
                                pair = new KeyValuePair<List<string>, int>(files.GetRange(i * numberOfImagesPerCore, numberOfImagesPerCore), startingIndexOnList);
                                threads.Add(new Thread(new ParameterizedThreadStart(LoadDroppedImages)));
                                threads.Last().Start(pair);
                            }
                            else
                            {
                                pair = new KeyValuePair<List<string>, int>(files.GetRange(i * numberOfImagesPerCore, numberOfImagesPerCore + remainedImages), startingIndexOnList);
                                threads.Add(new Thread(new ParameterizedThreadStart(LoadDroppedImages)));
                                threads.Last().Start(pair);
                            }
                        }
                        var waitBg = new Thread(() =>
                        {
                            foreach (var thread in threads)
                                thread.Join();
                            progressBar.Invoke(new Action(() =>
                            {
                                if (droppedImagesSuccessfulllyLoaded)
                                {
                                    imagesList.Images.AddRange(tempImages.ToArray());
                                    listViewImages.Items.AddRange(tempItems.ToArray());
                                    MaxRowNumber = tempMaxRowNumber;
                                    MaxColumnNumber = tempMaxColumnNumber;
                                    MaxHeight = tempMaxHeight;
                                    MaxWidth = tempMaxWidth;
                                }
                                tempItems.Clear();
                                tempItems = null;
                                tempImages.Clear();
                                tempImages = null;
                                tempMaxRowNumber = 0;
                                tempMaxColumnNumber = 0;
                                tempMaxWidth = 0;
                                tempMaxHeight = 0;
                                progressBar.Visible = false;
                            }));
                        }
                        );
                        waitBg.Start();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("lol");
                    }
                    finally
                    {
                        if (formAskForRowAndColumnNumeration != null)
                        {
                            formAskForRowAndColumnNumeration.Dispose();
                            formAskForRowAndColumnNumeration = null;
                        }
                        if (files != null) files = null;
                    }
                }
            }
        }

        /// <summary>
        /// Zdarzenie, które ma miejsce po kliknięciu przycisku "Join Images". Wywołuje funkcję łączącą obrazy.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJoinImages_Click(object sender, EventArgs e)
        {
            if (listViewImages.Items.Count > 0)
            {
                InitializePreviewImage();
                JoinImages();
            }
            else
            {
                MessageBox.Show("There are no images to join.");
            }
        }


        /// <summary>
        /// Włącza tryb przeciągania (panning = true), który umożliwia przeglądanie obrazu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBoxFinallImage_MouseDown(object sender, MouseEventArgs e)
        {
            panning = true;
            panningStartingPoint = e.Location;
        }

        /// <summary>
        /// Zdarzenie, na skutek którego zostaje przesunięta wyświetlana część obrazka zgodnie z ruchem myszki.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBoxFinallImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (panning && (bufferImage != null))
            {
                CalulatePositionInWholeImage(e.Location);
                int tempRow = currentRow;
                int tempColumn = currentColumn;
                CalculateRowAndColumnBasedOnCurrentPosition();
                if (tempRow != currentRow || tempColumn != currentColumn)
                {
                    LoadImagesIntoBuffer();
                }
                CalculatePictureBoxPositionRelatedToBuffer();
                pictureBoxFinallImage.Invalidate(); // Ponownie rysuje obraz
                pictureBoxPreview.Invalidate();
            }
        }
        /// <summary>
        /// Zdarzenie wywoływane po puszczeniu przycisku myszy, na skutek którego obraz nie ma być dalej przesuwany.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBoxFinallImage_MouseUp(object sender, MouseEventArgs e)
        {
            panning = false;
        }

        /// <summary>
        /// Rysuje wyświetlaną część obrazu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBoxFinallImage_Paint(object sender, PaintEventArgs e)
        {
            if (bufferImage != null)
            {
                e.Graphics.Clear(Color.Black);
                e.Graphics.DrawImage(bufferImage, new Point(-pictureBoxPositionRelatedToBuffer.X, -pictureBoxPositionRelatedToBuffer.Y));
            }
        }
        /// <summary>
        /// Rysuje podgląd.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBoxPreview_Paint(object sender, PaintEventArgs e)
        {
            if (previewImage != null)
            {
                e.Graphics.Clear(Color.White);
                Pen blackPen = new Pen(Color.Red, 1);
                int rectangleWidth = maxWidth > pictureBoxFinallImage.Width ?
                    pictureBoxFinallImage.Width * pictureBoxPreview.Width / maxWidth : pictureBoxPreview.Width - 1;
                int rectangleHeight = maxHeight > pictureBoxFinallImage.Height ?
                    pictureBoxFinallImage.Height * pictureBoxPreview.Height / maxHeight : pictureBoxPreview.Height - 1;
                int rectangleX = pictureBoxPositionRelatedToWholePicture.X * pictureBoxPreview.Width / maxWidth;
                int rectangleY = pictureBoxPositionRelatedToWholePicture.Y * pictureBoxPreview.Height / maxHeight;
                Rectangle rect = new Rectangle(rectangleX, rectangleY, rectangleWidth, rectangleHeight);
                e.Graphics.DrawRectangle(blackPen, rect);
            }
        }
        /// <summary>
        /// Wywołuje metodę, która czyści listę obrazów, bufor i zmienne pomocnicze.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRemoveImages_Click(object sender, EventArgs e)
        {
            RemoveAllImages();
        }
        /// <summary>
        /// Zdarzenie wywoływane po zmianie rozmiaru wyświetlanej części ekranu.
        /// Powoduje ponowne przeliczenie wszystkich zmiennych pomocnicznych przy przewijaniu ekranu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBoxFinallImage_SizeChanged(object sender, EventArgs e)
        {
            JoinImages();
        }
        #endregion
        #region Helper methods
        /// <summary>
        /// Ładuje zakres upuszczonych obrazkow do list.
        /// </summary>
        /// <param name="imagesToLoad"></param>
        /// <param name="startingIndexOnList"></param>
        private void LoadDroppedImages(object obj)
        {
            KeyValuePair<List<string>, int> pair = (KeyValuePair<List<string>, int>)obj;
            List<string> imagesToLoad = pair.Key;
            int startingIndexOnList = pair.Value;
            List<ListViewItem> items = new List<ListViewItem>();
            List<Image> images = new List<Image>();
            int i = startingIndexOnList;
            int tempRowNumber = 0;
            int tempColumnNumber = 0;
            int localTempMaxColumnNumber = 0;
            int localTempMaxRowNumber = 0;
            try
            {
                foreach (string image in imagesToLoad)
                {
                    if (!ValidateFileName(Path.GetFileNameWithoutExtension(image)))
                    {
                        throw new WrongFileNameException(String.Format("File \"{0}\" does not match the following expression:\n YnnnnXnnnn.extension (for example Y0000X0000.png)", Path.GetFileName(image)));
                    }
                    images.Add(Image.FromFile(image));
                    if ((images.Last().Height != smallImageHeight) || (images.Last().Width != smallImageWidth))
                    {
                        throw new ImagesDifferentSizeExcpetion("Images are not the same size");
                    }
                    tempRowNumber = this.GetRowFromFileName(Path.GetFileNameWithoutExtension(image));
                    tempColumnNumber = this.GetColumnFromFileName(Path.GetFileNameWithoutExtension(image));
                    if (localTempMaxRowNumber < tempRowNumber)
                    {
                        localTempMaxRowNumber = tempRowNumber;
                    }
                    if (localTempMaxColumnNumber < tempColumnNumber)
                    {
                        localTempMaxColumnNumber = tempColumnNumber;
                    }
                    ListViewItem item = new ListViewItem();
                    item.Text = image;
                    item.ImageIndex = i;
                    items.Add(item);
                    ++i;
                    lock (progressBarLock)
                    {
                        progressBar.Invoke(new Action(() => progressBar.PerformStep()));
                    }
                }
                lock (finallImageVariablesLock)
                {
                    this.Invoke(new MethodInvoker(() =>
                    {
                        if (localTempMaxColumnNumber >= tempMaxColumnNumber)
                        {
                            tempMaxColumnNumber = localTempMaxColumnNumber;
                            tempMaxWidth = (localTempMaxColumnNumber + 1) * smallImageWidth;
                        }
                    }));
                    this.Invoke(new MethodInvoker(() =>
                    {
                        if (localTempMaxRowNumber >= tempMaxRowNumber)
                        {
                            tempMaxRowNumber = localTempMaxRowNumber;
                            tempMaxHeight = (localTempMaxRowNumber + 1) * smallImageHeight;
                        }
                    }));
                }
                lock (imagesLock)
                {
                    listViewImages.Invoke(new MethodInvoker(() =>
                    {
                        tempImages.AddRange(images);
                    }));

                }
                lock (itemsLock)
                {
                    listViewImages.Invoke(new MethodInvoker(() =>
                    {
                        tempItems.AddRange(items);
                    }));

                }
            }
            catch (WrongFileNameException wrongFileNameException)
            {
                if (imagesToLoad != null) imagesToLoad = null;
                if (items != null) items = null;
                if (images != null) images = null;
                MessageBox.Show(wrongFileNameException.Message);
                this.Invoke(new MethodInvoker(() =>
                {
                    droppedImagesSuccessfulllyLoaded = false;
                }));
            }
            catch (ImagesDifferentSizeExcpetion imagesDifferentSizeExcpetion)
            {
                if (imagesToLoad != null) imagesToLoad = null;
                if (items != null) items = null;
                if (images != null) images = null;
                MessageBox.Show(imagesDifferentSizeExcpetion.Message);
                this.Invoke(new MethodInvoker(() =>
                {
                    droppedImagesSuccessfulllyLoaded = false;
                }));
            }
            catch (Exception exception)
            {
                if (imagesToLoad != null) imagesToLoad = null;
                if (items != null) items = null;
                if (images != null) images = null;
                MessageBox.Show(exception.Message);
                this.Invoke(new MethodInvoker(() =>
                {
                    droppedImagesSuccessfulllyLoaded = false;
                }));
            }
        }
        /// <summary>
        /// Funkcja ładująca obrazki do bufora i wyświetlająca lewy górny róg obrazu. Rysuje również podgląd.
        /// </summary>
        private void JoinImages()
        {
            if (listViewImages.Items.Count > 0)
            {
                currentRow = 0;
                currentColumn = 0;
                panningStartingPoint = Point.Empty;
                pictureBoxPositionRelatedToWholePicture = Point.Empty;
                pictureBoxPositionRelatedToBuffer = Point.Empty;
                CalculateBufferRowsAndColumns();
                LoadImagesIntoBuffer();
                pictureBoxFinallImage.Invalidate();
                pictureBoxPreview.Invalidate();
            }
        }

        /// <summary>
        /// Funkcja inicjalizująca obrazek podglądowy.
        /// </summary>
        private void InitializePreviewImage()
        {
            previewImage = new Bitmap(pictureBoxPreview.Width, pictureBoxPreview.Height);
        }

        /// <summary>
        /// Funkcja ładująca do bufora obrazki
        /// </summary>
        private void LoadImagesIntoBuffer()
        {
            List<Tuple<Image, Rectangle>> imagesWithRectangles = new List<Tuple<Image, Rectangle>>();
            List<string> files = GetBufferImages();
            string fileNameWithoutExtension;
            int row, column;
            int xPosition = 0;
            int yPosition = 0;
            int columnShift = 0;
            int rowShift = 0;
            if (currentColumn >= 2)
            {
                if (MaxColumnNumber - currentColumn <= 2)
                {
                    columnShift = MaxColumnNumber - 2;
                }
                else
                {
                    columnShift = currentColumn - 1;
                }
            }
            if (currentRow >= 2)
            {
                if (MaxRowNumber - currentRow <= 2)
                {
                    rowShift = MaxRowNumber - 2;
                }
                else
                {
                    rowShift = currentRow - 1;
                }
            }
            try
            {
                foreach (string image in files)
                {
                    // Pozyskiwanie numeru wiersza i kolumny z nazwy pliku
                    fileNameWithoutExtension = Path.GetFileNameWithoutExtension(image);
                    row = GetRowFromFileName(fileNameWithoutExtension);
                    column = GetColumnFromFileName(fileNameWithoutExtension);
                    if (rowAndColumnNumeration == RowAndColumnNumeration.XleftYdown || rowAndColumnNumeration == RowAndColumnNumeration.XleftYup)
                    {
                        column = maxColumnNumber - column;
                    }
                    if (rowAndColumnNumeration == RowAndColumnNumeration.XrightYup || rowAndColumnNumeration == RowAndColumnNumeration.XleftYup)
                    {
                        row = maxRowNumber - row;
                    }

                    // Obliczanie pozycji małego obrazka w buforze i dodanie go do tymczasowej listy
                    Bitmap bitmap = new Bitmap(image);
                    xPosition = (column - columnShift) * bitmap.Width;
                    yPosition = (row - rowShift) * bitmap.Height;
                    Tuple<Image, Rectangle> imageWithRectangle = new Tuple<Image, Rectangle>(bitmap, new Rectangle(xPosition, yPosition, bitmap.Width, bitmap.Height));
                    imagesWithRectangles.Add(imageWithRectangle);
                }

                // Utworzenie buforu z załadowanych obrazków
                bufferImage = new Bitmap(bufferColumns * smallImageWidth, bufferRows * smallImageHeight);
                using (Graphics g = Graphics.FromImage(bufferImage))
                {
                    g.Clear(Color.Black);
                    foreach (var imageWithRectangle in imagesWithRectangles)
                    {
                        g.DrawImage(imageWithRectangle.Item1, imageWithRectangle.Item2);
                    }
                }
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

        /// <summary>
        /// Oblicza pozycję lewego górnego rogu pictureboxa w odniesieniu do całego obrazu.
        /// </summary>
        /// <param name="currentPosition"></param>
        public void CalulatePositionInWholeImage(Point currentPosition)
        {
            pictureBoxPositionRelatedToBuffer.X = currentPosition.X - panningStartingPoint.X;
            pictureBoxPositionRelatedToBuffer.Y = currentPosition.Y - panningStartingPoint.Y;
            int newX = pictureBoxPositionRelatedToWholePicture.X - currentPosition.X + panningStartingPoint.X;
            int newY = pictureBoxPositionRelatedToWholePicture.Y - currentPosition.Y + panningStartingPoint.Y;
            //Sprawdzanie, czy nowy X nie wykracza poza zakres
            if (newX > MaxWidth - pictureBoxFinallImage.Width)
            {
                newX = MaxWidth > pictureBoxFinallImage.Width ? MaxWidth - pictureBoxFinallImage.Width : 0;
            }
            else if (newX < 0)
            {
                newX = 0;
            }

            // Sprawdzanie, czy nowy Y nie wykracza poza zakres
            if (newY > MaxHeight - pictureBoxFinallImage.Height)
            {
                newY = MaxHeight > pictureBoxFinallImage.Height ? MaxHeight - pictureBoxFinallImage.Height : 0;
            }
            else if (newY < 0)
            {
                newY = 0;
            }
            pictureBoxPositionRelatedToWholePicture = new Point(newX, newY);
        }

        /// <summary>
        /// Oblicza położenie pictureboxa w odniesieniu do bufora.
        /// </summary>
        public void CalculatePictureBoxPositionRelatedToBuffer()
        {
            int relativeX = pictureBoxPositionRelatedToWholePicture.X % smallImageWidth;
            int relativeY = pictureBoxPositionRelatedToWholePicture.Y % smallImageHeight;
            if (currentColumn == MaxColumnNumber)
            {
                relativeX += (bufferColumns - 1) * smallImageWidth;
            }
            else if (currentColumn > 0)
            {
                relativeX += smallImageWidth;
            }
            if (currentRow == MaxRowNumber)
            {
                relativeY += (bufferRows - 1) * smallImageHeight;
            }
            else if (currentRow > 0)
            {
                relativeY += smallImageHeight;
            }
            pictureBoxPositionRelatedToBuffer.X = relativeX;
            pictureBoxPositionRelatedToBuffer.Y = relativeY;
        }

        /// <summary>
        /// Spradza, czy podany plik ma nazwę w następującym formacie: YnnnnXnnnn, np Y0000X0000
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool ValidateFileName(string filename)
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

        /// <summary>
        /// Zwraca numer kolumny na podstawie nazwy pliku.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public int GetColumnFromFileName(string fileName)
        {
            return int.Parse(fileName.Substring(6, 4));
        }

        /// <summary>
        /// Zwraca numer wiersza na podstawie nazwy pliku.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public int GetRowFromFileName(string fileName)
        {
            return int.Parse(fileName.Substring(1, 4));
        }

        /// <summary>
        /// Zwraca lokalizacje obrazow potrzebnych do stworzenia bufora.
        /// </summary>
        /// <returns></returns>
        public List<string> GetBufferImages()
        {
            List<string> fileNames = new List<string>();
            string fileNameToFind = "";
            ListViewItem listViewItem;
            int startingColumn, startingRow;

            // Wyznaczania poczatkowej kolumny dla bufora
            if ((currentColumn - 1) < 0)
            {
                startingColumn = 0;
            }
            else if ((currentColumn + bufferColumns - 1) > MaxColumnNumber)
            {
                startingColumn = MaxColumnNumber - bufferColumns + 1;
            }
            else
            {
                startingColumn = currentColumn - 1;
            }

            // Wyznaczanie poczatkowego wiersza dla bufora
            if ((currentRow - 1) < 0)
            {
                startingRow = 0;
            }
            else if ((currentRow + bufferRows - 1) > MaxRowNumber)
            {
                startingRow = MaxRowNumber - bufferRows + 1;
            }
            else
            {
                startingRow = currentRow - 1;
            }
            int endingRow = startingRow + bufferRows - 1;
            int endingColumn = startingColumn + bufferColumns - 1;
            // Wyszukiwanie potrzebnych obrazow i dodawanie ich lokalizacji do listy
            for (int i = startingRow; i <= endingRow; ++i)
            {
                for (int j = startingColumn; j <= endingColumn; ++j)
                {
                    fileNameToFind = GetFileNameBasedOnRowAndColumn(i, j);
                    listViewItem = listViewImages.Items.Cast<ListViewItem>().FirstOrDefault(item => Path.GetFileNameWithoutExtension(item.Text) == fileNameToFind);
                    if (listViewItem != null)
                    {
                        fileNames.Add(listViewItem.Text);
                    }
                    listViewItem = null;
                }
            }
            return fileNames;
        }

        /// <summary>
        /// Zwraca nazwę pliku na podstawie wiersza i kolumny. Dokonuje konwersji w zależności od dobranej metody numeracji wierszy i kolumn.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private string GetFileNameBasedOnRowAndColumn(int row, int column)
        {
            switch (rowAndColumnNumeration)
            {
                case RowAndColumnNumeration.XleftYdown:
                    return "Y" + row.ToString().PadLeft(4, '0') + "X" + (maxColumnNumber - column).ToString().PadLeft(4, '0');
                case RowAndColumnNumeration.XleftYup:
                    return "Y" + (maxRowNumber - row).ToString().PadLeft(4, '0') + "X" + (maxColumnNumber - column).ToString().PadLeft(4, '0');
                case RowAndColumnNumeration.XrightYdown:
                    return "Y" + row.ToString().PadLeft(4, '0') + "X" + column.ToString().PadLeft(4, '0');
                case RowAndColumnNumeration.XrightYup:
                    return "Y" + (maxRowNumber - row).ToString().PadLeft(4, '0') + "X" + column.ToString().PadLeft(4, '0');
                default:
                    return "Y" + row.ToString().PadLeft(4, '0') + "X" + column.ToString().PadLeft(4, '0');
            }
        }

        /// <summary>
        /// Oblicza wiersz i kolumnę na podstawie położenia lewego górnego rogu pictureBoxa w odniesieniu do całego obrazu.
        /// </summary>
        private void CalculateRowAndColumnBasedOnCurrentPosition()
        {
            if (pictureBoxPositionRelatedToWholePicture.X < smallImageWidth)
            {
                if (pictureBoxPositionRelatedToWholePicture.Y < smallImageHeight)
                {
                    currentRow = 0;
                    currentColumn = 0;
                }
                else
                {
                    currentRow = (int)(pictureBoxPositionRelatedToWholePicture.Y / smallImageHeight);
                    currentColumn = 0;
                }
            }
            else
            {
                if (pictureBoxPositionRelatedToWholePicture.Y < smallImageHeight)
                {
                    currentRow = 0;
                    currentColumn = (int)(pictureBoxPositionRelatedToWholePicture.X / smallImageWidth);
                }
                else
                {
                    currentRow = (int)(pictureBoxPositionRelatedToWholePicture.Y / smallImageHeight);
                    currentColumn = (int)(pictureBoxPositionRelatedToWholePicture.X / smallImageWidth);
                }
            }
        }

        /// <summary>
        /// Funkcja licząca liczbę wierszy i kolumn dla bufora.
        /// </summary>
        public void CalculateBufferRowsAndColumns()
        {
            if (smallImageWidth >= pictureBoxFinallImage.Width)
            {
                if (smallImageHeight >= pictureBoxFinallImage.Height)
                {
                    bufferColumns = 3;
                    bufferRows = 3;
                }
                else
                {
                    bufferColumns = 3;
                    bufferRows = (int)(pictureBoxFinallImage.Height / smallImageHeight) + 3;
                }
            }
            else
            {
                if (smallImageHeight >= pictureBoxFinallImage.Height)
                {
                    bufferColumns = (int)(pictureBoxFinallImage.Width / smallImageWidth) + 3;
                    bufferRows = 3;
                }
                else
                {
                    bufferColumns = (int)(pictureBoxFinallImage.Width / smallImageWidth) + 3;
                    bufferRows = (int)(pictureBoxFinallImage.Height / smallImageHeight) + 3;
                }
            }
            if (bufferColumns > (MaxColumnNumber + 1)) bufferColumns = MaxColumnNumber + 1;
            if (bufferRows > (MaxRowNumber + 1)) bufferRows = MaxRowNumber + 1;
        }

        /// <summary>
        /// Czyści listę obrazów, bufor i zmienne pomocnicze.
        /// </summary>
        private void RemoveAllImages()
        {
            if (listViewImages.Items.Count > 0)
            {
                listViewImages.Items.Clear();
                imagesList.Images.Clear();
                if (bufferImage != null)
                {
                    bufferImage.Dispose();
                    bufferImage = null;
                    pictureBoxFinallImage.Invalidate(); // Odświeża obraz
                }
                if (pictureBoxFinallImage.Image != null)
                {
                    pictureBoxFinallImage.Image.Dispose();
                    pictureBoxFinallImage.Image = null;
                    pictureBoxFinallImage.Invalidate(); // Odświeża obraz
                }
                if (previewImage != null)
                {
                    previewImage.Dispose();
                    previewImage = null;
                    pictureBoxPreview.Invalidate(); // Odświeża obraz
                }
                if (pictureBoxPreview.Image != null)
                {
                    pictureBoxPreview.Image.Dispose();
                    pictureBoxPreview.Image = null;
                    pictureBoxPreview.Invalidate(); // Odświeża obraz
                }
                if (formAskForRowAndColumnNumeration != null)
                {
                    formAskForRowAndColumnNumeration.Close();
                }
                panningStartingPoint = Point.Empty;
                pictureBoxPositionRelatedToWholePicture = Point.Empty;
                pictureBoxPositionRelatedToBuffer = Point.Empty;
                smallImageWidth = 0;
                smallImageHeight = 0;
                bufferColumns = 0;
                bufferRows = 0;
                currentRow = 0;
                currentColumn = 0;
                MaxWidth = 0;
                MaxHeight = 0;
                MaxRowNumber = 0;
                MaxColumnNumber = 0;
                tempMaxRowNumber = 0;
                tempMaxColumnNumber = 0;
                tempMaxWidth = 0;
                tempMaxHeight = 0;
                if (tempImages != null)
                {
                    tempImages.Clear();
                    tempImages = null;
                }
                if (tempItems.Count > 0)
                {
                    tempItems.Clear();
                    tempItems = null;
                }
            }
            else
            {
                MessageBox.Show("There are no images to remove");
            }
        }
        #endregion
    }
}
