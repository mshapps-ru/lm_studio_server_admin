using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using LmStudioServerAdmin.Logging;
using LmStudioServerAdmin.Config;

namespace LmStudioServerAdmin.Server
{
    public class ModelsController
    {
public static void GetModels(HttpListenerContext ctx, AppConfig cfg)
        {

            var response = ctx.Response;
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "application/json";
            var models = cfg.LmStudioModelList ?? new List<ModelInfo>();

            // Ensure placeholder when empty
            if (cfg.VerboseLogging)
            {
                Logger.Info($"[GET] /api/models - current count: {models.Count}");
            }

            if (models == null || models.Count == 0)
                {
                    // Fallback placeholder
                    models = new List<ModelInfo>
                    {
                        new ModelInfo { Id = "placeholder", Object = "model", Owned_by = "unknown" }
                    };
                }
            var json = JsonSerializer.Serialize(new { models });
            using var writer = new StreamWriter(response.OutputStream, System.Text.Encoding.UTF8);
            writer.Write(json);
        }

        public static void PostDefaults(HttpListenerContext ctx, AppConfig cfg)
        {
            try
            {
                using var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding ?? System.Text.Encoding.UTF8);
                var body = reader.ReadToEnd();
                var defaults = JsonSerializer.Deserialize<Dictionary<string,int?>>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (defaults != null)
                {
                    cfg.LmStudioModelDefaultLoadParameter = defaults;
                    ConfigManager.Save(cfg);
                    SendJson(ctx, HttpStatusCode.OK, "{\"success\":true}");
                }
                else
                {
                    SendJson(ctx, HttpStatusCode.BadRequest, "{\"error\":\"Invalid payload\"}");
                }
            }
            catch (Exception)
            {
                SendJson(ctx, HttpStatusCode.InternalServerError, "{\"error\":\"Processing failed\"}");
            }
        }

        public static void PutOverride(HttpListenerContext ctx, string modelName, AppConfig cfg)
        {
            try
            {
                using var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding ?? System.Text.Encoding.UTF8);
                var body = reader.ReadToEnd();
                var overrides = JsonSerializer.Deserialize<Dictionary<string,int?>>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (overrides != null)
                {
                    // Find or create entry
                    var list = cfg.LmStudioModelLoadParameterList ?? new List<ModelLoadParametersEntry>();
                    var entry = list.Find(e => e.Model == modelName);
                    if (entry == null)
                    {
                        entry = new ModelLoadParametersEntry { Model = modelName, Parameters = overrides };
                        list.Add(entry);
                    }
                    else
                    {
                        entry.Parameters = overrides;
                    }
                    cfg.LmStudioModelLoadParameterList = list;
                    ConfigManager.Save(cfg);
                    SendJson(ctx, HttpStatusCode.OK, "{\"success\":true}");
                }
                else
                {
                    SendJson(ctx, HttpStatusCode.BadRequest, "{\"error\":\"Invalid payload\"}");
                }
            }
            catch (Exception)
            {
                SendJson(ctx, HttpStatusCode.InternalServerError, "{\"error\":\"Processing failed\"}");
            }
        }

        private static void SendJson(HttpListenerContext ctx, HttpStatusCode status, string json)
        {
            var response = ctx.Response;
            response.StatusCode = (int)status;
            response.ContentType = "application/json";
            using var writer = new StreamWriter(response.OutputStream, System.Text.Encoding.UTF8);
            writer.Write(json);
        }
    }
}
