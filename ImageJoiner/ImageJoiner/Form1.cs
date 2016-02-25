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
        public Form1()
        {
            InitializeComponent();
        }

        private void listViewImages_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if(files.Length != 0)
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
    }
}
