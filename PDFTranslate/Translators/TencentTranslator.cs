using PDFTranslate.Interfaces;
using System;
using System.Threading.Tasks;
using TencentCloud.Common;
using TencentCloud.Common.Profile;
using TencentCloud.Tmt.V20180321; // 确认这个版本号是否最新
using TencentCloud.Tmt.V20180321.Models;

namespace PDFTranslate.Translators
{
    public class TencentTranslator : ITranslator
    {
        private readonly string _secretId;
        private readonly string _secretKey;
        private readonly string _region;

        public string Name => "腾讯云翻译 (Tencent Cloud)";

        public TencentTranslator(string secretId, string secretKey, string region)
        {
            if (string.IsNullOrWhiteSpace(secretId)) throw new ArgumentNullException(nameof(secretId), "SecretId 不能为空。");
            if (string.IsNullOrWhiteSpace(secretKey)) throw new ArgumentNullException(nameof(secretKey), "SecretKey 不能为空。");
            if (string.IsNullOrWhiteSpace(region)) throw new ArgumentNullException(nameof(region), "区域信息不能为空。");

            _secretId = secretId;
            _secretKey = secretKey;
            _region = region;
        }

        public async Task<string> TranslateAsync(string textToTranslate, string sourceLanguage, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(textToTranslate)) return string.Empty;
            // 腾讯云的语言代码可能略有不同，需查文档确认
            // 常用：en - 英语, zh - 简体中文
            if (string.IsNullOrWhiteSpace(sourceLanguage) || string.IsNullOrWhiteSpace(targetLanguage))
                throw new ArgumentException("源语言和目标语言代码不能为空。");

            try
            {
                Credential cred = new Credential { SecretId = _secretId, SecretKey = _secretKey };

                ClientProfile clientProfile = new ClientProfile();
                HttpProfile httpProfile = new HttpProfile { Endpoint = "tmt.tencentcloudapi.com" };
                // httpProfile.ReqTimeout = 30; // 可选：设置超时（秒）
                clientProfile.HttpProfile = httpProfile;

                TmtClient client = new TmtClient(cred, _region, clientProfile);

                TextTranslateRequest req = new TextTranslateRequest
                {
                    SourceText = textToTranslate,
                    Source = sourceLanguage, // 例如 "en"
                    Target = targetLanguage, // 例如 "zh"
                    ProjectId = 0
                };

                TextTranslateResponse resp = await client.TextTranslate(req);
                return resp.TargetText ?? string.Empty;
            }
            catch (TencentCloudSDKException e)
            {
                Console.Error.WriteLine($"腾讯云翻译 API 错误: Code={e.ErrorCode}, Msg={e.Message}, RequestId={e.RequestId}");
                string errorMsg = $"腾讯云翻译失败: {e.Message}";
                // 根据错误码提供更友好的提示
                switch (e.ErrorCode)
                {
                    case "AuthFailure.SignatureFailure":
                    case "AuthFailure.SecretIdNotFound":
                        errorMsg = "腾讯云认证失败，请检查 SecretId 和 SecretKey。"; break;
                    case "LimitExceeded":
                    case "RequestLimitExceeded":
                        errorMsg = "腾讯云调用超限（频率或额度），请稍后再试或检查账户。"; break;
                    case "UnsupportedOperation.UnsupportedLanguage":
                        errorMsg = "腾讯云不支持所选的源语言或目标语言。"; break;
                    case "FailedOperation.NoFreeAmount":
                        errorMsg = "腾讯云免费额度已用完。"; break;
                    case "FailedOperation.ServiceIsolate":
                        errorMsg = "腾讯云账户欠费或服务被隔离。"; break;
                }
                throw new Exception(errorMsg, e); // 包装原始异常
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"调用腾讯云翻译时发生意外错误: {e.ToString()}");
                throw new Exception("调用腾讯云翻译时发生意外错误。", e);
            }
        }
    }
}