using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using LmStudioServerAdmin.Logging;
using LmStudioServerAdmin.Config;

namespace LmStudioServerAdmin.Server
{
    // DTO for response with parameters
    public class ModelEntryDto
    {
        public string Id { get; set; } = "";
        public string Object { get; set; } = "";
        public string Owned_by { get; set; } = "";
        public Dictionary<string,int?> Parameters { get; set; } = new();
    }


    public class ModelsController
    {
public static void GetModels(HttpListenerContext ctx, AppConfig cfg)
        {
            var response = ctx.Response;
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "application/json";

            var baseModels = cfg.LmStudioModelList ?? new List<ModelInfo>();
            var models = new List<ModelEntryDto>();
            if (baseModels.Any())
            {
                foreach(var m in baseModels)
                {
                    var paramEntry = (cfg.LmStudioModelLoadParameterList?.FirstOrDefault(e=>string.Equals(e.Model,m.Id,StringComparison.OrdinalIgnoreCase)))?.Parameters;
                    models.Add(new ModelEntryDto { Id = m.Id, Object = m.Object, Owned_by = m.Owned_by, Parameters = paramEntry ?? new Dictionary<string,int?>() });
                }
            }
            else
            {
                models.Add(new ModelEntryDto { Id="placeholder", Object="model", Owned_by="unknown", Parameters=new Dictionary<string,int?>() });
            }

            if (cfg.VerboseLogging)
            {
                Logger.Info($"[GET] /api/models - current count: {models.Count}");
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
