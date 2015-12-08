// Eric Chen 11381898

using Spreadsheet_EChen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpreadsheetEngine
{
    public class RestoreBackColor : IUndoRedoCmd
    {
        private int m_cellColor;
        private string m_cellName;

        // Getter for the cell color and name
        public RestoreBackColor(int cellColor, string cellName)
        {
            m_cellColor = cellColor;
            m_cellName = cellName;
        }

        // Restores a cell with the original color
        public IUndoRedoCmd Restore(Spreadsheet ss)
        {
            Cell cell = ss.GetCell(m_cellName);
            int originalColor = cell.BackColor;     // Store original color

            cell.BackColor = m_cellColor;
            return new RestoreBackColor(originalColor, m_cellName);
        }
    }
}
