using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoCompiladores.Structurs
{
    public enum ScopeName
    {
        Global,
        Local
    }
    public class Scope
    {
        public ScopeName Name { get; set; } // Nombre del alcance (por ejemplo, "global", "local", etc.)
        public Dictionary<string, Symbol> Symbols { get; private set; } // Símbolos definidos en este alcance
        public Scope Parent { get; set; } // Alcance padre (para manejar la jerarquía de alcances)

        public Scope(ScopeName name, Scope parent = null)
        {
            Name = name;
            Symbols = new Dictionary<string, Symbol>();
            Parent = parent; // Permite la referencia al alcance padre
        }

        // Método para agregar un símbolo al alcance
        public void AddSymbol(Symbol symbol)
        {
            if (Symbols.ContainsKey(symbol.Identifier))
            {
                throw new Exception($"Error: El símbolo '{symbol.Identifier}' ya está definido en el alcance '{Name}'.");
            }
            Symbols[symbol.Identifier] = symbol;
        }

        // Método para buscar un símbolo en el alcance actual y en los alcances padres
        public Symbol FindSymbol(string name)
        {
            if (Symbols.TryGetValue(name, out var symbol))
            {
                return symbol; // Se encontró en el alcance actual
            }
            else if (Parent != null)
            {
                return Parent.FindSymbol(name); // Buscar en el alcance padre
            }
            return null; // No se encontró el símbolo
        }
    }
}
