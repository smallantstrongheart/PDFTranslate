// PdfGoogleTranslatorApp/Interfaces/IPdfTextExtractor.cs
using System.Collections.Generic;
using System.Threading.Tasks;
namespace PDFTranslate.Interfaces
{
    public interface IPdfTextExtractor
    {
        Task<List<string>> ExtractTextPerPageAsync(string pdfFilePath);
    }
}