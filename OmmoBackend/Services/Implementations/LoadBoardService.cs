using OmmoBackend.Dtos;
using OmmoBackend.Helpers;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Implementations;
using OmmoBackend.Services.Interfaces;
using System.Net;
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
        private readonly IEncryptionService _encryption;
        private readonly IDefaultIntegrationRepository _defaultIntegrationRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoadBoardService> _logger;
        private readonly ICompanyRepository _companyRepository;
        private readonly IGlobalIntegrationCredentialRepository _globalIntegrationCredentialRepository;

        public LoadBoardService(
            IIntegrationRepository integrationRepository,
            IHttpClientFactory httpClientFactory,
            IEncryptionService encryption,
            IDefaultIntegrationRepository defaultIntegrationRepository,
            IConfiguration configuration,
            ILogger<LoadBoardService> logger,
            ICompanyRepository companyRepository,
            IGlobalIntegrationCredentialRepository globalIntegrationCredentialRepository)
        {
            _integrationRepository = integrationRepository;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _encryption = encryption;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _defaultIntegrationRepository = defaultIntegrationRepository;
            _logger = logger;
            _companyRepository = companyRepository;
            _globalIntegrationCredentialRepository = globalIntegrationCredentialRepository;
        }

        public async Task<ServiceResponse<List<NormalizedLoadDto>>> GetLoadsAsync(int companyId, LoadFiltersDto filters)
        {
            try
            {
                if (filters == null || (string.IsNullOrWhiteSpace(filters.Origin) && string.IsNullOrWhiteSpace(filters.Destination)))
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("Origin or Destination is required.", 400);

                var integrations = await _integrationRepository.GetByCompanyAsync(companyId);

                if (integrations == null || integrations.Count == 0)
                {
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("No loadboard integrations found.", 404);
                }

                var hasActiveOrPendingIntegration = integrations.Any(x => x.integration_status == "active" || x.integration_status == "pending");

                if (!hasActiveOrPendingIntegration)
                {
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("No active loadboard integrations found.", 403);
                }

                var combinedLoads = new List<NormalizedLoadDto>();

                foreach (var integration in integrations)
                {
                    ServiceResponse<List<NormalizedLoadDto>>? loadResponse = null;

                    // Fetch logo
                    var logoPath = await _defaultIntegrationRepository
                        .GetLogoPathByIntegrationIdAsync(integration.default_integration_id);

                    // ============================
                    // Truckstop
                    // ============================
                    if (integration.default_integration_id == 3)
                    {
                        if (integration.integration_status == "active")
                        {
                            loadResponse = await FetchFromTruckstopAsync(integration, filters);
                        }

                        else if (integration.integration_status == "pending")
                        {
                            var isVerified = await _companyRepository.IsCompanyVerifiedAsync(companyId);

                            if (!isVerified)
                            {
                                return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("Your account is not verified yet.", 403);
                            }

                            var lastUpdated = integration.last_updated;
                            var now = DateTime.UtcNow;

                            if ((now - lastUpdated).TotalDays < 3)
                            {
                                // Fetch global credential
                                var credential = await _globalIntegrationCredentialRepository.GetCredentialAsync(3, "truckstop_integration_id");

                                if (credential == null || string.IsNullOrWhiteSpace(credential.credential_value))
                                {
                                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("Unable to connect to Truckstop.", 403);
                                }

                                //var decryptedIntegrationId = _encryption.Decrypt(credential.credential_value);

                                // Override integration temporarily
                                // integration.integration_id = int.Parse(credential.credential_value);

                                loadResponse = await FetchFromTruckstopAsync(integration, filters, credential.credential_value);
                            }
                            else
                            {
                                return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("Your account verification is pending for more than 3 days.", 403);
                            }
                        }
                    }

                    // ====================================
                    // DAT
                    // ====================================
                    else if (integration.default_integration_id == 4)
                    {
                        if (integration.integration_status != "active")
                            continue;

                        loadResponse = await FetchFromDATAsync(integration, filters);
                    }

                    if (loadResponse == null)
                        continue;

                    if (!loadResponse.Success)
                    {
                        return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse(
                            string.IsNullOrWhiteSpace(loadResponse.ErrorMessage)
                                ? "Load provider request failed."
                                : loadResponse.ErrorMessage,
                            loadResponse.StatusCode > 0
                                ? loadResponse.StatusCode
                                : 503);
                    }

                    foreach (var load in loadResponse.Data)
                    {
                        load.ImageUrl = logoPath;
                    }

                    combinedLoads.AddRange(loadResponse.Data);
                }

                var filtered = ApplyFilters(combinedLoads, filters);

                return ServiceResponse<List<NormalizedLoadDto>>.SuccessResponse(filtered);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLoadsAsync for CompanyId {CompanyId}", companyId);

                return ServiceResponse<List<NormalizedLoadDto>>
                    .ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<List<NormalizedLoadDto>>> xGetLoadsAsync(int companyId, LoadFiltersDto filters)
        {
            try
            {
                if (filters == null || (string.IsNullOrWhiteSpace(filters.Origin) && string.IsNullOrWhiteSpace(filters.Destination)))
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("Origin or Destination is required.", 400);

                var integrations = await _integrationRepository.GetActiveIntegrationsAsync(companyId);
                if (integrations == null || integrations.Count == 0)
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("No active loadboard integrations", 400);

                var combinedLoads = new List<NormalizedLoadDto>();

                foreach (var integration in integrations)
                {
                    // Fetch logo for this integration (DAT/Truckstop)
                    var logoPath = await _defaultIntegrationRepository
                        .GetLogoPathByIntegrationIdAsync(integration.default_integration_id);

                    ServiceResponse<List<NormalizedLoadDto>>? loadResponse = null;

                    if (integration.integration_id != 1 && integration.integration_id != 2)
                    {
                        var credsJson = integration.credentials?.RootElement.GetRawText();
                        var creds = GetDecryptedCredentials(credsJson);
                        var email = creds.GetValueOrDefault("email");
                    }

                    if (integration.default_integration_id == 3) // Truckstop
                    {
                        loadResponse = await FetchFromTruckstopAsync(integration, filters);
                    }
                    else if (integration.default_integration_id == 4) // DAT
                    {
                        loadResponse = await FetchFromDATAsync(integration, filters);
                    }

                    if (loadResponse == null)
                    {
                        return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("Load provider returned no response.", 503);
                    }

                    if (!loadResponse.Success)
                    {
                        return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse(
                            string.IsNullOrWhiteSpace(loadResponse.ErrorMessage) ? "Load provider request failed." : loadResponse.ErrorMessage,
                            loadResponse.StatusCode > 0 ? loadResponse.StatusCode : 503);
                    }

                    if (loadResponse.Success)
                    {
                        // Apply logo to each load
                        foreach (var load in loadResponse.Data)
                        {
                            load.ImageUrl = logoPath;
                        }

                        combinedLoads.AddRange(loadResponse.Data);
                    }
                }

                // Apply filters AFTER data is fetched
                var filtered = ApplyFilters(combinedLoads, filters);

                return ServiceResponse<List<NormalizedLoadDto>>.SuccessResponse(filtered);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        private Dictionary<string, string> GetDecryptedCredentials(string credentialsJson)
        {
            if (string.IsNullOrWhiteSpace(credentialsJson))
                return new();

            var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(credentialsJson) ?? new();
            var decrypted = new Dictionary<string, string>();

            foreach (var kv in credentials)
                decrypted[kv.Key] = _encryption.Decrypt(kv.Value);

            return decrypted;
        }

        // ---------------- Truckstop Integration (SOAP) ----------------
        public async Task<ServiceResponse<List<NormalizedLoadDto>>> FetchFromTruckstopAsync(Integrations companyIntegration, LoadFiltersDto filters, string defaultTruckstopIntegrationId = null)
        {
            try
            {


                if (companyIntegration == null)
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("No Truckstop integration provided.", 400);

                string IntegrationID = null;

                if (companyIntegration.integration_status == "pending")
                {
                    IntegrationID = FieldCrypto.Decrypt(defaultTruckstopIntegrationId, _configuration);
                }
                else if (companyIntegration.credentials != null)
                {
                    companyIntegration.credentials.RootElement.TryGetProperty("IntegrationID", out var u);
                    IntegrationID = FieldCrypto.Decrypt(u.GetString(), _configuration);
                }


                var usernameEnc = await _integrationRepository
                .GetGlobalCredentialAsync(companyIntegration.default_integration_id, "Truckstop_username");

                var passwordEnc = await _integrationRepository
                    .GetGlobalCredentialAsync(companyIntegration.default_integration_id, "Truckstop_password");

                var username = FieldCrypto.Decrypt(usernameEnc, _configuration);
                var password = FieldCrypto.Decrypt(passwordEnc, _configuration);
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(IntegrationID))
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("Truckstop credentials or integration ID missing.", 400);

                //if (string.IsNullOrWhiteSpace(filters.Origin))
                //  return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("Truckstop origin is required.", 400);

                var hasOrigin = !string.IsNullOrWhiteSpace(filters.Origin);
                var hasDestination = !string.IsNullOrWhiteSpace(filters.Destination);

                var (originCity, originState, parsedOriginCountry) = ParseLocation(filters.Origin);
                var (destCity, destState, parsedDestCountry) = ParseLocation(filters.Destination);

                if (!hasOrigin && !hasDestination)
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("Origin or Destination is required.", 400);

                if (hasOrigin && (string.IsNullOrWhiteSpace(originCity) || string.IsNullOrWhiteSpace(originState)))
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("Truckstop origin city and state are required.", 400);

                string originCountry = string.IsNullOrWhiteSpace(parsedOriginCountry) ? "usa" : parsedOriginCountry.ToLowerInvariant();
                string destCountry = string.IsNullOrWhiteSpace(parsedDestCountry) ? "usa" : parsedDestCountry.ToLowerInvariant();

                var equipmentType = string.IsNullOrWhiteSpace(filters.EquipmentType)
                    ? "V,F,R"
                    : string.Join(",", filters.EquipmentType
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(e => e.ToUpperInvariant()));

                var normalizedLoadType = string.IsNullOrWhiteSpace(filters.LoadType)
                    ? "All"
                    : filters.LoadType.Trim().ToUpperInvariant();

                var loadType = normalizedLoadType switch
                {
                    "FULL" => "Full",
                    "PARTIAL" => "Partial",
                    _ => "All"
                };

                var hoursOld = filters.MaxAgeMinutes > 0
                    ? filters.MaxAgeMinutes
                    : 24;

                var destinationXml = string.IsNullOrWhiteSpace(filters.Destination)
                    ? @"
                                                    <web1:DestinationCountry>usa</web1:DestinationCountry>
                                                    <web1:DestinationRange>300</web1:DestinationRange>"
                    : $@"
                                                    <web1:DestinationCountry>{SecurityElement.Escape(destCountry)}</web1:DestinationCountry>
                                                    <web1:DestinationRange>{filters.MaxDestinationDeadheadMiles}</web1:DestinationRange>
                                                    {(string.IsNullOrWhiteSpace(destCity) ? string.Empty : $"<web1:DestinationCity>{SecurityElement.Escape(destCity)}</web1:DestinationCity>")}
                                                    {(string.IsNullOrWhiteSpace(destState) ? string.Empty : $"<web1:DestinationState>{SecurityElement.Escape(destState)}</web1:DestinationState>")}";

                var originXml = hasOrigin
                    ? $@"
                                                    <web1:OriginCity>{SecurityElement.Escape(originCity)}</web1:OriginCity>
                                                    <web1:OriginCountry>{SecurityElement.Escape(originCountry)}</web1:OriginCountry>
                                                    <web1:OriginRange>{filters.MaxOriginDeadheadMiles}</web1:OriginRange>
                                                    <web1:OriginState>{SecurityElement.Escape(originState)}</web1:OriginState>"
                    : @"
                                                    <web1:OriginCountry>usa</web1:OriginCountry>
                                                    <web1:OriginRange>300</web1:OriginRange>";

                DateTime[] dateSequence;
                if (filters.FromDate.HasValue && filters.ToDate.HasValue && filters.ToDate.Value.Date >= filters.FromDate.Value.Date)
                {
                    var start = filters.FromDate.Value.Date;
                    var end = filters.ToDate.Value.Date;
                    dateSequence = Enumerable
                        .Range(0, (end - start).Days + 1)
                        .Select(offset => start.AddDays(offset))
                        .ToArray();
                }
                else if (filters.FromDate.HasValue)
                {
                    dateSequence = new[] { filters.FromDate.Value.Date };
                }
                else
                {
                    dateSequence = new[]
                    {
                        DateTime.UtcNow.Date
                    };
                }

                var pickupDatesXml = string.Join(Environment.NewLine,
                    dateSequence.Select(d => $"<arr:dateTime>{d:yyyy-MM-dd}T00:00:00</arr:dateTime>"));

                var client = _httpClientFactory.CreateClient("truckstop");

                // ----------------- SOAP BODY (ONE REQUEST) -----------------

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
                                                <web:IntegrationId>{IntegrationID}</web:IntegrationId>
                                                <web:Password>{SecurityElement.Escape(password)}</web:Password>
                                                <web:UserName>{SecurityElement.Escape(username)}</web:UserName>
                                                <web1:Criteria>
                                                    {destinationXml}
                                                    <web1:EquipmentType>{SecurityElement.Escape(equipmentType)}</web1:EquipmentType>
                                                    <web1:HoursOld>{hoursOld}</web1:HoursOld>
                                                    <web1:LoadType>{SecurityElement.Escape(loadType)}</web1:LoadType>
                                                    {originXml}
                                                    <web1:PickupDates>
                                                        {pickupDatesXml}
                                                    </web1:PickupDates>
                                                   <web1:PageNumber>1</web1:PageNumber>
                                                   <web1:PageSize>10</web1:PageSize>
                                                   <web1:SortBy>Age</web1:SortBy>
                                                   <web1:SortDescending>true</web1:SortDescending>
                                                </web1:Criteria>
                                             </v12:searchRequest>
                                          </v12:GetMultipleLoadDetailResults>
                                       </soapenv:Body>
                                    </soapenv:Envelope>";

                var request = new HttpRequestMessage(HttpMethod.Post, "https://webservices.truckstop.com/V13/Searching/LoadSearch.svc")
                {
                    Content = new StringContent(soapBody, Encoding.UTF8, "text/xml")
                };
                request.Headers.Add("SOAPAction", "http://webservices.truckstop.com/v12/ILoadSearch/GetMultipleLoadDetailResults");

                // ----------------- SEND ONE REQUEST -----------------
                var response = await client.SendAsync(request);
                var xmlResponse = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("Truckstop request failed.", 503);

                var normalized = NormalizeTruckstopLoads(xmlResponse);
                return ServiceResponse<List<NormalizedLoadDto>>.SuccessResponse(normalized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FetchFromTruckstopAsync failed");
                return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse($"Truckstop fetch failed", 503);
            }
        }

        private List<NormalizedLoadDto> NormalizeTruckstopLoads(string soapResponse)
        {
            var list = new List<NormalizedLoadDto>();

            if (string.IsNullOrWhiteSpace(soapResponse))
                return list;

            XDocument doc;
            try
            {
                doc = XDocument.Parse(soapResponse);
            }
            catch
            {
                return list;
            }

            var loadNodes = doc.Descendants()
                .Where(x => x.Name.LocalName == "MultipleLoadDetailResult")
                .ToList();

            foreach (var ln in loadNodes)
            {
                // --- ORIGIN ---
                var originCity = GetValue(ln, "OriginCity");
                var originState = GetValue(ln, "OriginState");
                var originDist = GetInt(ln, "OriginDistance");

                // --- DESTINATION ---
                var destCity = GetValue(ln, "DestinationCity");
                var destState = GetValue(ln, "DestinationState");
                var destDist = GetInt(ln, "DestinationDistance");

                // --- RATE / PAY ---
                double payment = GetDouble(ln, "PaymentAmount");
                double mileage = GetDouble(ln, "Mileage");

                // fallback: sometimes Truckstop uses FuelCost or TotalPay
                if (payment <= 0)
                {
                    var fallback1 = GetDouble(ln, "FuelCost");
                    var fallback2 = GetDouble(ln, "TotalPay");
                    if (fallback1 > 0) payment = fallback1;
                    if (fallback2 > 0) payment = fallback2;
                }

                // --- RPM ---
                double rpm = 0;
                if (payment > 0 && mileage > 0)
                    rpm = Math.Round(payment / mileage, 2);

                // --- EQUIPMENT ---
                var equipment =
                    ln.Descendants().FirstOrDefault(x => x.Name.LocalName == "EquipmentTypes")
                     ?.Descendants().FirstOrDefault(x => x.Name.LocalName == "Code")
                     ?.Value;

                // --- DATE HANDLING ---
                var pickupDateString = GetValue(ln, "PickupDate");
                var deliveryDateString = GetValue(ln, "DeliveryDate");
                DateTime? pickupDate = TryParseDate(pickupDateString);
                DateTime? deliveryDate = TryParseDate(deliveryDateString);

                // --- Final DTO ---
                var dto = new NormalizedLoadDto
                {
                    OriginCity = originCity,
                    OriginState = originState,
                    DestinationCity = destCity,
                    DestinationState = destState,

                    DHO = originDist,
                    DHD = destDist,

                    FromDate = pickupDate?.ToString("yyyy-MM-dd") ?? pickupDateString,
                    ToDate = deliveryDate?.ToString("yyyy-MM-dd") ?? deliveryDateString,

                    Age = GetValue(ln, "Age"),

                    RPM = rpm == 0 ? (double?)null : rpm,
                    RateTotal = payment,
                    Mileage = mileage,

                    EquipmentType = equipment,
                    Length = GetValue(ln, "Length"),
                    Weight = GetInt(ln, "Weight"),
                    LoadType = GetValue(ln, "LoadType"),

                    ClientName = GetValue(ln, "TruckCompanyName"),
                    ClientMC = GetValue(ln, "MCNumber"),
                    ClientLocation = GetValue(ln, "TruckCompanyCity"),
                    ClientPhone = GetValue(ln, "TruckCompanyPhone"),
                    ClientEmail = GetValue(ln, "TruckCompanyEmail"),
                    ClientCreditScore = GetValue(ln, "Credit"),
                    ClientDaysOfPay = null,

                    Source = "Truckstop",
                    ID = GetValue(ln, "ID"),
                    MatchID = null
                };

                list.Add(dto);
            }

            return list;
        }

        private string GetValue(XElement parent, string name)
        {
            return parent.Descendants().FirstOrDefault(x => x.Name.LocalName == name)?.Value;
        }

        private int? GetInt(XElement parent, string name)
        {
            var candidates = parent
                .Descendants()
                .Where(x => x.Name.LocalName == name)
                .Select(x => x.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            if (candidates.Count == 0)
                return null;

            int? firstParsed = null;
            foreach (var raw in candidates)
            {
                var normalized = raw.Trim().Replace(",", "");

                if (int.TryParse(normalized, out var i))
                {
                    firstParsed ??= i;
                    if (i > 0)
                        return i;
                    continue;
                }

                var numericOnly = new string(normalized
                    .Where(ch => char.IsDigit(ch) || ch == '.' || ch == '-')
                    .ToArray());

                if (double.TryParse(numericOnly, out var d))
                {
                    var rounded = (int)Math.Round(d, MidpointRounding.AwayFromZero);
                    firstParsed ??= rounded;
                    if (rounded > 0)
                        return rounded;
                }
            }

            return firstParsed;
        }

        private double GetDouble(XElement parent, string name)
        {
            var v = GetValue(parent, name);
            return double.TryParse(v?.Replace("$", "").Replace(",", ""), out var n) ? n : 0;
        }

        private DateTime? TryParseDate(string v)
        {
            if (DateTime.TryParse(v, out var d))
                return d;
            return null;
        }

        // ---------------- DAT Integration (JSON) ----------------
        private async Task<ServiceResponse<List<NormalizedLoadDto>>> FetchFromDATAsync(Integrations integ, LoadFiltersDto filters)
        {
            try
            {

                string orgUsernameEnc = GetStringOrFallback(integ.credentials, "org_username", "username");
                string orgPasswordEnc = GetStringOrFallback(integ.credentials, "org_password", "password");
                string userEmailEnc = GetStringOrFallback(integ.credentials, "user_email", "user", "email");

                string orgUsername = FieldCrypto.Decrypt(orgUsernameEnc, _configuration);
                string orgPassword = FieldCrypto.Decrypt(orgPasswordEnc, _configuration);
                string userEmail = FieldCrypto.Decrypt(userEmailEnc, _configuration);


                if (string.IsNullOrWhiteSpace(orgUsername) || string.IsNullOrWhiteSpace(orgPassword))
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("DAT credentials missing", 400);

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
                    var text = await orgResp.Content.ReadAsStringAsync();
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse($"DAT org auth failed: {orgResp.StatusCode} {text}", orgResp.StatusCode == HttpStatusCode.Unauthorized ? 401 : 502);
                }

                var orgRespStr = await orgResp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(orgRespStr);

                string orgAccessToken = null;
                if (doc.RootElement.TryGetProperty("accessToken", out var a1))
                    orgAccessToken = a1.GetString();
                else if (doc.RootElement.TryGetProperty("access_token", out var a2))
                    orgAccessToken = a2.GetString();

                if (string.IsNullOrWhiteSpace(orgAccessToken))
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("DAT org auth returned no token", 502);

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
                    var text = await userResp.Content.ReadAsStringAsync();
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse($"DAT user auth failed: {userResp.StatusCode} {text}", userResp.StatusCode == HttpStatusCode.Unauthorized ? 401 : 502);
                }

                var userRespStr = await userResp.Content.ReadAsStringAsync();
                using var doc2 = JsonDocument.Parse(userRespStr);

                string userToken = null;
                if (doc2.RootElement.TryGetProperty("accessToken", out var b1))
                    userToken = b1.GetString();
                else if (doc2.RootElement.TryGetProperty("access_token", out var b2))
                    userToken = b2.GetString();

                if (string.IsNullOrWhiteSpace(userToken))
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("DAT user auth returned no token", 502);

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
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse($"DAT query create failed: {qResp.StatusCode} {txt}", 502);
                }

                var qRespStr = await qResp.Content.ReadAsStringAsync();
                using var qDoc = JsonDocument.Parse(qRespStr);
                if (!qDoc.RootElement.TryGetProperty("queryId", out var queryIdEl))
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("DAT query creation response missing queryId", 502);

                var queryId = queryIdEl.GetString();

                // 4) Get matches
                var matchesUrl = $"https://freight.api.staging.dat.com/search/v3/queryMatches/{queryId}";
                var mReq = new HttpRequestMessage(HttpMethod.Get, matchesUrl);
                mReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);
                using var mResp = await http.SendAsync(mReq);
                if (!mResp.IsSuccessStatusCode)
                {
                    var tmp = await mResp.Content.ReadAsStringAsync();
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse($"DAT get matches failed: {mResp.StatusCode} {tmp}", 502);
                }

                var mRespStr = await mResp.Content.ReadAsStringAsync();
                var normalized = ParseDATMatchesResponse(mRespStr);
                if (normalized == null || normalized.Count == 0)
                    return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse("No DAT loads found.", 404);

                return ServiceResponse<List<NormalizedLoadDto>>.SuccessResponse(normalized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FetchFromDATAsync failed");
                return ServiceResponse<List<NormalizedLoadDto>>.ErrorResponse($"DAT fetch failed", 503);

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
            var earliest = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var latest = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-ddTHH:mm:ssZ");

            // Parse equipment types from filters or default
            var equipmentTypes = ("AC, V, R, F")
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
                    maxAgeMinutes = (filters.MaxAgeMinutes > 0 ? filters.MaxAgeMinutes : 24) * 60,
                    maxOriginDeadheadMiles = filters.MaxOriginDeadheadMiles > 0 ? filters.MaxOriginDeadheadMiles : 100,
                    maxDestinationDeadheadMiles = filters.MaxDestinationDeadheadMiles > 0 ? filters.MaxDestinationDeadheadMiles : 100,
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

                            double mileage = 0;
                            if (m.TryGetProperty("mileage", out var mileEl) && mileEl.TryGetDouble(out var mileVal))
                            {
                                mileage = mileVal;
                            }
                            else if (deadheadOrigin.HasValue && deadheadDestination.HasValue)
                            {
                                mileage = deadheadOrigin.Value + deadheadDestination.Value; // fallback
                            }

                            double rateTotal = 0;
                            if (rpm.HasValue && mileage > 0)
                                rateTotal = rpm.Value * mileage;

                            // build DTO
                            var dto = new NormalizedLoadDto
                            {
                                OriginCity = originCity,
                                OriginState = originState,
                                DestinationCity = destCity,
                                DestinationState = destState,
                                DHO = deadheadOrigin,
                                DHD = deadheadDestination,
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
                                ID = null,

                                Mileage = mileage,
                                RateTotal = rateTotal
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

        // ---------------------------
        // Apply Filters
        // ---------------------------
        public List<NormalizedLoadDto> ApplyFilters(List<NormalizedLoadDto> loads, LoadFiltersDto filters)
        {
            if (!loads.Any()) return loads;

            // RPM filter
            if (filters.RPM.HasValue)
            {
                loads = loads.Where(x =>
                    x.RPM.HasValue && x.RPM.Value >= (double)filters.RPM.Value
                ).ToList();
            }

            // Equipment filter
            if (!string.IsNullOrWhiteSpace(filters.EquipmentType))
            {
                var allowed = filters.EquipmentType.Split(',').Select(e => e.Trim().ToUpper()).ToList();
                loads = loads.Where(x =>
                    !string.IsNullOrWhiteSpace(x.EquipmentType) &&
                    allowed.Contains(x.EquipmentType.ToUpper())
                ).ToList();
            }

            // Length filter
            if (filters.MaximumLengthFeet.HasValue)
            {
                loads = loads.Where(x =>
                {
                    if (!double.TryParse(x.Length, out var len))
                        return true;
                    return len <= filters.MaximumLengthFeet.Value;
                }).ToList();
            }

            // Weight filter
            if (filters.MaximumWeightPounds.HasValue)
            {
                loads = loads.Where(x =>
                    x.Weight.HasValue && x.Weight.Value <= filters.MaximumWeightPounds.Value
                ).ToList();
            }

            return loads;
        }
    }
}
