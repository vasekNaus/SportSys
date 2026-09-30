using Apollo.HttpService.Interface;
using Apollo.HttpService.Model.Response;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportSys.Contract.Config;
using SportSys.Contract.Models;
using System.Net;
using System.Text.Json;

namespace SportSys.Contract.Services;

public class HttpService : Apollo.HttpService.HttpJsonService
{
  public static string SportLocationLabel { get; } = "Zimní stadion";

  private readonly IMapyCom config;

  public HttpService(ILoggerFactory loggerFactory, IOptions<MapyCom> config, JsonSerializerOptions? jsonOptions = null)
    : base(loggerFactory, config.Value, jsonOptions)
  {
    this.config = config.Value;
  }

  protected override HttpClient CreateHttpClient(string baseUrl)
  {
    throw new NotImplementedException();
  }

  protected override HttpClient CreateHttpClient(IClientConfig clientConfig)
  {
    IMapyCom mapyCfg = (IMapyCom)clientConfig;
    return new HttpClient()
    {
      BaseAddress = new Uri(mapyCfg.BaseUrl)
    };
  }

  protected override ApiResponse ParseErrorResponse(HttpStatusCode httpStatusCode, string response)
  {
    throw new NotImplementedException();
  }

  public async Task<SportSys.Model.SportLocation?> Search(string query)
  {
    var result = await SearchInternal(query);
    if (result is not null)
      return result;

    // Fallback: pokud query obsahuje čárku, město je před ní – zkus "{Město}, Zimní stadion"
    var commaIdx = query.IndexOf(',');
    if (commaIdx > 0)
    {
      var city = query[..commaIdx].Trim();
      result = await SearchInternal($"{city}, {SportLocationLabel}");
    }

    return result;
  }

  private async Task<SportSys.Model.SportLocation?> SearchInternal(string query)
  {
    var qs = new Dictionary<string, string>()
    {
      { "query", query },
      { "lang", "cs" },
      { "limit", "10" },
      { "apikey", config.ApiKey }
    };
    var response = await GetAsync<Root>(config.GeocodeRoute, qs);
    if (!response.IsSuccess || !response.IsValid || response.Response is null)
      throw new InvalidOperationException($"Search request failed: {response}");

    var item = response.Response.Items.FirstOrDefault(x => x.Label == SportLocationLabel) ?? response.Response.Items.FirstOrDefault();

    if (item is null)
      return null;

    return new SportSys.Model.SportLocation(
      item.Name,
      item.RegionalStructure.FirstOrDefault(r => r.Type == "regional.street")?.Name ?? "",
      item.Zip ?? "",
      item.RegionalStructure.FirstOrDefault(r => r.Type == "regional.municipality")?.Name ?? "",
      item.Position.Lat,
      item.Position.Lon
    );
  }
}
