using System.IO;
using System.Linq;
using CodeModel;

namespace ConsoleGen
{
    public class AngularGenerator
    {
        public AngularGenerator(Api api, TextWriter writer)
        {
            Api = api;
            Writer = writer;
        }
        public Api Api { get; set; }
        public TextWriter Writer { get; set; }

        private void WriteLine(string format, params object[] args)
        {
            Writer.WriteLine(format, args);
        }

        private void WriteLine(string text = "")
        {
            Writer.WriteLine(text);
        }

        private void Write(string format, params object[] args)
        {
            Writer.Write(format, args);
        }

        private void Write(string text)
        {
            Writer.Write(text);
        }

        public void Run()
        {
            WriteLine(
@"/////////////////////////////////////////////////////////////////////////////////////////////////
//
//    TypeScript definitions for REST WebAPI services 
//    WARNING: This file has been automatically generated. Any changes may be lost on the next run.
//
/////////////////////////////////////////////////////////////////////////////////////////////////

/// <reference path=""../../scripts/typings/angularjs/angular.d.ts"" />");
            foreach (var controller in Api.Controllers)
            {
                var areaName = Utils.GetAreaNamespace(controller.Type.FullName);
                if (areaName == null) continue;
                WriteLine("");
                WriteLine("declare module {0} {{", areaName);
                Indent(1); WriteLine("// {0}", Utils.GetAreaLocalName(controller.Type.FullName));
                Indent(1); Write("export interface {0}", Utils.GetProxyName(controller.Type.FullName)); WriteLine(" {");

                foreach (var member in controller.Methods)
                {
                    WriteLine();
                    Indent(2); WriteLine("// {0}({1})", member.OriginalName, string.Join(", ", member.Parameters.Select(p => p.Name)));
                    if (member.IsValid)
                    {
                        Indent(2); Write("{0}(", member.Name);
                        Write(string.Join(", ", member.Parameters.Select(Utils.GetTSParamDefinition)));
                        WriteLine("): ng.IHttpPromise<{0}>;", Utils.GetTSName(member.Type));
                    }
                    else
                    {
                        WriteLine(""); Indent(2); WriteLine("// Error in method '{0}': {1}", member.OriginalName, member.ValidationError);
                    }
                }
                Indent(1); WriteLine("}");
                Indent(0); WriteLine("}");
            }

            foreach (var type in Api.Models)
            {
                GenerateModel(type);
            }

            foreach (var type in Api.Enums)
            {
                GenerateEnum(type);
            }

        }
        void GenerateEnum(Enum enumDef)
        {
            var fullName = enumDef.Type.FullName;
            var areaName = Utils.GetAreaNamespace(fullName);
            if (areaName == null) return;
            WriteLine("");
            WriteLine("module {0} {{", areaName);
            Indent(1); WriteLine("export enum {0} {{", fullName.Split('.').Last());
            foreach (var member in enumDef.Members)
            {
                // render name and value (optional)
                Indent(2);
                Write("{0}", member.Name);
                if (member.Value != null)
                    Write(" = {0}", member.Value);
                WriteLine(",");
            }
            Indent(1); WriteLine("}");
            WriteLine("}");
        }

        void Indent(int level)
        {
            for (int i = 0; i < level; i++)
            {
                Write("    ");
            }
        }


        void GenerateModel(Model modelDef)
        {
            string fullName = modelDef.Type.FullName;
            var areaName = Utils.GetAreaNamespace(fullName);
            if (areaName == null) return;
            WriteLine("");
            WriteLine("module {0} {{", areaName);
            Indent(1); WriteLine("export interface {0} {{", Utils.GetModelName(fullName).Split('.').Last());
            var members = modelDef.Properties;
            foreach (var property in members)
            {
                Indent(2); WriteLine("{0}: {1};", property.Name, Utils.GetTSName(property.Type));
            }
            Indent(1); WriteLine("}");
            WriteLine("}");
        }
    }
}