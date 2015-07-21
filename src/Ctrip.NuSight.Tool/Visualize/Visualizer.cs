using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NuGet;
using NuGet.VisualStudio;
using EnvDTE;

namespace Ctrip.NuSight.Tool
{
    public class Visualizer
    {
        private const string dgmlNS = "http://schemas.microsoft.com/vs/2009/dgml";
        private readonly IVsPackageInstallerServices _packageInstaller;
        private readonly Solution _solution;

        public Visualizer(IVsPackageInstallerServices packageInstaller, Solution solution)
        {
            _packageInstaller = packageInstaller;
            _solution = solution;
        }

        public string CreateGraph()
        {
            // We only use the package manager to locate the LocalRepository, we should be fine disabling fallback.

            var nodes = new List<DGMLNode>();
            var links = new List<DGMLLink>();
            VisitProjects(nodes, links);

            return GenerateDGML(nodes, links);
        }

        private void VisitProjects(List<DGMLNode> nodes, List<DGMLLink> links)
        {
            foreach (Project project in _solution.Projects)
            {
                DGMLNode projNode = null;
                var packages = _packageInstaller.GetInstalledPackages(project);
                foreach (var pck in packages)
                {
                    // Project has packages. Add a node for it
                    if (projNode == null)
                    {
                        projNode = new DGMLNode
                        {
                            Name = project.GetCustomUniqueName(),
                            Label = project.GetDisplayName(),
                            Category = Resources.Visualizer_Project
                        };
                        nodes.Add(projNode);
                    }

                    var pckNode = new DGMLNode { Name = pck.Id, Label = pck.Id, Category = Resources.Visualizer_Package };
                    nodes.Add(pckNode);
                    links.Add(new DGMLLink { SourceName = projNode.Name, DestName = pckNode.Name, Category = Resources.Visualizer_InstalledPackage });
                }
            }
        }

        private string GenerateDGML(List<DGMLNode> nodes, List<DGMLLink> links)
        {
            bool hasDependencies = links.Any(l => l.Category == Resources.Visualizer_PackageDependency);
            var document = new XDocument(
                new XElement(XName.Get("DirectedGraph", dgmlNS),
                    new XAttribute("GraphDirection", "LeftToRight"),
                    new XElement(XName.Get("Nodes", dgmlNS),
                        from item in nodes select new XElement(XName.Get("Node", dgmlNS), new XAttribute("Id", item.Name), new XAttribute("Label", item.Label), new XAttribute("Category", item.Category))),
                    new XElement(XName.Get("Links", dgmlNS),
                        from item in links
                        select new XElement(XName.Get("Link", dgmlNS), new XAttribute("Source", item.SourceName), new XAttribute("Target", item.DestName),
                            new XAttribute("Category", item.Category))),
                    new XElement(XName.Get("Categories", dgmlNS),
                        new XElement(XName.Get("Category", dgmlNS), new XAttribute("Id", Resources.Visualizer_Project)),
                        new XElement(XName.Get("Category", dgmlNS), new XAttribute("Id", Resources.Visualizer_Package))),
                    new XElement(XName.Get("Styles", dgmlNS),
                        StyleElement(Resources.Visualizer_Project, "Node", "Background", "Blue"),
                        hasDependencies ? StyleElement(Resources.Visualizer_PackageDependency, "Link", "Background", "Yellow") : null))
            );
            var saveFilePath = Path.Combine(Path.GetDirectoryName(_solution.FullName), "Packages.dgml");
            document.Save(saveFilePath);
            return saveFilePath;
        }

        private static XElement StyleElement(string category, string targetType, string propertyName, string propertyValue)
        {
            return new XElement(XName.Get("Style", dgmlNS), new XAttribute("TargetType", targetType), new XAttribute("GroupLabel", category), new XAttribute("ValueLabel", "True"),
                    new XElement(XName.Get("Condition", dgmlNS), new XAttribute("Expression", String.Format(CultureInfo.InvariantCulture, "HasCategory('{0}')", category))),
                    new XElement(XName.Get("Setter", dgmlNS), new XAttribute("Property", propertyName), new XAttribute("Value", propertyValue)));
        }

        private class DGMLNode : IEquatable<DGMLNode>
        {
            public string Name { get; set; }

            public string Label { get; set; }

            public string Category { get; set; }

            public bool Equals(DGMLNode other)
            {
                return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
            }
        }

        private class DGMLLink
        {
            public string SourceName { get; set; }

            public string DestName { get; set; }

            public string Category { get; set; }
        }
    }
}