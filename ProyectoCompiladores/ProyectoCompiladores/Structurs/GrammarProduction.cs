using ProyectoCompiladores.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoCompiladores.Structurs
{
    public class GrammarProduction
    {
        public string Left { get; private set; }
        public List<string> Right { get; private set; }
        public GrammarAction Action { get; private set; }
        public GrammarProduction(string left, List<string> right, GrammarAction action = null)
        {
            Left = TrimItem(left);
            Right = new List<string>();
            foreach (var item in right)
            {
                Right.Add(TrimItem(item));
            }
            if (action != null)
                Action = action;
            else
                Action = new GrammarAction();
        }
        
        public string TrimItem(string item)
        {
            string trimItem = item.Trim();
            if (item.StartsWith("<"))
            {
                trimItem = trimItem.Trim('<').Trim('>');
            }
            else if (item.StartsWith("\'"))
            {
                trimItem = trimItem.Trim('\'');
            }
            return trimItem;
        }
        public GrammarAction GetAction()
        {
            return Action;
        }
        public GrammarProduction Clone()
        {
            GrammarProduction clone = (GrammarProduction)MemberwiseClone();
            clone.Left = this.Left;
            clone.Right = this.Right;
            if (Action != null) 
                clone.Action = this.Action.Clone();
            return clone;
        }
        public override bool Equals(object obj)
        {
            // Verificar si el objeto es null o de un tipo diferente
            if (obj == null || GetType() != obj.GetType())
                return false;

            // Convertir el objeto a GrammarProduction
            GrammarProduction other = (GrammarProduction)obj;

            // Comparar las propiedades
            if (Action != null)
            {
                if (!Left.Equals(other.Left) || !Action.Equals(other.Action))
                {
                    return false;
                }
            }
            else
            {
                if (!Left.Equals(other.Left))
                {
                    return false;
                }
            }

            // Comparar las listas Right
            if (Right.Count != other.Right.Count)
                return false;

            for (int i = 0; i < Right.Count; i++)
            {
                if (Right[i] != other.Right[i])
                    return false;
            }

            return true;
        }
        public bool EqualsWAction(object obj)
        {
            // Verificar si el objeto es null o de un tipo diferente
            if (obj == null || GetType() != obj.GetType())
                return false;

            // Convertir el objeto a GrammarProduction
            GrammarProduction other = (GrammarProduction)obj;

            // Comparar las propiedades
            if (Left != other.Left)
                return false;

            // Comparar las listas Right
            if (Right.Count != other.Right.Count)
                return false;

            for (int i = 0; i < Right.Count; i++)
            {
                if (Right[i] != other.Right[i])
                    return false;
            }

            return true;
        }
        public override int GetHashCode()
        {
            // Combinar los códigos hash de las propiedades
            int hash , hash1 = 0;
            if (Action != null)
                hash = HashCode.Combine(Left, Action.GetHashCode());
            else
                hash = HashCode.Combine(Left, 31);
            foreach (var item in Right)
            {
                hash1 = HashCode.Combine(hash1, item);
            }
            return hash1 ^ hash;
        }
    }
}
