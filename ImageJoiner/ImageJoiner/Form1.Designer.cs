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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.listViewImages = new System.Windows.Forms.ListView();
            this.imagesList = new System.Windows.Forms.ImageList(this.components);
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // listViewImages
            // 
            this.listViewImages.AllowDrop = true;
            this.listViewImages.Location = new System.Drawing.Point(12, 12);
            this.listViewImages.Name = "listViewImages";
            this.listViewImages.Size = new System.Drawing.Size(228, 361);
            this.listViewImages.SmallImageList = this.imagesList;
            this.listViewImages.TabIndex = 0;
            this.listViewImages.UseCompatibleStateImageBehavior = false;
            this.listViewImages.View = System.Windows.Forms.View.List;
            this.listViewImages.DragDrop += new System.Windows.Forms.DragEventHandler(this.listViewImages_DragDrop);
            this.listViewImages.DragEnter += new System.Windows.Forms.DragEventHandler(this.listViewImages_DragEnter);
            // 
            // imagesList
            // 
            this.imagesList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imagesList.ImageStream")));
            this.imagesList.TransparentColor = System.Drawing.Color.Transparent;
            this.imagesList.Images.SetKeyName(0, "asd.bmp");
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(247, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(525, 361);
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.listViewImages);
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "Form1";
            this.Text = "ImageJoiner";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView listViewImages;
        private System.Windows.Forms.ImageList imagesList;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}

