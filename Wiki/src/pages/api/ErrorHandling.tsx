import { CodeBlock } from '../../components/CodeBlock'
import { HeaderAnchor } from '../../components/HeaderAnchor'

export default function ErrorHandling() {
  const validationErrorCode = `{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "请求参数验证失败",
    "details": {
      "email": ["邮箱格式不正确"],
      "password": ["密码长度至少为8位"]
    }
  },
  "timestamp": "2023-12-01T12:00:00Z"
}`

  const rateLimitErrorCode = `{
  "success": false,
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "请求频率超限",
    "retry_after": 60
  },
  "timestamp": "2023-12-01T12:00:00Z"
}`

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold text-gray-900 mb-4">错误处理</h1>
        <p className="text-lg text-gray-600">
          了解API返回的错误类型和处理方法
        </p>
      </div>

      <div className="space-y-6">
        <section id="error-response-format">
          <HeaderAnchor level={2} id="error-response-format" className="text-xl font-semibold text-gray-900 mb-3">
            错误格式
          </HeaderAnchor>
          <p className="text-gray-600 mb-4">
            当API请求失败时，服务器会返回统一的错误格式：
          </p>
          <div className="bg-white p-6 rounded-lg border border-gray-200">
            <h3 className="font-semibold text-gray-900 mb-2">错误响应结构</h3>
            <ul className="space-y-2 text-gray-600">
              <li><code className="bg-gray-100 px-2 py-1 rounded">success</code> - 始终为 <code>false</code></li>
              <li><code className="bg-gray-100 px-2 py-1 rounded">error.code</code> - 错误代码</li>
              <li><code className="bg-gray-100 px-2 py-1 rounded">error.message</code> - 错误消息</li>
              <li><code className="bg-gray-100 px-2 py-1 rounded">error.details</code> - 详细错误信息（可选）</li>
              <li><code className="bg-gray-100 px-2 py-1 rounded">timestamp</code> - 错误发生时间</li>
            </ul>
          </div>
        </section>

        <section id="error-codes">
          <HeaderAnchor level={2} id="error-codes" className="text-xl font-semibold text-gray-900 mb-3">
            常见错误代码
          </HeaderAnchor>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    错误代码
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    HTTP状态码
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    描述
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-900">
                    <code>VALIDATION_ERROR</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    <code>400</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    请求参数验证失败
                  </td>
                </tr>
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-900">
                    <code>UNAUTHORIZED</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    <code>401</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    未认证或认证失败
                  </td>
                </tr>
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-900">
                    <code>FORBIDDEN</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    <code>403</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    权限不足
                  </td>
                </tr>
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-900">
                    <code>NOT_FOUND</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    <code>404</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    资源不存在
                  </td>
                </tr>
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-900">
                    <code>RATE_LIMIT_EXCEEDED</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    <code>429</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    请求频率超限
                  </td>
                </tr>
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-900">
                    <code>INTERNAL_ERROR</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    <code>500</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    服务器内部错误
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <section>
          <h2 className="text-xl font-semibold text-gray-900 mb-3">错误示例</h2>
          <div className="space-y-6">
            <div>
              <h3 className="text-lg font-medium text-gray-900 mb-2">验证错误</h3>
              <p className="text-gray-600 mb-2">
                当请求参数不符合要求时返回：
              </p>
              <CodeBlock code={validationErrorCode} language="json" />
            </div>

            <div>
              <h3 className="text-lg font-medium text-gray-900 mb-2">频率限制</h3>
              <p className="text-gray-600 mb-2">
                当请求频率超过限制时返回：
              </p>
              <CodeBlock code={rateLimitErrorCode} language="json" />
            </div>
          </div>
        </section>

        <section>
          <h2 className="text-xl font-semibold text-gray-900 mb-3">错误处理建议</h2>
          <div className="bg-white p-6 rounded-lg border border-gray-200">
            <ul className="space-y-3 text-gray-600">
              <li className="flex items-start">
                <span className="text-green-500 mr-2">✓</span>
                <span>始终检查响应的 <code className="bg-gray-100 px-1 rounded">success</code> 字段</span>
              </li>
              <li className="flex items-start">
                <span className="text-green-500 mr-2">✓</span>
                <span>根据错误代码显示用户友好的错误消息</span>
              </li>
              <li className="flex items-start">
                <span className="text-green-500 mr-2">✓</span>
                <span>实现重试机制处理临时错误</span>
              </li>
              <li className="flex items-start">
                <span className="text-green-500 mr-2">✓</span>
                <span>记录错误信息以便调试</span>
              </li>
              <li className="flex items-start">
                <span className="text-green-500 mr-2">✓</span>
                <span>对于频率限制错误，等待指定时间后重试</span>
              </li>
            </ul>
          </div>
        </section>
      </div>
    </div>
  )
}