using OfficeOpenXml;
using ProyectoCompiladores.Structurs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static OfficeOpenXml.ExcelErrorValue;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;

namespace ProyectoCompiladores.Managers
{
    public enum Action
    {
        Shift,
        Goto,
        Reduce,
        Accept,
        Error
    }
    public class LALRParserGenerator
    {
        private GrammarVerifier Grammar;
        private LALR1SymbolTable lALR1SymbolTable;
        private List<GrammarProduction> Productions;
        private Dictionary<string, List<List<string>>> GrammarRules;
        private Dictionary<(int, string), (Action, int, GrammarAction)> LALRActionsTable;
        private Dictionary<(int, string), HashSet<(Action, int, GrammarAction)>> LALRConflictsTable;
        private Dictionary<string, bool> Nullable;
        private Dictionary<string, HashSet<string>> First;
        private Dictionary<string, Dictionary<string, HashSet<string>>> Follow;
        private Dictionary<(int, string), int> Transitions;
        private string ExtensionSymbol;
        private HashSet<LR0Item> initialState;
        private HashSet<int> ResolutedConflictIndex;
        private List<HashSet<LR0Item>> LR0States;
        private List<HashSet<LR1Item>> LR1States;
        public LALRParserGenerator(GrammarVerifier grammar)
        {
            Grammar = grammar;
            GrammarRules = GenerateProductionsDictionary();
            Productions = new(grammar.Productions);
            LALRActionsTable = new Dictionary<(int, string), (Action, int, GrammarAction)>();
            LALRConflictsTable = new Dictionary<(int, string), HashSet<(Action, int, GrammarAction)>>();
            Transitions = new Dictionary<(int, string), int>();
            ResolutedConflictIndex = new HashSet<int>();
            LR0States = new List<HashSet<LR0Item>>();
            LR1States = new List<HashSet<LR1Item>>();
            ExtensionSymbol = "SSymbol";
            initialState = GetInitialState();
            GenerateNullable();
            GenerateTable();
        }
        public Dictionary<(int, string), (Action, int, GrammarAction)> GetParsingTable()
        {
            return LALRActionsTable;
        }
        public void GenerateTable()
        {
            // Aquí iría la lógica para construir la tabla LALR(1)
            // 1. Crear estados LR(0) a partir de las reglas de producción
            var states = GenerateLR0States();
            LR0States = states._lr0States;
            ExportStates(states);
            // 3. Llenar la tabla de parseo LALR(1)
            BuildLALRTable(states._lr0States, states._transitions);
            ExportActionsTable();
        }
        public bool Parse(string filePath)
        {
            Console.WriteLine("PARSING");
            List<string> input = ExtractSymbols(filePath);
            lALR1SymbolTable = new LALR1SymbolTable();

            // Inicializar la pila de estados y la pila de símbolos
            Stack<int> stateStack = new Stack<int>();
            Stack<string> symbolStack = new Stack<string>();

            // Estado inicial
            stateStack.Push(0);
            input.Add("$"); // Agregar símbolo de fin de entrada
            int index = 0; // Índice de la entrada

            while (true)
            {
                // Obtener el estado actual y el símbolo de entrada
                int currentState = stateStack.Peek();
                string currentSymbol = input[index];

                // Si es un token, obtener su identificador
                if (Grammar.IsToken(currentSymbol))
                {
                    currentSymbol = Grammar.GetTokenIdentifier(currentSymbol);
                }

                Console.WriteLine($"Estado: {currentState}, Current (symbol: {currentSymbol})");

                // Consultar la tabla de acciones
                if (LALRConflictsTable.TryGetValue((currentState, currentSymbol), out var actions))
                {
                    // Resolver conflictos si hay múltiples acciones
                    bool result = ResolveActions(actions.ToList(), stateStack, symbolStack, ref index, currentSymbol, input);
                    if (!result)
                    {
                        return false; // Salir si hubo un error
                    }
                }
                else
                {
                    // Manejo de error si no se encuentra la acción
                    Console.WriteLine($"Error: no se puede encontrar acción para estado {currentState} y símbolo '{currentSymbol}'");
                    return false;
                }
            }
        }

        private bool ResolveActions(List<(Action a, int i, GrammarAction g)> actions,
            Stack<int> stateStack, Stack<string> symbolStack, ref int index, string currentSymbol, List<string> input)
        {
            // Aquí puedes implementar la lógica para resolver conflictos
            // Por ejemplo, priorizar Shift sobre Reduce
            var shiftActions = actions.Where(a => a.a == Action.Shift).ToList();
            var reduceActions = actions.Where(a => a.a == Action.Reduce).ToList();

            if (shiftActions.Count > 0)
            {
                // Si hay acciones de Shift, priorizarlas
                foreach (var action in shiftActions)
                {
                    if (!SwitchActions(true, action, stateStack, symbolStack, ref index, currentSymbol, input))
                    {
                        return false; // Salir si hubo un error
                    }
                }
            }
            else if (reduceActions.Count > 0)
            {
                // Si no hay Shift, pero hay Reduce, ejecutar la primera
                return SwitchActions(true, reduceActions.First(), stateStack, symbolStack, ref index, currentSymbol, input);
            }
            else
            {
                // Manejo de error si no hay acciones válidas
                Console.WriteLine("Error: no hay acciones válidas para resolver.");
                return false;
            }

            return true;
        }

        private bool SwitchActions(bool returnValue, (Action a, int i, GrammarAction g) action,
            Stack<int> stateStack, Stack<string> symbolStack, ref int index, string currentSymbol, List<string> input)
        {
            switch (action.Item1)
            {
                case Action.Shift:
                    return HandleShift(action.Item2, stateStack, symbolStack, currentSymbol, ref index);

                case Action.Reduce:
                    return HandleReduce(action.Item2, stateStack, symbolStack, input, ref index);

                case Action.Error:
                    Console.WriteLine($"Error de sintaxis en la entrada en la posición {index}");
                    return false;

                case Action.Accept:
                    Console.WriteLine("Entrada aceptada.");
                    return true;

                default:
                    Console.WriteLine("Acción desconocida.");
                    return false;
            }
        }

