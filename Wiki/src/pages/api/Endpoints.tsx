import { CodeBlock } from '../../components/CodeBlock'
import { HeaderAnchor } from '../../components/HeaderAnchor'

export default function Endpoints() {
  const getUsersResponse = `{
  "success": true,
  "data": {
    "users": [
      {
        "id": "123",
        "name": "张三",
        "email": "zhangsan@example.com",
        "created_at": "2023-01-01T00:00:00Z"
      }
    ],
    "total": 1,
    "page": 1,
    "limit": 10
  },
  "message": "获取用户列表成功"
}`

  const createUserRequest = `POST /api/v1/users
{
  "name": "李四",
  "email": "lisi@example.com",
  "password": "secure_password"
}`

  const createUserResponse = `{
  "success": true,
  "data": {
    "user": {
      "id": "456",
      "name": "李四",
      "email": "lisi@example.com",
      "created_at": "2023-12-01T12:00:00Z"
    }
  },
  "message": "用户创建成功"
}`

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold text-gray-900 mb-4">API 端点</h1>
        <p className="text-lg text-gray-600">
          详细的API端点文档，包含请求参数和响应格式
        </p>
      </div>

      <div className="space-y-8">
        <section id="user-endpoints">
          <HeaderAnchor level={2} id="user-endpoints" className="text-2xl font-semibold text-gray-900 mb-4">
            用户管理
          </HeaderAnchor>
          
          <div className="space-y-6">
            <div className="bg-white p-6 rounded-lg border border-gray-200">
              <div className="flex items-center mb-4">
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800 mr-3">
                  GET
                </span>
                <code className="text-sm text-gray-900">/api/v1/users</code>
              </div>
              <p className="text-gray-600 mb-4">
                获取用户列表，支持分页和筛选。
              </p>
              
              <h4 className="font-semibold text-gray-900 mb-2">查询参数</h4>
              <div className="overflow-x-auto mb-4">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">参数</th>
                      <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">类型</th>
                      <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">必需</th>
                      <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">描述</th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    <tr>
                      <td className="px-4 py-2 text-sm font-mono text-gray-900">page</td>
                      <td className="px-4 py-2 text-sm text-gray-500">integer</td>
                      <td className="px-4 py-2 text-sm text-gray-500">否</td>
                      <td className="px-4 py-2 text-sm text-gray-500">页码，默认为1</td>
                    </tr>
                    <tr>
                      <td className="px-4 py-2 text-sm font-mono text-gray-900">limit</td>
                      <td className="px-4 py-2 text-sm text-gray-500">integer</td>
                      <td className="px-4 py-2 text-sm text-gray-500">否</td>
                      <td className="px-4 py-2 text-sm text-gray-500">每页数量，默认为10</td>
                    </tr>
                    <tr>
                      <td className="px-4 py-2 text-sm font-mono text-gray-900">search</td>
                      <td className="px-4 py-2 text-sm text-gray-500">string</td>
                      <td className="px-4 py-2 text-sm text-gray-500">否</td>
                      <td className="px-4 py-2 text-sm text-gray-500">搜索关键词</td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <h4 className="font-semibold text-gray-900 mb-2">响应示例</h4>
              <CodeBlock code={getUsersResponse} language="json" />
            </div>

            <div className="bg-white p-6 rounded-lg border border-gray-200">
              <div className="flex items-center mb-4">
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800 mr-3">
                  POST
                </span>
                <code className="text-sm text-gray-900">/api/v1/users</code>
              </div>
              <p className="text-gray-600 mb-4">
                创建新用户。
              </p>

              <h4 className="font-semibold text-gray-900 mb-2">请求示例</h4>
              <CodeBlock code={createUserRequest} language="json" />

              <h4 className="font-semibold text-gray-900 mb-2 mt-6">响应示例</h4>
              <CodeBlock code={createUserResponse} language="json" />
            </div>
          </div>
        </section>

        <section id="error-codes">
          <HeaderAnchor level={2} id="error-codes" className="text-2xl font-semibold text-gray-900 mb-4">
            状态码
          </HeaderAnchor>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    状态码
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    描述
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    含义
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    <code>200</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">OK</td>
                  <td className="px-6 py-4 text-sm text-gray-500">请求成功</td>
                </tr>
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    <code>201</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">Created</td>
                  <td className="px-6 py-4 text-sm text-gray-500">资源创建成功</td>
                </tr>
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    <code>400</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">Bad Request</td>
                  <td className="px-6 py-4 text-sm text-gray-500">请求参数错误</td>
                </tr>
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    <code>401</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">Unauthorized</td>
                  <td className="px-6 py-4 text-sm text-gray-500">未认证或认证失败</td>
                </tr>
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    <code>404</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">Not Found</td>
                  <td className="px-6 py-4 text-sm text-gray-500">资源不存在</td>
                </tr>
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    <code>500</code>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">Internal Server Error</td>
                  <td className="px-6 py-4 text-sm text-gray-500">服务器内部错误</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </div>
  )
}