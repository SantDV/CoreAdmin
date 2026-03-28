using CoreAdmin.Models;

namespace CoreAdmin.Services;

public class DashboardCacheService
{
    private DashboardStats? _cachedStats;
    private bool _isValid;

    public DashboardStats? GetCachedStats()
    {
        return _isValid ? _cachedStats : null;
    }

    public void SetCachedStats(DashboardStats stats)
    {
        _cachedStats = stats;
        _isValid = true;
    }

    public void Invalidate()
    {
        _isValid = false;
        _cachedStats = null;
    }
}