        private bool HandleShift(int newState, Stack<int> stateStack, Stack<string> symbolStack, string currentSymbol, ref int index)
        {
            Console.WriteLine($"Shift({newState})");
            stateStack.Push(newState); // Agregar el nuevo estado a la pila
            symbolStack.Push(currentSymbol); // Agregar el símbolo a la pila
            index++; // Avanzar en la entrada
            return true;
        }

        private bool HandleReduce(int productionIndex, Stack<int> stateStack, Stack<string> symbolStack, List<string> input, ref int index)
        {
            var production = Productions[productionIndex];
            Console.WriteLine($"Reduce({productionIndex} -> {production.Left} = {String.Join(" ", production.Right)})");

            // Desapilar los estados y símbolos correspondientes
            for (int i = 0; i < production.Right.Count; i++)
            {
                if (symbolStack.Count == 0)
                {
                    Console.WriteLine("Error: pila de símbolos vacía durante la reducción.");
                    return false; // Error: no hay suficientes símbolos en la pila
                }

                string poppedSymbol = symbolStack.Pop();
                Console.WriteLine($"Pop {poppedSymbol} =? {production.Right[i]}");

                if (!poppedSymbol.Equals(production.Right[i]))
                {
                    Console.WriteLine($"Error: se esperaba '{production.Right[i]}', pero se encontró '{poppedSymbol}'");
                    return false; // Error: símbolo no esperado
                }
            }

            // Obtener el nuevo estado usando el símbolo no terminal
            int newState = stateStack.Peek(); // Estado actual después de la reducción
            if (LALRActionsTable.TryGetValue((newState, production.Left), out var gotoAction))
            {
                Console.WriteLine($"Goto({gotoAction.Item2})");
                stateStack.Push(gotoAction.Item2); // Agregar el nuevo estado a la pila
                symbolStack.Push(production.Left); // Agregar el no terminal a la pila
            }

            // Llamar a la tabla de símbolos
            lALR1SymbolTable.DoActions(production, input);
            return true;
        }
        public List<string> ExtractSymbols(string filePath)
        {
            // Leer el contenido del archivo
            string content = File.ReadAllText(filePath);

            // Utilizar una expresión regular para dividir el contenido en símbolos
            // \s+ significa uno o más espacios en blanco
            var symbols = Regex.Split(content, @"\s+");

            // Crear una lista para almacenar los símbolos no vacíos
            List<string> symbolList = new List<string>();

            // Agregar solo los símbolos que no son vacíos
            foreach (var symbol in symbols)
            {
                if (!string.IsNullOrWhiteSpace(symbol))
                {
                    symbolList.Add(symbol);
                }
            }

            return symbolList;
        }
        private (List<HashSet<LR0Item>> _lr0States, Dictionary<(int, string), int> _transitions) GenerateLR0States()
        {
            var transitions = new Dictionary<(int, string), int>();
            List<HashSet<LR0Item>> lr0States = new List<HashSet<LR0Item>>();
            // Item inicial
            var initialClosure = initialState;
            lr0States.Add(Closure(initialClosure));
            // Cola de estados, inicializada con el estado inicial
            var stateQueue = new Queue<HashSet<LR0Item>>([initialClosure]);

            while (stateQueue.Count > 0)
            {
                var currentState = stateQueue.Dequeue();
                var symbolsAfterDot = GetSymbolsAfterDot(currentState);
                foreach ( var symbol in symbolsAfterDot )
                {
                    // Agregar los items que consumen el mismo simbolo
                    var newItems = new HashSet<LR0Item>();
                    foreach(var item in currentState)
                    {
                        string itemSymbol = item.GetNextSymbol();
                        if (itemSymbol != null && itemSymbol.Equals(symbol))
                        {
                            newItems.Add(item.Shift());
                            
                        }
                        if (newItems.Count > 0)
                        {
                            // Calcular el cierre de los nuevos items
                            var newClosure = Closure(newItems);
                            var newState = new HashSet<LR0Item>(newClosure);

                            // Verificar si el nuevo estado ya existe
                            int newStateIndex = lr0States.FindIndex(s => s.SetEquals(newState));
                            if (newStateIndex == -1)
                            {
                                newStateIndex = lr0States.Count;
                                lr0States.Add(newState);
                                stateQueue.Enqueue(newState);
                            }
                            // Verificar si ya existe una transición para el estado y símbolo
                            if (transitions.TryGetValue((lr0States.IndexOf(currentState), symbol), out int existingStateIndex))
                            {
                                if(existingStateIndex != newStateIndex)
                                {
                                    Console.WriteLine($"Conflicto detectado en el estado {lr0States.IndexOf(currentState)} con símbolo '{symbol}' " +
                                    $"transiciona a {existingStateIndex}, se intentó asignar: {newStateIndex}");

                                    // Verificar si el nuevo estado contiene todas las producciones del estado actual
                                    if (DoesStateContainAllProductions(newState, lr0States[existingStateIndex]))
                                    {
                                        // Si el nuevo estado contiene todas las producciones, cambiar el número de estado
                                        Console.WriteLine($"El nuevo estado {newStateIndex} contiene todas las producciones del estado existente {existingStateIndex}. " +
                                                          $"Se cambiará el estado a {newStateIndex}.");
                                        transitions[(lr0States.IndexOf(currentState), symbol)] = newStateIndex;
                                        ResolutedConflictIndex.Add(existingStateIndex);
                                    }
                                    else
                                    {
                                        // Manejo de conflicto: puedes decidir qué hacer aquí
                                        Console.WriteLine($" !!! El nuevo estado {newStateIndex} no contiene todas las producciones del estado existente {existingStateIndex}.");
                                    }

                                }

                            }
                            else
                            {
                                // Agregar la transición al diccionario
                                transitions[(lr0States.IndexOf(currentState), symbol)] = newStateIndex;
                                if ((symbol.Equals("program") || symbol.Equals("PROGRAM")) && !transitions.ContainsKey((0, symbol)))
                                    transitions[(0, symbol)] = newStateIndex;
                            }
                        }
                    }
                }
            }
            return (lr0States, transitions);
        }
        private bool DoesStateContainAllProductions(HashSet<LR0Item> newState, HashSet<LR0Item> existingState)
        {
            // Verificar si el nuevo estado contiene todas las producciones del estado existente
            return existingState.All(item => newState.Contains(item));
        }
        private HashSet<LR0Item> Closure(HashSet<LR0Item> items)
        {
            // Crear un nuevo conjunto para el cierre
            var closure = new HashSet<LR0Item>(items);
            bool added;

            do
            {
                added = false; // Variable para controlar si se han agregado nuevos items
                               // Iterar sobre los items actuales en el cierre
                var addedItems = new HashSet<LR0Item>();
                foreach (var item in closure)
                {
                    // Obtener el símbolo que sigue al punto
                    string nextSymbol = item.GetNextSymbol();
                    // Verificar si el símbolo siguiente es un no terminal
                    if (nextSymbol != null && Grammar.IsNonTerminal(nextSymbol))
                    {
                        // Buscar todas las producciones que comienzan con el símbolo no terminal
                        
                        foreach (var production in Grammar.Productions.Where(grammarP => grammarP.Left.Equals(nextSymbol)))
                        {
                            if (production.Right.Count == 1 && production.Right[0].Equals("ε"))
                            {
                                // Crear un nuevo item con el punto al inicio
                                var newItem = GenerateLR0ItemBecuaseEpsilon(item, nextSymbol, production.Action);
                                // Agregar el nuevo item al cierre si no está ya presente
                                if (!closure.Contains(newItem))
                                {
                                    newItem.Context = new(item);
                                    addedItems.Add(newItem);
                                    added = true; // Se ha agregado un nuevo item
                                }
                            }
                            else
                            {
                                // Crear un nuevo item con el punto al inicio
                                var newItem = new LR0Item(production, 0);
                                // Agregar el nuevo item al cierre si no está ya presente
                                if (!closure.Contains(newItem))
                                {
                                    newItem.Context = new(item);
                                    addedItems.Add(newItem);
                                    added = true; // Se ha agregado un nuevo item
                                }
                            }
                        }
                    }
                }
                if(addedItems.Count > 0)
                {
                    closure.UnionWith(addedItems);
                }
            } while (added); // Repetir hasta que no se agreguen más items

            return closure; // Devolver el cierre completo
        }
        private LR0Item GenerateLR0ItemBecuaseEpsilon(LR0Item item, string epsilonNonTerminal, GrammarAction action)
        {
            // Crear una nueva producción basada en la producción original
            var production = new GrammarProduction(item.Rule.Left, new List<string>(item.Rule.Right), action);

            // Saltar el simbolo que se anulo y agregar la accion reduce_epsilon
            if (production.Right.Contains(epsilonNonTerminal))
            {
                LR0Item itemNew = item.Shift();
                // reduce_epsilon(indice de simbolo omitido)
                if(!itemNew.Rule.Action.Contains($"reduce_epsilon({itemNew.DotPosition - 1})"))
                    itemNew.Rule.Action.AddAction($"reduce_epsilon({itemNew.DotPosition - 1})");
                return itemNew;
            }

            return item;
        }
        private HashSet<string> GetSymbolsAfterDot(HashSet<LR0Item> state)
        {
            var symbols = new HashSet<string>();
            foreach (var item in state)
            {
                var nextSymbol = item.GetNextSymbol();
                if (nextSymbol != null)
                {
                    symbols.Add(nextSymbol);
                }
            }
            return symbols;
        }
        public void BuildLALRTable(List<HashSet<LR0Item>> lr0States, Dictionary<(int, string), int> transitionsLR0)
        {
            // Paso 1: Generar la tabla de estados LALR(1)
            var lalrStates = GenerateLALRStates(lr0States, transitionsLR0);

            // Paso 2: Construir la tabla de acciones a partir de la tabla de estados LALR(1)
            foreach (var state in lalrStates.Select((s, i) => new { State = s, Index = i }))
            {
                int stateIndex = state.Index;
                // Iterar sobre cada ítem en el estado
                foreach (var item in state.State)
                {
                    // Obtener el símbolo que sigue al punto
                    string nextSymbol = item.GetNextSymbol();
                    if (nextSymbol == null)
                    {
                        // Acción de reducción
                        var lookaheadSymbols = item.LookAhead;
                        foreach (var lookahead in lookaheadSymbols)
                        {
                            HandleReduction(stateIndex, lookahead, item);
                        }
                    }
                    else if (Grammar.IsTerminal(nextSymbol))
                    {
                        // Acción de desplazamiento
                        if (Transitions.TryGetValue((stateIndex, nextSymbol), out int nextStateIndex))
                        {
                            HandleShift(stateIndex, nextSymbol, nextStateIndex, item);
                        }
                        else
                        {
                            Console.WriteLine($"No se encontró estado para Shift en estado {stateIndex} con símbolo '{nextSymbol}'.");
                        }
                    }
                    else if (Grammar.IsNonTerminal(nextSymbol))
                    {
                        // Acción de goto
                        if (Transitions.TryGetValue((stateIndex, nextSymbol), out int nextStateIndex))
                        {
                            HandleGoto(stateIndex, nextSymbol, nextStateIndex, item);
                        }
                        else
                        {
                            Console.WriteLine($"No se encontró estado para Goto en estado {stateIndex} con símbolo '{nextSymbol}'.");
                        }
                    }
                }
            }
        }
        private LR1Item GenerateNewLR1ItemBecauseEpsilon(LR1Item item, string epsilonNonTerminal, GrammarAction action)
        {
            // Crear una nueva producción basada en la producción original
            var production = new GrammarProduction(item.LRItem.Rule.Left, new List<string>(item.LRItem.Rule.Right), action);
            var actions = item.LRItem.Rule.Action;
            // Encontrar el índice del símbolo que produce epsilon
            int epsilonIndex = production.Right.IndexOf(epsilonNonTerminal);

            // Si el símbolo que produce epsilon se encuentra en la producción
            if (epsilonIndex != -1)
            {
                // Crear un nuevo ítem LR1 con el punto desplazado y los lookaheads del ítem original
                var newItem = item.Shift();

                // Agregar la acción de reducción de epsilon
                if(!newItem.LRItem.Rule.Action.Actions.Contains($"reduce_epsilon({epsilonIndex})"))
                    newItem.LRItem.Rule.Action.AddAction($"reduce_epsilon({epsilonIndex})");

                return newItem;
            }

            // Si no se encuentra el símbolo, simplemente devolvemos el ítem original
            return item;
        }
        private List<HashSet<LR1Item>> GenerateLALRStates(List<HashSet<LR0Item>> lr0States, Dictionary<(int, string), int> transitionsLR0)
        {
            // Paso 1: Inicializar la lista de estados LALR(1)
            List<HashSet<LR1Item>> lalrStates = new List<HashSet<LR1Item>>();
            Dictionary<string, HashSet<LR1Item>> stateMap = new Dictionary<string, HashSet<LR1Item>>();

            // Paso 2: Iterar sobre los estados LR(0)
            foreach (var lr0State in lr0States)
            {
                // Calcular el cierre de los ítems en el estado LR(0)
                HashSet<LR1Item> closureState = new HashSet<LR1Item>();
                int currentLR0State = lr0States.IndexOf(lr0State);
                foreach (var item in lr0State)
                {
                    // Crear un nuevo LR1Item a partir del LR0Item
                    var lr1Item = new LR1Item(item);

                    // Obtener los símbolos lookahead para el ítem
                    var lookaheads = GetLookaheadsFromContext(item, currentLR0State);
                    foreach (var lookahead in lookaheads)
                    {
                        lr1Item.AddLookAheadSymbol(lookahead);
                    }
                    // Agregar el ítem LALR(1) al conjunto de cierre
                    closureState.Add(lr1Item);
                }

                // Generar una clave única para el estado basado en los ítems
                string stateKey = GetStateKey(closureState);
                if(closureState.Count != 0)
                // Si el estado ya existe, combinarlo
                    if (stateMap.ContainsKey(stateKey))
                    {
                        stateMap[stateKey].UnionWith(closureState);
                    }
                    else
                    {
                        stateMap[stateKey] = closureState;
                    }
            }

            // Paso 3: Agregar los estados combinados a la lista de estados LALR(1)
            foreach (var combinedState in stateMap.Values)
            {
                lalrStates.Add(combinedState);
            }
            // Paso 4: Generar transiciones entre los estados LALR(1)
            GenerateTransitions(lalrStates);
            // Escritura de archivo
            using (StreamWriter writer = new StreamWriter("lr1States.txt"))
            {
                int p = 0;
                int q = 0;
                foreach (var lr in lalrStates)
                {
                    writer.WriteLine(p);
                    foreach (var item in lr)
                    {
                        writer.WriteLine($"\t{q} {item.ToString()}");
                        
                    }
                    p++;
                    q = p;
                }
            }
            return lalrStates;
        }
        
