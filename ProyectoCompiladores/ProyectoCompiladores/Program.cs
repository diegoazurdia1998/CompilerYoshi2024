using ProyectoCompiladores.Crziness;
using ProyectoCompiladores.Managers;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        GrammarSectionsValidator sectionsValidator = new GrammarSectionsValidator();
        GrammarVerifier grammar = new GrammarVerifier();
        string filePath = "gramaticaReducida.txt"; // Ruta al archivo
        // Validar archivo y extraer secciones
        Dictionary<string, List<string>> sections = sectionsValidator.ValidateSections(filePath);
        // Validar secciones extraidas
        grammar.ValidateSections(sections);
        int i= 1;
        using (StreamWriter writer = new StreamWriter("Productions.txt"))
        {
            foreach (var prod in grammar.Productions)
            {
                writer.WriteLine($"{i}. {prod.ToString()}");
                i++;
            }
            
        }
        // Generar Tabla de parser
        //LALRParserGenerator parser = new LALRParserGenerator(grammar);
        var LALR = new lalrLecter(grammar);
        string inputPath = "input1.txt";
        LALR.Parse(inputPath);
        Console.WriteLine("");


    }
    
}