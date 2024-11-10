using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoCompiladores.Structurs
{
    public class GrammarAction
    {
        public List<string> Actions {  get; private set; }
        public GrammarAction()
        {
            Actions = new List<string>();
        }
        public GrammarAction(string[] actions)
        {
            Actions = new List<string>(actions);
        }
        public GrammarAction Clone()
        {
            GrammarAction clone = new GrammarAction();
            foreach (var action in Actions)
            {
                clone.AddAction(action);
            }
            return clone;
        }
        public void AddAction(string action)
        {
            Actions.Add(action);
        }
        public bool Contains(string action)
        {
            return Actions.Contains(action);
        }
        public override bool Equals(object obj)
        {
            // Verificar si el objeto es null o de un tipo diferente
            if (obj == null || GetType() != obj.GetType())
                return false;

            // Convertir el objeto a GrammarAction
            GrammarAction other = (GrammarAction)obj;

            // Comparar las listas Actions
            if (Actions.Count != other.Actions.Count)
                return false;

            for (int i = 0; i < Actions.Count; i++)
            {
                if (Actions[i] != other.Actions[i])
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            // Combinar los códigos hash de las acciones
            int hash = 17; // Un número primo para iniciar el hash
            foreach (var action in Actions)
            {
                hash = hash * 31 + (action != null ? action.GetHashCode() : 0); // Combinar el hash
            }
            return hash;
        }
    }
}