        private HashSet<string> GetLookaheadsFromContext(LR0Item item, int contextIndex)
        {
            HashSet<string> lookaheads = new HashSet<string>();
            string curretSymbol = item.Rule.Left; // La izquierda del item es el simbolo al cual determinar su lookahead
            if (item.Rule.Left.Equals(ExtensionSymbol))
            {
                lookaheads.Add("$");
                return lookaheads;
            }
            var contextState = LR0States[contextIndex];
            if(contextState.TryGetValue(item, out LR0Item ci))
            {
                if (Follow.TryGetValue(item.Rule.Left, out var contextFollow))
                {
                    int stateIndex = contextIndex;
                    do
                    {
                        if (contextFollow.TryGetValue(ci.Rule.Left, out var follow))
                        {
                            lookaheads.UnionWith(follow);
                            if (lookaheads.Count > 0)
                                break;
                        }
                        if (!contextState.Contains(ci.Context))
                        {
                            if(stateIndex < 0)
                                break ;
                            contextState = LR0States[stateIndex];
                            stateIndex--;
                                
                        }
                        else
                        {
                            foreach (var state in contextState)
                            {
                                if (state.Equals(ci.Context))
                                {
                                    ci = state;
                                    break;
                                }
                            }
                        }

                    } while (ci != null);
                    if (lookaheads.Count == 0)
                    {
                        if (contextFollow.TryGetValue(ci.Rule.Left, out var follow))
                        {

                        }
                    }
                }
                else Console.WriteLine($"No existe contexto valido para {item.ToString()}");
            }
            else
            {
                Console.WriteLine($"No existe contexto valido para {item.ToString()}");
            }
            
            lookaheads.Remove("ε");
            return lookaheads;
        }
        private void GenerateTransitions(List<HashSet<LR1Item>> lalrStates)
        {
            List<HashSet<LR1Item>> newStates = new List<HashSet<LR1Item>>();
            HashSet<string> existingStateKeys = new HashSet<string>();

            foreach (var state in lalrStates)
            {
                existingStateKeys.Add(GetStateKey(state));
            }
            int max = lalrStates.Count;
            for (int currentStateId = 0; currentStateId < max; currentStateId++)
            {
                HashSet<LR1Item> currentState;
                if(currentStateId < lalrStates.Count)
                    currentState = lalrStates[currentStateId];
                else
                {
                    int p = currentStateId - lalrStates.Count;
                    currentState = newStates[p];
                }
                var transitions = new Dictionary<string, HashSet<LR1Item>>();

                foreach (var item in currentState)
                {
                    string nextSymbol = item.GetNextSymbol();
                    if (nextSymbol != null)
                    {
                        var newItem = new LR1Item(item.LRItem.Shift(), item.LookAhead); 
                        if (!transitions.ContainsKey(nextSymbol))
                        {
                            transitions[nextSymbol] = new HashSet<LR1Item>();
                        }
                        transitions[nextSymbol].Add(newItem);
                    }
                }

                foreach (var transition in transitions)
                {
                    string symbol = transition.Key;
                    HashSet<LR1Item> newStateItems = new HashSet<LR1Item>(Closure(transition.Value));

                    // Generar una clave única para el nuevo estado
                    string newStateKey = GetStateKey(newStateItems);

                    // Verificar si el nuevo estado ya existe
                    if (!existingStateKeys.Contains(newStateKey))
                    {
                        existingStateKeys.Add(newStateKey);
                        newStates.Add(newStateItems);
                    }

                    // Obtener el ID del nuevo estado
                    int newStateId = GetStateId(newStateItems, lalrStates);
                    if (newStateId == -1)
                    {
                        newStateId = lalrStates.Count + newStates.Count - 1; // ID del nuevo estado
                        max = lalrStates.Count + newStates.Count;
                    }

                    // Agregar la transición al diccionario
                    Transitions[(currentStateId, symbol)] = newStateId;
                }
            }

            // Agregar los nuevos estados a lalrStates
            lalrStates.AddRange(newStates);

            // Escritura de archivo
            using (StreamWriter writer = new StreamWriter("lr1Transitions.txt"))
            {
                foreach (var lr in Transitions.Keys)
                {
                    writer.WriteLine($"\t{lr.Item1} -> {lr.Item2} -> {Transitions[lr]}");
                }
            }
        }
        public HashSet<LR1Item> Closure(HashSet<LR1Item> items)
        {
            // Crear un nuevo conjunto para el cierre
            var closure = new HashSet<LR1Item>(items);
            bool added;

            do
            {
                added = false; // Variable para controlar si se han agregado nuevos ítems
                               // Iterar sobre los ítems actuales en el cierre
                var addedItems = new HashSet<LR1Item>();
                foreach (var item in closure)
                {
                    // Obtener el símbolo que sigue al punto
                    string nextSymbol = item.LRItem.GetNextSymbol();
                    // Verificar si el símbolo siguiente es un no terminal
                    if (nextSymbol != null && Grammar.IsNonTerminal(nextSymbol))
                    {
                        // Buscar todas las producciones que comienzan con el símbolo no terminal
                        foreach (var production in Grammar.Productions.Where(grammarP => grammarP.Left.Equals(nextSymbol)))
                        {
                            // Si la producción es epsilon
                            if (production.Right.Count == 1 && production.Right[0].Equals("ε"))
                            {
                                // Crear un nuevo ítem LR1 debido a epsilon
                                var newItem = GenerateNewLR1ItemBecauseEpsilon(item, nextSymbol, production.Action);
                                // Agregar el nuevo ítem al cierre si no está ya presente
                                if (!closure.Contains(newItem))
                                {
                                    addedItems.Add(newItem);
                                    added = true; // Se ha agregado un nuevo ítem
                                }
                            }
                            else
                            {
                                // Crear un nuevo ítem con el punto al inicio
                                var newItem = new LR1Item(new LR0Item(production, 0), new HashSet<string>(item.LookAhead));
                                // Agregar el nuevo ítem al cierre si no está ya presente
                                if (!closure.Contains(newItem))
                                {
                                    addedItems.Add(newItem);
                                    added = true; // Se ha agregado un nuevo ítem
                                }
                            }
                        }
                    }
                }
                if (addedItems.Count > 0)
                {
                    closure.UnionWith(addedItems);
                }
            } while (added); // Repetir hasta que no se agreguen más ítems

            return closure; // Devolver el cierre completo
        }
        private string GetStateKey(HashSet<LR1Item> state)
        {
            // Generar una clave única para el estado basado en los ítems que contiene
            var items = state.Select(item => item.ToString()).OrderBy(i => i);
            return string.Join(",", items);
        }
        private int GetStateId(HashSet<LR1Item> stateItems, List<HashSet<LR1Item>> lalrStates)
        {
            // Generar una clave única para el estado basado en los ítems que contiene
            string stateKey = GetStateKey(stateItems);

            // Buscar el estado en la lista de estados LALR(1)
            for (int i = 0; i < lalrStates.Count; i++)
            {
                if (GetStateKey(lalrStates[i]).Equals(stateKey))
                {
                    return i; // Retornar el ID del estado existente
                }
            }

            return -1; // Retornar -1 si el estado no existe
        }
        private void HandleReduction(int stateIndex, string lookahead, LR1Item handleItem)
        {
            int reduceIndex = GetIndexOfProduction(handleItem.LRItem.Rule);
            if(reduceIndex < 0)
            {
                //
                Console.WriteLine($"No existe produccion por la cual reducir para {handleItem.ToString()}");
                return;
            }
            if (!LALRActionsTable.ContainsKey((stateIndex, lookahead)))
            {
                LALRActionsTable[(stateIndex, lookahead)] = (Action.Reduce, reduceIndex, handleItem.LRItem.Rule.Action);
                if (!LALRConflictsTable.ContainsKey((stateIndex, lookahead)))
                {
                    LALRConflictsTable[(stateIndex, lookahead)] = new HashSet<(Action, int, GrammarAction)>();
                }
                LALRConflictsTable[(stateIndex, lookahead)].Add((Action.Reduce, reduceIndex, handleItem.LRItem.Rule.Action));
            }
            else
            {
                // Conflicto de reducción
                var existingAction = LALRActionsTable[(stateIndex, lookahead)];
                Console.WriteLine($"Conflicto en reducción en estado {stateIndex} con lookahead '{lookahead}'. Ya existe la acción: {existingAction.Item1} {existingAction.Item2}");
                if (!LALRConflictsTable.ContainsKey((stateIndex, lookahead)))
                {
                    LALRConflictsTable[(stateIndex, lookahead)] = new HashSet<(Action, int, GrammarAction)>();
                }
                LALRConflictsTable[(stateIndex, lookahead)].Add((Action.Reduce, reduceIndex, handleItem.LRItem.Rule.Action));
                Console.WriteLine($"Resuelto Reduce ( {stateIndex}, '{lookahead}' ) = {reduceIndex} .");
            }
        }

