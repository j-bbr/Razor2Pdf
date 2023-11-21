using System;
using System.Linq;
using System.Threading.Tasks;
using Medallion.Shell;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IO;

namespace NanoByte.Razor2Pdf
{
    /// <summary>
    /// Renders Razor Pages as PDFs.
    /// </summary>
    public class PdfRenderer : IPdfRenderer
    {
        private readonly IRazorViewRenderer _renderer;
        private readonly IHostingEnvironment _environment;

        public PdfRenderer(IRazorViewRenderer renderer, IHostingEnvironment environment)
        {
            _renderer = renderer;
            _environment = environment;
        }

        private static readonly RecyclableMemoryStreamManager _streamManager = new();

        public async Task<FileStreamResult> RenderAsync<T>(string viewPath, T model, params string[] weasyprintArgs)
        {
            string html = await _renderer.RenderAsync(viewPath, model);
            var stream = _streamManager.GetStream();
            weasyprintArgs ??= Array.Empty<string>();
            var result = await
                Command.Run("weasyprint",
                            weasyprintArgs.Append("-").Append("-"),
                            opts => opts.WorkingDirectory(_environment.WebRootPath))
                       .RedirectFrom(html)
                       .RedirectTo(stream)
                       .Task;
            if (!result.Success) throw new Exception(result.StandardError);
            stream.Position = 0;

            return new FileStreamResult(stream, contentType: "application/pdf");
        }

        public Task<FileStreamResult> RenderAsync(string viewPath, params string[] weasyprintArgs)
            => RenderAsync(viewPath, new object());

        public Task<FileStreamResult> RenderAsync<T>(T model, params string[] weasyprintArgs) where T : IPdfModel
            => RenderAsync(model.ViewPath, model);
    }
}
