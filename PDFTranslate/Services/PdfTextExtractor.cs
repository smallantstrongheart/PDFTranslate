using PDFTranslate.Interfaces;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PDFTranslate.Services
{
    public class PdfTextExtractor : IPdfTextExtractor
    {
        public PdfTextExtractor()
        {
            // 确保注册编码提供程序，尤其在 .NET Core/.NET 5+
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public Task<List<string>> ExtractTextPerPageAsync(string pdfFilePath)
        {
            // PdfSharp 操作是同步的，用 Task.Run 包装以符合异步接口
            return Task.Run(() =>
            {
                List<string> pagesText = new List<string>();
                try
                {
                    using (PdfDocument document = PdfReader.Open(pdfFilePath, PdfDocumentOpenMode.ReadOnly))
                    {
                        for (int i = 0; i < document.PageCount; i++)
                        {
                            PdfPage page = document.Pages[i];
                            string pageContent = ExtractTextFromPage(page);
                            pagesText.Add(pageContent);
                        }
                    }
                }
                catch (PdfReaderException ex)
                {
                    throw new Exception($"读取 PDF 文件时出错: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"提取 PDF 文本时发生意外错误: {ex.Message}", ex);
                }
                return pagesText;
            });
        }

        // 从单个 PDF 页面提取文本 (简化实现)
        private string ExtractTextFromPage(PdfPage page)
        {
            try
            {
                // 尝试读取页面内容流
                var content = ContentReader.ReadContent(page);
                if (content == null) return string.Empty;
                // 从内容流中提取文本
                var text = ExtractTextFromContentStream(content);
                return text;
            }
            catch (Exception ex)
            {
                // 记录页面处理错误但继续
                Console.Error.WriteLine($"处理 PDF 页面时出错: {ex.Message}");
                return string.Empty; // 返回空表示此页处理失败
            }
        }

        // 从内容流序列中提取文本
        private string ExtractTextFromContentStream(CSequence content)
        {
            StringBuilder pageText = new StringBuilder();
            foreach (var element in content)
            {
                if (element is COperator op)
                {
                    ExtractTextFromOperator(op, pageText);
                }
                else if (element is CSequence subSequence)
                {
                    // 递归处理子序列 (不常见)
                    pageText.Append(ExtractTextFromContentStream(subSequence));
                }
            }
            // 清理文本，合并多余的换行和空格
            string result = pageText.ToString();
            result = System.Text.RegularExpressions.Regex.Replace(result, @"(\r\n|\n|\r){2,}", Environment.NewLine);
            result = System.Text.RegularExpressions.Regex.Replace(result, @" {2,}", " ");
            return result.Trim();
        }

        // 从 PDF 操作符中提取文本
        private void ExtractTextFromOperator(COperator op, StringBuilder currentText)
        {
            if (op.OpCode.Name == "Tj" || op.OpCode.Name == "'") // Show Text
            {
                if (op.Operands.Count > 0 && op.Operands[0] is CString pdfString)
                {
                    currentText.Append(DecodePdfString(pdfString));
                }
                if (op.OpCode.Name == "'") // ' also moves to next line
                {
                    currentText.AppendLine();
                }
            }
            else if (op.OpCode.Name == "TJ") // Show Text with Adjustments
            {
                if (op.Operands.Count > 0 && op.Operands[0] is CSequence array)
                {
                    foreach (var item in array)
                    {
                        if (item is CString pdfStr)
                        {
                            currentText.Append(DecodePdfString(pdfStr));
                        }
                        // Ignore numeric kerning values
                    }
                }
            }
            else if (op.OpCode.Name == "T*") // Move to start of next line
            {
                // 确保不在行首或已有换行符后添加重复换行
                if (currentText.Length > 0 && !currentText.ToString().EndsWith(Environment.NewLine))
                {
                    currentText.AppendLine();
                }
            }
            // 其他操作符 Td, TD 等也可以用于启发式换行判断，但 T* 和 ' 更直接
        }

        // 解码 PDF 字符串 (PdfSharp 通常自动处理)
        private string DecodePdfString(CString pdfString)
        {
            return pdfString.Value ?? string.Empty;
        }
    }
}