        private void HandleShift(int stateIndex, string terminal, int nextStateIndex, LR1Item handleItem)
        {
            if (!LALRActionsTable.ContainsKey((stateIndex, terminal)))
            {
                LALRActionsTable[(stateIndex, terminal)] = (Action.Shift, nextStateIndex, handleItem.LRItem.Rule.Action);
                if (!LALRConflictsTable.ContainsKey((stateIndex, terminal)))
                {
                    LALRConflictsTable[(stateIndex, terminal)] = new HashSet<(Action, int, GrammarAction)>();
                }
                LALRConflictsTable[(stateIndex, terminal)].Add((Action.Shift, nextStateIndex, handleItem.LRItem.Rule.Action));
            }
            else
            {
                // Conflicto de Shift
                if (LALRActionsTable[(stateIndex, terminal)].Item2 != nextStateIndex)
                {
                    Console.WriteLine($"Conflicto en Shift en estado {stateIndex} con símbolo '{terminal}' se intento Shift {nextStateIndex}.");
                    if (!LALRConflictsTable.ContainsKey((stateIndex, terminal)))
                    {
                        LALRConflictsTable[(stateIndex, terminal)] = new HashSet<(Action, int, GrammarAction)>();
                    }
                    LALRConflictsTable[(stateIndex, terminal)].Add((Action.Shift, nextStateIndex, handleItem.LRItem.Rule.Action));
                    Console.WriteLine($"Resuelto Shift ( {stateIndex}, '{terminal}' ) = {nextStateIndex} .");
                }
            }
        }

