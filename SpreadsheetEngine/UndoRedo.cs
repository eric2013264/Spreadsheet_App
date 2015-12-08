// Eric Chen 11381898

using Spreadsheet_EChen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpreadsheetEngine
{
    // Undo Redo interface
    public interface IUndoRedoCmd
    {
        IUndoRedoCmd Restore(Spreadsheet sheet);
    }

    // Handles a collection of commandObjects
    public class UndoRedoCollection
    {
        private IUndoRedoCmd[] m_commandObjects;
        public string m_title;

        // Constructor
        public UndoRedoCollection()
        {
        }

        // Collection of command objects with accompanying title that tells the user what is going to be undone/redone
        public UndoRedoCollection(IUndoRedoCmd[] commandObjects, string title)
        {
            m_commandObjects = commandObjects;

            m_title = title;
        }
        
        // Overloaded for AddUndos which takes a list of commands
        public UndoRedoCollection(List<IUndoRedoCmd> commandObjects, string title)
        {
            m_commandObjects = commandObjects.ToArray();

            m_title = title;
        }

        // Calls each action in the UndoRedoCollection as Evan discussed in lecture.
        public UndoRedoCollection Restore(Spreadsheet sheet)
        {
            List<IUndoRedoCmd> cmdList = new List<IUndoRedoCmd>();

            foreach (IUndoRedoCmd cmd in m_commandObjects)
            {
                cmdList.Add(cmd.Restore(sheet));
            }

            return new UndoRedoCollection(cmdList.ToArray(), this.m_title);
        }
    }

    // Redo Undo
    public class UndoRedoClass
    {
        private Stack<UndoRedoCollection> m_undos = new Stack<UndoRedoCollection>();     // Privately declared undo stack
        private Stack<UndoRedoCollection> m_redos = new Stack<UndoRedoCollection>();     // Privately declared redo stack

        // Check if the undo stack is empty. If so, return false.
        public bool CanUndo
        {
            get
            {
                return m_undos.Count != 0;
            }
        }

        // Check if the redo stack is empty. If so, return false.
        public bool CanRedo
        {
            get
            {
                return m_redos.Count != 0;
            }
        }

        // Checks for next redo action and if it exists. If so return a text description of it
        public string UndoTask
        {
            get
            {
                if (CanUndo)    // Check if the undo stack is empty
                {
                    return m_undos.Peek().m_title;
                }
                return "";
            }
        }

        // Checks for next redo action and if it exists. If so, return a text description of it
        public string RedoTask
        {
            get
            {
                if (CanRedo)    // Check if the redo stack is empty
                {
                    return m_redos.Peek().m_title;
                }
                return "";
            }
        }

        // Adds an undo event to the undo stack
        public void AddUndos(UndoRedoCollection undos)
        {
            m_undos.Push(undos);
            m_redos.Clear();
        }

        // Performs an undo action off the undo stack
        public void Undo(Spreadsheet ss)
        {
            UndoRedoCollection actions = m_undos.Pop();

            m_redos.Push(actions.Restore(ss));
        }

        // Performs an redo action off the redo stack
        public void Redo(Spreadsheet ss)
        {
            UndoRedoCollection actions = m_redos.Pop();

            m_undos.Push(actions.Restore(sheet));
        }

        // Clear both the undo stack and the redo stack
        public void Clear()
        {
            m_undos.Clear();
            m_redos.Clear();
        }
    }
}
