using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using ProyectoCompiladores.Structurs;

namespace ProyectoCompiladores.Managers
{
    public class LALR1SymbolTable
    {
        public List<Symbol> Symbols { get; private set; }
        public Dictionary<string, Symbol> VariableTable { get; private set; }
        public Dictionary<string, Symbol> ProcedureTable { get; private set; }
        public Stack<Structurs.Scope> ScopeStack { get; private set; }
        public List<string> Errors { get; private set; }
        public List<Parameter> CurrentParameters { get; private set; }
        public List<string> CurrentDeclarations { get; private set; }
        public List<string> TypeList { get; private set; }

        public LALR1SymbolTable()
        {
            Symbols = new List<Symbol>();
            VariableTable = new Dictionary<string, Symbol>();
            ProcedureTable = new Dictionary<string, Symbol>();
            ScopeStack = new Stack<Structurs.Scope>();
            Errors = new List<string>();
            CurrentParameters = new List<Parameter>();
            CurrentDeclarations = new List<string>();
            TypeList = new List<string>();
        }
        public void DoActions(GrammarProduction production, List<string> values)
        {
            foreach (var action in production.Action.Actions)
            {
                switch (action)
                {
                    // save_program
                    case "save_program":

                        break;
                    // save_block
                    // save_declarations
                    // keep_value
                    // save_var_declaration
                    // save_procedure_declaration
                    // save_parameter_list
                    // save_parameter_list_ext
                    // save_type
                    // save_compound_statement
                    // save_statement_list
                    // save_statement_list_ext
                    // save_statement
                    // save_assignment
                    // save_if_statement
                    // save_else_statement
                    // save_while_statement
                    // save_procedure_call
                    // save_argument_list
                    // save_argument_list_ext
                    // save_io_statement
                    // save_expression
                    // save_expression_extension
                    // save_simple_expression
                    // save_term
                    // save_factor
                    // save_identifier
                    // save_number
                    // save_operator
                    // save_string
                    // save_boolean
                    case "save_boolean":

                        break;

                    // Casos Tabla de simbolos
                    // setscope_global
                    // add_procedure
                    // check_variable_exists
                    // add_variable
                    // check_procedure_exists
                    // add_procedure
                    // setscope_local
                    // add_procedure_parameters
                    // keep_value --- Casos de reducir por epsilon
                    // assign_value
                    // 

                    default:

                        break;

                }
            }
        }
        private void Save1Symbol(Symbol symbol)
        {

        }
        private void AddVariable(Symbol variable)
        {
            if (VariableTable.ContainsKey(variable.Identifier))
            {
                Errors.Add($"Error: La variable '{variable.Identifier}' ya ha sido declarada.");
            }
            else
            {
                VariableTable[variable.Identifier] = variable;
                Symbols.Add(variable);
            }
        }

        private void AddProcedure(Symbol procedure)
        {
            if (ProcedureTable.ContainsKey(procedure.Identifier))
            {
                Errors.Add($"Error: El procedimiento '{procedure.Identifier}' ya ha sido declarado.");
            }
            else
            {
                ProcedureTable[procedure.Identifier] = procedure;
                Symbols.Add(procedure);
            }
        }
    }
}