        private void HandleGoto(int stateIndex, string nonTerminal, int nextStateIndex, LR1Item handleItem)
        {
            if (!LALRActionsTable.ContainsKey((stateIndex, nonTerminal)))
            {
                LALRActionsTable[(stateIndex, nonTerminal)] = (Action.Goto, nextStateIndex, handleItem.LRItem.Rule.Action);
                if (!LALRConflictsTable.ContainsKey((stateIndex, nonTerminal)))
                {
                    LALRConflictsTable[(stateIndex, nonTerminal)] = new HashSet<(Action, int, GrammarAction)>();
                }
                LALRConflictsTable[(stateIndex, nonTerminal)].Add((Action.Goto, nextStateIndex, handleItem.LRItem.Rule.Action));
            }
            else
            {
                // Conflicto de Goto
                if(LALRActionsTable[(stateIndex, nonTerminal)].Item2 != nextStateIndex)                
                {
                    Console.WriteLine($"Conflicto en Goto en estado {stateIndex} con símbolo '{nonTerminal}' se intento Goto {nextStateIndex}.");
                    if (!LALRConflictsTable.ContainsKey((stateIndex, nonTerminal)))
                    {
                        LALRConflictsTable[(stateIndex, nonTerminal)] = new HashSet<(Action, int, GrammarAction)>();
                    }
                    LALRConflictsTable[(stateIndex, nonTerminal)].Add((Action.Goto, nextStateIndex, handleItem.LRItem.Rule.Action));
                    Console.WriteLine($"Resuelto Goto ( {stateIndex}, '{nonTerminal}' ) = {nextStateIndex} .");
                }
            }
        }
        private int GetIndexOfProduction(GrammarProduction grammarProduction)
        {
            foreach(var Prod in Productions)
            {
                if (Prod.EqualsWAction(grammarProduction))
                {
                    return Productions.IndexOf(Prod);
                }
            }
            return -1;
        }
        public HashSet<LR0Item> GetInitialState()
        {
            // 'SSymbol' es el símbolo extendido
            var initialRule = new GrammarProduction(ExtensionSymbol, new List<string> { Grammar.Productions[0].Left });
            Productions.Add(initialRule);
            initialRule.Action.AddAction("initial_action");
            GrammarRules.Add(ExtensionSymbol, [[Productions[0].Left]]);
            var initialItem = new LR0Item(initialRule, 0);
            return Closure(new HashSet<LR0Item> { initialItem });
        }
        // Método para generar un diccionario de producciones como listas de listas
        private Dictionary<string, List<List<string>>> GenerateProductionsDictionary()
        {
            var productions = new Dictionary<string, List<List<string>>>();

            foreach (var production in Grammar.Productions)
            {
                if (!productions.ContainsKey(production.Left))
                {
                    productions[production.Left] = new List<List<string>>();
                }
                productions[production.Left].Add(production.Right);
            }

            return productions;
        }

