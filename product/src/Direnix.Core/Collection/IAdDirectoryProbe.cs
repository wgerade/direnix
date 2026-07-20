namespace Direnix.Core.Collection;

public interface IAdDirectoryProbe
{
    Task<DirectoryProbeResult> ProbeRootDseAsync(DirectoryTarget target, CancellationToken cancellationToken);
}
