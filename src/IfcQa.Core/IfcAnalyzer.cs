using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xbim.Ifc;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using IfcQa.Core.Rules;

namespace IfcQa.Core;

public sealed class IfcAnalyzer
{
    public IfcSummaryReport Analyze(string ifcPath)
    {
        if (string.IsNullOrWhiteSpace(ifcPath))
        {
            throw new ArgumentNullException("IFC path is empty", nameof(ifcPath));
        }
        if (!File.Exists(ifcPath))
        {
            throw new FileNotFoundException("IFC file not found.", ifcPath);
        }

        using var model = IfcStore.Open(ifcPath);

        var products = model.Instances.OfType<IIfcProduct>()
            .Where(p => p != null && p.ExpressType != null)
            .ToList();

        var byClass = products
            .GroupBy(p => p.ExpressType.Name)
            .Select(g => new IfcClassStats
            {
                IfcClass = g.Key,
                Count = g.Count(),
                WithAnyPsetCount = g.Count(HasAnyPropertySet),
                WithAnyQtoCount = g.Count(HasAnyQuantitySet)
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var allPsetNames = products
            .SelectMany(GetPropertySets)
            .Select(ps => ps.Name?.ToString())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .GroupBy(n => n!)
            .OrderByDescending(g => g.Count())
            .Select(g => new NameCount(g.Key, g.Count()))
            .ToList();

        var allQtoNames = products
            .SelectMany(GetQuantitySets)
            .Select(q => q.Name?.ToString())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .GroupBy(n => n!)
            .OrderByDescending(g => g.Count())
            .Select(g => new NameCount(g.Key, g.Count()))
            .ToList();

        var walls = products.Where(p => p.ExpressType.Name.Equals("IfcWall", StringComparison.OrdinalIgnoreCase)
                                     || p.ExpressType.Name.Equals("IfcWallStandardCase", StringComparison.OrdinalIgnoreCase))
                            .ToList();

        int wallsWithPsetWallCommon = walls.Count(w => GetPropertySets(w).Any(ps => ps.Name?.ToString() == "Pset_WallCommon"));
        int wallsWithQtoWallQuantities = walls.Count(w => GetQuantitySets(w).Any(q => q.Name?.ToString() == "Qto_WallQuantities"));

        return new IfcSummaryReport
        {
            IfcPath = ifcPath,
            ProductCount = products.Count,
            ByClass = byClass,
            TopPsets = allPsetNames.Take(30).ToList(),
            TopQtos = allQtoNames.Take(30).ToList(),
            WallQuickStats = new WallQuickStats
            {
                WallCount = walls.Count,
                WithPset_WallCommon = wallsWithPsetWallCommon,
                WithQto_WallQuantities = wallsWithQtoWallQuantities
            }
        };
    }

    public IfcQaRunResult AnalyzeWithRules(string ifcPath)
    {
        using var model = IfcStore.Open(ifcPath);

        IRule[] rules =
            [
                new RuleMissingName(),
                new RuleDuplicateGlobalId(),
                new RuleWallHasPsetWallCommon(),
                new RuleHasQtoWallBaseQuantities()
            ];

        var issues = rules.SelectMany(r => r.Evaluate(model)).ToList();
        return new IfcQaRunResult(ifcPath, issues);
    }
    private static bool HasAnyPropertySet(IIfcProduct p) => GetPropertySets(p).Any();
    private static bool HasAnyQuantitySet(IIfcProduct p) => GetQuantitySets(p).Any();
    private static IEnumerable<IIfcPropertySet> GetPropertySets(IIfcProduct p)
    {
        // IsDefinedBy includes both type + property relations;
        // filter to RelDefinesByProperties => PropertySet
        return p.IsDefinedBy
            .OfType<IIfcRelDefinesByProperties>()
            .Select(r => r.RelatingPropertyDefinition)
            .OfType<IIfcPropertySet>();
    }

    private static IEnumerable<IIfcElementQuantity> GetQuantitySets(IIfcProduct p)
    {
        return p.IsDefinedBy
            .OfType<IIfcRelDefinesByProperties>()
            .Select(r => r.RelatingPropertyDefinition)
            .OfType<IIfcElementQuantity>();
    }

    public sealed class IfcSummaryReport
    {
        public required string IfcPath { get; init; }
        public int ProductCount { get; init; }
        public List<IfcClassStats> ByClass { get; init; } = new();
        public List<NameCount> TopPsets { get; init; } = new();
        public List<NameCount> TopQtos { get; init; } = new();
        public WallQuickStats WallQuickStats { get; init; } = new();
    }

    public sealed class IfcClassStats
    {
        public required string IfcClass { get; init; }
        public int Count { get; init; }
        public int WithAnyPsetCount { get; init; }
        public int WithAnyQtoCount { get; init; }
        public double WithAnyPsetPct => Count == 0 ? 0 : (double)WithAnyPsetCount / Count;
        public double WithAnyQtoPct => Count == 0 ? 0 : (double)WithAnyQtoCount / Count;
    }

    public sealed class WallQuickStats
    {
        public int WallCount { get; init; }
        public int WithPset_WallCommon { get; init; }
        public int WithQto_WallQuantities { get; init; }
    }

    public sealed record NameCount(string Name, int Count);
}