        // Método para calcular Nullable
        private void GenerateNullable()
        {
            Nullable = new Dictionary<string, bool>();
            var productions = GrammarRules;

            // Inicializar nullable
            foreach (var nonTerminal in productions.Keys)
            {
                Nullable[nonTerminal] = false;
            }

            bool changed;
            do
            {
                changed = false;

                foreach (var production in productions)
                {
                    if (production.Value.Any(right => right.Count == 1 && right[0] == "ε"))
                    {
                        if (!Nullable[production.Key])
                        {
                            Nullable[production.Key] = true;
                            changed = true;
                        }
                    }
                    else
                    {
                        foreach (var right in production.Value)
                        {
                            if (right.All(symbol => IsNullable(symbol)))
                            {
                                if (!Nullable[production.Key])
                                {
                                    Nullable[production.Key] = true;
                                    changed = true;
                                }
                            }
                        }
                    }
                }
            } while (changed);
            GenerateFirst(productions);
        }

        // Método para verificar si un símbolo es nullable
        private bool IsNullable(string symbol)
        {
            return Nullable.ContainsKey(symbol) && Nullable[symbol];
        }

        // Método para calcular First
        private void GenerateFirst(Dictionary<string, List<List<string>>> productions)
        {
            First = new Dictionary<string, HashSet<string>>();

            foreach (var nonTerminal in productions.Keys)
            {
                First[nonTerminal] = new HashSet<string>();
                GenerateFirstForNonTerminal(nonTerminal, productions);
            }

            GenerateFollow(productions);
        }

        // Método para calcular First para un no terminal específico
        private void GenerateFirstForNonTerminal(string nonTerminal, Dictionary<string, List<List<string>>> productions)
        {
            if (First.ContainsKey(nonTerminal) && First[nonTerminal].Count > 0)
                return; // Ya calculado
            First[nonTerminal] = new HashSet<string>();
            foreach (var production in productions[nonTerminal])
            {
                foreach (var symbol in production)
                {
                    if (!Grammar.IsNonTerminal(symbol))
                    {
                        First[nonTerminal].Add(symbol);
                        break; // Terminal encontrado, salir
                    }
                    else
                    {
                        GenerateFirstForNonTerminal(symbol, productions);
                        First[nonTerminal].UnionWith(First[symbol]);

                        if (!IsNullable(symbol))
                            break; // Si no es nullable, salir
                    }
                }
            }
        }

