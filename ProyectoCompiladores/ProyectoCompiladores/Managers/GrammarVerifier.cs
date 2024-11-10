using ProyectoCompiladores.Structurs;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using static System.Collections.Specialized.BitVector32;

namespace ProyectoCompiladores.Managers
{
    public class GrammarVerifier
    {
        // Nombre e la gramatica
        public string GrammarName { get; private set; }
        //Sets
        public Dictionary<string , string> Sets { get; private set; }
        // Tokens
        public Dictionary<string, List<string>> Tokens { get; private set; }
        // Asociatividad de tokens
        public Dictionary<string, List<string>> TokensAssociativity { get; private set; }
        // Keywords
        public List<string> Keywords { get; private set; }
        // Productions
        public List<GrammarProduction> Productions { get; private set; }
        public List<string> TerminalSymbols { get; private set; }
        public List<string> NonTerminalSymbols { get; private set; }

        public GrammarVerifier()
        {
            Sets = new Dictionary<string, string>();
            TokensAssociativity = new Dictionary<string, List<string>>();
            Tokens = new Dictionary<string, List<string>>();
            Keywords = new List<string>();
            Productions = new List<GrammarProduction>();
            TerminalSymbols = new List<string>();
            NonTerminalSymbols = new List<string>();
        }

        public void AddToken(List<string> token, string identifier = "default")
        {
            if(!Tokens.ContainsKey(identifier))
            {
                Tokens.Add(identifier, new List<string>());
            }
            Tokens[identifier].AddRange(token);
            if(!identifier.Equals("default"))
            {
                TerminalSymbols.Add(identifier);
            }
            
        }
        public void AddTokenAssociativity(string token, string associativity)
        {
            if (!TokensAssociativity.ContainsKey(associativity))
            {
                TokensAssociativity.Add(associativity, new List<string>());
            }
            TokensAssociativity[associativity].Add(token);
        }
        public void AddKeyword(string keyword)
        {
            Keywords.Add(keyword);
        }

        public void AddProduction(string left, List<string> right, GrammarAction action = null)
        {
            Productions.Add(new GrammarProduction(left, right, action));
        }

