using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Newtonsoft.Json;

namespace RoslynLocalizeDetector
{
	class Program
	{
		static void Main(string[] args)
		{
			// ** Only works if run from VSDev Command prompt **
			//
			//var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
			//var instanceToUse = instances.First(a => a.Version.Major == 15);
			//Environment.SetEnvironmentVariable("VSINSTALLDIR", instanceToUse.VisualStudioRootPath);

			Environment.SetEnvironmentVariable("VSINSTALLDIR", @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community");
			Environment.SetEnvironmentVariable("VisualStudioVersion", "15.0");

			string matterControlRoot = GetMatterControlDirectory();

			var properties = new Dictionary<string, string>
			{
			   { "Configuration", "Debug" },
				{"Platform", "AnyCPU" }
			};
			//var workspace = MSBuildWorkspace.Create(properties);
			var workspace = MSBuildWorkspace.Create();

			workspace.WorkspaceFailed += (s, e) => Console.WriteLine(e.Diagnostic.Message);
			workspace.LoadMetadataForReferencedProjects = true;

			string solutionPath = Path.GetFullPath(Path.Combine(matterControlRoot, "..", "MatterControl.sln"));

			Solution solution = workspace.OpenSolutionAsync(solutionPath).Result;

			var translationStrings = new HashSet<string>();

			DiagnosticAnalyzer analyzer = new LocalizeDetector((locstring) =>
			{
				// Add detected Localize() strings to translations list
				translationStrings.AddLocalization(locstring);
			});

			ProjectDependencyGraph projectGraph = solution.GetProjectDependencyGraph();

			// Use Roslyn to find localize calls
			foreach (ProjectId projectId in projectGraph.GetTopologicallySortedProjects())
			{
				var project = solution.GetProject(projectId);
				var compilationWithAnalyzers = project.GetCompilationAsync().Result.WithAnalyzers(ImmutableArray.Create(analyzer));
				var diags = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
			}

			// Push properies.json results into newResults
			string json = File.ReadAllText(Path.Combine(matterControlRoot, "StaticData", "SliceSettings", "Properties.json"));
			foreach (var setting in JsonConvert.DeserializeObject<List<SettingItem>>(json))
			{
				// Guard for null reference errors when properties.json definitions lack HelpText
				if (setting.HelpText != null)
				{
					translationStrings.AddLocalization(setting.HelpText);
				}
				translationStrings.AddLocalization(setting.PresentationName);
				if (!string.IsNullOrWhiteSpace(setting.Units))
				{
					translationStrings.AddLocalization(setting.Units);
				}
			}

			// Push layouts.txt results into newResults
			foreach (var line in File.ReadAllLines(Path.Combine(matterControlRoot, "StaticData", "SliceSettings", "Layouts.txt")).Select(s => s.Trim()))
			{
				var isSliceSettingsKey = line.Contains("_") && !line.Contains(" ");
				if (!isSliceSettingsKey)
				{
					translationStrings.AddLocalization(line);
				}
			}

			string outputPath = Path.Combine(matterControlRoot, "StaticData", "Translations", "Master.txt");
			using (var outstream = new StreamWriter(outputPath))
			{
				foreach (var line in translationStrings.OrderBy(x => x).ToArray())
				{
					outstream.WriteLine($"English:{line}");
					outstream.WriteLine($"Translated:{line}");
					outstream.WriteLine("");
				}
			}

			System.Diagnostics.Process.Start(outputPath);

			//GenerateComparisonData();
		}

		private static string GetMatterControlDirectory()
		{
			var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

			do
			{
				currentDirectory = currentDirectory.Parent;
			} while (currentDirectory.Name != "MatterControl");

			return currentDirectory.FullName;
		}

		private static void GenerateComparisonData(string matterControlRoot)
		{
			string classicFilePath = Path.Combine(matterControlRoot, "StaticData", "Translations", "Master.txt_");
			// Parse classic master.txt, extract English: lines, sort, dump as common master.txt format
			var masterTextLines = File.ReadAllLines(classicFilePath);

			var classicResults = new HashSet<string>();

			foreach (var line in masterTextLines)
			{
				if (line.StartsWith("English:"))
				{
					classicResults.Add(line.Substring(line.IndexOf(":") + 1).Trim());
				}
			}

			using (var outstream = new StreamWriter(Path.Combine(Path.GetDirectoryName(classicFilePath), "re-mastered.txt")))
			{
				foreach (var line in classicResults.OrderBy(x => x).ToArray())
				{
					outstream.WriteLine($"English:{line}");
					outstream.WriteLine($"Translated:{line}");
					outstream.WriteLine("");
				}
			}
		}

		private class SettingItem
		{
			public string PresentationName { get; set; }
			public string HelpText { get; set; }
			public string Units { get; set; }
		}
	}

	public static class HashsetExtensions
	{
		public static void AddLocalization(this HashSet<string> hashset, string line)
		{
			hashset.Add(line.Replace("\r\n", "\\n").Replace("\n", "\\n"));
		}
	}
}
