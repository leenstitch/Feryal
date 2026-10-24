using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace Feryal.API.Controllers;

[ApiController]
[Route("api/cache-test")]
//[Authorize(Roles = "Admin")]
public class CacheTestController : ControllerBase
{
    private readonly IDistributedCache _cache;

    // Initializes the controller with the distributed cache service.
    public CacheTestController(IDistributedCache cache)
    {
        _cache = cache;
    }

    // Tests Redis caching by storing a value, retrieving it, and verifying that the retrieved value matches the original.
    [HttpGet]
    public async Task<IActionResult> Test()
    {
        var key = "feryal:redis-test:persistent";
        var value = $"Feryal Redis test {DateTimeOffset.UtcNow:O}";

        await _cache.SetStringAsync(
            key,
            value,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromMinutes(2)
            });

        var result = await _cache.GetStringAsync(key);

        //  await _cache.RemoveAsync(key);

        if (result != value)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Redis read/write test failed."
            });
        }

        return Ok(new
        {
            success = true,
            message = "Cache read/write succeeded!",
            value = result
        });
    }
}