        public void ValidateSections(Dictionary<string, List<string>> sections)
        {
            // secciones esperadas "COMPILER", "UNITS", "SETS", "TOKENS", "KEYWORDS", "PRODUCTIONS"
            string currentSectionKey = "COMPILER";
            // Validar "COMPILER"
            List<string> sectionValues = sections[currentSectionKey];
            foreach (string value in sectionValues)
            {
                if (!value.Equals(String.Empty))
                {
                    GrammarName = value;
                }
            }
            // Validar "UNITS"
            currentSectionKey = "UNITS";
            sectionValues = sections[currentSectionKey];
            foreach (string value in sectionValues)
            {
                if (value.Equals(String.Empty))
                {
                    // UNITS
                }
            }
            // Validar "SETS" -----------------------------------------------------------------------------------------------------
            currentSectionKey = "SETS";
            sectionValues = sections[currentSectionKey];
            foreach (string value in sectionValues)
            {
                if (!value.Equals(String.Empty) || value != null)
                {
                    int equalsIndex = value.IndexOf('=');
                    if (equalsIndex != -1)
                    {
                        string left = value.Substring(0, equalsIndex).Trim();
                        string right = value.Substring(equalsIndex + 1).Trim().Trim(';');
                        if(!value.Contains("chr"))
                            right = '[' + right.Replace("..", "-").Replace("\'", "").Replace("+", "") + ']';
                        else
                        {
                            right = right.Replace("chr(", "").Replace(")..", "-").Trim(')');
                            string[] digitvalues = right.Split('-');
                            string[] symbolValues = new string[digitvalues.Length];
                            int i = 0;
                            foreach (string value2 in digitvalues)
                            {
                                if(int.TryParse(value2, out int digit))
                                {
                                    char symbol = Convert.ToChar(digit);
                                    symbolValues[i] = symbol.ToString();
                                    i++;
                                }
                            }
                            right = '[' + String.Join('-', symbolValues) + "]";
                        }
                        if (!Sets.ContainsKey(left))
                        {

                            Sets.Add(left, right);
                        }
                        else
                        {
                            Sets[left] = right;
                        }
                    }
                }
            }
            // Validar "TOKENS" -----------------------------------------------------------------------------------------------------
            currentSectionKey = "TOKENS";
            sectionValues = sections[currentSectionKey];
            foreach (string value in sectionValues)
            {
                int equalsIndex = value.IndexOf('=');
                if(equalsIndex < 0 || value.Contains("'='"))
                {
                    string[] tokens = value.Split(",");
                    string associativity = String.Empty;
                    if (value.Contains(" "))
                    {
                        associativity = value.Split(' ')[1].Trim(';');
                    }
                    foreach (string value2 in tokens)
                    {
                        string token = value2;
                        if (value2.Contains("Left") || value2.Contains("Right"))
                        {
                            token = value2.Trim(';').Replace("Left", "").Replace("Right", "").Trim();
                        }
                        token = token.Trim(';').Trim('\'');
                        AddToken(token.Trim().Split(' ').ToList());
                        if (!associativity.Equals(String.Empty))
                        {
                            AddTokenAssociativity(token, associativity);
                        }
                    }
                }
                else
                {
                    string identifier = value.Substring(0, equalsIndex).Trim();
                    string tokenDefinition = value.Substring(equalsIndex + 1).Trim(';');
                    tokenDefinition = Regex.Replace(tokenDefinition, @"\s+", " ");
                    AddToken(tokenDefinition.Trim().Split(' ').ToList(), identifier);
                }
            }
            // Aplicar SETS a TOKENS
            var tempTokens = new Dictionary<string, List<string>>();
            foreach (var token in Tokens)
            {
                string setifyToken = String.Join(" ", token);
                List<string> listSetifyToken = new List<string>();
                if (!token.Key.Equals("default"))
                {
                    foreach(var symbol in token.Value)
                    {
                        if (Sets.ContainsKey(symbol))
                        {
                            listSetifyToken.Add(Sets[symbol]);
                        }
                        else
                        {
                            listSetifyToken.Add(symbol);
                        }
                    }
                    if (!tempTokens.ContainsKey(token.Key))
                        tempTokens.Add(token.Key, listSetifyToken);
                    tempTokens[token.Key] = listSetifyToken;
                }
                else
                {
                    if (!tempTokens.ContainsKey(token.Key))
                        tempTokens.Add(token.Key, token.Value);
                    
                }
            }
            Tokens = tempTokens;
            // Validar "KEYWORDS" ---------------------------------------------------------------------------------------------------
            currentSectionKey = "KEYWORDS";
            sectionValues = sections[currentSectionKey];
            foreach (string value in sectionValues)
            {
                string[] keywords = value.Split(",");
                foreach (string value2 in keywords)
                {
                    if (!value2.Equals(String.Empty))
                    {
                        string keyword = value2.Trim('\'').Trim(';');
                        AddKeyword(keyword);
                    }
                }
            }
            // Validar "PRODUCTIONS" ------------------------------------------------------------------------------------------------
            currentSectionKey = "PRODUCTIONS";
            sectionValues = sections[currentSectionKey];
            foreach (string value in sectionValues)
            {
                int equalsIndex = value.IndexOf('=');
                string left = value.Substring(0, equalsIndex).Trim();
                string rightWithActions = value.Substring(equalsIndex + 1).Trim();
                Regex actionsRegex = new Regex(@"\{[^}]*\}");
                Match match = actionsRegex.Match(rightWithActions);

                // Si hay acciones, separamos las acciones de las producciones
                if (match.Success)
                {
                    int actionIndex = match.Index;
                    string[] actions = rightWithActions.Substring(actionIndex + 1).Trim().Trim('}').Split(',');

                    // Obtenemos la parte de la producción sin las acciones
                    string productionsPart = rightWithActions.Substring(0, actionIndex).Trim();

                    // Dividimos las producciones por el símbolo '|'
                    string[] productions = productionsPart.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string production in productions)
                    {
                        string[] realRight = production.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        AddSymbols(left, realRight);
                        AddProduction(left, realRight.ToList(), new GrammarAction(actions));
                    }
                }
                else
                {
                    // Si no hay acciones, simplemente procesamos las producciones
                    string[] productions = rightWithActions.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string production in productions)
                    {
                        string[] realRight = production.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        AddProduction(left, realRight.ToList());
                    }
                }
            }
        }
        private void AddSymbols(string nonTerminal,string[] symbols)
        {
            if (!NonTerminalSymbols.Contains(nonTerminal.Trim().Trim('<').Trim('>')))
            {
                NonTerminalSymbols.Add(nonTerminal.Trim().Trim('<').Trim('>'));
            }
            foreach (string symbol in symbols) 
            { 
                if(symbol.StartsWith("<"))
                {
                    if (!NonTerminalSymbols.Contains(symbol.Trim().Trim('<').Trim('>')))
                    {
                        NonTerminalSymbols.Add((symbol.Trim().Trim('<').Trim('>')));
                    }
                }
                else if(symbol.StartsWith("\'"))
                {
                    if (!TerminalSymbols.Contains(symbol.Trim('\'')))
                    {
                        TerminalSymbols.Add(symbol.Trim('\''));
                    }
                }
            }
        }
        public bool IsNonTerminal(string symbol)
        {
            return NonTerminalSymbols.Contains(symbol);
        }
        public bool IsTerminal(string symbol)
        {
            return !NonTerminalSymbols.Contains(symbol);
        }
        public bool IsToken(string token)
        {
            foreach(var tokenT in  Tokens)
            {
                if (!tokenT.Key.Equals("default"))
                {
                    Regex tokenRegex = new Regex(String.Join("", tokenT.Value));
                    if (tokenRegex.IsMatch(token))
                    {
                        return !TerminalSymbols.Contains(token) && !Keywords.Contains(token);
                    }
                }
                else
                {
                    foreach(var symbol in tokenT.Value)
                    {
                        if (symbol.Equals(token))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public GrammarAction SearchAction(string identifier, List<string> production)
        {
            foreach(var prod in Productions)
            {
                if (prod.EqualsWAction(new GrammarProduction(identifier, production)))
                {
                    return prod.GetAction();
                }
            }
            return null;
        }
        public string GetTokenIdentifier(string token)
        {
            foreach (var tokenT in Tokens)
            {
                if (!tokenT.Key.Equals("default"))
                {
                    Regex tokenRegex = new Regex($@"^{String.Join("", tokenT.Value)}$");
                    if (tokenRegex.IsMatch(token))
                    {
                        return tokenT.Key;
                    }
                }
            }
            return token;
        }
    }

}
