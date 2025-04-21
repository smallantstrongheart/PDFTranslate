using System.Collections.Generic;
using System.Threading.Tasks;

namespace PDFTranslate.Interfaces
{
    // 文档写入选项
    public class DocumentWritingOptions
    {
        public string FontName { get; set; } = "Microsoft YaHei UI"; // 默认中文字体
    }

    // 文档写入接口
    public interface IDocumentWriter
    {
        Task WriteDocumentAsync(string outputFilePath, IEnumerable<string> contentToWrite, DocumentWritingOptions options = null);
    }
}