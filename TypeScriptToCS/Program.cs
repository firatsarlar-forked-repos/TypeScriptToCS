﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeScriptToCS
{
    static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Namespace name?");
            string nameSpaceName = Console.ReadLine();
            Console.WriteLine("Typescript file location?");
            string tsFileLocation = Console.ReadLine();
            string tsFile = File.ReadAllText(tsFileLocation);
            List<NamespaceDefinition> nameSpaceDefinitions = new List<NamespaceDefinition>();
            int index = 0;
            ReadTypeScriptFile(tsFile, ref index, nameSpaceDefinitions);
            string endFile = 
                "using Bridge;\n\n\n";
            foreach (var namespaceItem in nameSpaceDefinitions)
            {
                if ((namespaceItem.name ?? "") != "")
                    endFile += $"namespace {namespaceItem.name}\n{ "{" }\n";
                foreach (var rItem in namespaceItem.typeDefinitions)
                {
                    if (rItem is ClassDefinition)
                    {
                        ClassDefinition classItem = (ClassDefinition)rItem;
                        string extendString = classItem.extends.Count != 0 ? " : " : string.Empty;
                        endFile += $"\t[External]\n\tpublic {classItem.type} {classItem.name}{extendString}{string.Join(", ", classItem.extends.ConvertAll(GetType)) + "\n\t{"}";
                        foreach (var item in classItem.fields)
                            endFile += "\n\t\tpublic " + (item.@static ? "static " : "") + $"{item.typeAndName.type} {char.ToUpper(item.typeAndName.name[0])}{item.typeAndName.name.Substring(1)};";
                        foreach (var item in classItem.methods)
                            endFile += "\n\t\tpublic " + (item.@static ? "static " : "") + "extern " + $"{item.typeAndName.type} {char.ToUpper(item.typeAndName.name[0])}{item.typeAndName.name.Substring(1)} (" + string.Join(", ", item.parameters.ConvertAll(v => (v.@params ? "params " : string.Empty) + v.type + (v.optional ? "? " : " ") + v.name)) + ");";
                        foreach (var item in classItem.properties)
                            endFile += "\n\t\tpublic " + (item.@static ? "static " : "") + $"extern {item.typeAndName.type} {char.ToUpper(item.typeAndName.name[0])}{item.typeAndName.name.Substring(1)}" + "{ " + (item.get ? "get; " : "") + (item.set ? "set; " : "") + "}";
                    }
                    else if (rItem is EnumDefinition)
                    {
                        EnumDefinition enumItem = (EnumDefinition)rItem;
                        endFile += $"\t[External]\n\tpublic enum {enumItem.name + "\n\t{"}\n\t\t{string.Join(",\n\t\t", enumItem.members)}";
                    }
                    endFile += "\n\t}\n";
                }
                if ((namespaceItem.name ?? "") != "")
                    endFile += "\n}\n";
            }
            File.WriteAllText("output.cs", endFile);
        }

        public static string GetType (string value)
        {
            return value.Replace("any", "object").Replace("number", "double").Replace("Number", "Double");
        }

        private static void ReadTypeScriptFile(string tsFile, ref int index, List<NamespaceDefinition> namespaces)
        {
            NamespaceDefinition global = new NamespaceDefinition();
            List<NamespaceDefinition> namespaceTop = new List<NamespaceDefinition>();
            namespaces.Add(global);
            List<TypeDefinition> typeTop = new List<TypeDefinition>();
            for (; index < tsFile.Length; index++)
            {
                while (tsFile[index] == '/')
                {
                    index++;
                    if (tsFile[index] == '/')
                        index = tsFile.IndexOf('\n', index);
                    else if (tsFile[index] == '*')
                        index = tsFile.IndexOf("*/", index);
                    SkipEmpty(tsFile, ref index);
                }
                SkipEmpty(tsFile, ref index);
                if (index >= tsFile.Length)
                    break;
                BracketLoop:
                if (tsFile[index] == '{')
                {
                    index++;
                    SkipEmpty(tsFile, ref index);
                }
                if (tsFile[index] == '}')
                {
                    switch (typeTop.Count)
                    {
                        case 0:
                            break;
                        default:
                            if (typeTop.Last() is ClassDefinition)
                                if ((typeTop.Last() as ClassDefinition).name == "GlobalClass")
                                    break;
                            if (namespaceTop.Count == 0)
                            {
                                global.typeDefinitions.Add(typeTop.Last());
                                typeTop.RemoveAt(typeTop.Count - 1);
                                goto OutIfBreak;
                            }
                            namespaceTop.Last().typeDefinitions.Add(typeTop[0]);
                            typeTop.RemoveAt(typeTop.Count - 1);
                            goto OutIfBreak;
                    }
                    namespaces.Add(namespaceTop.Last());
                    namespaceTop.RemoveAt(namespaceTop.Count - 1);
                    goto OutIfBreak;
                }
                goto After;
                OutIfBreak:
                if (++index >= tsFile.Length) return;
                After:
                string word;
                bool @static = false;
                bool get = false;
                bool set = false;
                do
                {
                    word = SkipToEndOfWord(tsFile, ref index);
                    switch (word)
                    {
                        case "static":
                            @static = true;
                            break;
                        case "get":
                            get = true;
                            break;
                        case "set":
                            set = true;
                            break;
                    }
                    SkipEmpty(tsFile, ref index);
                }
                while (word == "export" || word == "declare" || word == "static" || word == "get" || word == "set");
                switch (word)
                {
                    case "class":
                    case "interface":
                        typeTop.Add(new ClassDefinition
                        {
                            name = SkipToEndOfWord(tsFile, ref index),
                            type = (TypeType)Enum.Parse(typeof(TypeType), word)
                        });
                        SkipEmpty(tsFile, ref index);
                        var nWord = SkipToEndOfWord(tsFile, ref index);
                        if (nWord == "extends")
                        {
                            SkipEmpty(tsFile, ref index);
                            (typeTop.Last() as ClassDefinition).extends.Add(SkipToEndOfWord(tsFile, ref index));
                        }
                        break;
                    case "enum":
                        typeTop.Add(new EnumDefinition
                        {
                            name = SkipToEndOfWord(tsFile, ref index)
                        });
                        break;
                    case "module":
                    case "namespace":
                        namespaceTop.Add(new NamespaceDefinition
                        {
                            name = SkipToEndOfWord(tsFile, ref index)
                        });
                        typeTop.Add(new ClassDefinition
                        {
                            name = "GlobalClass",
                            type = TypeType.@class
                        });
                        break;
                    default:
                        char item = tsFile[index++];
                        switch (item)
                        {
                            case ',':
                            case '}':
                                var enumItem = typeTop.Last() as EnumDefinition;
                                if (enumItem != null)
                                    enumItem.members.Add(word);
                                if (item == '}')
                                {
                                    index--;
                                    goto BracketLoop;
                                }
                                break;
                            case ':':
                                SkipEmpty(tsFile, ref index);
                                var type = SkipToEndOfWord(tsFile, ref index);
                                (typeTop.Last() as ClassDefinition).fields.Add(new Field
                                {
                                    @static = @static,
                                    typeAndName = new TypeAndName
                                    {
                                        type = type,
                                        name = word
                                    }
                                });
                                SkipEmpty(tsFile, ref index);
                                if (tsFile[index] == ';')
                                    index++;
                                continue;
                            default:
                                continue;
                            case '(':
                                Method method = new Method();
                                method.typeAndName.name = word;
                                method.@static = @static;

                                SkipEmpty(tsFile, ref index);
                                if (tsFile[index] == ')')
                                {
                                    index++;
                                    SkipEmpty(tsFile, ref index);
                                    goto Break;
                                }
                                
                                for (; index < tsFile.Length; index++)
                                {
                                    SkipEmpty(tsFile, ref index);
                                    bool optional = false;
                                    bool @params = false;
                                    if (tsFile[index] == '.') { index += 3; SkipEmpty(tsFile, ref index); @params = true; }
                                    string word2 = SkipToEndOfWord(tsFile, ref index);
                                    SkipEmpty(tsFile, ref index);
                                    if (tsFile[index] == '?')
                                    {
                                        optional = true;
                                        index++;
                                    }
                                    SkipEmpty(tsFile, ref index);
                                    switch (tsFile[index])
                                    {
                                        case ':':
                                            index++;
                                            SkipEmpty(tsFile, ref index);
                                            string type2 = SkipToEndOfWord(tsFile, ref index);
                                            method.parameters.Add(new TypeNameAndOptional
                                            {
                                                optional = optional,
                                                @params = @params,
                                                name = word2,
                                                type = type2
                                            });
                                            SkipEmpty(tsFile, ref index);
                                            if (tsFile[index] != ',')
                                                goto case ')';
                                            break;
                                        case ')':
                                            index++;
                                            SkipEmpty(tsFile, ref index);
                                            goto Break;
                                    }
                                }
                                Break:
                                if (tsFile[index] == ':')
                                {
                                    index++;
                                    SkipEmpty(tsFile, ref index);
                                    method.typeAndName.type = SkipToEndOfWord(tsFile, ref index);
                                    SkipEmpty(tsFile, ref index);
                                }
                                else
                                    method.typeAndName.type = "object";
                                if (get || set)
                                    (typeTop.Last() as ClassDefinition).properties.Add(new Property
                                    {
                                        get = get,
                                        set = set,
                                        @static = @static,
                                        typeAndName = method.typeAndName
                                    });
                                else
                                   (typeTop.Last() as ClassDefinition).methods.Add(method);
                                goto DoubleBreak;
                        }
                        break;
                }
                DoubleBreak:;
            }
        }

        private static string SkipToEndOfWord (string tsFile, ref int index)
        {
            if (!char.IsLetter(tsFile, index))
                SkipEmpty(tsFile, ref index);
            string result = "";
            for (; index < tsFile.Length; index++)
            {
                var item = tsFile[index];
                if (char.IsLetterOrDigit(item) || item == '[' || item == ']')
                    result += item;
                else
                    return result;
            }
            return result;
        }

        private static void SkipEmpty (string tsFile, ref int index)
        {
            for (; index < tsFile.Length; index++)
                switch (tsFile[index])
                {
                    case '\n':
                    case '\r':
                    case '\t':
                    case ' ':
                        break;
                    default:
                        return;
                }
        }
    }
}