        // Método para calcular Follow
        private void GenerateFollow(Dictionary<string, List<List<string>>> productions)
        {
            // Inicializamos el diccionario de FOLLOW con conjuntos vacíos
            Follow = new Dictionary<string, Dictionary<string, HashSet<string>>>();
            foreach (var nonTerminal in productions.Keys)
            {
                Follow[nonTerminal] = new Dictionary<string, HashSet<string>>();
            }

            // Agregamos el símbolo de fin de entrada al conjunto FOLLOW del símbolo inicial
            Follow[ExtensionSymbol] = new Dictionary<string, HashSet<string>>();
            Follow[ExtensionSymbol][productions.Keys.First()] = new HashSet<string> { "$" }; 

            bool changed;
            do
            {
                changed = false;

                foreach (var production in productions)
                {
                    foreach (var right in production.Value)
                    {
                        for (int i = 0; i < right.Count; i++)
                        {
                            if (Grammar.IsNonTerminal(right[i]))
                            {
                                string currentContext = production.Key; // Contexto actual es la producción

                                if (i + 1 < right.Count)
                                {
                                    // Símbolo siguiente
                                    var nextSymbol = right[i + 1];
                                    if (!Grammar.IsNonTerminal(nextSymbol))
                                    {
                                        // Si el siguiente símbolo es terminal, lo agregamos a FOLLOW
                                        if (!Follow[right[i]].ContainsKey(currentContext))
                                            Follow[right[i]][currentContext] = new HashSet<string>();
                                        if (Follow[right[i]][currentContext].Add(nextSymbol))
                                            changed = true;
                                    }
                                    else
                                    {
                                        // Agregamos FIRST(nextSymbol) a FOLLOW
                                        foreach (var firstSymbol in First[nextSymbol])
                                        {
                                            if (firstSymbol != "ε")
                                            {
                                                if (!Follow[right[i]].ContainsKey(currentContext))
                                                {
                                                    Follow[right[i]][currentContext] = new HashSet<string>();
                                                }
                                                if (Follow[right[i]][currentContext].Add(firstSymbol))
                                                    changed = true;
                                            }
                                        }

                                        // Si nextSymbol es nullable, continuamos
                                        if (IsNullable(nextSymbol))
                                        {
                                            int e = i + 1;
                                            for (; e < right.Count; e++)
                                            {
                                                nextSymbol = right[e];
                                                if (!Grammar.IsNonTerminal(nextSymbol))
                                                {
                                                    if (!Follow[right[i]].ContainsKey(currentContext))
                                                    {
                                                        Follow[right[i]][currentContext] = new HashSet<string>();
                                                    }
                                                    if (Follow[right[i]][currentContext].Add(nextSymbol))
                                                        changed = true;
                                                    break;
                                                }
                                                else
                                                {
                                                    foreach (var firstSymbol in First[nextSymbol])
                                                    {
                                                        if (firstSymbol != "ε")
                                                        {
                                                            if (!Follow[right[i]].ContainsKey(currentContext))
                                                            {
                                                                Follow[right[i]][currentContext] = new HashSet<string>();
                                                            }
                                                            if (Follow[right[i]][currentContext].Add(firstSymbol))
                                                                changed = true;
                                                        }
                                                    }
                                                    if (!IsNullable(nextSymbol)) break;
                                                }
                                            }
                                            // Si llegamos al final de la producción, agregamos FOLLOW de la producción actual
                                            if (e == right.Count)
                                            {
                                                if (!Follow[right[i]].ContainsKey(currentContext))
                                                {
                                                    Follow[right[i]][currentContext] = new HashSet<string>();
                                                }
                                                int tempCount = Follow[right[i]][currentContext].Count;
                                                foreach (var followSymbol in Follow[production.Key])
                                                {
                                                    Follow[right[i]][currentContext].UnionWith(followSymbol.Value);
                                                    if(Follow[right[i]][currentContext].Count !=  tempCount)
                                                        changed = true;
                                                }
                                            }
                                        }
                                    }
                                }
                                else // Último símbolo
                                {
                                    if (!Follow[right[i]].ContainsKey(currentContext))
                                    {
                                        Follow[right[i]][currentContext] = new HashSet<string>();
                                    }
                                    int tempCount = Follow[right[i]][currentContext].Count;
                                    foreach (var followSymbol in Follow[production.Key])
                                    {
                                        Follow[right[i]][currentContext].UnionWith(followSymbol.Value);
                                        if (Follow[right[i]][currentContext].Count != tempCount)
                                            changed = true;
                                    }
                                }
                            }
                        }
                    }
                }
            } while (changed);
        }
        public void ExportActionsTable()
        {
            // Establecer el contexto de licencia
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gen.xlsx");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Cargar el archivo existente o crear uno nuevo si no existe
            ExcelPackage package;
            if (File.Exists(filePath))
            {
                package = new ExcelPackage(new FileInfo(filePath));
            }
            else
            {
                package = new ExcelPackage();
            }

            // Crear la hoja para las acciones básicas
            var basicActionsWorksheet = package.Workbook.Worksheets.Add("Basic Actions");

            // Obtener todos los símbolos de la gramática
            var terminalSymbols = Grammar.TerminalSymbols; // Método que debes implementar para obtener los terminales
            terminalSymbols.Sort();
            var nonTerminalSymbols = Grammar.NonTerminalSymbols; // Método que debes implementar para obtener los no terminales
            nonTerminalSymbols.Sort();
            var allSymbols = terminalSymbols.Concat(nonTerminalSymbols).ToList();

            // Escribir encabezados en la hoja de acciones básicas
            basicActionsWorksheet.Cells[1, 1].Value = "Estado";
            for (int i = 0; i < allSymbols.Count; i++)
            {
                basicActionsWorksheet.Cells[1, i + 2].Value = allSymbols[i];
            }

            // Escribir datos de la tabla de acciones básicas
            int row = 2;
            foreach (var state in LALRActionsTable.Keys.Select(k => k.Item1).Distinct())
            {
                basicActionsWorksheet.Cells[row, 1].Value = row - 2; // Número de estado

                foreach (var symbol in allSymbols)
                {
                    if (LALRActionsTable.TryGetValue((state, symbol), out var action))
                    {
                        // Mapeo de acciones
                        string actionSymbol = action.Item1 switch
                        {
                            Action.Shift => "S",
                            Action.Reduce => "R",
                            Action.Goto => "G",
                            Action.Accept => "A",
                            _ => action.Item1.ToString() // En caso de que no coincida, se puede usar el valor original
                        };
                        basicActionsWorksheet.Cells[row, allSymbols.IndexOf(symbol) + 2].Value = $"{actionSymbol} {action.Item2}";

                    }
                }
                row++;
            }
            row++;
            basicActionsWorksheet.Cells[row, 1].Style.Numberformat.Format = "General";
            basicActionsWorksheet.Cells["BG2"].Value = $"=INDICE(B2:BE753; COINCIDIR(BG3; A2:A753; 0); COINCIDIR(BG4; B1:BE1; 0))";
            // Ajustar el ancho de las columnas
            basicActionsWorksheet.Cells.AutoFitColumns();
            basicActionsWorksheet.Cells.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center; 

            // Crear la hoja para acciones y conflictos
            var actionsAndConflictsWorksheet = package.Workbook.Worksheets.Add("Actions and Conflicts");

            // Escribir encabezados en la hoja de acciones y conflictos
            actionsAndConflictsWorksheet.Cells[1, 1].Value = "Estado";
            for (int i = 0; i < allSymbols.Count; i++)
            {
                actionsAndConflictsWorksheet.Cells[1, i + 2].Value = allSymbols[i];
            }

            // Escribir datos de la tabla de acciones y conflictos
            row = 2;
            foreach (var state in LALRActionsTable.Keys.Select(k => k.Item1).Distinct())
            {
                actionsAndConflictsWorksheet.Cells[row, 1].Value = row - 2; // Número de estado

                foreach (var symbol in allSymbols)
                {
                    // Manejo de acciones
                    if (LALRActionsTable.TryGetValue((state, symbol), out var action))
                    {
                        // Mapeo de acciones
                        string actionSymbol = action.Item1 switch
                        {
                            Action.Shift => "S",
                            Action.Reduce => "R",
                            Action.Goto => "G",
                            Action.Accept => "A",
                            _ => action.Item1.ToString() // En caso de que no coincida, se puede usar el valor original
                        };

                        actionsAndConflictsWorksheet.Cells[row, allSymbols.IndexOf(symbol) + 2].Value = $"{actionSymbol} {action.Item2}";
                    }

                    // Manejo de conflictos
                    if (LALRConflictsTable.TryGetValue((state, symbol), out var conflicts))
                    {
                        // Concatenar los conflictos en una cadena separada por comas
                        var conflictList = conflicts.Select(c => $"{GetActionsString(c.Item1)} {c.Item2}").ToList();
                        if (conflictList.Contains(actionsAndConflictsWorksheet.Cells[row, allSymbols.IndexOf(symbol) + 2].Value.ToString())) 
                        {
                            actionsAndConflictsWorksheet.Cells[row, allSymbols.IndexOf(symbol) + 2].Value = $"{string.Join(", ", conflictList)}";
                        }
                        else
                            actionsAndConflictsWorksheet.Cells[row, allSymbols.IndexOf(symbol) + 2].Value = $"{actionsAndConflictsWorksheet.Cells[row, allSymbols.IndexOf(symbol) + 2].Value.ToString()}, {string.Join(", ", conflictList)}";
                    }
                }
                row++;

            }
            row++;
            actionsAndConflictsWorksheet.Cells[row, 1].Style.Numberformat.Format = "General";
            actionsAndConflictsWorksheet.Cells["BG2"].Value = $"=INDICE(B2:BE753; COINCIDIR(BG3; A2:A753; 0); COINCIDIR(BG4; B1:BE1; 0))";
            // Ajustar el ancho de las columnas
            actionsAndConflictsWorksheet.Cells.AutoFitColumns();
            actionsAndConflictsWorksheet.Cells.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            // Guardar el archivo
            package.Save();
        }
        private string GetActionsString(Action action)
        {
            string actionSymbol = action switch
            {
                Action.Shift => "S",
                Action.Reduce => "R",
                Action.Goto => "G",
                Action.Accept => "A",
                _ => action.ToString() // En caso de que no coincida, se puede usar el valor original
            };
            return actionSymbol;
        }
        public void ExportStates((List<HashSet<LR0Item>> _lr0States, Dictionary<(int, string), int> _transitions) states)
        {

            // Establecer el contexto de licencia
            string filePath = "";
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            if (!Path.IsPathRooted(filePath) || !Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                // Si la ruta no es válida, establece la ruta predeterminada
                string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gen.xlsx");
                filePath = defaultPath;
            }// Verifica si el archivo ya existe y lo elimina
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("LR0 States");

                int row = 1;
                foreach (var state in states._lr0States.Select((s, i) => new { State = s, Index = i }))
                {
                    worksheet.Cells[row, 1].Value = $"Estado {state.Index}";

                    foreach (var item in state.State)
                    {
                        worksheet.Cells[row, 2].Value = $"{item.Rule.Left}";                    // Producción
                        worksheet.Cells[row, 3].Value = $"{string.Join(" ", item.Rule.Right)}"; // Aquí puedes agregar el estado siguiente si es necesario
                        if (!item.IsComplete())
                        {
                            worksheet.Cells[row, 4].Value = $"{item.Rule.Right[item.DotPosition]}";
                            int transition = states._transitions[(state.Index, item.Rule.Right[item.DotPosition])];
                            worksheet.Cells[row, 5].Value = $"{state.Index} > {item.Rule.Right[item.DotPosition]} > {transition}"; // worksheet.Cells[row, 3].Value = "Estado siguiente"; // Ejemplo

                        }
                        worksheet.Cells[row, 6].Value = $"{string.Join(" ", item.Rule.Action.Actions)}";
                        row++;
                    }
                    row++; // Espacio entre estados
                }
                // Ajustar el ancho de las columnas
                worksheet.Cells.AutoFitColumns();
                // Guardar el archivo
                package.SaveAs(new FileInfo(filePath));
            }
        }
    }
}
