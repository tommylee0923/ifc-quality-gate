using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc4.Interfaces;

namespace IfcQa.Core
{
    public static class IfcPropertyUtils
    {
        public static IEnumerable<IIfcPropertySet> GetPropertySets(IIfcProduct p) =>
            p.IsDefinedBy
            .OfType<IIfcRelDefinesByProperties>()
            .Select(r => r.RelatingPropertyDefinition)
            .OfType<IIfcPropertySet>();

        public static IEnumerable<IIfcElementQuantity> GetQuantitySets(IIfcProduct p) =>
            p.IsDefinedBy
            .OfType<IIfcRelDefinesByProperties>()
            .Select(r => r.RelatingPropertyDefinition)
            .OfType<IIfcElementQuantity>();

        public static bool HasPset(IIfcProduct p, string psetName) =>
            GetPropertySets(p).Any(ps => ps.Name?.ToString() == psetName);

        public static bool HasQto(IIfcProduct p, string qtoName) =>
            GetPropertySets(p).Any(q => q.Name?.ToString() == qtoName);
    }
}
