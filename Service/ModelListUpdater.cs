using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using LmStudioServerAdmin.Config;
using LmStudioServerAdmin.Logging;
using System.Threading; // for Timer

namespace LmStudioServerAdmin.Service
{
    public class ModelListUpdater : IDisposable
    {
        private readonly Timer _timer;
        private readonly HttpClient _client = new();
        private readonly int _port;
        private List<ModelInfo>? _lastKnownList;

        public ModelListUpdater(int port, TimeSpan interval)
        {
            _port = port;
            // Start immediately and then every interval
            _timer = new Timer(_ => { var _t = TickAsync(); }, null, TimeSpan.Zero, interval);
        }

        private async Task TickAsync()
        {
            try
            {
                var url = $"http://localhost:{_port}/v1/models";
                using var response = await _client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warning($"Failed to fetch model list: {(int)response.StatusCode} {response.ReasonPhrase}");
                    return;
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);
                if (!doc.RootElement.TryGetProperty("data", out var dataArray))
                {
                    Logger.Warning("/v1/models response missing 'data' field");
                    return;
                }

                var newList = new List<ModelInfo>();
                foreach (var item in dataArray.EnumerateArray())
                {
                    if (!item.TryGetProperty("id", out var idProp) || !item.TryGetProperty("object", out var objectProp))
                        continue;
                    var modelInfo = new ModelInfo
                    {
                        Id = idProp.GetString() ?? string.Empty,
                        Object = objectProp.GetString() ?? string.Empty,
                        Owned_by = item.TryGetProperty("owned_by", out var owned) ? owned.GetString() ?? string.Empty : string.Empty
                    };
                    newList.Add(modelInfo);
                }

                // Compare with last known list
                if (_lastKnownList == null || !AreListsEqual(_lastKnownList, newList))
                {
                    Logger.Info($"Model list updated: {newList.Count} models detected.");
                    _lastKnownList = new List<ModelInfo>(newList);
                    var config = ConfigManager.Load();
                    config.LmStudioModelList = new List<ModelInfo>(_lastKnownList!);
                    ConfigManager.Save(config);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating model list: {ex.Message}", ex);
            }
        }

        private static bool AreListsEqual(List<ModelInfo> a, List<ModelInfo> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                var ai = a[i];
                var bi = b[i];
                if (ai.Id != bi.Id || ai.Object != bi.Object || ai.Owned_by != bi.Owned_by) return false;
            }
            return true;
        }

        public void Dispose()
        {
            _timer.Dispose();
            _client.Dispose();
        }
    }
}
