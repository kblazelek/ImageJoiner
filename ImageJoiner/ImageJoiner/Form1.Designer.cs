namespace ImageJoiner
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.listViewImages = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.imagesList = new System.Windows.Forms.ImageList(this.components);
            this.pictureBoxFinallImage = new System.Windows.Forms.PictureBox();
            this.buttonJoinImages = new System.Windows.Forms.Button();
            this.buttonRemoveImages = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.labelColumns = new System.Windows.Forms.Label();
            this.labelRows = new System.Windows.Forms.Label();
            this.labelWidth = new System.Windows.Forms.Label();
            this.labelHeight = new System.Windows.Forms.Label();
            this.labelFinalImageInfo = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxFinallImage)).BeginInit();
            this.SuspendLayout();
            // 
            // listViewImages
            // 
            this.listViewImages.AllowDrop = true;
            this.listViewImages.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.listViewImages.Location = new System.Drawing.Point(13, 12);
            this.listViewImages.Name = "listViewImages";
            this.listViewImages.Size = new System.Drawing.Size(228, 361);
            this.listViewImages.SmallImageList = this.imagesList;
            this.listViewImages.TabIndex = 0;
            this.listViewImages.UseCompatibleStateImageBehavior = false;
            this.listViewImages.View = System.Windows.Forms.View.SmallIcon;
            this.listViewImages.DragDrop += new System.Windows.Forms.DragEventHandler(this.listViewImages_DragDrop);
            this.listViewImages.DragEnter += new System.Windows.Forms.DragEventHandler(this.listViewImages_DragEnter);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Width = -1;
            // 
            // imagesList
            // 
            this.imagesList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.imagesList.ImageSize = new System.Drawing.Size(16, 16);
            this.imagesList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // pictureBoxFinallImage
            // 
            this.pictureBoxFinallImage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxFinallImage.Location = new System.Drawing.Point(247, 12);
            this.pictureBoxFinallImage.Name = "pictureBoxFinallImage";
            this.pictureBoxFinallImage.Size = new System.Drawing.Size(525, 537);
            this.pictureBoxFinallImage.TabIndex = 1;
            this.pictureBoxFinallImage.TabStop = false;
            this.pictureBoxFinallImage.SizeChanged += new System.EventHandler(this.pictureBoxFinallImage_SizeChanged);
            this.pictureBoxFinallImage.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBoxFinallImage_Paint);
            this.pictureBoxFinallImage.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBoxFinallImage_MouseDown);
            this.pictureBoxFinallImage.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBoxFinallImage_MouseMove);
            this.pictureBoxFinallImage.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBoxFinallImage_MouseUp);
            // 
            // buttonJoinImages
            // 
            this.buttonJoinImages.Location = new System.Drawing.Point(13, 388);
            this.buttonJoinImages.Name = "buttonJoinImages";
            this.buttonJoinImages.Size = new System.Drawing.Size(228, 23);
            this.buttonJoinImages.TabIndex = 2;
            this.buttonJoinImages.Text = "Join images";
            this.buttonJoinImages.UseVisualStyleBackColor = true;
            this.buttonJoinImages.Click += new System.EventHandler(this.buttonJoinImages_Click);
            // 
            // buttonRemoveImages
            // 
            this.buttonRemoveImages.Location = new System.Drawing.Point(13, 417);
            this.buttonRemoveImages.Name = "buttonRemoveImages";
            this.buttonRemoveImages.Size = new System.Drawing.Size(228, 23);
            this.buttonRemoveImages.TabIndex = 3;
            this.buttonRemoveImages.Text = "Remove images";
            this.buttonRemoveImages.UseVisualStyleBackColor = true;
            this.buttonRemoveImages.Click += new System.EventHandler(this.buttonRemoveImages_Click);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(80, 180);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(100, 23);
            this.progressBar.TabIndex = 4;
            this.progressBar.Visible = false;
            // 
            // labelColumns
            // 
            this.labelColumns.AutoSize = true;
            this.labelColumns.Location = new System.Drawing.Point(134, 492);
            this.labelColumns.Name = "labelColumns";
            this.labelColumns.Size = new System.Drawing.Size(59, 13);
            this.labelColumns.TabIndex = 7;
            this.labelColumns.Text = "Columns: 0";
            // 
            // labelRows
            // 
            this.labelRows.AutoSize = true;
            this.labelRows.Location = new System.Drawing.Point(134, 479);
            this.labelRows.Name = "labelRows";
            this.labelRows.Size = new System.Drawing.Size(46, 13);
            this.labelRows.TabIndex = 6;
            this.labelRows.Text = "Rows: 0";
            // 
            // labelWidth
            // 
            this.labelWidth.AutoSize = true;
            this.labelWidth.Location = new System.Drawing.Point(13, 479);
            this.labelWidth.Name = "labelWidth";
            this.labelWidth.Size = new System.Drawing.Size(47, 13);
            this.labelWidth.TabIndex = 5;
            this.labelWidth.Text = "Width: 0";
            // 
            // labelHeight
            // 
            this.labelHeight.AutoSize = true;
            this.labelHeight.Location = new System.Drawing.Point(13, 492);
            this.labelHeight.Name = "labelHeight";
            this.labelHeight.Size = new System.Drawing.Size(50, 13);
            this.labelHeight.TabIndex = 8;
            this.labelHeight.Text = "Height: 0";
            // 
            // labelFinalImageInfo
            // 
            this.labelFinalImageInfo.AutoSize = true;
            this.labelFinalImageInfo.Location = new System.Drawing.Point(13, 463);
            this.labelFinalImageInfo.Name = "labelFinalImageInfo";
            this.labelFinalImageInfo.Size = new System.Drawing.Size(83, 13);
            this.labelFinalImageInfo.TabIndex = 9;
            this.labelFinalImageInfo.Text = "Final image info:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.labelFinalImageInfo);
            this.Controls.Add(this.labelHeight);
            this.Controls.Add(this.labelColumns);
            this.Controls.Add(this.labelRows);
            this.Controls.Add(this.labelWidth);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.buttonRemoveImages);
            this.Controls.Add(this.buttonJoinImages);
            this.Controls.Add(this.pictureBoxFinallImage);
            this.Controls.Add(this.listViewImages);
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "Form1";
            this.Text = "ImageJoiner";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxFinallImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView listViewImages;
        private System.Windows.Forms.ImageList imagesList;
        private System.Windows.Forms.PictureBox pictureBoxFinallImage;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.Button buttonJoinImages;
        private System.Windows.Forms.Button buttonRemoveImages;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label labelWidth;
        private System.Windows.Forms.Label labelRows;
        private System.Windows.Forms.Label labelColumns;
        private System.Windows.Forms.Label labelHeight;
        private System.Windows.Forms.Label labelFinalImageInfo;
    }
}

