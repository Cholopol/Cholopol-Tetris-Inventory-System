import { CodeBlock } from '../../components/CodeBlock'
import { HeaderAnchor } from '../../components/HeaderAnchor'

export default function Authentication() {
  const basicAuthCode = `curl -X GET \\
  https://api.example.com/v1/users \\
  -H "Authorization: Bearer YOUR_API_TOKEN"`

  const tokenResponseCode = `{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "def50200..."
}`

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold text-gray-900 mb-4">认证</h1>
        <p className="text-lg text-gray-600">
          了解如何使用API密钥和访问令牌进行身份验证
        </p>
      </div>

      <div className="space-y-6">
        <section id="api-key-security">
          <HeaderAnchor level={2} id="api-key-security" className="text-xl font-semibold text-gray-900 mb-3">
            API密钥
          </HeaderAnchor>
          <p className="text-gray-600 mb-4">
            所有API请求都需要在请求头中包含API密钥。你可以在账户设置页面找到你的API密钥。
          </p>
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-yellow-400" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <h3 className="text-sm font-medium text-yellow-800">安全提示</h3>
                <div className="mt-2 text-sm text-yellow-700">
                  <p>请妥善保管你的API密钥，不要将其暴露在客户端代码或公开的代码仓库中。</p>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section id="bearer-token-format">
          <HeaderAnchor level={2} id="bearer-token-format" className="text-xl font-semibold text-gray-900 mb-3">
            Bearer Token
          </HeaderAnchor>
          <p className="text-gray-600 mb-4">
            在请求头中使用Bearer token格式：
          </p>
          <CodeBlock code={basicAuthCode} language="bash" />
        </section>

        <section>
          <h2 className="text-xl font-semibold text-gray-900 mb-3">获取访问令牌</h2>
          <p className="text-gray-600 mb-4">
            使用你的API密钥来获取访问令牌：
          </p>
          <CodeBlock 
            code={`POST /auth/token
{
  "api_key": "your_api_key_here"
}`} 
            language="json" 
          />
          <p className="text-gray-600 mt-4 mb-4">
            成功的响应将包含访问令牌：
          </p>
          <CodeBlock code={tokenResponseCode} language="json" />
        </section>

        <section>
          <h2 className="text-xl font-semibold text-gray-900 mb-3">令牌刷新</h2>
          <p className="text-gray-600 mb-4">
            访问令牌过期后，可以使用刷新令牌获取新的访问令牌：
          </p>
          <CodeBlock 
            code={`POST /auth/refresh
{
  "refresh_token": "your_refresh_token_here"
}`} 
            language="json" 
          />
        </section>

        <section>
          <h2 className="text-xl font-semibold text-gray-900 mb-3">错误响应</h2>
          <p className="text-gray-600 mb-4">
            如果认证失败，API将返回401状态码：
          </p>
          <CodeBlock 
            code={`{
  "success": false,
  "error": {
    "code": "UNAUTHORIZED",
    "message": "无效的API密钥或访问令牌"
  },
  "timestamp": "2023-12-01T12:00:00Z"
}`} 
            language="json" 
          />
        </section>
      </div>
    </div>
  )
}