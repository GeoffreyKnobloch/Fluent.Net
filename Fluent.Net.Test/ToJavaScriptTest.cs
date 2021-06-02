using Fluent.Net.Ast;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
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
    class ToJavaScriptTest : ParserTestBase
    {
        string ParseAndCheck(string ftl, Ast.SyntaxNode expected,
            bool withSpans = false)
        {
            using (var sr = new StringReader(Ftl(ftl)))
            {
                var ps = new Parser(withSpans);
                var actual = ps.ParseEntry(sr);
                actual.Should().BeEquivalentTo(expected,
                    options => options.RespectingRuntimeTypes());

                
                return JavaScriptSerializer.ToJsFunction(actual);
            }
        }

        [Test]
        public void SimpleMessage()
        {
            string ftl = @"
                foo = Foo
            ";

            var output = new Ast.Message()
            {
                Id = new Ast.Identifier()
                {
                    Name = "foo",
                    Span = new Ast.Span(
                        new Position(0, 1, 1),
                        new Position(3, 1, 4))
                },
                Span = new Ast.Span(
                    new Position(0, 1, 1),
                    new Position(9, 1, 10)),
                Value = new Ast.Pattern()
                {
                    Elements = new Ast.SyntaxNode[] {
                        new Ast.TextElement()
                        {
                            Value = "Foo",
                            Span = new Ast.Span(
                                new Position(6, 1, 7),
                                new Position(9, 1, 10))
                        }
                    },
                    Span = new Ast.Span(
                        new Position(6, 1, 7),
                        new Position(9, 1, 10))
                },
            };
            var jsFunction = ParseAndCheck(ftl, output, true);

            Assert.That(jsFunction, Is.EqualTo("function trad_foo() : string { return 'Foo'; }"));

            // Si on veut on peut aussi obtenir le js direct à partir de la chaîne:
            jsFunction = ToJsFunction(ftl);
            // Et ça ne devrait pas changer l'output:
            Assert.That(jsFunction, Is.EqualTo("function trad_foo() : string { return 'Foo'; }"));

        }

        public string ToJsFunction(string ftl, bool withSpans = false)
        {
            using (var sr = new StringReader(Ftl(ftl)))
            {
                var ps = new Parser(withSpans);
                var entry = ps.ParseEntry(sr);

                return JavaScriptSerializer.ToJsFunction(entry);
            }
        }

        [Test]
        public void TestCanLoadFtlAndGetJs()
        {
            var jsSerializer = new JavaScriptSerializer();
            var errors = jsSerializer.LoadContext("en");
            Assert.That(errors, Is.Null.Or.Empty);
            jsSerializer.GenerateJs();
            var js = jsSerializer.GetResult();
            Console.WriteLine("Final js output:");
            Console.WriteLine(js);

        }

        /*
         * Valid JS:
         * let sync_brand_name = 'Firefox Account';
    function trad_tabs_close_button() { return 'Close'; }
    function trad_tabs_close_tooltip() { return 'tabs-close-tooltip'; }
    function trad_tabs_close_warning() { return 'tabs-close-warning'; }
    function trad_sync_dialog_title() { return 'sync-dialog-title'; }
    function trad_sync_headline_title() { return 'sync-headline-title'; }
    function trad_sync_signedout_title() { return 'sync-signedout-title'; }

    let tr = {'tabs-close-button' : trad_tabs_close_button,
    'tabs-close-tooltip' : trad_tabs_close_tooltip,
    'tabs-close-warning' : trad_tabs_close_warning,
    'sync-dialog-title' : trad_sync_dialog_title,
    'sync-headline-title' : trad_sync_headline_title,
    'sync-signedout-title' : trad_sync_signedout_title
    };
         * 
         * */

    }

    public class JavaScriptSerializer
    {
        public List<MessageContext> MessageContexts {get; } = new List<MessageContext>();
        private Dictionary<string, string> TermsJs {get; } = new Dictionary<string, string>();
        private Dictionary<string, RuntimeAst.Message> TermsMessage{get; } = new Dictionary<string, RuntimeAst.Message>();
        private Dictionary<string, string> MessagesJs {get; } = new Dictionary<string, string>();
        private Dictionary<string, RuntimeAst.Message> MessagesMessage {get; } = new Dictionary<string, RuntimeAst.Message>();
        private List<string> Keys {get; } = new List<string>();
        public bool WithSpans { get; }

        public JavaScriptSerializer(bool withSpans = false)
        {
            WithSpans = withSpans; // ?
        }

        public MessageContext LoadContext(string lang) // Load le .ftl, sauvegarde les messages 
        {
            string ftlPath = Path.Combine("..", "..", "..", $"{lang}.ftl");
            using (var sr = new StreamReader(ftlPath))
            {
                /*
                var options = new MessageContextOptions { UseIsolating = false };
                var mc = new MessageContext(lang, options);
                var errors = mc.AddMessages(sr);
                foreach (var error in errors)
                {
                    Console.WriteLine(error);
                }
                */
                var fluentRessource = FluentResource.FromReader(sr);
                foreach (var entry in fluentRessource.Entries)
                {
                    if (!this.Keys.Contains(entry.Key)) this.Keys.Add(entry.Key);

                    if (entry.Key.StartsWith("-"))
                    {
                        if (!this.TermsMessage.ContainsKey(entry.Key))
                        {
                            this.TermsMessage.Add(entry.Key, entry.Value);
                        }
                    }
                    else
                    {
                        if (!this.MessagesMessage.ContainsKey(entry.Key))
                        {
                            this.MessagesMessage.Add(entry.Key, entry.Value);
                        }
                    }
                }

                // this.MessageContexts.Add(mc);
                return null;
            }
        }


        public void GenerateJs()
        {
            // On prend les Dico de TermsMessage et MessagesMessage, et on en fait du js.
            foreach (var term in TermsMessage)
            {
                var variableName = term.Key.Substring(1).Replace('-', '_');
                // Parcour le Term, et alimente
                if (term.Value.Value is RuntimeAst.StringLiteral stringLiteral)
                {
                    this.TermsJs.Add(term.Key, $"let {variableName} = '{stringLiteral.Value}';");
                }
                else
                {
                    Debugger.Break(); // Fallback, mais pas prévu d'arriver là!
                    this.TermsJs.Add(term.Key, $"let {variableName} = '{variableName}';");
                }
            }

            foreach (var message in this.MessagesMessage)
            {
                this.MessagesJs.Add(message.Key, ToJsFunction(message.Key, message.Value));
            }
        }

        /* On drop ça, on load le ftl direct.
        public void LoadEntrys(Ast.Entry[] entries)
        {
            foreach (var entry in entries)
            {
                LoadEntry(entry);
            }
            this.IsDirty = false;
        }
        public void LoadEntry(Ast.Entry entry)
        {
            // Si c'est une reference (-key = value), il faut stocker dans le dico de References
            // Si c'est une trad (key = value), il faut stocker dans le dico des Translations
        }*/

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

        // Iplémentation naive qui fonctionne que pour le cas simple:
        [Obsolete("On drop ça pour l'autre façon de faire")]
        public static string ToJsFunction(Ast.Entry entry)
        {
            var message = entry as Ast.Message;
            if (message == null) return null;
            var jsFunction = new StringBuilder($"function {GetFunctionName(message.Id.Name)}");
            if (message.Value is Ast.Pattern pattern)
            {
                if (pattern.Elements.Count == 1)
                {
                    if (pattern.Elements[0] is Ast.TextElement textElement)
                    {
                         // Le cas ez
                        jsFunction.Append($"() {{ return '{textElement.Value}'; }}");
                    }
                }
            }

            return jsFunction.ToString();
        }

        public string ToJsFunction(string id, RuntimeAst.Message message)
        {
             if (message == null) return null;
            var jsFunction = new StringBuilder($"function {GetFunctionName(id)}");

            if (message.Value is RuntimeAst.StringLiteral stringLiteral)
            {
                // ça va être facile ça!
                // HttpUtility.JavaScriptStringEncode
               
                jsFunction.Append($"() {{ return '{ HttpUtility.JavaScriptStringEncode(stringLiteral.Value)}'; }}");
            }
            else
            {
                // Cas non implémenté! Il faut implémenter le support de ce type de message complexe!
                // Contacter Geoffrey si vous breakez ici.
                Debugger.Break();
                return FallBackJsFunction(id);
            }
            /* if (message.Value is RuntimeAst.Pattern pattern)
            {
                if (pattern.Elements.Count == 1)
                {
                    if (pattern.Elements.First() is RuntimeAst.Message textElement)
                    {
                         // Le cas ez
                        jsFunction.Append($"() : string {{ return '{textElement.Value}'; }}");
                    }
                   
                }
            }*/

            return jsFunction.ToString();
        }

        private static string GetFunctionName(string id)
        {
            return $"trad_{id.Replace('-', '_')}";
        }

        // When the RuntimeAst.Message was too complex, and its js equivalent is not implemented, we fallback to a simple function that returns the key
        private string FallBackJsFunction(string id)
        {
            return $"function {GetFunctionName(id)}() {{ return '{id}'; }}";
        }
    }
}
