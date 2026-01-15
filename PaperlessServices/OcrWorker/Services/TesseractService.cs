using OcrWorker.Exceptions;
using Tesseract;

namespace OcrWorker.Services
{
    public interface IDocumentExtractorService
    {
        public Task<string> ExtractDocument(string localPath);
    }

    public class TesseractService : IDocumentExtractorService
    {
        private readonly ILogger<TesseractService> _logger;

        public TesseractService(ILogger<TesseractService> logger)
        {
            _logger = logger;
        }

        public async Task<string> ExtractDocument(string localPath)
        {
            string fileName = Path.GetFileName(localPath);

            // Check file extension
            string[] supportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff", ".pbm", ".pgm", ".ppm", ".pdf" };
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!supportedExtensions.Contains(extension))
            {
                _logger.LogError($"{extension} files are not supported. Please upload an image or pdf");
                throw new UnsupportedFileExtensionException(extension);
            }

            // OCR Processing
            try
            {
                // Convert PDF to PNG
                if (Path.GetExtension(fileName) == ".pdf")
                {
                    localPath = await ConvertPdfToPngAsync(localPath);
                }

                using var engine = new TesseractEngine("/usr/share/tesseract-ocr/5/tessdata", "eng", EngineMode.Default);
                using var img = Pix.LoadFromFile(localPath);
                using var page = engine.Process(img);
                string text = page.GetText();

                if (String.IsNullOrEmpty(text))
                {
                    throw new ImageToTextConverterException(fileName);
                }

                _logger.LogInformation($"Extracted text from {fileName}");
                return text;
            }
            catch (GhostscriptPdfToImageConverterException ex)
            {
                _logger.LogError(ex, $"Ghostscript failed to convert PDF {fileName} to image");
                throw new TesseractExtractorException(ex);
            }
            catch (ImageToTextConverterException ex)
            {
                _logger.LogError(ex, $"Failed to extract text from document {fileName}");
                throw new TesseractExtractorException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error");
                throw new TesseractExtractorException(ex);
            }
        }

        private Task<string> ConvertPdfToPngAsync(string localPath)
        {
            string outputBase = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(localPath));
            string outputPattern = $"{outputBase}-%03d.png";

            // Info
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "gs",
                Arguments = $"-dNOPAUSE -dBATCH -sDEVICE=png16m -r300 -sOutputFile=\"{outputPattern}\" \"{localPath}\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Start process
            using (var proc = System.Diagnostics.Process.Start(psi))
            {
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                {
                    string err = proc.StandardError.ReadToEnd();
                    throw new GhostscriptPdfToImageConverterException(Path.GetFileName(localPath));
                }
            }

            // Pick the first page for OCR (can be extended to multi-page)
            string imageFileName = $"{outputBase}-001.png";
            
            _logger.LogInformation($"Converted pdf to image");
            return Task.FromResult(imageFileName);
        }
    }
}
