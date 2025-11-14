using OcrWorker.Exceptions;
using Tesseract;

namespace OcrWorker.Services
{
    public interface IDocumentExtractorService
    {
        public string ExtractDocument(string localPath);
    }

    public class TesseractService : IDocumentExtractorService
    {
        private readonly ILogger<TesseractService> _logger;

        public TesseractService(ILogger<TesseractService> logger)
        {
            _logger = logger;
        }

        public string ExtractDocument(string localPath)
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

            // Convert PDF to PNG
            if (Path.GetExtension(fileName) == ".pdf")
            {
                localPath = PdfToPngConverter(localPath);
            }

            // OCR Processing
            try
            {
                using var engine = new TesseractEngine("/usr/share/tesseract-ocr/5/tessdata", "eng", EngineMode.Default);
                using var img = Pix.LoadFromFile(localPath);
                using var page = engine.Process(img);
                string text = page.GetText();

                if (String.IsNullOrEmpty(text))
                {
                    _logger.LogError($"Failed to extract text from {fileName}");
                    throw new Exception($"Failed to extract text from {fileName}");
                }

                _logger.LogInformation($"Extracted text from {fileName}");
                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to convert image to text ({fileName})");
                throw new ImageToTextConverterException(fileName);
            }
        }

        private string PdfToPngConverter(string localPath)
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
                    _logger.LogError("Ghostscript failed to convert PDF to image");
                    throw new GhostscriptPdfToImageConverterException(Path.GetFileName(localPath));
                }
            }

            _logger.LogInformation($"Converted pdf to image");
            // Pick the first page for OCR (can be extended to multi-page)
            return $"{outputBase}-001.png";
        }
    }
}
