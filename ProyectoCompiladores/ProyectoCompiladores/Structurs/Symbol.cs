using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using ProyectoCompiladores.Structurs;

namespace ProyectoCompiladores.Structurs
{
    public class Symbol
    {
        public string Identifier { get; set; } // Nombre del símbolo (por ejemplo, el identificador de la variable o procedimiento)
        public string Type { get; set; } // Tipo del símbolo (por ejemplo, "INTEGER", "REAL", "BOOLEAN", etc.)
        public Scope Scope { get; set; } // Alcance del símbolo (puede ser útil para manejar variables locales y globales)
        public object Value { get; set; } // Valor actual del símbolo (opcional, puede ser útil para variables)
        public bool IsProcedure { get; set; } // Indica si el símbolo es un procedimiento
        public List<(string identifier, string type)> Parameters { get; set; } // Parámetros del procedimiento (si es un procedimiento)

        public Symbol(string name, string type, Scope scope, bool isProcedure = false)
        {
            Identifier = name;
            Type = type;
            Scope = scope;
            IsProcedure = isProcedure;
            Parameters = new List<(string identifier, string type)>();
        }
        public void AddValue(List<string> values)
        {
            if(Value is List<string>)
            {
                List<string> value = (List<string>)Value;
                value.AddRange(values);
            }
        }
        // Método para agregar parámetros (si es un procedimiento)
        public void AddParameter((string identifier, string type) parameter)
        {
            if (IsProcedure)
            {
                Parameters.Add(parameter);
            }
        }

        // Método para obtener una representación en cadena del símbolo
        public override string ToString()
        {
            return $"{Identifier}: {Type} (Scope: {Scope})";
        }
    }
}
