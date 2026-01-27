using System.Diagnostics;

namespace Spx.DeltaWorker.Observability;

public static class DeltaActivity
{
    public static readonly ActivitySource Source = new("Spx.DeltaWorker");

    public static Activity? StartTick()
        => Source.StartActivity("delta.tick", ActivityKind.Internal);
}
