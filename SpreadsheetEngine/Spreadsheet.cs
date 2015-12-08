// Eric Chen 11381898

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Spreadsheet_Echen;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using SpreadsheetEngine;    // for undoredo class

namespace Spreadsheet_EChen
{
    public class Spreadsheet
    {
        public Cell[,] Array2D;

        public event PropertyChangedEventHandler CellPropertyChanged;
        private Dictionary<string, HashSet<string>> dependencyDict;

        public UndoRedoClass UndoRedo = new UndoRedoClass();

        private class InstanciateCell : Cell
        {
            // Cell constructor
            public InstanciateCell(int row, int col)
                // Base keyword https://msdn.microsoft.com/en-us/library/hfw7t1ce.aspx
                : base(row, col)
            {
            }
            public void SetValue(string value) { m_Value = value; }
        }

        public Spreadsheet(int RowIndex, int ColumnIndex)
        {
            Array2D = new Cell[RowIndex, ColumnIndex];

            dependencyDict = new Dictionary<string, HashSet<string>>();

            for (int x = 0; x < RowIndex; x++)
            {
                for (int y = 0; y < ColumnIndex; y++)
                {
                    Array2D[x, y] = new InstanciateCell(x, y);
                    Array2D[x, y].PropertyChanged += OnPropertyChanged;
                }
            }
        }

        public int RowCount
        {
            get { return Array2D.GetLength(0); }
        }

        public int ColumnCount
        {
            get { return Array2D.GetLength(1); }
        }

        // Function that takes a row and column index and returns the cell at that location
        public Cell GetCell(int row, int col)
        {
            return Array2D[row, col];

        }

        // Function that takes a location string and returns the cell at that location, null otherwise
        public Cell GetCell(string location)
        {
            char letter = location[0];
            Int16 number;
            Cell result;

            if (!Char.IsLetter(letter)) // Doesn't begin with letter
            {
                return null;
            }

            if (!Int16.TryParse(location.Substring(1), out number)) // Doesn't have number
            {
                return null;
            }

            try
            {
                result = GetCell(number - 1, letter - 'A');
            }
            catch (Exception c)
            {
                return null;
            }

            return result;  // Return cell
        }

        // Evaluates a cell based on cell text
        private void Eval(Cell cell)
        {
            InstanciateCell m_Cell = cell as InstanciateCell;

            bool reference_error = false;

            // CELL CASE 1: check for empty
            if (string.IsNullOrEmpty(m_Cell.Text))
            {
                m_Cell.SetValue("");
                CellPropertyChanged(cell, new PropertyChangedEventArgs("Value"));
            }
            // CELL CASE 2: f it is an expression...
            else if (m_Cell.Text[0] == '=' && m_Cell.Text.Length > 1)   
            {
                // Parse out the "="
                string expString = m_Cell.Text.Substring(1);
                Spreadsheet_Echen.Expression m_expression = new Spreadsheet_Echen.Expression(expString);
                string[] variables = m_expression.GetAllVariables();

                // Set vars
                foreach (string varName in variables)
                {
                    // REFERENCES CASE 1: Bad reference
                    if (GetCell(varName) == null)
                    {
                        m_Cell.SetValue("!(Bad Reference)");    // Print error message to cell
                        CellPropertyChanged(cell, new PropertyChangedEventArgs("Value"));   // Value changed, add to undo stack

                        reference_error = true;
                        break;
                    }

                    // Try setting variable to the value
                    SetExpressionVariable(m_expression, varName);
                    
                    // REFERENCES CASE 2: Self reference
                    if (varName == m_Cell.Name)
                    {
                        m_Cell.SetValue("!(Self Reference)");   // Print error message to cell
                        CellPropertyChanged(cell, new PropertyChangedEventArgs("Value"));   // Value changed, add to undo stack

                        reference_error = true;
                        break;
                    }
                }

                if (reference_error) { return; } // Get out of here after we find a self reference, prevents a stack overflow

                // REFERENCES CASE 3: Circular reference
                foreach (string varName in variables)
                {
                    if (HasCircularReference(varName, m_Cell.Name))
                    {
                        m_Cell.SetValue("!(Circular Reference)");   // Print error message to cell
                        CellPropertyChanged(cell, new PropertyChangedEventArgs("Value"));   // Value changed, add to undo stack

                        reference_error = true;
                        break;
                    }
                }

                if (reference_error) { return; } // Get out of here after we find a circular reference, prevents a stack overflow

                m_Cell.SetValue(m_expression.Eval().ToString());
                CellPropertyChanged(cell, new PropertyChangedEventArgs("Value"));
            }
            // CELL CASE  3: Not an expression
            else
            {
                m_Cell.SetValue(m_Cell.Text);
                CellPropertyChanged(cell, new PropertyChangedEventArgs("Value"));
            }

            // Evaluate all dependencies that contain each key
            if (dependencyDict.ContainsKey(m_Cell.Name))
            {
                foreach (string dependentCell in dependencyDict[m_Cell.Name])
                {
                    Eval(dependentCell);
                }
            }

        }

        // Evaluates cell based on location string
        private void Eval(string location)
        {
            Eval(GetCell(location));
        }

