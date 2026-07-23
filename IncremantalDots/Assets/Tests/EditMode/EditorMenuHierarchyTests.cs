using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DeadWalls.Tests
{
    public sealed class EditorMenuHierarchyTests
    {
        [Test]
        public void AllEditorMenuItems_UseSingleDeadWallsMenuRoot()
        {
            string editorRoot = Path.Combine(Application.dataPath, "Scripts", "Editor");
            string[] sources = Directory.GetFiles(editorRoot, "*.cs", SearchOption.AllDirectories);
            string[] menuItemLines = sources
                .SelectMany(File.ReadAllLines)
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("[MenuItem(", System.StringComparison.Ordinal))
                .ToArray();

            Assert.That(menuItemLines, Has.Length.EqualTo(46),
                "Editor araci kayboldu veya yeni arac menu sozlesmesine eklenmeden olusturuldu.");
            Assert.That(menuItemLines,
                Has.All.Contains("DeadWallsEditorMenuPaths."),
                "Butun Dead Walls MenuItem tanimlari merkezi menu sabitlerini kullanmalidir.");

            string combinedSources = string.Join("\n", sources.Select(File.ReadAllText));
            Assert.That(combinedSources, Does.Not.Contain("Window/DeadWalls/"));
            Assert.That(combinedSources, Does.Not.Contain("Tools/DeadWalls/"));
            Assert.That(combinedSources, Does.Not.Contain("Tools/Analyze Profiler Data"));

            string menuPathsSource = File.ReadAllText(Path.Combine(
                editorRoot,
                "DeadWallsEditorMenuPaths.cs"));
            Assert.That(menuPathsSource, Does.Contain("Tools/Dead Walls/"));
            Assert.That(menuPathsSource, Does.Contain("Audio/"));
            Assert.That(menuPathsSource, Does.Contain("Balancing/"));
            Assert.That(menuPathsSource, Does.Contain("Content/"));
            Assert.That(menuPathsSource, Does.Contain("Maps/"));
            Assert.That(menuPathsSource, Does.Contain("Profiling/"));
            Assert.That(menuPathsSource, Does.Contain("Setup & Repair/"));
        }
    }
}
