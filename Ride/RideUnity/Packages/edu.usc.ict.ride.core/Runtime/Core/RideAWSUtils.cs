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
    public static class RideAWSUtils
    {
        public static IEnumerator CheckCapability(UnityEngine.MonoBehaviour behaviour, string capability, Action<string> onComplete)
        {
            // "valid-key"
            // "cache-enabled"

            var configSystem = Globals.api.GetSystem<ConfigurationSystemUnity>();
            string cognitoIdentityPoolId = configSystem.GetTerrainKey();  // us-west-2:00x0xxx0-000x-000x-00x0-0000000xxxxx
            string region = configSystem.GetTerrainKeyRegion();

            var aws = Globals.api.GetSystem<AWS.AWSFileStorageS3System>();
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
                {
                    RideLog.Log($"RideIO.CheckCapabilitiy() - Request Failed: {url} - {ret}");
                }
                else
                {
                    capabilitiyReturn = "success";
                }
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
        /// Calculates authorization header for AWS requests.
        /// </summary>
        /// <param name="uri">URI to send request to</param>
        /// <param name="content">Content to be sent</param>
        /// <param name="httpMethod">HTPP method (e.g., POST, PUT)</param>
        /// <param name="awsRegion">AWS region</param>
        /// <param name="awsAccessKey">Access key</param>
        /// <param name="awsSecretKey">Secret key</param>
        /// <param name="awsHeaders">Headers used in the request, both keys and values (e.g., host, x-amz-date)</param>
        /// <param name="awsDateTime">Date and time, in form yyyyMMddTHHmmssZ</param>
        /// <param name="awsDate">Date, in form yyyyMMdd</param>
        /// <param name="awsServiceName">AWS service request is sent to (e.g., lex)</param>
        /// <returns>Value for authorization request header</returns>
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
            {
                req.Headers.Add(header.Key, header.Value);
            }

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

        static string GetHash(string data)
        {
            return GetStringFromHash(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(data)));
        }

        static byte[] GetKeyedHash(byte[] key, string content)
        {
            return new HMACSHA256(key).ComputeHash(Encoding.UTF8.GetBytes(content));
        }

        static string GetStringFromHash(byte[] content)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < content.Length; i++)
            {
                sb.Append(content[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
