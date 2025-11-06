using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OmmoBackend.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace OmmoBackend.Services.Implementations
{
    public class TokenValidationService : ITokenValidationService
    {
        private readonly TokenValidationOptions _opts;
        private readonly ILogger<TokenValidationService> _logger;
        private readonly TokenValidationParameters _validationParameters;

        public TokenValidationService(IOptions<TokenValidationOptions> opts, ILogger<TokenValidationService> logger)
        {
            _opts = opts.Value;
            _logger = logger;

            _validationParameters = BuildValidationParameters();
        }

        public async Task<(bool IsValid, IDictionary<string, string>? Claims, string? ErrorMessage)> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (false, null, "Token is missing.");

            try
            {
                var handler = new JwtSecurityTokenHandler();

                // Validate signature/lifetime/issuer/audience
                var principal = handler.ValidateToken(token, _validationParameters, out var validatedToken);

                // Additional checks: ensure algorithm matches expected
                if (!(validatedToken is JwtSecurityToken jwtToken))
                    return (false, null, "Invalid token.");

                // Required claim: user_id
                var userId = principal.Claims.FirstOrDefault(c => string.Equals(c.Type, "user_id", StringComparison.OrdinalIgnoreCase)
                                                                  || string.Equals(c.Type, ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase))
                             ?.Value;

                if (string.IsNullOrEmpty(userId))
                    return (false, null, "Required claim 'user_id' is missing.");

                // Build dictionary of claims we want to return (string values only)
                var claimsDict = principal.Claims
                    .GroupBy(c => c.Type)
                    .ToDictionary(g => g.Key, g => string.Join(",", g.Select(c => c.Value)));

                return (true, claimsDict, null);
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogInformation(ex, "Token expired.");
                return (false, null, "Token expired.");
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogWarning(ex, "Token signature invalid.");
                return (false, null, "Token signature invalid.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation failed.");
                return (false, null, "Token invalid or validation error.");
            }
        }

        private TokenValidationParameters BuildValidationParameters()
        {
            var parameters = new TokenValidationParameters
            {
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                ValidateIssuer = !string.IsNullOrEmpty(_opts.ValidIssuer),
                ValidIssuer = _opts.ValidIssuer,
                ValidateAudience = !string.IsNullOrEmpty(_opts.ValidAudience),
                ValidAudience = _opts.ValidAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(60) // small skew
            };

            if (string.Equals(_opts.Mode, "RSA", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(_opts.RsaPublicKeyPem))
                    throw new ArgumentException("RSA mode requires RsaPublicKeyPem in configuration.");

                var rsa = RSA.Create();
                // parse PEM
                var rsaParams = PemToRSAParameters(_opts.RsaPublicKeyPem);
                rsa.ImportParameters(rsaParams);
                parameters.IssuerSigningKey = new RsaSecurityKey(rsa);
                parameters.ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256, SecurityAlgorithms.RsaSha256Signature };
            }
            else // HMAC
            {
                if (string.IsNullOrWhiteSpace(_opts.HmacSecret))
                    throw new ArgumentException("HMAC mode requires HmacSecret in configuration.");

                var secretBytes = Encoding.UTF8.GetBytes(_opts.HmacSecret);
                parameters.IssuerSigningKey = new SymmetricSecurityKey(secretBytes);
                parameters.ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256, SecurityAlgorithms.HmacSha256Signature };
            }

            return parameters;
        }

        // Simple PEM parser for public key (expects PKCS#1 or PKCS#8 RSA public key)
        private static RSAParameters PemToRSAParameters(string pem)
        {
            // remove header / footer
            var header = "-----BEGIN PUBLIC KEY-----";
            var footer = "-----END PUBLIC KEY-----";
            var pkcs1Header = "-----BEGIN RSA PUBLIC KEY-----";
            var pkcs1Footer = "-----END RSA PUBLIC KEY-----";

            string base64;
            if (pem.Contains(header))
            {
                base64 = pem.Replace(header, "").Replace(footer, "").Replace("\n", "").Replace("\r", "").Trim();
                var keyBytes = Convert.FromBase64String(base64);
                using var mem = new MemoryStream(keyBytes);
                using var binr = new BinaryReader(mem);
                var rsa = ReadX509PublicKey(binr);
                return rsa;
            }
            else if (pem.Contains(pkcs1Header))
            {
                base64 = pem.Replace(pkcs1Header, "").Replace(pkcs1Footer, "").Replace("\n", "").Replace("\r", "").Trim();
                var keyBytes = Convert.FromBase64String(base64);
                using var mem = new MemoryStream(keyBytes);
                using var binr = new BinaryReader(mem);
                var rsa = ReadRsaPublicKey(binr);
                return rsa;
            }
            else
            {
                throw new ArgumentException("Unsupported PEM format for RSA public key.");
            }
        }

        // Helpers adapted from common PEM/X509 parsing code (kept concise)
        private static RSAParameters ReadRsaPublicKey(BinaryReader reader)
        {
            var seq = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            using var ms = new MemoryStream(seq);
            using var br = new BinaryReader(ms);
            var twoBytes = br.ReadUInt16();
            if (twoBytes == 0x8130) br.ReadByte();
            else if (twoBytes == 0x8230) br.ReadInt16();

            // read integer - modulus
            var modint = br.ReadUInt16();
            if (modint == 0x8102) br.ReadByte();
            else if (modint == 0x8202) br.ReadInt16();
            var modsize = br.ReadByte();
            if (modsize == 0x00) modsize = br.ReadByte();
            var modulus = br.ReadBytes(modsize);

            // exponent
            if (br.ReadByte() != 0x02) throw new Exception("Invalid RSA public key format.");
            var expbytes = br.ReadByte();
            var exponent = br.ReadBytes(expbytes);

            return new RSAParameters
            {
                Modulus = modulus,
                Exponent = exponent
            };
        }

        private static RSAParameters ReadX509PublicKey(BinaryReader reader)
        {
            var seq = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            // For brevity and robustness in production use a tested library like BouncyCastle.
            // Here, throw to indicate production code should use a reliable parser.
            throw new NotSupportedException("X.509 public key parsing is not implemented in this simplified example. Use HMAC or provide a parsed RSAParameters.");
        }
    }

    public class TokenValidationOptions
    {
        public string Mode { get; set; } = "HMAC"; // "HMAC" or "RSA"
        public string? HmacSecret { get; set; }
        public string? RsaPublicKeyPem { get; set; }
        public string? ValidIssuer { get; set; }
        public string? ValidAudience { get; set; }
    }
}
