using GatherUp.API;
using GatherUp.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GatherUp.UnitTests
{
    public class GlobalExceptionMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_NotFoundException_Returns404()
        {
            // Arrange
            RequestDelegate next = (ctx) => throw new NotFoundException("Not found test");
            var middleware = new GlobalExceptionMiddleware(next);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(404, context.Response.StatusCode);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var body = await reader.ReadToEndAsync();
            Assert.Contains("Not found test", body);
        }

        [Fact]
        public async Task InvokeAsync_BusinessException_Returns400()
        {
            RequestDelegate next = (ctx) => throw new BusinessException("Bad request test");
            var middleware = new GlobalExceptionMiddleware(next);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            Assert.Equal(400, context.Response.StatusCode);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var body = await reader.ReadToEndAsync();
            Assert.Contains("Bad request test", body);
        }

        [Fact]
        public async Task InvokeAsync_GenericException_Returns500()
        {
            RequestDelegate next = (ctx) => throw new System.Exception("Boom");
            var middleware = new GlobalExceptionMiddleware(next);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            Assert.Equal(500, context.Response.StatusCode);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var body = await reader.ReadToEndAsync();
            Assert.Contains("An unexpected error occurred", body);
        }
    }
}