        // By cell name, value, this function makes dependencies
        private void MakeDependencies(string cellName, string[] variablesUsed)
        {
            foreach (string varName in variablesUsed)
            {
                if (!dependencyDict.ContainsKey(varName))
                {
                    // Build dictionary entry for this variable name.
                    dependencyDict[varName] = new HashSet<string>();
                }

                // Add this cell name to dependencies for this variable name.
                dependencyDict[varName].Add(cellName);
            }
        }

        // Function that removes dependencies by cell name or cell value
        private void RemoveDependencies(string cellName)
        {
            List<string> dependenciesList = new List<string>();

            foreach (string s in dependencyDict.Keys)
            {   // At each cellName with key, at to list
                if (dependencyDict[s].Contains(cellName)) { dependenciesList.Add(s); }
            }

            foreach (string s in dependenciesList)
            {   // Remove the corresponding name from the list for each key.
                HashSet<string> set = dependencyDict[s];
                if (set.Contains(cellName)) { set.Remove(cellName); }
            }
        }


        // Based on the empression, sets variable to the value of cell.
        private void SetExpressionVariable(Expression exp, string varName)
        {
            Cell varCell = GetCell(varName);
            double value;

            // CASE 1: Empty, set it to 0
            if (string.IsNullOrEmpty(varCell.Value))
            {
                exp.SetVar(varCell.Name, 0);
            }

            // CASE 2: Not a value, set it to 0
            else if (!double.TryParse(varCell.Value, out value))
            {
                exp.SetVar(varName, 0);
            }

            // CASE 3: All good, set variable to the value
            else
            {
                exp.SetVar(varName, value);
            }
        }

        // Saves spreadsheet to a stream. XML file. Only "writes data from cells that have one or more non-default properties".
        public void Save(Stream outfile)
        {
            XmlWriter writeXml = XmlWriter.Create(outfile);
            // Start element Spreadsheet
            writeXml.WriteStartElement("Spreadsheet");

            foreach (Cell m_cell in Array2D)
            {
                if (m_cell.Text != "" || m_cell.Value != "" || m_cell.BackColor != -1)  // Check if cell has default values, why write blank cells?
                {   // Start element cell
                    writeXml.WriteStartElement("cell"); // Formatted to match HW9 guildlines

                    // Element string
                    writeXml.WriteElementString("name", m_cell.Name.ToString());
                    writeXml.WriteElementString("backgroundcolor", m_cell.BackColor.ToString());
                    writeXml.WriteElementString("text", m_cell.Text.ToString());

                    // End element cell
                    writeXml.WriteEndElement();
                }
            }
            // End element Spreadsheet
            writeXml.WriteEndElement();

            writeXml.Close();   // Close it after writing to it
        }

        // Loads file from a stream, reads and parses out background color and text value changes and is "resilient to XML that has different ordering as well as extra tags."
        public void Load(Stream infile)
        {
            XDocument fromXml = XDocument.Load(infile);

            foreach (XElement tag in fromXml.Root.Elements("cell"))
            {
                Cell m_cell = GetCell(tag.Element("name").Value);       // We could do this by row/col number too

                if (tag.Element("text") != null)                        // Text has a value, aka isn't default
                {
                    m_cell.Text = tag.Element("text").Value.ToString(); // Set cell text to the tag's text element value
                }
                if (tag.Element("backgroundcolor") != null)             // Background color has a value, aka isn't default
                {
                    m_cell.BackColor = int.Parse(tag.Element("backgroundcolor").Value.ToString());  // Set cell background color to the tag's backgroundcolor element value
                }
            }
        }

        // Propertychanged handler for spreadsheet that handles changes in cell text and color
        public void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Text")
            {
                InstanciateCell tmpCell = sender as InstanciateCell;
                RemoveDependencies(tmpCell.Name);

                if (tmpCell.Text != "" && tmpCell.Text[0] == '=' && tmpCell.Text.Length > 1)
                {
                    Expression exp = new Expression(tmpCell.Text.Substring(1)); // Parsed expression
                    MakeDependencies(tmpCell.Name, exp.GetAllVariables()); // Log the dependencies in the expression
                }
                Eval(sender as Cell);
            }
            else if (e.PropertyName == "BackColor")
            {
                CellPropertyChanged(sender, new PropertyChangedEventArgs("BackColor"));
            }
        }

        // Recursive function that checks the dependencies of the current cell for ciruclar references. Returns true if found.
        public bool HasCircularReference(string varName, string currCell)
        {
            // CASE 1: Current cell = starting cell. Circular reference found
            if (varName == currCell)
            {
                return true;
            }

            // CASE 2: Check the dictionary first, if the key is not found in dependencyDict. No circular reference.
            if (!dependencyDict.ContainsKey(currCell))
            {
                return false;
            }

            // CASE 3: Seems like there might be a circular reference... recursively check for circular references in all dependent cells!
            foreach (string dependentCell in dependencyDict[currCell])
            {
                if (HasCircularReference(varName, dependentCell))
                {
                    return true;
                    // break;           // Debug/just in case an overflow occurs.
                }
            }
            
            return false;   // No circular reference
        }

    } // Class Spreadsheet
} // namespace Spreadsheet
