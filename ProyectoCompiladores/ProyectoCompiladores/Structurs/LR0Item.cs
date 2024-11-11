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
        public LR0Item Context {  get; set; }
        public LR0Item(string left, List<string> right, int dotPosition, GrammarAction action = null)
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
        public LR0Item Clone()
        {
            LR0Item clone = (LR0Item)this.MemberwiseClone();
            clone.Rule = Rule.Clone();
            clone.DotPosition = DotPosition;
            if(Context != null) 
                clone.Context = new LR0Item(Context);
            return clone;
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
                var item = new LR0Item(Rule.Clone(), DotPosition + 1);
                item.Context = this.Clone();
                return item;
            }
            throw new InvalidOperationException("Cannot shift past the end of the rule.");
        }
        public bool IsComplete()
        {
            return DotPosition >= Rule.Right.Count;
        }
        public override string ToString()
        {
            // Representar el ítem LR(0) y los lookaheads
            return $"{Rule.ToString()} at({DotPosition},{GetNextSymbol()})";
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
        public bool EqualsWDotWActions(LR0Item item)
        {
            if (Rule.EqualsWAction(item.Rule))
            {
                return true;
            }
            return false;
        }
        public bool EqualsWDot(LR0Item item)
        {
            if (Rule.Equals(item.Rule))
                return true;
            return false;
        }
        public override int GetHashCode()
        {
            // Combinar los códigos hash de las propiedades
            return HashCode.Combine(Rule.GetHashCode(), DotPosition);
        }
       
    }
}
