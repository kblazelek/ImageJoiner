using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageJoiner
{
    public partial class FormAskForRowAndColumnNumeration : Form
    {
        public RowAndColumnNumeration rowAndColumnNumeration;
        public FormAskForRowAndColumnNumeration()
        {
            InitializeComponent();
            comboBox1.DataSource = Enum.GetValues(typeof(RowAndColumnNumeration));
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Enum.TryParse<RowAndColumnNumeration>(comboBox1.SelectedValue.ToString(), out rowAndColumnNumeration);
            this.DialogResult = DialogResult.OK;
        }
    }
}
