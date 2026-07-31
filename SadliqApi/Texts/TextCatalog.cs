using Business.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace SadliqApi.Texts
{
    // Mətnlər bazadan bir dəfə oxunub yaddaşda saxlanılır. Cədvəl kiçikdir (bir neçə yüz
    // sətir), ona görə hamısını birdən götürmək ən sadə və ən sürətli yoldur.
    // Admin mətni dəyişəndə Invalidate() çağırılır və növbəti sorğuda yenidən oxunur.
    // Yaddaşda saxlanılan bir sətir: mətn və (lazımdırsa) əlavə dəyər
    public record TextEntry(string Value, string? Extra);

    public class TextCatalog : ITextCatalog
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TextCatalog> _logger;
        private readonly object _lock = new();

        private ConcurrentDictionary<string, Dictionary<string, TextEntry>>? _cache;

        public TextCatalog(IServiceScopeFactory scopeFactory, ILogger<TextCatalog> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public string Get(string category, string key)
        {
            var map = Load();
            if (map.TryGetValue(category, out var items) && items.TryGetValue(key, out var item))
                return item.Value;

            // Mətn tapılmasa açarın özü göstərilir — səhv görünsün, sistem dayanmasın
            return key;
        }

        public IReadOnlyDictionary<string, string> Map(string category)
        {
            var map = Load();
            if (!map.TryGetValue(category, out var items))
                return new Dictionary<string, string>();

            return items.ToDictionary(x => x.Key, x => x.Value.Value);
        }

        public bool TryGetPair(string category, string key, out string value, out string? extra)
        {
            var map = Load();

            if (map.TryGetValue(category, out var items) && items.TryGetValue(key, out var item))
            {
                value = item.Value;
                extra = item.Extra;
                return true;
            }

            value = key;
            extra = null;
            return false;
        }

        public void Invalidate()
        {
            lock (_lock) _cache = null;
        }

        private ConcurrentDictionary<string, Dictionary<string, TextEntry>> Load()
        {
            var cached = _cache;
            if (cached is not null) return cached;

            lock (_lock)
            {
                if (_cache is not null) return _cache;

                var result = new ConcurrentDictionary<string, Dictionary<string, TextEntry>>();

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<SadliqDbContext>();

                    var rows = context.AppTexts.AsNoTracking().ToList();

                    foreach (var group in rows.GroupBy(t => t.Category))
                    {
                        result[group.Key] = group
                            .GroupBy(t => t.Key)
                            .ToDictionary(g => g.Key, g => new TextEntry(g.First().Value, g.First().Extra));
                    }
                }
                catch (Exception ex)
                {
                    // Baza əlçatmazdırsa mətnlər açar kimi görünəcək, sayt işləməyə davam edəcək
                    _logger.LogError(ex, "Mətn kataloqu oxuna bilmədi.");
                }

                _cache = result;
                return result;
            }
        }
    }
}
