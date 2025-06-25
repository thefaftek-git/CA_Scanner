using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;

namespace ConditionalAccessExporter.Tests.Mocks
{
    /// <summary>
    /// Mock implementation for Microsoft Graph API integration testing
    /// </summary>
    public static class MicrosoftGraphMock
    {
        /// <summary>
        /// Creates a mock HttpMessageHandler that simulates Microsoft Graph API responses
        /// </summary>
        public static Mock<HttpMessageHandler> CreateMockHandler(params JObject[] policies)
        {
            var handlerMock = new Mock<HttpMessageHandler>();

            // Setup the handler to return different responses based on the request URI
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().Contains("/conditionalAccess/policies")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(policies.Length > 0 ?
                        JArray.FromObject(policies).ToString() :
                        "[]")
                });

            // Setup for specific policy by ID
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().Contains("/conditionalAccess/policies/")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(policies.Length > 0 ?
                        policies[0].ToString() : "{}")
                });

            // Setup for creating a policy
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString().Contains("/conditionalAccess/policies")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.Created,
                    Content = new StringContent("{}")
                });

            // Setup for updating a policy
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Patch &&
                        req.RequestUri.ToString().Contains("/conditionalAccess/policies/")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent("{}")
                });

            // Setup for deleting a policy
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Delete &&
                        req.RequestUri.ToString().Contains("/conditionalAccess/policies/")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.NoContent
                });

            return handlerMock;
        }

        /// <summary>
        /// Creates a mock HttpClient with the mock handler
        /// </summary>
        public static HttpClient CreateMockHttpClient(params JObject[] policies)
        {
            var handlerMock = CreateMockHandler(policies);
            return handlerMock.Object;
        }
    }
}
