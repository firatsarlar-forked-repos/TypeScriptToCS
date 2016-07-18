﻿using System.Collections.Generic;

namespace TypeScriptToCS
{
    public class Method : ClassElement, TypeDefinition
    {
        public TypeAndName typeAndName = new TypeAndName();

        public List<TypeNameOptionalAndParams> parameters = new List<TypeNameOptionalAndParams>();

        public Method Clone() => (Method)MemberwiseClone();

        public Dictionary<string, string> typeWheres = new Dictionary<string, string>();

        public string name { get { return typeAndName.name; } set { typeAndName.name = value; } }
        public string CapitalName { get {
                if (string.IsNullOrEmpty(name))
                    return string.Empty;
                return char.ToUpper(name[0]) + name.Substring(1); } }
        public char StartBracket => indexer ? '[' : '(';
        public char EndBracket => indexer ? ']' : ')';
        public bool indexer;
    }
}