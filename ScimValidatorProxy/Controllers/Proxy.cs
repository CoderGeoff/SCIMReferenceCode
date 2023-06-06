using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace ScimValidatorProxy.Controllers
{
    public interface IProxy
    {
        Task ForwardAsync(HttpContext context);
    }

    public class Proxy : IProxy
    {
        private readonly string targetPort;
        private readonly HttpClient httpClient = new HttpClient();

        public Proxy(string targetPort)
        {
            this.targetPort = targetPort;
        }

        public async Task ForwardAsync(HttpContext context)
        {
            var originalRequest = context.Request;
            var targetUri = BuildTargetUri(originalRequest);

            if (targetUri != null)
            {
                var targetRequestMessage = CreateTargetMessage(context, targetUri);
                using var streamReader = new StreamReader(originalRequest.Body);
                var content = await streamReader.ReadToEndAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    var (encoding, mediaType) = ToEncodingAndMediaType(originalRequest.ContentType);
                    var correctedContent = Correct(content);
                    targetRequestMessage.Content = new StringContent(correctedContent, encoding, mediaType);
                }

                Console.WriteLine(ForwardingMessage(originalRequest, targetRequestMessage));

                using var responseMessage = await httpClient.SendAsync(targetRequestMessage,
                    HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
                context.Response.StatusCode = (int)responseMessage.StatusCode;
                CopyFromTargetResponseHeaders(context, responseMessage);
                await responseMessage.Content.CopyToAsync(context.Response.Body);
            }
        }

        private string Correct(string content)
        {
            var regex = new Regex("\\\"primary\\\": *\\\"[0-9\\-\\(\\)x\\. ]*\\\"");
            var corrected = regex.Replace(content, "\"primary\": false");
            return corrected;
        }

        private static (Encoding encoding, string mediaType) ToEncodingAndMediaType(string contentType)
        {
            var parts = contentType.Split(";").Select(s => s.Trim()).ToArray();
            var mediaType = string.Join("; ", parts[..^1]);
            var encodingAsString = parts[^1].ToUpper().Replace("CHARSET=", "");
            var encoding = Encoding.GetEncoding(encodingAsString);
            return (encoding, mediaType);
        }

        private static string ForwardingMessage(HttpRequest originalRequest, HttpRequestMessage forwardedRequest)
        {
            var s = new StringBuilder();
            s.AppendLine("Forwarding")
             .AppendLine(originalRequest.GetDisplayUrl());
            AppendHeaders(s, originalRequest.Headers.Select(entry => KeyValuePair.Create(entry.Key, entry.Value.AsEnumerable())))
               .AppendLine("  to")
               .AppendLine($"{forwardedRequest.Method} {forwardedRequest.RequestUri}");
            AppendHeaders(s, forwardedRequest.Headers)
               .AppendLine("");
            return s.ToString();
        }

        private static StringBuilder AppendHeaders(StringBuilder s, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            foreach (var header in headers)
            {
                s.Append(header.Key).Append(" ").AppendJoin(", ", header.Value).AppendLine("");
            }

            return s;
        }

        private HttpRequestMessage CreateTargetMessage(HttpContext context, Uri targetUri)
        {
            var requestMessage = new HttpRequestMessage();
            CopyFromOriginalRequestContentAndHeaders(context, requestMessage);

            requestMessage.RequestUri = targetUri;
            requestMessage.Headers.Host = targetUri.Host;
            requestMessage.Method = GetMethod(context.Request.Method);

            return requestMessage;
        }

        private void CopyFromOriginalRequestContentAndHeaders(HttpContext context, HttpRequestMessage requestMessage)
        {
            var requestMethod = context.Request.Method;

            if (!HttpMethods.IsGet(requestMethod) &&
              !HttpMethods.IsHead(requestMethod) &&
              !HttpMethods.IsDelete(requestMethod) &&
              !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(context.Request.Body);
                requestMessage.Content = streamContent;
            }

            foreach (var header in context.Request.Headers)
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        private void CopyFromTargetResponseHeaders(HttpContext context, HttpResponseMessage responseMessage)
        {
            foreach (var header in responseMessage.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
            context.Response.Headers.Remove("transfer-encoding");
        }

        private static HttpMethod GetMethod(string method)
        {
            if (HttpMethods.IsDelete(method))
                return HttpMethod.Delete;
            if (HttpMethods.IsGet(method))
                return HttpMethod.Get;
            if (HttpMethods.IsHead(method))
                return HttpMethod.Head;
            if (HttpMethods.IsOptions(method))
                return HttpMethod.Options;
            if (HttpMethods.IsPost(method))
                return HttpMethod.Post;
            if (HttpMethods.IsPut(method))
                return HttpMethod.Put;
            if (HttpMethods.IsTrace(method))
                return HttpMethod.Trace;
            return new HttpMethod(method);
        }

        private Uri BuildTargetUri(HttpRequest request)
        {
            return new Uri($"https://localhost:{targetPort}{request.Path}");
        }
    }
}