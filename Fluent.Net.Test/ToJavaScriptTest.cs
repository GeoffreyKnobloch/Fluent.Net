using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

                var javaScriptSerializer = new JavaScriptSerializer();
                return javaScriptSerializer.ToJsFunction(actual);
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

                return new JavaScriptSerializer().ToJsFunction(entry);
            }
        }
    }

    public class JavaScriptSerializer
    {
        public List<MessageContext> MessageContexts {get; private set;}
        private Dictionary<string, string> TermsJs {get; } = new Dictionary<string, string>();
        private Dictionary<string, RuntimeAst.Message> TermsMessage{get; } = new Dictionary<string, RuntimeAst.Message>();
        private Dictionary<string, string> MessagesJs {get; } = new Dictionary<string, string>();
        private Dictionary<string, RuntimeAst.Message> MessagesMessage {get; } = new Dictionary<string, RuntimeAst.Message>();
        private List<string> Keys {get; } = new List<string>();
        public bool WithSpans { get; }
        public bool IsDirty {get; private set; } = true;

        public JavaScriptSerializer(bool withSpans = false)
        {
            WithSpans = withSpans; // ?
        }

        public MessageContext LoadContext(string lang) // Load le .ftl, sauvegarde les messages 
        {
             string ftlPath = Path.Combine("..", "..", "..", $"{lang}.ftl");
            using (var sr = new StreamReader(ftlPath))
            {
                var options = new MessageContextOptions { UseIsolating = false };
                var mc = new MessageContext(lang, options);
                var errors = mc.AddMessages(sr);
                foreach (var error in errors)
                {
                    Console.WriteLine(error);
                }

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

                // TODO: Alimenter la Liste des keys en parsant directement le stream reader.
                this.MessageContexts.Add(mc);
                return mc;
            }
        }


        public void GenerateJs()
        {
            // On prend les Dico de TermsMessage et MessagesMessage, et on en fait du js.
            foreach (var term in TermsMessage)
            {
                // Parcour le Term, et alimente 
                this.TermsJs.Add(term.Key, $"let {term.Key} = '{term.Value.Value}'"); // Impl naive, faut regarder term.Value à quoi ça ressemble pour un term.
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
            if (this.IsDirty) return string.Empty; // ?
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
            return sb;
        }

        private StringBuilder DeclareFunctions(StringBuilder sb)
        {
            return sb;
        }

        private StringBuilder DeclareFinalDictionary(StringBuilder sb)
        {
            // On construit un dictionnaire tr
            // { 'key', func }

            // Pour obtenir la trad: tr['key'](params)
            return sb;
        } 

        // Iplémentation naive qui fonctionne que pour le cas simple:
        public string ToJsFunction(Ast.Entry entry)
        {
            var message = entry as Ast.Message;
            if (message == null) return null;
            var jsFunction = new StringBuilder($"function trad_{message.Id.Name}");
            if (message.Value is Ast.Pattern pattern)
            {
                if (pattern.Elements.Count == 1)
                {
                    if (pattern.Elements[0] is Ast.TextElement textElement)
                    {
                         // Le cas ez
                        jsFunction.Append($"() : string {{ return '{textElement.Value}'; }}");
                    }
                   
                }
            }

            return jsFunction.ToString();
        }

        public string ToJsFunction(string id, RuntimeAst.Message message) // impl à l'arache, faut tester et corriger
        {
             if (message == null) return null;
            var jsFunction = new StringBuilder($"function trad_{id}");
            if (message.Value is RuntimeAst.Pattern pattern)
            {
                if (pattern.Elements.Count == 1)
                {
                    if (pattern.Elements.First() is RuntimeAst.Message textElement)
                    {
                         // Le cas ez
                        jsFunction.Append($"() : string {{ return '{textElement.Value}'; }}");
                    }
                   
                }
            }

            return jsFunction.ToString();
        }
    }
}
