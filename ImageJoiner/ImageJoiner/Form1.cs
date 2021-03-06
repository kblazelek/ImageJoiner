﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ImageJoiner.CustomExceptions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Drawing.Imaging;
using System.Diagnostics;

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
        private Bitmap previewImageBackground;
        private bool panning = false;
        RowAndColumnNumeration rowAndColumnNumeration;
        FormAskForRowAndColumnNumeration formAskForRowAndColumnNumeration;
        Object progressBarLock = new Object();
        Object finallImageVariablesLock = new Object();
        Object itemsLock = new Object();
        List<ListViewItem> tempItems;
        Boolean droppedImagesSuccessfulllyLoaded = true;
        int tempMaxColumnNumber = 0;
        int tempMaxRowNumber = 0;
        int tempMaxHeight = 0;
        int tempMaxWidth = 0;
        int imagesCount = 0;
        string directory = "";
        string[] filesArray;
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
                if (imagesCount > 0)
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
                if (imagesCount > 0)
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
            listViewImages.Items.Clear();
            listViewImages.Items.Add(new ListViewItem("Drop images here..."));
        }
        #endregion
        #region Event handlers
        /// <summary>
        /// Rozpoczyna zapis obrazu do pliku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSaveImage_Click(object sender, EventArgs e)
        {
            if (imagesCount == 0)
            {
                MessageBox.Show("There are no images to save");
            }
            else
            {
                Size imageToSaveSize = AskForDimensions();
                if (imageToSaveSize.Width > 0 && imageToSaveSize.Height > 0)
                {
                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        Tuple<Size, string> imageToSaveInfo = new Tuple<Size, string>(imageToSaveSize, saveFileDialog1.FileName);
                        progressBarSaveImage.Visible = true;
                        progressBarSaveImage.Minimum = 1;
                        progressBarSaveImage.Maximum = imagesCount;
                        progressBarSaveImage.Value = 1;
                        progressBarSaveImage.Step = 1;
                        backgroundWorkerSaveImage.RunWorkerAsync(imageToSaveInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Zapisuje obraz do pliku w tle
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorkerSaveImage_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            var imageToSaveInfo = e.Argument as Tuple<Size, string>;
            string filename = imageToSaveInfo.Item2;
            int imageWidth = imageToSaveInfo.Item1.Width / (MaxColumnNumber + 1);
            int imageHeight = imageToSaveInfo.Item1.Height / (MaxRowNumber + 1);
            Bitmap bitmapToFitWholeImages = new Bitmap(
                imageWidth * (MaxColumnNumber + 1),
                imageHeight * (MaxRowNumber + 1));
            var graphics = Graphics.FromImage(bitmapToFitWholeImages);
            Image tempImage = null;
            Bitmap bitmapWithUserSize = null;
            try
            {
                string fileNameToFind = "";
                string imageLocation = "";
                for (int i = 0; i <= MaxRowNumber; ++i)
                {
                    for (int j = 0; j <= MaxColumnNumber; ++j)
                    {
                        fileNameToFind = GetFileNameBasedOnRowAndColumn(i, j);
                        if (File.Exists(Directory.GetFiles(directory, fileNameToFind + @".*").FirstOrDefault()))
                        {
                            imageLocation = Directory.GetFiles(directory + @"\", fileNameToFind + @".*").FirstOrDefault();
                        }
                        else
                        {
                            continue;
                        }
                        if (this.backgroundWorkerLoadBackground.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }
                        var extension = Path.GetExtension(imageLocation);
                        if (extension != ".jpg" && extension != ".png" && extension != ".bmp" && extension != ".TIF")
                        {
                            continue;
                        }
                        tempImage = Image.FromFile(imageLocation);
                        if ((tempImage.Height != smallImageHeight) || (tempImage.Width != smallImageWidth))
                        {
                            continue;
                        }
                        int imageRow = GetRowFromFileName(Path.GetFileNameWithoutExtension(imageLocation));
                        int imageColumn = GetColumnFromFileName(Path.GetFileNameWithoutExtension(imageLocation));
                        if (rowAndColumnNumeration == RowAndColumnNumeration.XleftYdown || rowAndColumnNumeration == RowAndColumnNumeration.XleftYup)
                        {
                            imageColumn = maxColumnNumber - imageColumn;
                        }
                        if (rowAndColumnNumeration == RowAndColumnNumeration.XrightYup || rowAndColumnNumeration == RowAndColumnNumeration.XleftYup)
                        {
                            imageRow = maxRowNumber - imageRow;
                        }
                        int imageX = imageColumn * imageWidth;
                        int imageY = imageRow * imageHeight;
                        var rectangle = new Rectangle(imageX, imageY, imageWidth, imageHeight);
                        graphics.DrawImage(tempImage, rectangle);
                        if (tempImage != null)
                        {
                            tempImage.Dispose();
                            tempImage = null;
                        }
                        progressBarSaveImage.Invoke(new Action(() => progressBarSaveImage.PerformStep()));
                    }
                }
                bitmapWithUserSize = new Bitmap(bitmapToFitWholeImages, imageToSaveInfo.Item1.Width, imageToSaveInfo.Item1.Height);
                bitmapWithUserSize.Save(filename);
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show(String.Format("Successfully saved image to {0}", filename));
                }));
            }
            catch (InvalidOperationException ex)
            {
                Debug.Write(ex.Message);
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show(ex.Message);
                }));
            }
            finally
            {
                this.Invoke(new Action(() =>
                {
                    progressBarSaveImage.Visible = false;
                }));
                progressBarSaveImage.Invoke(new Action(() => progressBarSaveImage.Visible = false));
                if (graphics != null)
                {
                    graphics = null;
                }
                if (tempImage != null)
                {
                    tempImage.Dispose();
                    tempImage = null;
                }
                if (imageToSaveInfo != null) imageToSaveInfo = null;
                if (bitmapToFitWholeImages != null)
                {
                    bitmapToFitWholeImages.Dispose();
                    bitmapToFitWholeImages = null;
                }
                if (bitmapWithUserSize != null)
                {
                    bitmapWithUserSize.Dispose();
                    bitmapWithUserSize = null;
                }
            }
        }
        /// <summary>
        /// Zdarzenie, które ma miejsce gdy użytkownik przeciągnię jeden lub więcej plików na listę, ale jeszcze nie upuści ich.
        /// Wyświetla ikonkę informującą o tym, czy pliki można upuścić lub nie.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewImages_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
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
            e.Effect = DragDropEffects.None;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                RemoveAllImages();
                backgroundWorkerGetDroppedImages.RunWorkerAsync(e);
            }
        }

        /// <summary>
        /// Pobiera upuszczone pliki do tablicy.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorkerGetDroppedImages_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            DragEventArgs args = e.Argument as DragEventArgs;
            this.Invoke(new Action(() =>
            {
                progressBarGetDroppedImages.Visible = true;
            }));

            var thread = new Thread(() =>
            {
                filesArray = (string[])args.Data.GetData(DataFormats.FileDrop);
            });

            thread.Start();
            thread.Join();
            this.Invoke(new Action(() =>
            {
                progressBarGetDroppedImages.Visible = false;
                CalculateFinalImageVariables();
            }));
        }
        /// <summary>
        /// Ładuje obrazki do podglądu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorkerLoadBackground_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            int imageWidth = previewImageBackground.Width / (MaxColumnNumber + 1);
            int imageHeight = previewImageBackground.Height / (MaxRowNumber + 1);
            int skipColumns = 0;
            int skipRows = 0;
            int previousColumn = 0;
            int previousRow = 0;
            if (imageWidth == 0)
            {
                imageWidth = 1;
                skipColumns = ((MaxColumnNumber + 1) / previewImageBackground.Width) + 1;
            }
            if (imageHeight == 0)
            {
                imageHeight = 1;
                skipRows = ((MaxRowNumber + 1) / previewImageBackground.Height) + 1;
            }
            var graphics = Graphics.FromImage(previewImageBackground);
            Image tempImage = null;
            string imageLocation = "";
            try
            {
                for (int i = 0; i <= MaxRowNumber; ++i)
                {
                    if (skipRows != 0)
                    {
                        if ((i % skipRows == 0) && i != 0) continue;
                    }
                    for (int j = 0; j <= MaxColumnNumber; ++j)
                    {
                        if (skipColumns != 0)
                        {
                            if ((j % skipColumns == 0) && j != 0) continue;
                        }
                        imageLocation = Directory.GetFiles(
                            directory,
                            GetFileNameBasedOnRowAndColumn(i > 0 ? previousRow + 1 : i, j > 0 ? previousColumn + 1 : j) + @".*").FirstOrDefault();
                        if (File.Exists(imageLocation))
                        {
                            var extension = Path.GetExtension(imageLocation);
                            if (extension != ".jpg" && extension != ".png" && extension != ".bmp" && extension != ".TIF")
                            {
                                previousColumn = j;
                                continue;
                            }
                            tempImage = Image.FromFile(imageLocation);
                            if ((tempImage.Width != smallImageWidth) || (tempImage.Height != smallImageHeight))
                            {
                                previousColumn = j;
                                continue;
                            }
                            int imageRow = GetRowFromFileName(Path.GetFileNameWithoutExtension(imageLocation));
                            int imageColumn = GetColumnFromFileName(Path.GetFileNameWithoutExtension(imageLocation));
                            if (rowAndColumnNumeration == RowAndColumnNumeration.XleftYdown || rowAndColumnNumeration == RowAndColumnNumeration.XleftYup)
                            {
                                imageColumn = maxColumnNumber - imageColumn;
                            }
                            if (rowAndColumnNumeration == RowAndColumnNumeration.XrightYup || rowAndColumnNumeration == RowAndColumnNumeration.XleftYup)
                            {
                                imageRow = maxRowNumber - imageRow;
                            }
                            int imageX = imageColumn * imageWidth;
                            int imageY = imageRow * imageHeight;
                            var rectangle = new Rectangle(imageX, imageY, imageWidth, imageHeight);
                            graphics.DrawImage(tempImage, rectangle);
                            if (tempImage != null)
                            {
                                tempImage.Dispose();
                                tempImage = null;
                            }

                        }
                        if (this.backgroundWorkerLoadBackground.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }
                        this.Invoke(new Action(() =>
                        {
                            pictureBoxPreview.Invalidate();
                        }));
                        previousColumn = j;
                    }
                    previousRow = i;
                }
                if (graphics != null)
                {
                    graphics = null;
                }
            }
            catch (InvalidOperationException ex)
            {
                Debug.Write(ex.Message);
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show(ex.Message);
                }));
            }
            finally
            {
                if (tempImage != null)
                {
                    tempImage.Dispose();
                    tempImage = null;
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
            if (imagesCount > 0)
            {
                //if (previewImage == null) InitializePreviewImage();
                if (Directory.Exists(directory))
                {
                    JoinImages();
                }
                else
                {
                    MessageBox.Show("Please specify existing directory");
                }
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
                e.Graphics.Clear(Color.Black);
                if (previewImageBackground != null) e.Graphics.DrawImage(previewImageBackground, 0, 0, pictureBoxPreview.Width, pictureBoxPreview.Height);
                Pen blackPen = new Pen(Color.Red, 1);
                int rectangleWidth = maxWidth > pictureBoxFinallImage.Width ?
                    pictureBoxFinallImage.Width * pictureBoxPreview.Width / maxWidth : pictureBoxPreview.Width - 1;
                int rectangleHeight = maxHeight > pictureBoxFinallImage.Height ?
                    pictureBoxFinallImage.Height * pictureBoxPreview.Height / maxHeight : pictureBoxPreview.Height - 1;
                if (rectangleWidth == 0) rectangleWidth = 1;
                if (rectangleHeight == 0) rectangleHeight = 1;
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
        /// Otwiera formatkę z pytaniem o wymiary obrazu do zapisu.
        /// </summary>
        /// <returns></returns>
        public Size AskForDimensions()
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 120,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Please specify the image dimensions in px",
                StartPosition = FormStartPosition.CenterScreen
            };
            int width = 0;
            int height = 0;
            Label labelWidth = new Label() { Left = 50, Top = 20, Text = "Width:" };
            Label labelHeight = new Label() { Left = 50, Top = 50, Text = "Height:" };
            TextBox textBoxWidth = new TextBox() { Left = 100, Top = 20, Width = 40 };
            TextBox textBoxHeight = new TextBox() { Left = 100, Top = 50, Width = 40 };
            Button confirmation = new Button() { Text = "OK", Left = 150, Width = 200, Height = 50, Top = 20, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBoxWidth);
            prompt.Controls.Add(textBoxHeight);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(labelWidth);
            prompt.Controls.Add(labelHeight);
            prompt.AcceptButton = confirmation;
            var dialogResult = prompt.ShowDialog();
            bool isWidthNumeric = int.TryParse(textBoxWidth.Text, out width);
            bool isHeightNumeric = int.TryParse(textBoxHeight.Text, out height);
            if (dialogResult == DialogResult.OK)
            {
                if (isWidthNumeric && isHeightNumeric)
                {
                    if (width > MaxWidth || height > MaxHeight)
                    {
                        MessageBox.Show("Both dimensions must be less or equal than whole image combined");
                        return Size.Empty;
                    }
                    // Każdy pixel w nieskompresowanym obrazie ma 3B. Jeżeli rozmiar nieskompresowanego obrazu byłby większy od 100MB to zwróć pusty rozmiar.
                    if (width * height * 3 > 1024 * 1024 * 100)
                    {
                        MessageBox.Show(String.Format("Width * Height * 3 should be less or equal than {0}", 1024 * 1024 * 100));
                        return Size.Empty;
                    }
                    else
                    {
                        return new Size(width, height);
                    }
                }
                else
                {
                    MessageBox.Show("Both dimensions must be numeric");
                    return Size.Empty;
                }
            }
            return Size.Empty;
        }
        /// <summary>
        /// Formatka, która pyta o sposób numeracji obrazków
        /// </summary>
        private bool AskForRowAndColumnNumeration()
        {
            formAskForRowAndColumnNumeration = new FormAskForRowAndColumnNumeration();
            formAskForRowAndColumnNumeration.StartPosition = FormStartPosition.Manual;
            formAskForRowAndColumnNumeration.Location = this.PointToScreen(Point.Empty);
            var result = formAskForRowAndColumnNumeration.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.rowAndColumnNumeration = formAskForRowAndColumnNumeration.rowAndColumnNumeration;
                formAskForRowAndColumnNumeration.Close();
                return true;
            }
            else
            {
                formAskForRowAndColumnNumeration.Close();
                return false;
            }
        }
        /// <summary>
        /// Ładuje dane wytworzone przez watki do listy i rozpoczyna ładować je do podglądu.
        /// </summary>
        /// <param name="threads"></param>
        private void GatherDataFromThreads(List<Thread> threads)
        {
            var waitBg = new Thread(() =>
            {
                foreach (var thread in threads)
                {
                    thread.Join();
                }
                if (threads != null)
                {
                    threads.Clear();
                    threads = null;
                }
                progressBarLoadImages.Invoke(new Action(() =>
                {
                    if (droppedImagesSuccessfulllyLoaded)
                    {
                        if (backgroundWorkerLoadBackground.IsBusy)
                            backgroundWorkerLoadBackground.CancelAsync();
                        while (backgroundWorkerLoadBackground.IsBusy)
                            Application.DoEvents();
                        MaxRowNumber = tempMaxRowNumber;
                        MaxColumnNumber = tempMaxColumnNumber;
                        MaxHeight = tempMaxHeight;
                        MaxWidth = tempMaxWidth;
                        InitializePreviewImage();
                        backgroundWorkerLoadBackground.RunWorkerAsync();
                    }
                    else
                    {
                        imagesCount = 0;
                    }
                    tempMaxRowNumber = 0;
                    tempMaxColumnNumber = 0;
                    tempMaxWidth = 0;
                    tempMaxHeight = 0;
                    progressBarLoadImages.Visible = false;
                }));
            }
                                    );
            waitBg.Start();
        }
        /// <summary>
        /// Ładuje zakres upuszczonych obrazkow do list.
        /// </summary>
        /// <param name="imagesToLoad"></param>
        /// <param name="startingIndexOnList"></param>
        private void LoadDroppedImages(object obj)
        {
            var tuple = (Tuple<string[], int, int>)obj;
            string[] imagesToLoad = tuple.Item1;
            int startingIndex = tuple.Item2;
            int endingIndex = tuple.Item3;
            int tempRowNumber = 0;
            int tempColumnNumber = 0;
            int localTempMaxColumnNumber = 0;
            int localTempMaxRowNumber = 0;
            string lastFileName = "";
            int comparisonResult = 0;
            try
            {
                for (int i = startingIndex; i <= endingIndex; ++i)
                {
                    comparisonResult = String.Compare(imagesToLoad[i], lastFileName);
                    if (comparisonResult > 0) lastFileName = imagesToLoad[i];
                    progressBarLoadImages.Invoke(new Action(() => progressBarLoadImages.PerformStep()));
                }
                if (!ValidateFileName(Path.GetFileNameWithoutExtension(lastFileName)))
                {
                    throw new WrongFileNameException(String.Format("File \"{0}\" does not match the following expression:\n YnnnnXnnnn.extension (for example Y0000X0000.png)", Path.GetFileName(lastFileName)));
                }
                tempRowNumber = this.GetRowFromFileName(Path.GetFileNameWithoutExtension(lastFileName));
                tempColumnNumber = this.GetColumnFromFileName(Path.GetFileNameWithoutExtension(lastFileName));
                if (localTempMaxRowNumber < tempRowNumber)
                {
                    localTempMaxRowNumber = tempRowNumber;
                }
                if (localTempMaxColumnNumber < tempColumnNumber)
                {
                    localTempMaxColumnNumber = tempColumnNumber;
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
                        if (localTempMaxRowNumber >= tempMaxRowNumber)
                        {
                            tempMaxRowNumber = localTempMaxRowNumber;
                            tempMaxHeight = (localTempMaxRowNumber + 1) * smallImageHeight;
                        }
                    }));
                }
            }
            catch (WrongFileNameException wrongFileNameException)
            {
                if (imagesToLoad != null) imagesToLoad = null;
                MessageBox.Show(wrongFileNameException.Message);
                droppedImagesSuccessfulllyLoaded = false;
            }
            catch (ImagesDifferentSizeExcpetion imagesDifferentSizeExcpetion)
            {
                if (imagesToLoad != null) imagesToLoad = null;
                MessageBox.Show(imagesDifferentSizeExcpetion.Message);
                droppedImagesSuccessfulllyLoaded = false;
            }
            catch (Exception exception)
            {
                if (imagesToLoad != null) imagesToLoad = null;
                MessageBox.Show(exception.Message);
                droppedImagesSuccessfulllyLoaded = false;
            }
        }
        /// <summary>
        /// Funkcja ładująca obrazki do bufora i wyświetlająca lewy górny róg obrazu. Rysuje również podgląd.
        /// </summary>
        private void JoinImages()
        {
            if (imagesCount > 0)
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
            int backgroundWidth = (previewImage.Width / (MaxColumnNumber + 1)) * (MaxColumnNumber + 1);
            int backgroundHeight = (previewImage.Height / (MaxRowNumber + 1)) * (MaxRowNumber + 1);
            if (backgroundHeight == 0) backgroundHeight = previewImage.Height;
            if (backgroundWidth == 0) backgroundWidth = previewImage.Width;
            previewImageBackground = new Bitmap(backgroundWidth, backgroundHeight);
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
                    var extension = Path.GetExtension(image);
                    if (extension != ".jpg" && extension != ".png" && extension != ".bmp" && extension != ".TIF")
                    {
                        continue;
                    }
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
                    if (bitmap.Width != smallImageWidth || bitmap.Height != smallImageHeight) continue;
                    xPosition = (column - columnShift) * bitmap.Width;
                    yPosition = (row - rowShift) * bitmap.Height;
                    Tuple<Image, Rectangle> imageWithRectangle = new Tuple<Image, Rectangle>(bitmap, new Rectangle(xPosition, yPosition, bitmap.Width, bitmap.Height));
                    imagesWithRectangles.Add(imageWithRectangle);
                }

                // Utworzenie buforu z załadowanych obrazków
                if (bufferImage != null)
                {
                    bufferImage.Dispose();
                    bufferImage = null;
                }
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
                    if (File.Exists(System.IO.Directory.GetFiles(directory, fileNameToFind + @".*").FirstOrDefault()))
                    {
                        fileNames.Add(System.IO.Directory.GetFiles(directory, fileNameToFind + @".*").FirstOrDefault());
                    }
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
        /// Oblicza zmienne dotyczące końcowego obrazu
        /// </summary>
        private void CalculateFinalImageVariables()
        {
            if (AskForRowAndColumnNumeration())
            {
                imagesCount = filesArray.Count();
                try
                {
                    Image tempImage = Image.FromFile(filesArray[0]);
                    directory = Path.GetDirectoryName(filesArray[0]);
                    listViewImages.Items.Clear();
                    listViewImages.Items.Add(new ListViewItem("Loaded images from: " + directory));
                    smallImageWidth = tempImage.Width;
                    smallImageHeight = tempImage.Height;
                    tempImage.Dispose();
                    tempImage = null;
                    progressBarLoadImages.Maximum = imagesCount;
                    progressBarLoadImages.Visible = true;
                    progressBarLoadImages.Minimum = 1;
                    progressBarLoadImages.Value = 1;
                    progressBarLoadImages.Step = 1;
                    int neededProcessors = 0;
                    int numberOfImagesPerCore = 0;
                    int remainedImages = 0;
                    tempMaxColumnNumber = MaxColumnNumber;
                    tempMaxRowNumber = MaxRowNumber;
                    tempMaxWidth = MaxWidth;
                    tempMaxHeight = MaxHeight;
                    if (tempItems == null) tempItems = new List<ListViewItem>();
                    // Gdy liczba obrazow do zaladowania jest mala wykonaj wszystko na 1 rdzeniu, w przeciwnym wypadku użyj wszystkich rdzeni
                    if (imagesCount <= Environment.ProcessorCount)
                    {
                        neededProcessors = 1;
                        numberOfImagesPerCore = imagesCount;
                        remainedImages = 0;
                    }
                    else
                    {
                        neededProcessors = Environment.ProcessorCount;
                        numberOfImagesPerCore = imagesCount / Environment.ProcessorCount;
                        remainedImages = imagesCount % Environment.ProcessorCount != 0 ? imagesCount % Environment.ProcessorCount : 0;
                    }
                    List<Thread> threads = new List<Thread>();
                    droppedImagesSuccessfulllyLoaded = true;
                    for (int i = 0; i < neededProcessors; ++i)
                    {
                        threads.Add(new Thread(new ParameterizedThreadStart(LoadDroppedImages)));
                        if (i < neededProcessors - 1)
                        {
                            threads.Last().Start(new Tuple<string[], int, int>(filesArray, i * numberOfImagesPerCore, i * numberOfImagesPerCore + numberOfImagesPerCore - 1));
                        }
                        else
                        {
                            threads.Last().Start(new Tuple<string[], int, int>(filesArray, i * numberOfImagesPerCore, i * numberOfImagesPerCore + numberOfImagesPerCore + remainedImages - 1));
                        }
                    }
                    GatherDataFromThreads(threads);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    if (formAskForRowAndColumnNumeration != null)
                    {
                        formAskForRowAndColumnNumeration.Dispose();
                        formAskForRowAndColumnNumeration = null;
                    }
                }
            }
        }

        /// <summary>
        /// Czyści listę obrazów, bufor i zmienne pomocnicze.
        /// </summary>
        private void RemoveAllImages()
        {
            if (listViewImages.Items.Count > 0)
            {
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
                listViewImages.Items.Clear();
                listViewImages.Items.Add(new ListViewItem("Drop images here..."));
                panningStartingPoint = Point.Empty;
                pictureBoxPositionRelatedToWholePicture = Point.Empty;
                pictureBoxPositionRelatedToBuffer = Point.Empty;
                smallImageWidth = 0;
                smallImageHeight = 0;
                bufferColumns = 0;
                bufferRows = 0;
                currentRow = 0;
                currentColumn = 0;
                imagesCount = 0;
                MaxWidth = 0;
                MaxHeight = 0;
                MaxRowNumber = 0;
                MaxColumnNumber = 0;
                tempMaxRowNumber = 0;
                tempMaxColumnNumber = 0;
                tempMaxWidth = 0;
                tempMaxHeight = 0;
                directory = "";
                if (filesArray != null)
                {
                    filesArray = null;
                }
                if (tempItems != null)
                {
                    tempItems.Clear();
                    tempItems = null;
                }
                if (previewImageBackground != null)
                {
                    previewImageBackground.Dispose();
                    previewImageBackground = null;
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