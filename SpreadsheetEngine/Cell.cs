// Eric Chen 11381898

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Spreadsheet_EChen
{
    public abstract class Cell : INotifyPropertyChanged
    {
        protected string m_Text = "", m_Value = "";
        protected int m_BackColor = -1;
        private readonly int m_RowIndex, m_ColumnIndex;
        private readonly string m_Name;

        public event PropertyChangedEventHandler PropertyChanged;

        // Cell constructor
        public Cell(int RowIndex, int ColumnIndex)
        {
            m_RowIndex = RowIndex;
            m_ColumnIndex = ColumnIndex;
            m_Name += Convert.ToChar('A' + ColumnIndex);
            m_Name += (RowIndex + 1).ToString();
        }

        // Row property
        public int RowIndex { get { return m_RowIndex; } }

        // Column property
        public int ColumnIndex { get { return m_ColumnIndex; } }

        public string Text
        {
            get { return m_Text; }
            set
            {   // Same m_Text == Value
                if (m_Text == value) { return; }
                m_Text = value;

                PropertyChanged(this, new PropertyChangedEventArgs("Text"));
            }
        }
        // Cell name property
        public string Name { get { return m_Name; } }
        // Cell value property
        public string Value { get { return m_Value; } }

        // Cell background color property
        public int BackColor
        {
            get { return m_BackColor; }

            set
            {
                if (value != m_BackColor)
                {
                    m_BackColor = value;

                    PropertyChanged(this, new PropertyChangedEventArgs("BackColor"));   // Cell color changed event
                }
            }
        }

        public void Clear()
        {
            Text = "";
            BackColor = -1;
        }
    }
}
