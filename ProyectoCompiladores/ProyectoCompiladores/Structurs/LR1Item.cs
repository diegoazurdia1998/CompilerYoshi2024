using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoCompiladores.Structurs
{
    public class LR1Item
    {
        public LR0Item LRItem { get; private set; }
        public HashSet<string> LookAhead {  get; private set; }
        public LR1Item(LR0Item item) 
        { 
            LRItem = item;
            LookAhead = new HashSet<string>();
        }
        public LR1Item(LR0Item item, HashSet<string> la)
        {
            LRItem = item;
            LookAhead = new HashSet<string>(la);
        }
        public LR1Item(LR0Item item, string symbol)
        {
            LRItem = item;
            LookAhead = new HashSet<string>([symbol]);
        }
        public void AddLookAheadSymbol(string symbol)
        {
            LookAhead.Add(symbol);
        }
        public string GetNextSymbol()
        {
            return LRItem.GetNextSymbol();
        }
        public override string ToString()
        {
            // Representar el ítem LR(0) y los lookaheads
            return $"{LRItem} [{string.Join(", ", LookAhead)}]";
        }
        public override bool Equals(object obj)
        {
            if (obj is LR1Item other)
            {
                // Compara el ítem LR(0) y los lookaheads
                return LRItem.Equals(other.LRItem) && LookAhead.SetEquals(other.LookAhead);
            }
            return false;
        }

        public override int GetHashCode()
        {
            // Combina el hash del ítem LR(0) y el hash de los lookaheads
            int hash = LRItem.GetHashCode();
            foreach (var symbol in LookAhead)
            {
                hash ^= symbol.GetHashCode(); // Usar XOR para combinar hashes
            }
            return hash;
        }
    }
}
