using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoCompiladores.Structurs
{
    public class LR0Item
    {
        public GrammarProduction Rule { get; set; }
        public int DotPosition { get; set; }
        public LR0Item(string left, List<string> right, GrammarAction action, int dotPosition)
        {
            Rule = new GrammarProduction(left, right, action);
            DotPosition = dotPosition;
        }
        public LR0Item(GrammarProduction rule, int dotPosition)
        {
            Rule = rule;
            DotPosition = dotPosition;
        }
        public LR0Item(LR0Item item)
        {
            Rule = item.Rule.Clone();
            DotPosition = item.DotPosition;
        }
        public string GetNextSymbol()
        {
            if (DotPosition < Rule.Right.Count)
            {
                return Rule.Right[DotPosition];
            }
            return null; // No hay símbolo siguiente
        }
        public LR0Item Shift()
        {
            if (DotPosition < Rule.Right.Count)
            {
                return new LR0Item(Rule.Clone(), DotPosition + 1);
            }
            throw new InvalidOperationException("Cannot shift past the end of the rule.");
        }
        public bool IsComplete()
        {
            return DotPosition >= Rule.Right.Count;
        }
        public override bool Equals(object obj)
        {
            // Verificar si el objeto es null
            if (obj == null || GetType() != obj.GetType())
                return false;

            // Convertir el objeto a LR0Item
            LR0Item other = (LR0Item)obj;

            // Comparar las propiedades
            return Rule.Equals(other.Rule) && DotPosition == other.DotPosition;
        }

        public override int GetHashCode()
        {
            // Combinar los códigos hash de las propiedades
            return HashCode.Combine(Rule.GetHashCode(), DotPosition);
        }
       
    }
}
