using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcQa.Core.Rules
{
    public sealed class RuleWallHasPsetWallCommon : IRule
    {
        public string Id => "W101";
        public Severity Severity => Severity.Error;

        public IEnumerable<Issue> Evaluate(IfcStore model)
        {
            var walls = model.Instances.OfType<IIfcProduct>()
                .Where(p =>
                p.ExpressType.Name.Equals("IfcWall", System.StringComparison.OrdinalIgnoreCase) ||
                p.ExpressType.Name.Equals("IfcStandardCase", System.StringComparison.OrdinalIgnoreCase));
        
            foreach (var wall in walls)
            {
                if (!IfcPropertyUtils.HasPset(wall, "Pset_WallCommon"))
                {
                    yield return new Issue(
                        Id,
                        Severity,
                        wall.ExpressType.Name,
                        wall.GlobalId.ToString() ?? "",
                        wall.Name?.ToString(),
                        "Wall is Missing reqruiredproperty set: Pset_WallCommon"
                        );
                }
            }
        }
    }
}
