using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
            var input = Ftl(@"
                foo = Foo
            ");
            Pretty(input).Should().Be(input);
        }
    }


    // Nouvelle approche : On a déjà l'objet Serializable tout fait, et ils ont déjà un Serializer qui fait des lignes fluent, on reprend le Serializer sauf qu'il fait du JS.
    public class JavaScriptSerializer2
    {
        public JavaScriptSerializer2()
        {

        }

        public void Serialize(TextWriter writer, RuntimeAst.Node resource)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }
            
            // State state = 0;

            /*
            foreach (var entry in resource.)
            {
                if (this._withJunk || !(entry is Ast.Junk))
                {
                    SerializeEntry(indentingWriter, entry, state);
                    state |= State.HasEntries;
                }
            }*/


        }

        /*
         *        public string ToJsFunction(string id, RuntimeAst.Message message)
        {
            if (message == null) return null;
            var jsFunction = new StringBuilder($"function {GetFunctionName(id)}(parameters)");

            if (message.Value is RuntimeAst.StringLiteral stringLiteral) // La Node est une simple string
            {
                jsFunction.Append($"{{ return '{ HttpUtility.JavaScriptStringEncode(stringLiteral.Value)}'; }}");
            }
            else if (message.Value is RuntimeAst.Pattern pattern) // La node est un pattern
            {
                jsFunction.Append("{ return ");
                foreach (var element in pattern.Elements)
                {
                    if (element is RuntimeAst.StringLiteral sl)
                    {
                        jsFunction.Append($"'{HttpUtility.JavaScriptStringEncode(sl.Value)}' +");
                    }

                    if (element is RuntimeAst.VariableReference variableReference)
                    {
                        jsFunction.Append($"parameters['{variableReference.Name}'] +");
                    }

                    if (element is RuntimeAst.SelectExpression selectExpression) // ça devient trop compliqué, faut faire une pile et on push les items
                    {
                        // jsFunction.Append($"{selectExpression.Expression.Name")
                        if (selectExpression.Expression is RuntimeAst.VariableReference variableRef)
                        {
                            jsFunction.Append($"parameters['{variableRef.Name}']");
                        }

                        foreach (var variant in selectExpression.Variants)
                        {
                            jsFunction.Append($"== {variant.Key} ? '{variant.Value}'"); // variant.Value est une node, ça peut être une simple string, ça va souvent être un pattern (qui va mentionner la variableRef)
                        }
                    }
                }
                jsFunction.Append("''; }");
            }
            else
            {
                // Cas non implémenté! Il faut implémenter le support de ce type de message complexe!
                Debugger.Break();
                return FallBackJsFunction(id);
            }
            return jsFunction.ToString();
        }
      
         * */

    }

}
