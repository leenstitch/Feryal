
namespace Feryal.Application.Events.ProductGroups;

public sealed record ProductGroupCacheInvalidationRequested(
    int? ProductGroupId);