using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace OmmoBackend.Services.Implementations
{
    public class LoadBoardService : ILoadBoardService
    {
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;

        private readonly ILogger<LoadBoardService> _logger;
        public LoadBoardService(
            IIntegrationRepository integrationRepository,
            IHttpClientFactory httpClientFactory,
            ILogger<LoadBoardService> logger)
        {
            _integrationRepository = integrationRepository;
            _httpClientFactory = httpClientFactory;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _logger = logger;
        }

        public async Task<ServiceResponse<List<NormalizedLoadDto>>> GetLoadsAsync(int companyId, LoadFiltersDto filters)
        {
            try
            {
                var loadboards = await _integrationRepository.GetActiveIntegrationsAsync(companyId);
                if (loadboards == null || loadboards.Count == 0)
                    return new ServiceResponse<List<NormalizedLoadDto>> { Success = false, ErrorMessage = "No active loadboard integration available" };

                var results = new List<NormalizedLoadDto>();

                // For each active integration, call provider-specific fetch
                foreach (var integ in loadboards)
                {
                    if (integ.integration_id == 1) // Truckstop
                    {
                        var truckRes = await FetchFromTruckstopAsync(integ, filters);

                        //var truckRes = await FetchFromTruckstopAsync(companyId, filters);

                        if (truckRes?.Success == true && truckRes.Data != null)
                            results.AddRange(truckRes.Data);
                    }
                    else if (integ.integration_id == 2) // DAT
                    {
                        var datRes = await FetchFromDATAsync(integ, filters);

                        if (datRes?.Success == true && datRes.Data != null)
                            results.AddRange(datRes.Data);
                    }
                    else
                    {
                        // unknown provider - skip
                    }
                }

                return new ServiceResponse<List<NormalizedLoadDto>> { Success = true, Data = results, Message = "Fetched loads" };

            }
            catch (Exception ex)
            {
                return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        // ---------------- Truckstop Integration (SOAP) ----------------
        public async Task<ServiceResponse<List<NormalizedLoadDto>>> FetchFromTruckstopAsync(Integrations companyIntegration, LoadFiltersDto filters)
        {
            try
            {
                if (companyIntegration == null)
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("No Truckstop integration provided.");

                // credentials from integrations.credentials JSON
                string username = null, password = null;
                if (companyIntegration.credentials != null)
                {
                    companyIntegration.credentials.RootElement.TryGetProperty("username", out var u);
                    companyIntegration.credentials.RootElement.TryGetProperty("password", out var p);
                    username = u.GetString();
                    password = p.GetString();
                }

                // global integration id from global_integration_credentials
                var integrationId = await _integrationRepository.GetGlobalCredentialAsync(companyIntegration.default_integration_id, "Truckstop_IntegrationID");

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(integrationId))
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("Truckstop credentials or integration id missing.");

                // Build SOAP body using credentials and some filters (example - extend as needed)
                var pickupDate = (filters.FromDate ?? DateTime.UtcNow.AddDays(6)).ToString("yyyy-MM-ddT00:00:00");

                var equipmentType = string.IsNullOrWhiteSpace(filters.EquipmentType) ? "V,F,R" : filters.EquipmentType; // adapt to Truckstop format

                var loadType = string.IsNullOrWhiteSpace(filters.LoadType) ? "All" :
                    filters.LoadType.Equals("BOTH", StringComparison.OrdinalIgnoreCase) ? "All" :
                    filters.LoadType;

                var hoursOld = filters.MaxAgeMinutes > 0 ? filters.MaxAgeMinutes / 60 : 24;

                // If user selects specific states, split and use them
                var destinationStates = !string.IsNullOrWhiteSpace(filters.Destination)
                    ? filters.Destination.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    // fallback: search all states if no filter
                    : new[] { "TX", "CA", "NY", "FL", "GA", "IL", "OH", "PA", "NC", "MI", "AZ", "WA", "CO", "NV", "NJ", "VA", "WI", "MO", "TN", "IN", "MN", "MA", "MD", "AL", "SC", "KY", "OR", "OK", "CT", "IA", "UT", "AR", "KS", "MS", "NM", "NE", "ID", "HI", "ME", "NH", "MT", "RI", "WV", "SD", "ND", "DE", "VT", "WY", "LA", "AK" };

                var destinationStatesXml = string.Join(Environment.NewLine,
                    destinationStates.Select(state => $"<web1:DestinationState>{SecurityElement.Escape(state)}</web1:DestinationState>"));



                var (originCity, originState, originCountry) = ParseLocation(filters.Origin);
                var (destCity, destState, destCountry) = ParseLocation(filters.Destination);

                originCountry ??= "usa";
                destCountry ??= "usa";

                var soapBody = $@"
                        <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                                          xmlns:v12=""http://webservices.truckstop.com/v12""
                                          xmlns:web=""http://schemas.datacontract.org/2004/07/WebServices""
                                          xmlns:web1=""http://schemas.datacontract.org/2004/07/WebServices.Searching""
                                          xmlns:arr=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
                           <soapenv:Header/>
                           <soapenv:Body>
                              <v12:GetMultipleLoadDetailResults>
                                 <v12:searchRequest>
                                    <web:IntegrationId>{integrationId}</web:IntegrationId>
                                    <web:Password>{SecurityElement.Escape(password)}</web:Password>
                                    <web:UserName>{SecurityElement.Escape(username)}</web:UserName>
                                    <web1:Criteria>

<web1:DestinationCountry>{SecurityElement.Escape(destCountry)}</web1:DestinationCountry>


                                       <web1:DestinationRange>{filters.MaxDestinationDeadheadMiles}</web1:DestinationRange>
                                       {destinationStatesXml}
                                       <web1:EquipmentType>{SecurityElement.Escape(equipmentType)}</web1:EquipmentType>
                                       <web1:LoadType>{SecurityElement.Escape(loadType)}</web1:LoadType>
                                       <web1:HoursOld>{hoursOld}</web1:HoursOld>

<web1:OriginCountry>{SecurityElement.Escape(originCountry)}</web1:OriginCountry>


                                       <web1:OriginLatitude>0</web1:OriginLatitude>
                                       <web1:OriginLongitude>0</web1:OriginLongitude>
                                       <web1:OriginRange>{filters.MaxOriginDeadheadMiles}</web1:OriginRange>
                                       <web1:PickupDates>
                                          <arr:dateTime>{pickupDate}</arr:dateTime>
                                       </web1:PickupDates>
                                       <web1:PageNumber>1</web1:PageNumber>
                                       <web1:PageSize>100</web1:PageSize>
                                       <web1:SortBy>Age</web1:SortBy>
                                       <web1:SortDescending>true</web1:SortDescending>
                                    </web1:Criteria>
                                 </v12:searchRequest>
                              </v12:GetMultipleLoadDetailResults>
                           </soapenv:Body>
                        </soapenv:Envelope>";

                var client = _httpClientFactory.CreateClient("truckstop");
                var request = new HttpRequestMessage(HttpMethod.Post, "http://testws.truckstop.com:8080/V13/Searching/LoadSearch.svc")
                {
                    Content = new StringContent(soapBody, Encoding.UTF8, "text/xml")
                };
                request.Headers.Add("SOAPAction", "http://webservices.truckstop.com/v12/ILoadSearch/GetMultipleLoadDetailResults");

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse($"Truckstop API call failed: {response.StatusCode}");

                var xmlResponse = await response.Content.ReadAsStringAsync();
                var loads = ParseTruckstopResponse(xmlResponse); // your parser

                return ServiceResponse<List<NormalizedLoadDto>>.SuccessResponse(loads);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FetchFromTruckstopAsync failed");
                return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse($"Truckstop fetch failed: {ex.Message}");
            }
        }

        private (string city, string state, string country) ParseLocation(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return (null, null, null);

            // Normalize and split by comma
            var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 1)
            {
                // Could be state, city, or country
                var val = parts[0];
                if (val.Length == 2) // assume state code like AZ, TX
                    return (null, val.ToUpperInvariant(), "usa");
                else if (val.Length == 3 && val.ToUpperInvariant() == "USA") // whole country
                    return (null, null, "usa");
                else
                    return (val, null, "usa"); // assume city
            }
            else if (parts.Length == 2)
            {
                // "Phoenix, AZ"
                return (parts[0], parts[1].ToUpperInvariant(), "usa");
            }

            return (null, null, null);
        }

        private List<NormalizedLoadDto> ParseTruckstopResponse(string soapResponse)
        {
            var list = new List<NormalizedLoadDto>();

            try
            {
                var doc = XDocument.Parse(soapResponse);

                // Example: find all elements named "LoadDetailResult" or "Load" etc.
                var loadNodes = doc.Descendants()
                    .Where(x => x.Name.LocalName.Contains("LoadDetailResult") ||
                                x.Name.LocalName.Contains("MultipleLoadDetailResult"))
                    .Take(50);  // safety cap

                // In real implementation find correct nodes. Here we'll simulate parsing.
                foreach (var ln in loadNodes)
                {
                    var originCity = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("OriginCity", StringComparison.OrdinalIgnoreCase))?.Value;
                    var originState = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("OriginState", StringComparison.OrdinalIgnoreCase))?.Value;
                    var destCity = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("DestinationCity", StringComparison.OrdinalIgnoreCase))?.Value;
                    var destState = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("DestinationState", StringComparison.OrdinalIgnoreCase))?.Value;

                    // Payment and mileage
                    var paymentAmountNode = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("PaymentAmount", StringComparison.OrdinalIgnoreCase))?.Value;
                    var mileageNode = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("Mileage", StringComparison.OrdinalIgnoreCase))?.Value;

                    double paymentAmount = 0;
                    double mileage = 0;

                    double.TryParse(paymentAmountNode, out paymentAmount);
                    double.TryParse(mileageNode, out mileage);

                    // --- FALLBACKS ---
                    // If PaymentAmount = 0, try FuelCost or other provided totals
                    if (paymentAmount <= 0)
                    {
                        var fuelCostNode = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("FuelCost", StringComparison.OrdinalIgnoreCase))?.Value;
                        if (double.TryParse(fuelCostNode?.Replace("$", "").Replace(",", ""), out var fuelCost) && fuelCost > 0)
                        {
                            paymentAmount = fuelCost;
                        }
                    }

                    // Now calculate RPM
                    double ratePerMile = 0;
                    if (paymentAmount > 0 && mileage > 0)
                        ratePerMile = Math.Round(paymentAmount / mileage, 2);

                    var dto = new NormalizedLoadDto
                    {
                        Origin = CombineCityState(originCity, originState),
                        DHO = int.TryParse(ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("OriginDistance", StringComparison.OrdinalIgnoreCase))?.Value, out var dho) ? dho : (int?)null,
                        DHD = int.TryParse(ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("DestinationDistance", StringComparison.OrdinalIgnoreCase))?.Value, out var dhd) ? dhd : (int?)null,
                        Destination = CombineCityState(destCity, destState),
                        FromDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        ToDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
                        Age = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("Age", StringComparison.OrdinalIgnoreCase))?.Value ?? "0",

                        RPM = ratePerMile == 0 ? (double?)null : ratePerMile,

                        EquipmentType = ln.Descendants().Where(x => x.Name.LocalName.Equals("EquipmentTypes", StringComparison.OrdinalIgnoreCase)).Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("Code", StringComparison.OrdinalIgnoreCase))?.Value,

                        Length = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("Length", StringComparison.OrdinalIgnoreCase))?.Value,
                        Weight = int.TryParse(ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("Weight", StringComparison.OrdinalIgnoreCase))?.Value, out var w) ? w : (int?)null,
                        LoadType = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("LoadType", StringComparison.OrdinalIgnoreCase))?.Value,

                        ClientName = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("TruckCompanyName", StringComparison.OrdinalIgnoreCase))?.Value,
                        ClientMC = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("MCNumber", StringComparison.OrdinalIgnoreCase))?.Value,
                        ClientLocation = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("TruckCompanyCity", StringComparison.OrdinalIgnoreCase))?.Value,
                        ClientPhone = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("TruckCompanyPhone", StringComparison.OrdinalIgnoreCase))?.Value,
                        ClientEmail = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("TruckCompanyEmail", StringComparison.OrdinalIgnoreCase))?.Value,
                        ClientCreditScore = ln.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("Credit", StringComparison.OrdinalIgnoreCase))?.Value,
                        ClientDaysOfPay = null,

                        Source = "Truckstop",
                        MatchID = null,
                        ID = ln.Elements().FirstOrDefault(x => x.Name.LocalName.Equals("ID", StringComparison.OrdinalIgnoreCase))?.Value
                    };

                    list.Add(dto);
                }
            }
            catch
            {
                // parsing errors ignored; return what we have
            }

            return list;
        }

        private string CombineCityState(string city, string state)
        {
            if (string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(state)) return null;
            if (string.IsNullOrWhiteSpace(city)) return state;
            if (string.IsNullOrWhiteSpace(state)) return city;
            return $"{city}, {state}";
        }

        // ---------------- DAT Integration (JSON) ----------------
        private async Task<ServiceResponse<List<NormalizedLoadDto>>> FetchFromDATAsync(Integrations integ, LoadFiltersDto filters)
        {
            try
            {
                // read credentials
                string orgUsername = GetStringOrFallback(integ.credentials, "org_username", "username");
                string orgPassword = GetStringOrFallback(integ.credentials, "org_password", "password");
                string userEmail = GetStringOrFallback(integ.credentials, "user_email", "user", "email");

                if (string.IsNullOrWhiteSpace(orgUsername) || string.IsNullOrWhiteSpace(orgPassword))
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("DAT credentials missing");

                var http = _httpClientFactory.CreateClient();

                // 1) Org auth
                var orgBody = new { username = orgUsername, password = orgPassword };
                var orgReq = new HttpRequestMessage(HttpMethod.Post, "https://identity.api.staging.dat.com/access/v1/token/organization")
                {
                    Content = new StringContent(JsonSerializer.Serialize(orgBody), Encoding.UTF8, "application/json")
                };

                using var orgResp = await http.SendAsync(orgReq);
                if (!orgResp.IsSuccessStatusCode)
                {
                    var txt = await orgResp.Content.ReadAsStringAsync();
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse($"DAT org auth failed: {orgResp.StatusCode} {txt}");
                }

                var orgRespStr = await orgResp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(orgRespStr);

                string orgAccessToken = null;
                if (doc.RootElement.TryGetProperty("accessToken", out var a1))
                    orgAccessToken = a1.GetString();
                else if (doc.RootElement.TryGetProperty("access_token", out var a2))
                    orgAccessToken = a2.GetString();

                if (string.IsNullOrWhiteSpace(orgAccessToken))
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("DAT org auth returned no token");

                // 2) User auth
                var userBody = new { username = userEmail };
                var userReq = new HttpRequestMessage(HttpMethod.Post, "https://identity.api.staging.dat.com/access/v1/token/user")
                {
                    Content = new StringContent(JsonSerializer.Serialize(userBody), Encoding.UTF8, "application/json")
                };
                userReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", orgAccessToken);

                using var userResp = await http.SendAsync(userReq);
                if (!userResp.IsSuccessStatusCode)
                {
                    var t = await userResp.Content.ReadAsStringAsync();
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse($"DAT user auth failed: {userResp.StatusCode} {t}");
                }

                var userRespStr = await userResp.Content.ReadAsStringAsync();
                using var doc2 = JsonDocument.Parse(userRespStr);

                string userToken = null;
                if (doc2.RootElement.TryGetProperty("accessToken", out var b1))
                    userToken = b1.GetString();
                else if (doc2.RootElement.TryGetProperty("access_token", out var b2))
                    userToken = b2.GetString();

                if (string.IsNullOrWhiteSpace(userToken))
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("DAT user auth returned no token");

                // 3) Create search query
                var queryUrl = "https://freight.api.staging.dat.com/search/v3/queries";
                var payload = BuildDATSearchQuery(filters);

                var qReq = new HttpRequestMessage(HttpMethod.Post, queryUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
                };
                qReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

                using var qResp = await http.SendAsync(qReq);
                if (!qResp.IsSuccessStatusCode)
                {
                    var txt = await qResp.Content.ReadAsStringAsync();
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse($"DAT query create failed: {qResp.StatusCode} {txt}");
                }

                var qRespStr = await qResp.Content.ReadAsStringAsync();
                using var qDoc = JsonDocument.Parse(qRespStr);
                if (!qDoc.RootElement.TryGetProperty("queryId", out var queryIdEl))
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("DAT query creation response missing queryId");

                var queryId = queryIdEl.GetString();

                // 4) Get matches
                var matchesUrl = $"https://freight.api.staging.dat.com/search/v3/queryMatches/{queryId}";
                var mReq = new HttpRequestMessage(HttpMethod.Get, matchesUrl);
                mReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);
                using var mResp = await http.SendAsync(mReq);
                if (!mResp.IsSuccessStatusCode)
                {
                    var tmp = await mResp.Content.ReadAsStringAsync();
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse($"DAT get matches failed: {mResp.StatusCode} {tmp}");
                }

                var mRespStr = await mResp.Content.ReadAsStringAsync();
                var normalized = ParseDATMatchesResponse(mRespStr);
                return ServiceResponse<List<NormalizedLoadDto>>.SuccessResponse(normalized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FetchFromDATAsync failed");
                return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse($"DAT fetch failed: {ex.Message}");
            }
        }

        private static string? GetStringOrFallback(JsonDocument doc, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (doc.RootElement.TryGetProperty(name, out var element) &&
                    element.ValueKind != JsonValueKind.Null &&
                    element.ValueKind != JsonValueKind.Undefined)
                {
                    return element.GetString();
                }
            }
            return null;
        }

        private object BuildDATSearchQuery(LoadFiltersDto filters)
        {
            var earliest = filters.FromDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var latest = filters.ToDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? DateTime.UtcNow.AddDays(14).ToString("yyyy-MM-ddTHH:mm:ssZ");

            // Parse equipment types from filters or default
            var equipmentTypes = (filters.EquipmentType ?? "AC, V, R, F")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);


            var (datOriginCity, datOriginState, datOriginCountry) = ParseLocation(filters.Origin);
            var (datDestCity, datDestState, datDestCountry) = ParseLocation(filters.Destination);

            var payload = new
            {
                criteria = new
                {
                    lane = new
                    {
                        assetType = "SHIPMENT",
                        equipment = new
                        {
                            types = equipmentTypes
                        },
                        origin = new
                        {
                            area = new
                            {
                                states = !string.IsNullOrWhiteSpace(datOriginState) ? new[] { datOriginState } : new[] { "OR", "WA", "CA", "ID", "NV", "UT", "AZ" },
                                cities = !string.IsNullOrWhiteSpace(datOriginCity) ? new[] { datOriginCity } : Array.Empty<string>(),
                                countries = !string.IsNullOrWhiteSpace(datOriginCountry) ? new[] { datOriginCountry } : Array.Empty<string>()
                            }
                        },
                        destination = new
                        {
                            area = new
                            {
                                states = !string.IsNullOrWhiteSpace(datDestState) ? new[] { datDestState } : new[] { "CA", "WA", "UT", "AZ", "NV", "ID", "MT", "CO", "NM" },
                                cities = !string.IsNullOrWhiteSpace(datDestCity) ? new[] { datDestCity } : Array.Empty<string>(),
                                countries = !string.IsNullOrWhiteSpace(datDestCountry) ? new[] { datDestCountry } : Array.Empty<string>()
                            }
                        },
                    },
                    maxAgeMinutes = filters.MaxAgeMinutes > 0 ? filters.MaxAgeMinutes : 4320,
                    maxOriginDeadheadMiles = filters.MaxOriginDeadheadMiles > 0 ? filters.MaxOriginDeadheadMiles : 450,
                    maxDestinationDeadheadMiles = filters.MaxDestinationDeadheadMiles > 0 ? filters.MaxDestinationDeadheadMiles : 450,
                    availability = new
                    {
                        earliestWhen = earliest,
                        latestWhen = latest
                    },
                    capacity = new
                    {
                        shipment = new
                        {
                            fullPartial = string.IsNullOrWhiteSpace(filters.LoadType) ? "BOTH" : filters.LoadType,
                            maximumLengthFeet = filters.MaximumLengthFeet ?? 53,
                            maximumWeightPounds = filters.MaximumWeightPounds ?? 50000
                        }
                    },
                    audience = new
                    {
                        includePrivateNetwork = true,
                        includeLoadBoard = true
                    },
                    includeOnlyBookable = false,
                    includeOnlyHasLength = false,
                    includeOnlyHasWeight = false,
                    includeOnlyQuickPayable = false,
                    includeOnlyFactorable = false,
                    includeOnlyAssurable = false,
                    includeOnlyNegotiable = false,
                    includeOnlyTrackable = false,
                    excludeForeignAssets = false,
                    countsOnly = false,
                    includeOpenDestinationTrucks = false,
                    includeRanked = false,
                    includeCompanies = Array.Empty<string>(),
                    excludeCompanies = Array.Empty<string>()
                }
            };

            return payload;
        }

        private List<NormalizedLoadDto> ParseDATMatchesResponse(string responseJson)
        {
            var list = new List<NormalizedLoadDto>();

            try
            {
                using var doc = JsonDocument.Parse(responseJson);

                // read deadhead values from query.definition.criteria
                int? deadheadOrigin = null;
                int? deadheadDestination = null;

                if (doc.RootElement.TryGetProperty("query", out var query) &&
                    query.TryGetProperty("definition", out var def) &&
                    def.TryGetProperty("criteria", out var criteria))
                {
                    if (criteria.TryGetProperty("maxOriginDeadheadMiles", out var doh))
                        deadheadOrigin = doh.GetInt32();

                    if (criteria.TryGetProperty("maxDestinationDeadheadMiles", out var ddh))
                        deadheadDestination = ddh.GetInt32();
                }

                if (doc.RootElement.TryGetProperty("matches", out var matches))
                {
                    foreach (var m in matches.EnumerateArray())
                    {
                        try
                        {
                            // navigate into matchingAssetInfo
                            var assetInfo = m.GetProperty("matchingAssetInfo");

                            var originCity = assetInfo.GetProperty("origin").GetProperty("city").GetString();
                            var originState = assetInfo.GetProperty("origin").GetProperty("stateProv").GetString();
                            var destCity = assetInfo.GetProperty("destination").GetProperty("place").GetProperty("city").GetString();
                            var destState = assetInfo.GetProperty("destination").GetProperty("place").GetProperty("stateProv").GetString();

                            // dates are inside availability
                            var fromDate = m.TryGetProperty("availability", out var avail) &&
                                           avail.TryGetProperty("earliestWhen", out var e) ?
                                           e.GetDateTime().ToString("yyyy-MM-dd") : null;

                            var toDate = m.TryGetProperty("availability", out var avail2) &&
                                         avail2.TryGetProperty("latestWhen", out var l) ?
                                         l.GetDateTime().ToString("yyyy-MM-dd") : null;

                            // equipment type
                            var equipmentType = assetInfo.TryGetProperty("equipmentType", out var eq) ? eq.GetString() : null;

                            // length & weight
                            var length = assetInfo.TryGetProperty("capacity", out var cap) &&
                                         cap.TryGetProperty("shipment", out var ship) &&
                                         ship.TryGetProperty("maximumLengthFeet", out var lenEl) ? lenEl.GetInt32().ToString() : null;

                            var weight = assetInfo.TryGetProperty("capacity", out var cap2) &&
                                         cap2.TryGetProperty("shipment", out var ship2) &&
                                         ship2.TryGetProperty("maximumWeightPounds", out var wtEl) ? wtEl.GetInt32() : (int?)null;

                            // load type
                            var loadType = assetInfo.TryGetProperty("capacity", out var cap3) &&
                                           cap3.TryGetProperty("shipment", out var ship3) &&
                                           ship3.TryGetProperty("fullPartial", out var fp) ? fp.GetString() : null;

                            // client info
                            var clientName = m.TryGetProperty("posterInfo", out var poster) &&
                                             poster.TryGetProperty("companyName", out var comp) ? comp.GetString() : null;

                            var clientPhone = poster.TryGetProperty("contact", out var contact1) &&
                                              contact1.TryGetProperty("phone", out var phone) ? phone.GetString() : null;

                            var clientEmail = poster.TryGetProperty("contact", out var contact2) &&
                                              contact2.TryGetProperty("email", out var email) ? email.GetString() : null;

                            var clientMc = m.TryGetProperty("posterDotIds", out var dotIds) &&
                                           dotIds.TryGetProperty("brokerMcNumber", out var mc) ? mc.GetInt32().ToString() : null;

                            var clientLocation = poster.TryGetProperty("city", out var clCity) &&
                                         poster.TryGetProperty("state", out var clState)
                                         ? $"{clCity.GetString()}, {clState.GetString()}"
                                         : null;

                            var clientCreditScore = poster.TryGetProperty("credit", out var credit) &&
                                                    credit.TryGetProperty("creditScore", out var cs) ? cs.GetInt32().ToString() : null;

                            var clientDaysOfPay = credit.TryGetProperty("daysToPay", out var dp) ? dp.GetInt32().ToString() : null;

                            // rate per mile
                            var rpm = m.TryGetProperty("estimatedRatePerMile", out var rpmEl) && rpmEl.TryGetDouble(out var r)
                                ? r : (double?)null;

                            // Age (in days since servicedWhen)
                            var age = m.TryGetProperty("servicedWhen", out var sw)
                                ? (int?)(DateTime.UtcNow - sw.GetDateTime()).TotalDays
                                : null;

                            // build DTO
                            var dto = new NormalizedLoadDto
                            {
                                Origin = CombineCityState(originCity, originState),
                                DHO = deadheadOrigin,
                                DHD = deadheadDestination,
                                Destination = CombineCityState(destCity, destState),
                                FromDate = fromDate,
                                ToDate = toDate,
                                Age = age?.ToString(),
                                RPM = rpm,
                                EquipmentType = equipmentType,
                                Length = length,
                                Weight = weight,
                                LoadType = loadType,
                                ClientName = clientName,
                                ClientMC = clientMc,
                                ClientLocation = clientLocation,
                                ClientPhone = clientPhone,
                                ClientEmail = clientEmail,
                                ClientCreditScore = clientCreditScore,
                                ClientDaysOfPay = clientDaysOfPay,
                                Source = "DAT",
                                MatchID = m.TryGetProperty("matchId", out var mid) ? mid.GetString() : null,
                                ID = null
                            };

                            list.Add(dto);
                        }
                        catch (Exception exItem)
                        {
                            // log/skip malformed item
                            Console.WriteLine($"Skipping one match due to parse error: {exItem.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing DAT matches response: {ex.Message}");
            }

            return list;
        }
    }
}
