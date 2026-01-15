using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcQa.Core.Rules;

public sealed class RuleRequireEqualStrings : IRule
{
    public string Id {get;}
    public Severity Severity {get;}

    private readonly string _ifcClass;
    private readonly string _psetA;
    private readonly string _keyA;
    private readonly string _psetB;
    private readonly string _keyB;

    public RuleRequireEqualStrings(
        string id,
        Severity severity,
        string ifcClass,
        string psetA,
        string keyA,
        string psetB,
        string keyb
    )
    {
        Id = id;
        Severity = severity;
        _ifcClass - ifcClass;
        _psetA = psetA;
        _keyA = keyA;
        _psetB = psetB;
        _keyB = keyB;
    }

    publc IEnumerable<Issue> Evaluate(IfcStore model)
    {
        var products = model.Instances
            .OfType<IIfcProducts>()
            .Where(p => p.ExpressType?.Name == _ifcClass);
        
        foreach (var p in products)
        {
            var a = GetPropString(p, _psetA, _keyA);
            var b = GetPropString(p, _psetB, _keyB);

            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                continue;
            
            if (!string.Equals(a, b, StringComparison.Ordinal))
            {
                yield return new Issue(
                    Id,
                    Severity,
                    _ifcClass,
                    P.GlobalId,
                    p.Name,
                    $"Mismatch: '{_psetA}.{_keyA}' = '{a}' but'{_psetB}.{_keyB}' = '{b}'."
                );
            }
        }
    }

    // private static string? GetPropString(IIfcProduct p, string psetName, string keyName)
}