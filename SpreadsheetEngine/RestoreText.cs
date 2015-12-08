// Eric Chen 11381898

using Spreadsheet_EChen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpreadsheetEngine
{
    public class RestoreText : IUndoRedoCmd
    {
        private string m_Text, m_cellName;

        // Getter for the cell text and name
        public RestoreText(string cellText, string cellName)
        {
            m_Text = cellText;
            m_cellName = cellName;
        }

        // Restores a cell with the original text
        public IUndoRedoCmd Restore(Spreadsheet ss)
        {
            Cell cell = ss.GetCell(m_cellName);
            string originalText = cell.Text;      // Store original text

            cell.Text = m_Text;
            return new RestoreText(originalText, m_cellName);
        }
    }
}
