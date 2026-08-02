using GameMacro.Core.Models;

namespace GameMacro.Core.Storage;

public interface IProfileStore
{
    Task<IReadOnlyList<MacroProfile>> LoadAllAsync(CancellationToken cancellationToken);
    Task SaveAsync(MacroProfile profile, CancellationToken cancellationToken);
    Task DeleteAsync(Guid profileId, CancellationToken cancellationToken);
}
