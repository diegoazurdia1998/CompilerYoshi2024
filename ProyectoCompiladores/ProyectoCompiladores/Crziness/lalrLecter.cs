using ProyectoCompiladores.Managers;
using ProyectoCompiladores.Structurs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProyectoCompiladores.Crziness
{
    public class lalrLecter
    {
        LR1Table table;
        GrammarVerifier Grammar;
        private Dictionary<(int stateIndex, string symbol), int> transitions;
        private Dictionary<(int stateIndex, string symbol), int> reductions;
        public List<Action> Actions { get; private set; }
        LALR1SymbolTable symbolsTable { get; set; }
        public lalrLecter(GrammarVerifier grammar) 
        { 
            table = new LR1Table(grammar.TerminalSymbols, grammar.NonTerminalSymbols);
            transitions = new Dictionary<(int stateIndex, string symbol), int>();
            reductions = new Dictionary<(int stateIndex, string symbol), int>();
            Actions = new List<Action>();
            symbolsTable = new LALR1SymbolTable();
            Grammar = grammar;
            LoadTransitions("lr1Transitions.txt");
            ReadLR1Table("lr1States.txt");

        }

        public LR1Table ReadLR1Table(string filePath)
        {
            State currentState = null;
            int stateInd = 0;
            foreach (var line in File.ReadLines(filePath))
            {
                var trimmedLine = line.Trim();

                // Si es un número de estado
                if (int.TryParse(trimmedLine, out int stateId))
                {
                    currentState = new State(stateId);
                    stateInd = stateId; 
                    table.States.Add(currentState);
                }
                else if (!string.IsNullOrEmpty(trimmedLine))
                {
                    // Procesar producción
                    var match = Regex.Match(trimmedLine, @"(\d+) (.+) \-> (.+) \[(.*?)\] at\((\d+),(.*?)\) \[(.*)\]");
                    if (match.Success)
                    {
                        int currentStateIndex = int.Parse(match.Groups[1].Value);
                        string left = match.Groups[2].Value.Trim();
                        List<string> right = match.Groups[3].Value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                        string currentItem = match.Groups[6].Value.Trim();
                        List<string> lookahead = match.Groups[7].Value.Split(',').ToList();
                        Action actions;
                        int dotIndex = int.Parse(match.Groups[5].ToString());
                        GrammarProduction prod = new GrammarProduction(left, right, new GrammarAction(match.Groups[4].Value.Split(",")));
                        if (Grammar.IsNonTerminal(currentItem))
                        {
                            actions = new Action("G", currentItem, transitions[(stateInd, currentItem)]);
                            Actions.Add(actions);
                        }
                        else if(Grammar.IsTerminal(currentItem) && !currentItem.Equals(""))
                        {
                            actions = new Action("S", currentItem, transitions[(stateInd, currentItem)]);
                            Actions.Add(actions);
                        }
                        if(dotIndex == right.Count)
                        {
                            GrammarProduction tem = new GrammarProduction(left, right, new GrammarAction());
                            int reduceIndex = Grammar.Productions.IndexOf(tem);
                            foreach (string la in lookahead)
                            {
                                if(reduceIndex > 0)
                                {
                                    reductions.Add((currentStateIndex, la), reduceIndex);
                                    actions = new Action("R", la, reduceIndex);
                                    Actions.Add(actions);
                                }
                            }
                        }
                        // Agregar producción al estado actual
                        currentState.Productions.Add(new LR1Item(new(prod, dotIndex), lookahead.ToHashSet()));

                        
                    }
                }
            }

            return table;
        }
        public void LoadTransitions(string filePath)
        {
            try
            {
                foreach (var line in File.ReadLines(filePath))
                {
                    // Limpiar la línea y omitir líneas vacías o encabezados
                    var cleanedLine = line.Trim();
                    if (string.IsNullOrEmpty(cleanedLine) || cleanedLine.StartsWith("indice del estado"))
                    {
                        continue;
                    }

                    // Separar la línea en partes
                    var parts = cleanedLine.Split("->", StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 3)
                    {
                        continue;
                    }

                    // Obtener el índice del estado actual, símbolo consumido y estado siguiente
                    int stateIndex = int.Parse(parts[0].Trim());
                    string symbol = parts[1].Trim();
                    int nextState = int.Parse(parts[2].Trim());

                    // Almacenar la transición en el diccionario
                    transitions[(stateIndex, symbol)] = nextState;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer el archivo: {ex.Message}");
            }
        }
        public void Parse(string filePath)
        {
            Console.WriteLine("Limpiando consola ...");
            Console.Clear();
            Console.WriteLine("Inicia el parseo\n\nLeyendo cadena");
            // Leer el contenido del archivo
            string input = File.ReadAllText(filePath);

            // Normalizar espacios en blanco (reemplazar múltiples espacios con uno solo)
            input = Regex.Replace(input, @"\s+", " ").Trim();

            // Dividir la cadena en tokens
            var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            var stateStack = new Stack<int>(); // Pila para los estados
            var symbolStack = new Stack<string>(); // Pila para los símbolos leídos
            stateStack.Push(0); // Comenzamos en el estado 0

            int index = 0; // Índice del token actual

            while (true)
            {
                int currentState = stateStack.Peek();
                string currentToken = index < tokens.Count ? tokens[index] : "$"; // "$" representa el fin de la entrada
                string tokenAux = currentToken; 
                if(Grammar.IsToken(tokenAux))
                {
                    currentToken = Grammar.GetTokenIdentifier(currentToken); // Si el currentToken coincide con algun identificador se asigna el identificador si no se queda igual
                    
                }
                // Buscar la acción correspondiente
                var action = GetAction(currentState, currentToken);
                if (action == null)
                {
                    Console.WriteLine($"Error de análisis: acción no encontrada.");
                    Console.WriteLine($"Top del stack: {currentState} con el token '{currentToken}'");
                    return;
                }
                int actionState = action.NextState;
                switch (action.Type)
                {
                    case "S": // Shift
                        
                        Console.WriteLine($"Shift{actionState} con el token: '{currentToken}'");
                        stateStack.Push(actionState);
                        symbolStack.Push(currentToken); // Agregar el símbolo leído a la pila
                        index++;
                        break;

                    case "R": // Reduce
                        Console.WriteLine($"Reduce{actionState} con el token: '{currentToken}'");
                        if(action.NextState > 0)
                        {
                            var production = Grammar.Productions[action.NextState]; // Obtén la producción correspondiente
                            int popCount = production.Right.Count; // Número de símbolos en el lado derecho de la producción
                            Console.WriteLine($"Produccion : {production.ToString()}");
                            // Verificar si los símbolos leídos coinciden con la producción
                            var symbolsToReduce = new List<string>();
                            for (int i = 0; i < popCount; i++)
                            {
                                if (symbolStack.Count > 0)
                                {
                                    string pop = symbolStack.Pop();
                                    if (pop.Equals(tokenAux))
                                        symbolsToReduce.Add(pop); // Desapilar los símbolos leídos
                                    else
                                        symbolsToReduce.Add(tokenAux);
                                }
                                else
                                {
                                    Console.WriteLine($"!!! No hay dimbolos para Reduce{actionState} con el token: '{currentToken}'");
                                }
                            }

                            // Verificar si los símbolos coinciden (en orden inverso)
                            var right = production.Right;
                            right.Reverse();
                            if (!symbolsToReduce.SequenceEqual(right))
                            {
                                Console.WriteLine("Error de análisis: los símbolos leídos no coinciden con la producción.");
                                Console.WriteLine($"Simbolos: {String.Join(" ", symbolsToReduce)}");
                                return;
                            }
                            Console.WriteLine("Reduccion correxta");
                            // Obtener el nuevo estado en la cima de la pila
                            int nextState = stateStack.Peek();
                            var gotoAction = GetAction(nextState, production.Left); // Obtener la acción de Goto
                            if (gotoAction == null)
                            {
                                Console.WriteLine("Error de análisis: acción de Goto no encontrada.");
                                Console.WriteLine($"Para el estado {nextState} con el simbolo '{production.Left}' no hay siguiente estado");
                                return;
                            }
                            Console.WriteLine($"Goto{gotoAction.NextState} con el token: '{production.Left}'");
                            stateStack.Push(gotoAction.NextState);

                            // Llamar a DoActions con la producción y los símbolos verificados
                            symbolsTable.DoActions(production, symbolsToReduce);
                        }
                        break;

                    case "G": // Goto
                        stateStack.Push(action.NextState);
                        break;

                    default:
                        Console.WriteLine("Error de análisis: tipo de acción desconocido.");
                        return;
                }

                // Si se ha alcanzado el estado de aceptación
                if (action.Type == "Accept")
                {
                    Console.WriteLine("Cadena aceptada.");
                    return;
                }
            }
        }
        private Action GetAction(int state, string token)
        {
            // Verificar si el estado y el token son válidos
            if (state < 0 || string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Estado o token no válidos.");
            }

            // Intentar obtener la acción de transición
            if (transitions.TryGetValue((state, token), out int actionInt))
            {
                return Actions.FirstOrDefault(a => a.Type != "R" && a.NextState == actionInt && a.Symbol == token);
            }

            // Si no se encontró una acción de transición, intentar obtener una acción de reducción
            if (reductions.TryGetValue((state, token), out actionInt))
            {
                return Actions.FirstOrDefault(a => a.Type == "R" && a.NextState == actionInt && a.Symbol == token);
            }

            // Si no se encontró ninguna acción, devolver null o lanzar una excepción
            return null; // O lanzar una excepción
        }
        public int? GetNextState(int stateIndex, string symbol)
        {
            // Intentar obtener el estado siguiente
            if (transitions.TryGetValue((stateIndex, symbol), out int nextState))
            {
                return nextState;
            }
            return null; // Si no se encuentra la transición
        }
        public class State
        {
            public int Id { get; set; }
            public List<LR1Item> Productions { get; set; }

            public State(int id)
            {
                Id = id;
                Productions = new List<LR1Item>();
            }
        }
        public class Action
        {
            public string Type { get; set; } // "Shift", "Reduce", "Goto"
            public string Symbol { get; set; }
            public int NextState { get; set; }
            private string Default = "Default";

            public Action(string type, string symbol, int nextState)
            {
                Type = type;
                Symbol = symbol;
                NextState = nextState;
            }
            public Action()
            {
                Type = Default;
                Symbol = Default;
                NextState= -1;
            }
            public int GetNext()
            {
                return NextState;
            }
            public override string ToString()
            {
                return $"{Type}: {Symbol}" + $" (Next State: {NextState})";
            }
        }
        public class LR1Table
        {
            public List<State> States { get; set; }
            public List<string> Terminals { get; set; }
            public List<string> NonTerminals { get; set; }
            Action accept {  get; set; }

            public LR1Table(List<string> t, List<string> nt)
            {
                States = new List<State>();
                Terminals = t;
                NonTerminals = nt;
                accept = new Action("Accept", "$", 0);
            }
        }
    }
    
}
