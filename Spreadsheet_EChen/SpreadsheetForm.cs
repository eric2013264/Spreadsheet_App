// Eric Chen 11381898

using SpreadsheetEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace Spreadsheet_EChen
{
    public partial class SpreadsheetForm : Form
    {
        private Spreadsheet ss = new Spreadsheet(50, 26);
        public UndoRedoClass UndoRedo = new UndoRedoClass();

        public SpreadsheetForm()
        {
            InitializeComponent();

            ss.CellPropertyChanged += OnCellPropertyChanged;
            dataGridView1.CellBeginEdit += dataGridView1_CellBeginEdit;
            dataGridView1.CellEndEdit += dataGridView1_CellEndEdit;

            DataTable dt = new DataTable();
            string namestring = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";   // For the column

            for (int i = 0; i <= 25; i++) // Create columns
            {
                dataGridView1.Columns.Add(namestring[i].ToString(), namestring[i].ToString());
            }

            dataGridView1.RowHeadersWidth = 65; // Row number was being blocked
            for (int x = 0; x < 50; x++) // Create rows
            {
                dataGridView1.Rows.Add();
                dataGridView1.Rows[x].HeaderCell.Value = ((x + 1).ToString());
            }

            UpdateMenuText();  // If the undo/redo stack is empty, disable the corresponding menu items.
        }

        void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            int row = e.RowIndex, column = e.ColumnIndex;
            // Get the cell
            Cell spreadsheetCell = ss.GetCell(row, column);
            // dataGridView Cell value = spreadsheet Cell Text
            dataGridView1.Rows[row].Cells[column].Value = spreadsheetCell.Text;
        }

        // CellEndEdit given in lecture by Evan. Writes new cell value to cell.
        void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            int row = e.RowIndex, column = e.ColumnIndex;
            string m_Text;
            IUndoRedoCmd[] undos = new IUndoRedoCmd[1];

            Cell spreadsheetCell = ss.GetCell(row, column);     // Get the cell

            try
            {
                m_Text = dataGridView1.Rows[row].Cells[column].Value.ToString();    // dataGridView cell text = cell's Text
            }
            catch (NullReferenceException)
            {
                m_Text = "";
            }

            undos[0] = new RestoreText(spreadsheetCell.Text, spreadsheetCell.Name); // Restore text

            spreadsheetCell.Text = m_Text;      // Set text in the spreadsheet cell

            UndoRedo.AddUndos(new UndoRedoCollection(undos, "cell text change"));   // Undos added to UndoRedoClass

            dataGridView1.Rows[row].Cells[column].Value = spreadsheetCell.Value;    // Set value in the spreadsheet cell
            UpdateMenuText();   // Refresh the undo redo menu text
        }

        // Menu item that gracefully exits the applicaiton. FILE>Exit
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // Property Changed handler for cell text and color
        private void OnCellPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Cell selectedCell = sender as Cell;

            if (e.PropertyName == "Value" && selectedCell != null)      // If the value changed, also check that it's not null
            {
                dataGridView1.Rows[selectedCell.RowIndex].Cells[selectedCell.ColumnIndex].Value = selectedCell.Value;
            }
            if (e.PropertyName == "BackColor" && selectedCell != null)  // If the cell color changed, also check that it's not null
            {
                dataGridView1.Rows[selectedCell.RowIndex].Cells[selectedCell.ColumnIndex].Style.BackColor = Color.FromArgb(selectedCell.BackColor); // FormArgb method
            }
        }

        // Menu item for HW4's demo button. FILE>Demo
        private void demoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Random rnd = new Random();
            // 50 randomly placed "Hello World"s
            for (int i = 0; i < 50; i++)
            {
                int row = rnd.Next(0, 49);
                int column = rnd.Next(2, 25); // columns start from >2 so nothing conflicts

                ss.Array2D[row, column].Text = "Hello World!";
            }
            // Fill column B
            for (int i = 0; i < 50; i++) { ss.Array2D[i, 1].Text = "This is cell B" + (i + 1).ToString(); }
            // Fill column A with contents of column B
            for (int i = 0; i < 50; i++) { ss.Array2D[i, 0].Text = "=B" + (i + 1).ToString(); }
        }

        // Menu item for changing cell background color. Changes the background color of all selected cells. CELL>Choose Background Color
        private void chooseBackgroundColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int selectedColor = 0;
            List<IUndoRedoCmd> undos = new List<IUndoRedoCmd>();
            ColorDialog colorDialog = new ColorDialog();

            if (colorDialog.ShowDialog() == DialogResult.OK)    // Prompt user to choose a color
            {
                selectedColor = colorDialog.Color.ToArgb();

                foreach (DataGridViewCell cell in dataGridView1.SelectedCells)
                {
                    Cell spreadsheetCell = ss.GetCell(cell.RowIndex, cell.ColumnIndex);                 // Get each selected cell from spreadsheet

                    undos.Add(new RestoreBackColor(spreadsheetCell.BackColor, spreadsheetCell.Name));   // Color change undo

                    spreadsheetCell.BackColor = selectedColor;  // Set cell to the selected color
                }
                UndoRedo.AddUndos(new UndoRedoCollection(undos, "cell background color change"));
                UpdateMenuText();
            }
        }

        // Menu item for undo. EDIT>Undo
        private void undoToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            UndoRedo.Undo(ss);
            UpdateMenuText();  // Check if we can undo again
        }

        // Menu item for redo. EDIT>Redo
        private void redoToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            UndoRedo.Redo(ss);
            UpdateMenuText();  // Check if we can redo again
        }

        // Updates the tool strip menu items. Namely undo and redo based on if you can and what actions the user just did.
        private void UpdateMenuText()
        {
            ToolStripMenuItem menuItems = menuStrip1.Items[1] as ToolStripMenuItem;

            foreach (ToolStripItem item in menuItems.DropDownItems)     // Check undo and redo
            {
                if (item.Text.Substring(0, 4) == "Undo")
                {
                    item.Enabled = UndoRedo.CanUndo;                    // Is the stack empty, if not, don't enable to menu item
                    item.Text = "Undo " + UndoRedo.UndoTask;            // Change the undo text based on the stack item
                }
                else if (item.Text.Substring(0, 4) == "Redo")
                {
                    item.Enabled = UndoRedo.CanRedo;                    // Is the stack empty, if not, don't enable to menu item
                    item.Text = "Redo " + UndoRedo.RedoTask;            // Change the redo text based on the stack item
                }
            }
        }

        // Menu item for loading from a stream. Assumed to be dealing with a valid XML file. FILE>Load
        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == DialogResult.OK) // So we don't clear before confirming
            {
                Clear();                                        // Clear all spreadsheet data before loading file data, this calls cell.Clear()

                Stream infile_filestream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read); // Open with permission to read
                ss.Load(infile_filestream);                     // Load to spreadsheet ss

                infile_filestream.Dispose();                    // Dispose after done

                UndoRedo.Clear();                               // Clear undo/redo stacks after loading a file
            }

            UpdateMenuText();       // Update for redo/undo
        }

        // Menu item for saving the current spreadsheet to a stream. Assumed to be dealing with a valid XML file. FILE>Save
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                Stream outfile_filestream = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write); // Create with permission to write
                ss.Save(outfile_filestream);        // Save spreadsheet ss

                outfile_filestream.Dispose();       // Dispose after done
            }
        }
        
        // Clears the spreadsheet
        public void Clear()
        {
            int m_rowCount = ss.RowCount, m_columnCount = ss.ColumnCount;

            for (int i = 0; i < m_rowCount; i++)
            {
                for (int j = 0; j < m_columnCount; j++)
                {
                    if (ss.Array2D[i, j].Text != "" || ss.Array2D[i, j].Value != "" || ss.Array2D[i, j].BackColor != -1)    // Only changed cells
                    {
                        ss.Array2D[i, j].Clear();   // Cell.Clear()
                    }
                }
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {

            Clear();
        }


    }
}
