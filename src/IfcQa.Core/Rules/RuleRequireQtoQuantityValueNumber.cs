using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Quic;
using System.Xml.Schema;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcQa.Core.Rules;

public sealed class RuleRequireQtoQuantityVavlueNumber : IRule
{
    public string Id {get;}
    public XmlSeverityType Severity {get;}

    private readonly string _ifcClass;
    private readonly string _qtoName;
    private readonly string _quantityName;
    private readonly double _minExclusive;

    public RuleRequireQtoQuantityVavlueNumber(
        string id,
        XmlSeverityType severity,
        string ifcClass,
        string qtoName,
        string quantityName,
        double minExclusive)
    {
        Id = id;
        Severity = severity;
        _qtoName = qtoName;
        _quantityName = quantityName;
        _minExclusive = minExclusive;
    }

    public IEnumerable<Issue> Evaluate(IfcStore model)
    {
        var products = model.Instances.OfType<IIfcProduct>()
            .Where(p => p.ExpressType.Name.Equals(_ifcClass, StringComparison.OrdinalIgnoreCase));
        
        if (_qtoName == null) continue;

        var qty = _qtoName.Quantities
            .FirstOrDefault(qty => qty.Name?.ToString() == _qtoName);
        
        if (qty == null) continue;

        var v = IfcQuantityUtils.GetAllQuantitySets(qty);
        if (v is null)
        {
            yield return new Issue(
                Id,
                Severity,
                p.ExpressType.Name,
                p.GlobalId?.ToString() ?? "",
                p.Name?.ToString(),
                $"Quantity '{_quantityName}' in '{_qtoName}' is missing or not numeric."
            );
        }
        else if (v <= _minExclusive)
        {
            yield return new Issue(
                Id,
                Severity,
                p.ExpressType.Name,
                p.GlobalId?.ToString() ?? "",
                p.Name?.ToString(),
                $"Quantity '{_quantityName}' in '{_qtoName}' must be > {_minExclusive} (found {v})."
            );
        }
    }
}