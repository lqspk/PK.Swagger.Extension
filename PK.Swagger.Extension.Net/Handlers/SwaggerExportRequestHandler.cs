/*
 * By PK
*/
using PK.Swagger.Extension.Net.Providers;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace PK.Swagger.Extension.Net.Handlers
{
    /// <summary>
    /// 导出请求处理者
    /// </summary>
    public class SwaggerExportRequestHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var routeData = request.GetRouteData();
            string action = routeData.Values["action"]?.ToString();
            string url = routeData.Values["url"]?.ToString().Replace("|", "/");
            if (string.IsNullOrWhiteSpace(url))
            {
                return request.CreateErrorResponse(HttpStatusCode.NotFound, "url参数无效");
            }

            url = $"{request.RequestUri.Scheme}://{request.RequestUri.Authority}/{url}";
            try
            {
                HttpContent httpContent = null;
                string fileName = Guid.NewGuid().ToString("N");
                switch (action)
                {
                    //导出为markdown文件
                    case "ToMarkdown":
                    {
                        httpContent = new ByteArrayContent(await SwaggerExportProvider.CreateMarkdownFile(url));
                        fileName += ".md";
                        break;
                    }
                    default:
                    {
                        return request.CreateErrorResponse(HttpStatusCode.NotFound, "action参数无效");
                    }
                }

                var response = new HttpResponseMessage();
                response.Content = httpContent;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName
                };

                return response;
            }
            catch (Exception e)
            {
                return request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }
    }
}
