using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpressionEngine
{
    public class Expression
    {
        // varDict is a dict that maps vars to their values
        private Dictionary<string, double> varDict = new Dictionary<string, double>();
        private Node root;                   // Root node of the ExpTree
        private string m_expressionString;         // Local var for user inputted expression string
        private double m_result;             // Double result of the expression, only used in ExpTree constructor
        public readonly static char[] operators = { '+', '-', '*', '/' };
        bool invalidInput = false;

        // Central base class for const, var, and op nodes to inherit from
        private abstract class Node
        {
            protected string _name;         // symbol/name of node, operand
            protected double _value;        // evaluated value

            public Node m_left, m_right;

            // Getters and setters
            public string getName()
            {
                return _name;
            }

            public double getValue()
            {
                return _value;
            }

            public void setValue(double number)
            {
                _value = number;
            }
        }

        // Node representing a binary operator
        private class OpNode : Node
        {
            // Constructor
            public OpNode(string name, Node left, Node right)
            {
                _name = name;
                m_left = left;
                m_right = right;
            }
        }

        // Node representing a VarNode
        private class VarNode : Node
        {
            // Constructor
            public VarNode(string name)
            {
                _name = name;
            }
        }

        // Node representing a constant numerical value
        private class ConstNode : Node
        {
            // Constructor
            public ConstNode(double number)
            {
                _value = number;
            }
        }

        // Function covered in lecture that finds the next operator in the expression and returns -1 if null
        private int LocateOperator(string expression)
        {
            for (int i = 0; i < expression.Length; i++)
            {
                // Look for the first operator, so we reverse the order of operations.
                if ((expression[i] == '*') || (expression[i] == '/') || (expression[i] == '+') || (expression[i] == '-'))
                {
                    return i;
                }
            }
            return -1;
        }

        public string ExpressionString
        {
            get { return m_expressionString; }
            set 
            {
                // On set, clear variable dictionary and recompile tree as well as setting the string.
                m_expressionString = value;
                varDict.Clear();
                root = Compile(m_expressionString);
            }
        }

        public Expression()
        {
            varDict = new Dictionary<string, double>();
        }

        public Expression(string expString)
        {
            string result = "";
            if (expString.Contains(' '))
            {
                foreach (char letter in expString)
                {
                    if (letter != ' ')
                    {
                        result += letter;
                    }
                }
            }
            else
            {
                result = expString;
            }

            // Not using ExpString's set because it would unnecessarily clear the dictionary.
            m_expressionString = result;
            varDict = new Dictionary<string, double>();
            root = Compile(m_expressionString);
        }

        // Constructs the tree
        private Node Compile(string expression)
        {
            int parenthesisCounter = 0;
            char[] ops = Expression.operators;

            // Check for empty or null expression
            if ((expression == "") || (expression == null))
            {
                return null;
            }

            // Start looking for a right parenthesis if a left parenthesis is found at the start of the string.
            if (expression[0] == '(')
            {
                // For each char in the expression
                for (int i = 0; i < expression.Length; i++)
                {
                    if (expression[i] == '(')       // If we hit a left parenthesis
                    {
                        parenthesisCounter++;       // Increment the counter
                    }
                    else if (expression[i] == ')')  // If we hit a corresponding right parenthesis
                    {
                        parenthesisCounter--;       // Decrement the counter

                        if (parenthesisCounter == 0)// If != 0, parenthesis did not match up
                        {
                            // If we're done
                            if (expression.Length - 1 != i)
                            {
                                break;
                            }
                            // If we're not done, keep recursively evaluating the substrings
                            else
                            {
                                return Compile(expression.Substring(1, expression.Length - 2));
                            }
                        } // if
                    } // else if
                } // for
            } // if

            // Evaluates operators by precedence
            foreach (char op in ops)
            {
                Node n = Compile(expression, op);
                if (invalidInput == true)
                {
                    break;
                }
                if (n != null)
                {
                    return n;
                }
            }

            // If constant
            double constDouble;
            if (double.TryParse(expression, out constDouble))
            {
                return new ConstNode(constDouble);
            }
            // If var & found in dict
            else
            {
                varDict[expression] = 0;
                return new VarNode(expression);
            }

            int index = LocateOperator(expression), constDoubleValue;

            if (index == -1)    // Operator not found
            {                   // Assume its a const
                if (int.TryParse(expression, out constDoubleValue))
                {
                    return new ConstNode(constDoubleValue);
                }
                else            // If it's not a op or const, its gotta be a var
                {
                    return new VarNode(expression);
                }
            }

            Node Left = Compile(expression.Substring(0, index));        // Left and right recursive steps
            Node Right = Compile(expression.Substring(index + 1));      // Alg given by Evan in lecture

            return new OpNode(expression[index].ToString(), Left, Right);
        }

        // Function that checks for matching parenthesis and removes extraeneous parenthesis while moving arguments into their respective spots.
        private Node Compile(string exp, char op)
        {
            int parenthesisCounter = 0;
            bool quit = false;
            bool rightAssociative = false; // Default to left

            int i = exp.Length - 1; // Left associative

            while (!quit)
            {
                // If left parenthesis
                if (exp[i] == '(')
                {
                    if (rightAssociative) parenthesisCounter--;   // and right associative, decrement
                    else parenthesisCounter++;                      // and left associative, increment
                }
                // If right parenthesis
                else if (exp[i] == ')')
                {
                    if (rightAssociative) parenthesisCounter++;   // and right associative, increment
                    else parenthesisCounter--;                      // and left associative, decrement
                }

                // If it's not in parenthesis
                if (parenthesisCounter == 0 && exp[i] == op)
                {
                    // Set current op as the root and evaluate
                    return new OpNode(op.ToString(), Compile(exp.Substring(0, i)), Compile(exp.Substring(i + 1)));
                }

                // If right associative
                if (rightAssociative)
                {
                    if (i == exp.Length - 1)  // Reached the end
                    {
                        quit = true;       // We're done!
                    }
                    i++;
                }
                // If left associative
                else
                {
                    if (i == 0)         // Reached the end/"start"
                    {
                        quit = true;   // We're done!
                    }
                    i--;
                }
            }

            // Parenthesis counter != 0, parenthesis did not match up
            if (parenthesisCounter != 0)
            {
                Console.WriteLine("Invalid Expression: Unbalanced Parenthesis");
                invalidInput = true;
            }
            return null;
        }

        // Function evaluates based on parameter: current node 
        private double Eval(Node n)
        {
            if (n == null)  // Check for null node
            {
                return -1;
            }

            ConstNode constn = n as ConstNode;  // Check for const node
            if (constn != null)
            {
                return constn.getValue();
            }

            VarNode varn = n as VarNode;  // Check for variable node
            if (varn != null)
            {
                try
                {
                    return varDict[n.getName()];
                }
                catch (Exception c)
                {
                    Console.WriteLine("Key Not Found: Attempting To Use Undefined Variable");
                }
            }

            OpNode opn = n as OpNode;  // Check for operand node
            if (opn != null)
            {

                if (opn.getName() == "+")      // Addition: left expression tree + right
                {
                    return (Eval(opn.m_left) + Eval(opn.m_right));
                }

                else if (opn.getName() == "-") // Subtraction: left expression tree - right
                {
                    return (Eval(opn.m_left) - Eval(opn.m_right));
                }

                else if (opn.getName() == "*")  // Multiplication: left expression tree * right
                {
                    return (Eval(opn.m_left) * Eval(opn.m_right));
                }

                else if (opn.getName() == "/")  // Multiplication: left expression tree / right
                {
                    return (Eval(opn.m_left) / Eval(opn.m_right));
                }
            }
            return 0.0;
        }

        // Evaluates the expression to a double value
        public double Eval()
        {
            return Eval(root);
        }

        // Function that sets the specified variabel variabel within the ExpTree variables dict
        public void SetVar(string varName, double varValue)
        {
            varDict[varName] = varValue;
        }
        // Returns all variable names in this expression.
        //A string array containing all variable names in this expression.</returns>
        public string[] GetAllVariables()
        {
            return varDict.Keys.ToArray();
        }

    }   // public class Expression
} // namespace ExpressionEngine

