import { CodeBlock } from '../../components/CodeBlock'
import { HeaderAnchor } from '../../components/HeaderAnchor'

export default function ApiOverview() {
  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold text-foreground mb-4">API 概览</h1>
        <p className="text-lg text-muted-foreground">
          了解我们的REST API架构、版本控制和使用约定
        </p>
      </div>

      <div className="space-y-6">
        <section id="base-url-auth">
          <HeaderAnchor level={2} id="base-url-auth" className="text-xl font-semibold text-foreground mb-3">
            基础信息
          </HeaderAnchor>
          <div className="cholopols-card p-6">
            <dl className="space-y-4">
              <div>
                <dt className="text-sm font-medium text-muted-foreground">基础URL</dt>
                <dd className="mt-1 text-sm text-foreground">
                  <code className="bg-muted px-2 py-1 rounded">https://api.example.com/v1</code>
                </dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-muted-foreground">认证方式</dt>
                <dd className="mt-1 text-sm text-foreground">Bearer Token</dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-muted-foreground">数据格式</dt>
                <dd className="mt-1 text-sm text-foreground">JSON</dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-muted-foreground">API版本</dt>
                <dd className="mt-1 text-sm text-foreground">v1.0.0</dd>
              </div>
            </dl>
          </div>
        </section>

        <section id="http-methods">
          <HeaderAnchor level={2} id="http-methods" className="text-xl font-semibold text-foreground mb-3">
            HTTP方法
          </HeaderAnchor>
          <div className="cholopols-card p-6 overflow-x-auto">
            <table className="cholopols-table">
              <thead>
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                    方法
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                    描述
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                    用途
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-foreground">
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                      GET
                    </span>
                  </td>
                  <td className="px-6 py-4 text-sm text-muted-foreground">
                    获取资源
                  </td>
                  <td className="px-6 py-4 text-sm text-muted-foreground">
                    读取数据，不修改服务器状态
                  </td>
                </tr>
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-foreground">
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                      POST
                    </span>
                  </td>
                  <td className="px-6 py-4 text-sm text-muted-foreground">
                    创建资源
                  </td>
                  <td className="px-6 py-4 text-sm text-muted-foreground">
                    创建新资源或执行操作
                  </td>
                </tr>
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-foreground">
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
                      PUT
                    </span>
                  </td>
                  <td className="px-6 py-4 text-sm text-muted-foreground">
                    更新资源
                  </td>
                  <td className="px-6 py-4 text-sm text-muted-foreground">
                    完整更新现有资源
                  </td>
                </tr>
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-foreground">
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-orange-100 text-orange-800">
                      PATCH
                    </span>
                  </td>
                  <td className="px-6 py-4 text-sm text-muted-foreground">
                    部分更新资源
                  </td>
                  <td className="px-6 py-4 text-sm text-muted-foreground">
                    部分更新现有资源
                  </td>
                </tr>
                <tr>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-foreground">
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800">
                      DELETE
                    </span>
                  </td>
                  <td className="px-6 py-4 text-sm text-muted-foreground">
                    删除资源
                  </td>
                  <td className="px-6 py-4 text-sm text-muted-foreground">
                    删除现有资源
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <section id="response-format">
          <HeaderAnchor level={2} id="response-format" className="text-xl font-semibold text-foreground mb-3">
            响应格式
          </HeaderAnchor>
          <div className="cholopols-card p-6">
            <p className="text-muted-foreground mb-4">
              所有API响应都遵循统一的JSON格式：
            </p>
            <CodeBlock 
              code={`{
  "success": true,
  "data": {
    // 响应数据
  },
  "message": "操作成功",
  "timestamp": "2023-12-01T12:00:00Z"
}`} 
              language="json" 
            />
          </div>
        </section>
      </div>
    </div>
  )
}