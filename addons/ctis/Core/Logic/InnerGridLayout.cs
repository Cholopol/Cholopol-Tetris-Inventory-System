namespace Ctis.Core;

public sealed class EmptyInnerGridLayout : IInnerGridLayout
{
    public static readonly EmptyInnerGridLayout Instance = new();

    /// <summary>No panel specs; inner insert cannot run without a layout.</summary>
    public IReadOnlyList<InnerGridSpec> SpecsFor(ItemDetails? details) => Array.Empty<InnerGridSpec>();
}

public sealed class FixedInnerGridLayout : IInnerGridLayout
{
    private readonly IReadOnlyList<InnerGridSpec> _specs;

    public FixedInnerGridLayout(params InnerGridSpec[] specs)
        => _specs = specs ?? Array.Empty<InnerGridSpec>();

    public FixedInnerGridLayout(IReadOnlyList<InnerGridSpec> specs)
        => _specs = specs ?? Array.Empty<InnerGridSpec>();

    /// <summary>Returns the same specs for every item that has an inner-grid panel.</summary>
    public IReadOnlyList<InnerGridSpec> SpecsFor(ItemDetails? details)
        => details?.HasInnerGrid == true ? _specs : Array.Empty<InnerGridSpec>();
}
