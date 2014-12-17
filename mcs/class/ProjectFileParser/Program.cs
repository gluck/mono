using Microsoft.Build.BuildEngine;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ProjectFileParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var solution = new Project();
            var obj = JObject.Parse(Console.In.ReadToEnd());
            foreach (var prop in obj)
            {
                solution.GlobalProperties[prop.Key] = new BuildProperty(prop.Key, prop.Value.ToString());
            }

            solution.Load(args[0]);
            var result = ToJson(solution);
            if (Path.GetExtension(args[0]) == ".sln")
            {
                var jSolution = result;
                result = new JObject();
                result["Solution"] = jSolution;
                foreach (var proj in solution.EvaluatedItemsByName.Where(entry => entry.Key.StartsWith("BuildLevel")).Select(entry => entry.Value.Cast<BuildItem>().First()))
                {
                    var project = solution.ParentEngine.CreateNewProject();
                    foreach (var meta in proj.EvaluatedMetadata.Cast<DictionaryEntry>())
                    {
                        project.GlobalProperties[(string)meta.Key] = new BuildProperty((string)meta.Key, (string)meta.Value);
                    }
                    project.Load(proj.FinalItemSpec);
                    var jProject = ToJson(project);
                    result[Path.GetFileNameWithoutExtension(proj.FinalItemSpec)] = jProject;
                }
            }
            Console.WriteLine(result.ToString());
        }

        private static JObject ToJson(Project project)
        {
            JObject jProject = new JObject();
            JObject properties = new JObject();
            jProject["Properties"] = properties;
            foreach (var entry in project.EvaluatedProperties.Cast<BuildProperty>())
            {
                properties[entry.Name] = entry.ToString();
            }
            foreach (var entry in project.EvaluatedItemsByName)
            {
                JArray items = new JArray();
                jProject[entry.Key] = items;
                foreach (var item in entry.Value.Cast<BuildItem>())
                {
                    items.Add(item.FinalItemSpec);
                }
            }
            return jProject;
        }
    }
}
