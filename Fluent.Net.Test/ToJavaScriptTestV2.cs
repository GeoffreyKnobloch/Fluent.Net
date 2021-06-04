using Fluent.Net.Ast;
using FluentAssertions;
using FluentAssertions.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace Fluent.Net.Test
{
    class ToJavaScriptTestV2 : FtlTestBase
    {
        [Test]
        public void SimpleMessage()
        {
            var input = Ftl(@"
                foo = Foo
            ");
            Pretty(input).Should().Be(input);
        }

        static string Pretty(string text)
        {
            using (var sr = new StringReader(text))
            using (var sw = new StringWriter())
            {
                var parser = new Parser();
                var resource = parser.Parse(sr);
                var serializer = new Serializer();
                serializer.Serialize(sw, resource);
                return sw.ToString();
            }
        }

         [Test]
        public void SimpleMessageJsSerialize()
        {
            var jsSeri = new JavaScriptSerializer2();
            jsSeri.LoadContext("en");
            using(var sw = new StringWriter())
            {
                jsSeri.Visit(sw);
                var js = sw.ToString();
                Console.WriteLine("Final js output:");
                Console.WriteLine(js);
            }
        }
    }


    // Nouvelle approche : On a déjà l'objet Serializable tout fait, et ils ont déjà un Serializer qui fait des lignes fluent, on reprend le Serializer sauf qu'il fait du JS.
    public class JavaScriptSerializer2
    {
        public List<MessageContext> MessageContexts {get; } = new List<MessageContext>();
        private Dictionary<string, string> TermsJs {get; } = new Dictionary<string, string>();
        private Dictionary<string, RuntimeAst.Message> Messages { get; } = new Dictionary<string, RuntimeAst.Message>();
        private Dictionary<string, string> MessagesJs {get; } = new Dictionary<string, string>();
        private Dictionary<string, string> IntermediateFunctions {get; } = new Dictionary<string, string>();
        public bool WithSpans { get; }

        public JavaScriptSerializer2(bool withSpans = false)
        {
            WithSpans = withSpans; // ?
        }

        public MessageContext LoadContext(string lang) // Load le .ftl, sauvegarde les messages 
        {
            string ftlPath = Path.Combine("..", "..", "..", $"{lang}.ftl");
            using (var sr = new StreamReader(ftlPath))
            {
                // var message = new MessageContext(null).GetMessage("key");
                
                var fluentRessource = FluentResource.FromReader(sr);
                foreach (var entry in fluentRessource.Entries)
                {
                    //TODO: BUGBUG: quoi faire si doublon de clé??
                    if (!this.Messages.TryAdd(entry.Key, entry.Value))
                    {
                        // déja présent??
                    }
                }

                // this.MessageContexts.Add(mc);
                return null;
            }
        }


        //public void GenerateJs()
        //{
        //    // On prend les Dico de TermsMessage et MessagesMessage, et on en fait du js.
        //    foreach (var term in TermsMessage)
        //    {
        //        var variableName = TermNameToVariableName(term.Key);
        //        // Parcour le Term, et alimente
        //        if (term.Value.Value is RuntimeAst.StringLiteral stringLiteral)
        //        {
        //            this.TermsJs.Add(term.Key, $"let {variableName} = '{stringLiteral.Value}';");
        //        }
        //        else
        //        {
        //            Debugger.Break(); // Fallback, mais pas prévu d'arriver là!
        //            this.TermsJs.Add(term.Key, $"let {variableName} = '{variableName}';");
        //        }
        //    }

        //    foreach (var message in this.MessagesMessage)
        //    {
        //        using (var sw = new StringWriter())
        //        {
        //            Serialize(sw, message.Key, message.Value);
        //            this.MessagesJs.Add(message.Key, sw.ToString());
        //        }
                
        //    }
        //}

        private string TermNameToVariableName(string termName)
        {
            return termName.Substring(1).Replace('-', '_');
        }

        public string GetResult()
        {
            // Prendre les ref pour déclarer les variables
            StringBuilder sb = new StringBuilder();
            DeclareTerms(sb);
            DeclareFunctions(sb);
            DeclareFinalDictionary(sb);

            return sb.ToString();

        }

        private StringBuilder DeclareTerms(StringBuilder sb)
        {
            // On prend les références, et on déclare les variables
            foreach (var termJs in this.TermsJs)
            {
                sb.Append(termJs.Value);
                sb.Append("\n");
            }
            return sb;
        }

        private StringBuilder DeclareFunctions(StringBuilder sb)
        {
            foreach (var messageJs in this.MessagesJs)
            {
                sb.Append(messageJs.Value);
                sb.Append("\n");
            }

            foreach (var intermediateFunction in this.IntermediateFunctions)
            {
                sb.Append(intermediateFunction.Value);
                sb.Append("\n");
            }
            return sb;
        }

        private StringBuilder DeclareFinalDictionary(StringBuilder sb)
        {
            // On construit un dictionnaire tr
            // { 'key', func }

            // Pour obtenir la trad: tr['key'](params)

            sb.Append("let tr = {");
            foreach (var item in this.MessagesJs)
            {
                sb.Append($"'{item.Key}' : {GetFunctionName(item.Key)},\n");
            }
            sb.Append("};");

            return sb;
        } 

        public void Serialize(TextWriter writer, string id, RuntimeAst.Message message)
        {
            if (message == null) return;
            OpenFunction(writer, id);

            if (message.Value is RuntimeAst.StringLiteral stringLiteral) // La Node est une simple string
            {
                DeclareReturn(writer);
                SerializeStringLiteral(writer, id, stringLiteral);
                Plus(writer);
                CloseComposition(writer);
                CloseFunction(writer);
            }
            else if (message.Value is RuntimeAst.Pattern pattern) // La node est un pattern
            {
                SerializePattern(writer, id, pattern);
                CloseFunction(writer);
            }
            else
            {
                // Cas non implémenté! Il faut implémenter le support de ce type de message complexe!
                Debugger.Break();
                FallBackJsFunction(writer, id);
            }
            
        }

        private void OpenFunction(TextWriter writer, string id)
        {
            writer.Write($"function {GetFunctionName(id)}(params) {{");
        }

        private void DeclareReturn(TextWriter writer)
        {
            writer.Write("return ");
        }

        private void Plus(TextWriter writer)
        {
            writer.Write(" + ");
        }

        private void CloseComposition(TextWriter writer)
        {
            writer.Write("'';");
        }

        private void CloseFunction(TextWriter writer)
        {
            writer.Write("}");
        }

        public void SerializeNode(TextWriter writer, string id, RuntimeAst.Node node)
        {
            Debugger.Break();
            writer.Write("[Faut sérializer la node]");
        }

        public void SerializeStringLiteral(TextWriter writer, string id, RuntimeAst.StringLiteral stringLiteral)
        {
            writer.Write($"'{ HttpUtility.JavaScriptStringEncode(stringLiteral.Value)}'");
        }

        public void SerializePattern(TextWriter writer, string id, RuntimeAst.Pattern pattern)
        {
            // 2 cas, soit y'a un select, soit y'en a pas.
            // Si y'en a pas on peut return la composition
            bool isSelect = true;
            if (!pattern.Elements.Any(elem => elem is RuntimeAst.SelectExpression))
            {
                isSelect = false;
                DeclareReturn(writer);
            }

                foreach (var element in pattern.Elements)
                {
                    if (element is RuntimeAst.StringLiteral sl)
                    {
                        
                        SerializeStringLiteral(writer, id, sl);
                        if (!isSelect)
                        {
                            Plus(writer);
                        }

                    }

                    if (element is RuntimeAst.VariableReference variableReference)
                    {
                        SerializeVariableReference(writer, id, variableReference);
                        if (!isSelect)
                        {
                            Plus(writer);
                        }
                    }

                    if (element is RuntimeAst.SelectExpression selectExpression)
                    {
                        SerializeSelectExpression(writer, id, selectExpression);
                    }

                    if (element is RuntimeAst.MessageReference messageReference)
                    {
                        SerializeMessageReference(writer, id, messageReference);
                        if (!isSelect)
                        {
                            Plus(writer);
                        }
                    }

                    else
                    {
                        Debugger.Break(); // faut gérer le cas
                    }
                }

                if (!isSelect)
                {
                    CloseComposition(writer);
               
                }
                

        }

        private void SerializeMessageReference(TextWriter writer, string id, RuntimeAst.MessageReference messageReference)
        {
            writer.Write(TermNameToVariableName(messageReference.Name));
        }

        private void SerializeSelectExpression(TextWriter writer, string id, RuntimeAst.SelectExpression selectExpression)
        {
            string switchContent;
            if (selectExpression.Expression is RuntimeAst.VariableReference variableReference)
            {
                switchContent = $"params['{variableReference.Name}']";
            } // Je pense l'autre alternative, c'est une référence vers un truc défini dans le ftl (genre le firefox là)
            else
            {
                // Oops cas non géré?
                switchContent = "BugBug";
                Debugger.Break();
            }


            writer.Write($"switch ({switchContent}){{\n");
            foreach (var variant in selectExpression.Variants)
            {
                VisitVariant(writer, variant);
            }
            writer.Write("}\n");
        }

        private void VisitMessage(TextWriter writer, string key, RuntimeAst.Node value)
        {
            writer.Write($"\t\"{key}\": ");
            if (value is RuntimeAst.StringLiteral lit)
            {
                VisitStringLiteral(writer, lit);
            }
            else
            {
                writer.WriteLine("function(context, params) {");
                writer.Write("\t\treturn ");
                VisitNode(writer, value);
                writer.WriteLine(";");
                writer.Write("\t}");
            }
            writer.WriteLine(",");
        }

        public void Visit(TextWriter writer)
        {

            writer.WriteLine("{");
            foreach (var (key, message) in this.Messages)
            {
                VisitMessage(writer, key, message.Value);
                if (message.Attributes != null)
                {
                    foreach(var (attrName, attrValue) in message.Attributes)
                    {
                        VisitMessage(writer, key + "." + attrName, attrValue);
                    }
                }
            }
            writer.WriteLine("};");
        }

        public void VisitNode(TextWriter writer, RuntimeAst.Node node)
        {
            switch(node)
            {
                case RuntimeAst.StringLiteral str: VisitStringLiteral(writer, str); break;
                case RuntimeAst.Pattern pattern: VisitPattern(writer, pattern); break;
                //case RuntimeAst.Message message: VisitMessage(writer, message); break;
                case RuntimeAst.VariableReference var: VisitVariableReference(writer, var); break;
                case RuntimeAst.MessageReference msgRef: VisitMessageReference(writer, msgRef); break;
                case RuntimeAst.SelectExpression expr: VisitSelect(writer, expr); break;
                case RuntimeAst.Variant variant: VisitVariant(writer, variant); break;
                case RuntimeAst.NamedArgument arg: VisitNamedArgument(writer, arg); break;
                case RuntimeAst.CallExpression expr: VisitCallExpression(writer, expr); break;
                case RuntimeAst.GetAttribute attr: VisitGetAttribute(writer, attr); break;
                default:
                {
                    if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                    throw new NotSupportedException($"Does not know how to visit {node.GetType().Name} nodes.");
                }
            }
        }

        /*
         * class FluentEvaluator = {
         * 
         *     ctor(culture: ...., data: ....) { this.culture = ....; this.data= data; }
         *     
         *     culture: string,
         *     data: { [name: id]: ((context, params): string) },
         *     
         *     evaluate(name: string, params: { [key: string]: any } | undefined, recursive?: bool): string | undefined {
         *         if (!recursive && name.charAt(0) === '-') return undefined; //TODO: hook?
         *         const message = this.data[name];
         *         if (!message) return undefined; //TODO: hook?
         *         return message(this, params);
         *     },
         *     
         *     variable: function(params: { [key: string]: any } | undefined, key: string) : any {
         *          // logic pour gérer le cas ou manquant, ou c'est une func, etc...
         *     },
         *     
         *     select: function(value: any, defaultIndex: number, variants: (name: string, expr: (context: FluentEvaluator, params: { [key: string]: any } | undefined) => string)[] {
         *          //TODO: logic qui map value en fct de son type
         *          // - number => ('0', '1', '2', 'few', 'many', 'other')
         *          // - date => ???
         *          const variantName = this.map(value);
         *          for(const variant of variants) {
         *              if (variant[0] === varianteName) {
         *                  return variant[1](context, params);
         *              }
         *          }
         *          // default!
         *          return variants[defaultIndex][1](context, params);
         *     },
         * 
         * }
         * let tr = {
         *     'KEY': function(context, params) {
         *          return "....";
         *     },
         *     'KEY': function(context, params) {
         *          return "...." + (context.variable(params, 'foo')) + ".....";
         *     },
         *     'KEY': function(context, params) {
         *          return "...." + (context.select(params, 'count', {
         *              'one': ....,
         *              'two': ....,
         *              'other': ...
         *          }, 'other') + ".....";
         *     },
         * 
         * 
         * };
         */ 

        public void VisitStringLiteral(TextWriter writer, RuntimeAst.StringLiteral node)
        {
            // "Hello World"
            writer.Write(HttpUtility.JavaScriptStringEncode(node.Value, addDoubleQuotes: true));
        }

        public void VisitNumberLiteral(TextWriter writer, RuntimeAst.NumberLiteral node)
        {
            writer.Write(node.Value);
        }

        public void VisitPattern(TextWriter writer, RuntimeAst.Pattern node)
        {
            // "...." + (...) + "...."

            //TODO: OPTIM: si on se rendait que tt les elems sont des stringliteral, on pourrait pre-merger en une seule string?

            bool first = true;
            foreach(var elem in node.Elements)
            {
                if (first) first = false; else writer.Write(" + ");
                VisitNode(writer, elem);
            }
        }

        public void VisitNamedArgument(TextWriter writer, RuntimeAst.NamedArgument node)
        {
            // ['NAME', value]
            writer.Write("[ ");
            writer.Write(HttpUtility.JavaScriptStringEncode(node.Name, addDoubleQuotes: true));
            writer.Write(", ");
            VisitNode(writer, node.Value);
            writer.Write("] ");
        }

        public void VisitVariableReference(TextWriter writer, RuntimeAst.VariableReference node)
        {
            // "context.variable(params, 'NAME')"
            writer.Write("context.variable(params, " + HttpUtility.JavaScriptStringEncode(node.Name, addDoubleQuotes: true) + ")");
        }

        public void VisitMessageReference(TextWriter writer, RuntimeAst.MessageReference node)
        {
            // "context.evaluate(params, 'NAME', true)"
            if (this.Messages.TryGetValue(node.Name, out var msg))
            {
                //TODO: optimisation possible: si le message référencé est un StinngLiteral, on pourrait l'inline directement
                if (/*bugbug: quid des attributs?*/ msg.Value is RuntimeAst.StringLiteral lit)
                {
                    VisitStringLiteral(writer, lit);
                    return;
                }
            }
            writer.Write("context.evaluate(params, " + HttpUtility.JavaScriptStringEncode(node.Name, addDoubleQuotes: true) + ", true)");
        }

        public void VisitGetAttribute(TextWriter writer, RuntimeAst.GetAttribute node)
        {
            //TODO: pour l'instant on simule que l'attribut 'attr' du message 'foo' est en fait un autre message 'foo.attr')

            if (this.Messages.TryGetValue(node.Id.Name, out var msg) && msg.Attributes != null && msg.Attributes.TryGetValue(node.Name, out var attr))
            {
                if (/*bugbug: quid des attributs?*/ attr is RuntimeAst.StringLiteral lit)
                {
                    VisitStringLiteral(writer, lit);
                    return;
                }
            }

            // "(cache['UNIQUEKEY'] ? cache['UNIQUEKEY'] : (cache['UNIQUEKEY'] = [..,. .., ....]))

            // "context.evaluate(params, 'NAME', true)"
            string name = node.Id.Name + "." + node.Name;
            writer.Write("context.evaluate(params, " + HttpUtility.JavaScriptStringEncode(name, addDoubleQuotes: true) + ", true)");
        }

        public void VisitSelect(TextWriter writer, RuntimeAst.SelectExpression node)
        {
            // "context.select(..EXPRESSION.., [ variant0, variant1, ...], defaultIndex)"
            writer.Write("context.select(");
            VisitNode(writer, node.Expression);
            writer.Write(", ");
            writer.Write(node.DefaultIndex);
            writer.WriteLine(", [");
            bool first = true;
            foreach(var variant in node.Variants)
            {
                if (first) first = false; else writer.WriteLine(", ");
                writer.Write("\t\t\t");
                VisitVariant(writer, variant);
            }
            writer.WriteLine();
            writer.Write("\t\t])");
        }

        public void VisitVariant(TextWriter writer, RuntimeAst.Variant node)
        {
            // "[ KEY, .... ]"
            writer.Write("[ ");
            switch (node.Key)
            {
                case RuntimeAst.VariantName vn:
                {
                    writer.Write(HttpUtility.JavaScriptStringEncode(vn.Name, addDoubleQuotes: true));
                    break;
                }
                case RuntimeAst.NumberLiteral nl:
                {
                    VisitNumberLiteral(writer, nl);
                    break;
                }
                default:
                {
                    throw new NotImplementedException($"Variant key of type {node.Key.GetType().Name} not supported.");
                }
            }
            writer.Write(", ");

            if (node.Value is RuntimeAst.StringLiteral lit)
            {
                VisitStringLiteral(writer, lit);
            }
            else
            {
                writer.Write("function() { return ");
                VisitNode(writer, node.Value);
                writer.Write(" }");
            }
            writer.Write(" ]");
        }

        public void VisitCallExpression(TextWriter writer, RuntimeAst.CallExpression node)
        {
            // "context.call('FUNCNAME', arg0, arg1, ....., argN)"
            writer.Write("context.call(");
            writer.Write(HttpUtility.JavaScriptStringEncode(node.Function, addDoubleQuotes: true));
            foreach(var arg in node.Args)
            {
                writer.Write(", ");
                VisitNode(writer, arg);
            }
            writer.Write(")");
        }

        private string GetRandomFuncName()
        {
            return Guid.NewGuid().ToString().Replace('-', '_');
        }

        private string ResolveVariantKey(RuntimeAst.Node variantKey)
        {
            // Faut regarder le type de la node, et résoudre ça..
            Debugger.Break();
            return "ResoudreVariantKeyPlz";
        }

        private string variantNameToJsCase(string variantName)
        {
            if (variantName == "one") return "1"; // Faut voir si ils ont un truc pour interprétter ça.
            if (variantName == "other") return "default";
            return variantName;
        }

        private void SerializeVariableReference(TextWriter writer, string id, RuntimeAst.VariableReference variableReference)
        {
            writer.Write($"params['{variableReference.Name}']");
            // throw new NotImplementedException();
        }

        private static string GetFunctionName(string id)
        {
            return $"trad_{id.Replace('-', '_')}";
        }

        // When the RuntimeAst.Message was too complex, and its js equivalent is not implemented, we fallback to a simple function that returns the key
        private void FallBackJsFunction(TextWriter writer, string id)
        {
            writer.Write($"function {GetFunctionName(id)}() {{ return '{id}'; }}");
        }
    }

}
