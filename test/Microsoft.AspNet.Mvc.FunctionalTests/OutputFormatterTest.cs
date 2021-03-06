// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ContentNegotiationWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class OutputFormatterTest
    {
        private const string SiteName = nameof(ContentNegotiationWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new Startup().ConfigureServices;

        [Theory]
        [InlineData("ReturnTaskOfString")]
        [InlineData("ReturnTaskOfObject_StringValue")]
        [InlineData("ReturnString")]
        [InlineData("ReturnObject_StringValue")]
        public async Task StringOutputFormatter_ForStringValues_GetsSelectedReturnsTextPlainContentType(string actionName)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse("text/plain;charset=utf-8");
            var expectedBody = actionName;

            // Act
            var response = await client.GetAsync("http://localhost/TextPlain/" + actionName);

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Theory]
        [InlineData("ReturnTaskOfObject_ObjectValue")]
        [InlineData("ReturnObject_ObjectValue")]
        public async Task JsonOutputFormatter_ForNonStringValue_GetsSelected(string actionName)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse("application/json;charset=utf-8");

            // Act
            var response = await client.GetAsync("http://localhost/TextPlain/" + actionName);

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
        }

        [Theory]
        [InlineData("ReturnTask")]
        [InlineData("ReturnVoid")]
        public async Task NoContentFormatter_ForVoidAndTaskReturnType_GetsSelectedAndWritesResponse(string actionName)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/NoContent/" + actionName);

            // Assert
            Assert.Null(response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            // Response body is empty instead of null.
            Assert.Empty(body);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(0, response.Content.Headers.ContentLength);
        }

        [Theory]
        [InlineData("ReturnTaskOfString_NullValue")]
        [InlineData("ReturnTaskOfObject_NullValue")]
        [InlineData("ReturnObject_NullValue")]
        public async Task NoContentFormatter_ForNullValue_ByDefault_GetsSelectedAndWritesResponse(string actionName)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/NoContent/" + actionName);

            // Assert
            Assert.Null(response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            // Response body is empty instead of null.
            Assert.Empty(body);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(0, response.Content.Headers.ContentLength);
        }

        [Theory]
        [InlineData("ReturnTaskOfString_NullValue")]
        [InlineData("ReturnTaskOfObject_NullValue")]
        [InlineData("ReturnObject_NullValue")]
        public async Task
            NoContentFormatter_ForNullValue_AndTreatNullAsNoContentFlagSetToFalse_DoesNotGetSelected(string actionName)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/NoContentDoNotTreatNullValueAsNoContent/" +
                                                 actionName);

            // Assert
            Assert.Null(response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            // Response body is empty instead of null.
            Assert.Empty(body);
            Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
        }
    }
}