﻿using ProyectoCompiladores.Crziness;
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
        // Generar Tabla de parser
        LALRParserGenerator parser = new LALRParserGenerator(grammar);
        var LALR = new lalrLecter(grammar);
        string inputPath = "prueba1.txt";
        LALR.Parse(inputPath);


        parser.Parse("prueba1.txt");
        Console.WriteLine("");


    }
    
}