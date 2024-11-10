using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoCompiladores.Managers
{
    class GrammarSectionsValidator
    {
        public Dictionary<string, List<string>> ValidateSections(string filePath)
        {
            // Secciones requeridas
            string[] requiredSections = { "COMPILER", "UNITS","SETS", "TOKENS", "KEYWORDS", "PRODUCTIONS" };
            Dictionary<string, List<string>> sections = new Dictionary<string, List<string>>();

            // Inicializamos las listas para cada sección
            foreach (var section in requiredSections)
            {
                sections[section] = new List<string>();
            }

            // Leer el archivo
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    string currentSection = null;

                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line.Trim(); // Limpiar espacios en blanco

                        // Verificar si la línea es una sección
                        if (Array.Exists(requiredSections, section => section.Equals(line, StringComparison.OrdinalIgnoreCase)))
                        {
                            currentSection = line; // Cambiar a la nueva sección
                        }
                        else if (!string.IsNullOrEmpty(currentSection))
                        {
                            // Agregar la línea a la sección actual
                            sections[currentSection].Add(line);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer el archivo: {ex.Message}");
                return null; // O manejar el error de otra manera
            }

            // Verificar si todas las secciones están presentes
            foreach (var section in requiredSections)
            {
                if (sections[section].Count == 0)
                {
                    Console.WriteLine($"La sección '{section}' no se encontró o está vacía.");
                    return null; // O manejar el error de otra manera
                }
            }

            return sections;
        }
    }

}
