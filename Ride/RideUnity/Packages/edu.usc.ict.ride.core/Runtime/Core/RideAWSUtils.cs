using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace Ride
{
    /// <summary>
    /// Shared AWS helper methods used by Ride systems for capability checks and Signature Version 4 request signing.
    /// </summary>
    /// <remarks>
    /// The current callers primarily use these helpers when validating S3-backed Ride capabilities and when generating
    /// AWS Signature Version 4 authorization headers for Lex requests.
    /// See: https://docs.aws.amazon.com/general/latest/gr/signature-version-4.html
    /// </remarks>
    public static class RideAWSUtils
    {
        /// <summary>
        /// Checks whether a named Ride capability is available by requesting a signed URL from the AWS file-storage system and then
        /// probing the returned resource.
        /// </summary>
        /// <param name="behaviour">Behaviour used to run the internal web-request coroutine.</param>
        /// <param name="capability">Capability object name or key to query from the <c>ride-capabilities</c> bucket.</param>
        /// <param name="onComplete">
        /// Callback invoked with <c>"success"</c> when the capability probe succeeds, or <c>null</c> when the signed URL cannot be
        /// generated or the request fails.
        /// </param>
        /// <returns>
        /// Coroutine that waits for the signed URL lookup and capability request to complete before invoking <paramref name="onComplete"/>.
        /// </returns>
        public static IEnumerator CheckCapability(UnityEngine.MonoBehaviour behaviour, string capability, Action<string> onComplete)
        {
            // "valid-key"
            // "cache-enabled"

            var configSystem = Systems.Get<ConfigurationSystemUnity>();
            string cognitoIdentityPoolId = configSystem.GetTerrainKey();  // us-west-2:00x0xxx0-000x-000x-00x0-0000000xxxxx
            string region = configSystem.GetTerrainKeyRegion();

            var aws = Systems.Get<AWS.AWSFileStorageS3System>();
            aws.m_cognitoIdentityPoolId = cognitoIdentityPoolId;
            aws.m_regionName = region;
            bool finished = false;
            string url = "";
            aws.GetSignedURL("ride-capabilities", capability, (returnedurl) =>
            {
                finished = true;

                if (string.IsNullOrEmpty(returnedurl))
                    RideLog.LogWarning("RideIO.CheckCapabilitiy() - GetSignedURL() - Error getting URL");

                url = returnedurl;
            });

            while (!finished)
                yield return new UnityEngine.WaitForEndOfFrame();

            if (string.IsNullOrEmpty(url))
            {
                onComplete?.Invoke(null);
                yield break;
            }

            string capabilitiyReturn = "";
            yield return behaviour.StartCoroutine(RideIO.Request(url, (ret) =>
            {
                if (string.IsNullOrEmpty(ret))
                    RideLog.Log($"RideIO.CheckCapabilitiy() - Request Failed: {url} - {ret}");
                else
                    capabilitiyReturn = "success";
            }));

            onComplete?.Invoke(capabilitiyReturn);
        }

        static class HttpUtility
        {
            sealed class HttpQSCollection : Dictionary<string, string>
            {
                public override string ToString()
                {
                    if (Count == 0)
                        return "";
                    var sb = new StringBuilder();
                    foreach (var (key, value) in this)
                        sb.AppendFormat($"{key}={value}&");
                    if (sb.Length > 0)
                        sb.Length--;
                    return sb.ToString();
                }
            }

            public static Dictionary<string, string> ParseQueryString(string query) => ParseQueryString(query, Encoding.UTF8);
            static Dictionary<string, string> ParseQueryString(string query, Encoding encoding)
            {
                if (query == null)
                    throw new ArgumentNullException("query");
                if (encoding == null)
                    throw new ArgumentNullException("encoding");
                if (string.IsNullOrEmpty(query) || (query.Length == 1 && query[0] == '?'))
                    return new HttpQSCollection();
                if (query [0] == '?')
                    query = query[1..];

                var result = new HttpQSCollection();
                ParseQueryString(query, encoding, result);
                return result;
            }

            static void ParseQueryString(string query, Encoding encoding, Dictionary<string, string> result)
            {
                if (string.IsNullOrEmpty(query))
                    return;

                var decodedQuery = WebUtility.HtmlDecode(query);
                var pairs = decodedQuery.Split('&', StringSplitOptions.RemoveEmptyEntries);

                foreach (var pair in pairs)
                {
                    var keyValue = pair.Split('=', 2);
                    var name = WebUtility.UrlDecode(keyValue[0]);
                    var value = keyValue.Length > 1 ? WebUtility.UrlDecode(keyValue[1]) : string.Empty;

                    if (!result.ContainsKey(name))
                        result[name] = value;
                }
            }
        }

        /// <summary>
        /// Builds the AWS Signature Version 4 <c>Authorization</c> header value for an HTTP request.
        /// </summary>
        /// <param name="uri">Full request URI that will be sent to AWS, including any query-string parameters.</param>
        /// <param name="content">Serialized request body used as the payload when hashing and signing the request.</param>
        /// <param name="httpMethod">HTTP method used by the request, such as <see cref="HttpMethod.Get"/>, <see cref="HttpMethod.Post"/>, or <see cref="HttpMethod.Put"/>.</param>
        /// <param name="awsRegion">AWS region used in the credential scope, such as <c>us-west-2</c>.</param>
        /// <param name="awsAccessKey">AWS access key identifier included in the generated credential scope.</param>
        /// <param name="awsSecretKey">AWS secret key used to derive the Signature Version 4 signing key.</param>
        /// <param name="awsHeaders">
        /// Request headers that participate in canonicalization and signing, typically including at least <c>host</c> and <c>x-amz-date</c>.
        /// </param>
        /// <param name="awsDateTime">Timestamp used for signing, formatted as <c>yyyyMMddTHHmmssZ</c>.</param>
        /// <param name="awsDate">Date portion of the signing timestamp, formatted as <c>yyyyMMdd</c>.</param>
        /// <param name="awsServiceName">AWS service name used in the credential scope, such as <c>lex</c>.</param>
        /// <returns>
        /// Fully formatted value for the outgoing HTTP <c>Authorization</c> header, including the credential scope, signed headers list,
        /// and computed request signature.
        /// </returns>
        /// <remarks>
        /// This helper follows AWS Signature Version 4 canonical-request and string-to-sign rules.
        /// See: https://docs.aws.amazon.com/general/latest/gr/sigv4_signing.html
        /// </remarks>
        public static string GetAWSAuthorizationHeader(string uri, string content, HttpMethod httpMethod, string awsRegion, string awsAccessKey, string awsSecretKey, Dictionary<string, string> awsHeaders, string awsDateTime, string awsDate, string awsServiceName)
        {
            // Sign request per https://docs.aws.amazon.com/general/latest/gr/sigv4_signing.html

            var req = new HttpRequestMessage(httpMethod, uri);

            // Task 1: Create a canonical request

            var canRequest = new StringBuilder();

            // Step 1: HTTP request method

            canRequest.Append(httpMethod + "\n");

            // Step 2: Canonical URI parameters

            canRequest.Append(string.Join("/", req.RequestUri.AbsolutePath.Split('/').Select(Uri.EscapeDataString)) + "\n");

            // Step 3: Canonical URI query string

            var values = new SortedDictionary<string, string>();
            var querystring = HttpUtility.ParseQueryString(req.RequestUri.Query);
            var keys = querystring.Keys;
            foreach (var key in keys)
            {
                if (key == null)
                    values.Add(Uri.EscapeDataString(querystring[key]), Uri.EscapeDataString(querystring[key]) + "=");
                else
                    values.Add(Uri.EscapeDataString(key), Uri.EscapeDataString(key) + "= " + Uri.EscapeDataString(querystring[key]));
            }
            canRequest.Append(string.Join("&", values.Select(a => a.Value)) + "\n");

            // Step 4: Canonical headers

            foreach (var header in awsHeaders)
                req.Headers.Add(header.Key, header.Value);

            var headers = new List<string>();
            foreach (var header in req.Headers.OrderBy(a => a.Key.ToLower()))
            {
                canRequest.Append(header.Key.ToLower());
                canRequest.Append(":");
                canRequest.Append(string.Join(",", header.Value.Select(s => s.Trim())));
                canRequest.Append("\n");
                headers.Add(header.Key.ToLower());
            }
            canRequest.Append("\n");

            // Step 5: Signed headers

            var signedHeaders = string.Join(";", headers);
            canRequest.Append(signedHeaders + "\n");

            // Step 6: Hashed payload

            canRequest.Append(GetHash(content));

            // Step 7: Finished canonical string done along the way
            // Step 8: Hash canonical request

            var canRequestHash = GetHash(canRequest.ToString());

            // Task 2: Create string to sign

            string stringToSign = "AWS4-HMAC-SHA256" + "\n" + awsDateTime + "\n" + awsDate + "/" + awsRegion + "/" + awsServiceName + "/aws4_request" + "\n" + canRequestHash;

            // Task 3: Calculate the signature

            var dateKey = GetKeyedHash(Encoding.UTF8.GetBytes("AWS4" + awsSecretKey), awsDate);
            var dateRegionKey = GetKeyedHash(dateKey, awsRegion);
            var dateRegionServiceKey = GetKeyedHash(dateRegionKey, awsServiceName);
            var signingKey = GetKeyedHash(dateRegionServiceKey, "aws4_request");

            var signature = GetStringFromHash(GetKeyedHash(signingKey, stringToSign.ToString()));
            var credentialScope = awsDate + "/" + awsRegion + "/" + awsServiceName + "/aws4_request";
            return "AWS4-HMAC-SHA256 Credential=" + awsAccessKey + "/" + credentialScope + ", SignedHeaders=" + signedHeaders + ", Signature=" + signature;
        }

        static string GetHash(string data) => GetStringFromHash(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(data)));
        static byte[] GetKeyedHash(byte[] key, string content) => new HMACSHA256(key).ComputeHash(Encoding.UTF8.GetBytes(content));

        static string GetStringFromHash(byte[] content)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < content.Length; i++)
                sb.Append(content[i].ToString("x2"));

            return sb.ToString();
        }
    }
}
