using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcQa.Core.Rules
{
    public sealed class RuleHasQtoWallBaseQuantities : IRule
    {
        public string Id => "W002";
        public Severity Severity => Severity.Warning;

        public IEnumerable<Issue> Evaluate(IfcStore model)
        {
            var walls = model.Instances.OfType<IIfcProduct>()
                .Where(p =>
                p.ExpressType.Name.Equals("IfcWall", System.StringComparison.OrdinalIgnoreCase) ||
                p.ExpressType.Name.Equals("IfcWallStandCase", System.StringComparison.OrdinalIgnoreCase));

            foreach (var wall in walls)
            {
                if (!IfcPropertyUtils.HasQto(wall, "Qto-WallBaseQuantities"))
                {
                    yield return new Issue(
                        Id,
                        Severity,
                        wall.ExpressType.Name,
                        wall.GlobalId.ToString() ?? "",
                        wall.Name?.ToString(),
                        "Wall is missing quantity set (recommended): Qto_WallBaseQuantities"
                        );
                }
            }
        }
    }
}
