// GtkSharp.Generation.GenerationInfo.cs - Generation information class.
//
// Author: Mike Kestner <mkestner@ximian.com>
//
// Copyright (c) 2003-2008 Novell Inc.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of version 2 of the GNU General Public
// License as published by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// General Public License for more details.
//
// You should have received a copy of the GNU General Public
// License along with this program; if not, write to the
// Free Software Foundation, Inc., 59 Temple Place - Suite 330,
// Boston, MA 02111-1307, USA.

using System;
using System.IO;
using System.Xml;

namespace GapiCodegen
{
    /// <summary>
    /// Stores info passed in on the command line, such as the assembly name and glue library name.
    /// </summary>
    public class GenerationInfo
    {
        private readonly string _abiCFile;
        private readonly string _abiCsFile;

        public GenerationInfo(XmlElement namespaceElement)
        {
            var nsName = namespaceElement.GetAttribute("name");
            var separatorChar = Path.DirectorySeparatorChar;
            Directory = $"..{separatorChar}{nsName.ToLower()}{separatorChar}generated";
            AssemblyName = $"{nsName.ToLower()}-sharp";
        }

        public GenerationInfo(string directory, string assemblyName) :
            this(directory, assemblyName, "", "", "", "", "", "")
        { }

        public GenerationInfo(string directory, string assemblyName, string glueFilename,
                string glueIncludes, string glueLibName, string abiCFile,
                string abiCsFile, string abiCsUsings)
        {
            Directory = directory;
            AssemblyName = assemblyName;
            GlueLibName = glueLibName;
            _abiCFile = abiCFile;
            _abiCsFile = abiCsFile;

            InitializeWriters(glueFilename, glueIncludes, glueLibName, abiCsUsings);
        }

        private void InitializeWriters(string glueFilename, string glueIncludes,
            string gluelibName, string abiCsUsings)
        {
            FileStream stream;

            if (gluelibName != string.Empty && glueFilename != string.Empty)
            {
                try
                {
                    stream = new FileStream(glueFilename, FileMode.Create, FileAccess.Write);
                }
                catch (Exception)
                {
                    Console.Error.WriteLine("Unable to create specified glue file.  Glue will not be generated.");
                    return;
                }

                GlueWriter = new StreamWriter(stream);

                GlueWriter.WriteLine("// This file was generated by the Gtk# code generator.");
                GlueWriter.WriteLine("// Any changes made will be lost if regenerated.");
                GlueWriter.WriteLine();

                if (glueIncludes != "")
                {
                    foreach (var header in glueIncludes.Split(',', ' '))
                    {
                        if (header != "")
                            GlueWriter.WriteLine("#include <{0}>", header);
                    }

                    GlueWriter.WriteLine("");
                }

                GlueEnabled = true;
            }

            if (CAbiWriter != null || _abiCFile == "" || _abiCsFile == "" || abiCsUsings == "") return;

            stream = new FileStream(_abiCFile, FileMode.Create, FileAccess.Write);

            CAbiWriter = new StreamWriter(stream);
            CAbiWriter.WriteLine("// This file was generated by the Gtk# code generator.");
            CAbiWriter.WriteLine("// Any changes made will be lost if regenerated.");
            CAbiWriter.WriteLine();

            if (glueIncludes != "")
            {
                foreach (var header in glueIncludes.Split(',', ' '))
                {
                    if (header != "")
                        CAbiWriter.WriteLine("#include <{0}>", header);
                }

                CAbiWriter.WriteLine("");
            }

            CAbiWriter.WriteLine("int main (int argc, char *argv[]) {");

            stream = new FileStream(_abiCsFile, FileMode.Create, FileAccess.Write);

            AbiWriter = new StreamWriter(stream);
            AbiWriter.WriteLine("// This file was generated by the Gtk# code generator.");
            AbiWriter.WriteLine("// Any changes made will be lost if regenerated.");
            AbiWriter.WriteLine();

            var name = "";

            foreach (var @using in abiCsUsings.Split(',', ' '))
            {
                if (@using == "") continue;

                AbiWriter.WriteLine("using {0};", @using);
                if (name == "")
                    name = @using;
            }

            AbiWriter.WriteLine("using System;");
            AbiWriter.WriteLine();
            AbiWriter.WriteLine("namespace AbiTester {");
            AbiWriter.WriteLine($"\tclass ___{name} {{");
            AbiWriter.WriteLine("\t\tpublic static void Main (string[] args) {");
        }

        public string AssemblyName { get; }

        public StreamWriter AbiWriter { get; private set; }

        public StreamWriter CAbiWriter { get; private set; }

        public string Directory { get; }

        public string GlueLibName { get; }

        public bool GlueEnabled { get; private set; }

        public StreamWriter GlueWriter { get; private set; }

        public StreamWriter Writer { get; set; }

        public void CloseWriters()
        {
            GlueWriter?.Close();

            if (CAbiWriter == null) return;

            CAbiWriter.WriteLine("\treturn 0;");
            CAbiWriter.WriteLine("}");
            CAbiWriter.Close();

            AbiWriter.WriteLine("\t\t}");
            AbiWriter.WriteLine("\t}");
            AbiWriter.WriteLine("}");
            AbiWriter.Close();
        }

        private string _member;

        public string CurrentMember
        {
            get => $"{CurrentType}.{_member}";
            set => _member = value;
        }

        public string CurrentType { get; set; }

        public StreamWriter OpenStream(string name, string @namespace)
        {
            var genDir = Path.Combine(Directory, @namespace);
            System.IO.Directory.CreateDirectory(genDir);

            var filename = Path.Combine(genDir, $"{name}.cs");

            var stream = new FileStream(filename, FileMode.Create, FileAccess.Write);
            var streamWriter = new StreamWriter(stream);

            streamWriter.WriteLine("// This file was generated by the Gtk# code generator.");
            streamWriter.WriteLine("// Any changes made will be lost if regenerated.");
            streamWriter.WriteLine();

            return streamWriter;
        }
    }
}
