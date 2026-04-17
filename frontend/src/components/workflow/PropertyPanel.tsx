import React from 'react';
import { Card, Form, Input, Select, Button, Space, message, Alert, Divider, Tooltip, Typography } from 'antd';
import { DeleteOutlined, QuestionCircleOutlined, InfoCircleOutlined } from '@ant-design/icons';
import type { WorkflowNode, ParameterDefinition } from '../../types/workflow-template';
import { NodeType } from '../../types/workflow-template';

const { TextArea } = Input;
const { Option } = Select;
const { Text, Paragraph } = Typography;

/**
 * 获取节点类型说明
 */
const getNodeTypeDescription = (type: NodeType) => {
  switch (type) {
    case NodeType.START:
      return {
        title: '开始节点',
        description: '工作流的起始点，每个工作流只能有一个开始节点。',
        usage: '自动生成，无需配置参数。',
        tips: ['开始节点会自动创建', '不需要手动添加']
      };
    case NodeType.AGENT:
      return {
        title: 'Agent节点',
        description: '执行具体任务的智能体节点，可以调用LLM完成特定工作。',
        usage: '选择Agent角色，编写任务描述，可以使用参数模板。',
        tips: [
          '使用 {{参数名}} 表示参数',
          '参数值在工作流参数中配置',
          'Agent会根据角色自动选择合适的LLM'
        ]
      };
    case NodeType.AGGREGATOR:
      return {
        title: '聚合节点',
        description: '汇总多个并发节点的结果，用于并发执行后的结果合并。',
        usage: '自动汇总所有输入节点的结果，无需额外配置。',
        tips: [
          '用于Fan-In边的目标节点',
          '自动合并所有输入结果',
          '可以自定义合并策略'
        ]
      };
    case NodeType.CONDITION:
      return {
        title: '条件节点',
        description: '根据条件表达式决定执行路径，实现分支逻辑。',
        usage: '编写条件表达式，返回true/false来决定执行哪个分支。',
        tips: [
          '支持JavaScript表达式',
          '可以使用上下文变量',
          '例如：result.contains("APPROVED")',
          '例如：score > 80',
          '例如：status === "success"'
        ]
      };
    case NodeType.LOOP:
      return {
        title: '循环节点',
        description: '循环执行某个节点，直到满足退出条件。',
        usage: '编写循环条件，返回true继续循环，false退出循环。',
        tips: [
          '支持JavaScript表达式',
          '可以使用循环变量',
          '例如：iteration < 5',
          '例如：!result.contains("完成")',
          '注意避免死循环'
        ]
      };
    default:
      return {
        title: '未知节点',
        description: '未知类型的节点。',
        usage: '无',
        tips: []
      };
  }
};

/**
 * 获取条件表达式示例
 */
const getConditionExamples = () => [
  {
    label: '结果包含特定文本',
    value: 'result.contains("APPROVED")',
    description: '检查结果是否包含"APPROVED"'
  },
  {
    label: '数值比较',
    value: 'score > 80',
    description: '检查分数是否大于80'
  },
  {
    label: '状态判断',
    value: 'status === "success"',
    description: '检查状态是否为success'
  },
  {
    label: '循环次数限制',
    value: 'iteration < 5',
    description: '循环次数小于5次'
  },
  {
    label: '结果不为空',
    value: 'result && result.length > 0',
    description: '检查结果不为空'
  },
  {
    label: '多个条件组合',
    value: 'score > 60 && status === "pass"',
    description: '分数大于60且状态为pass'
  }
];

/**
 * 属性面板组件
 */
interface PropertyPanelProps {
  node: WorkflowNode | null;
  onUpdate: (node: WorkflowNode) => void;
  onDelete: (nodeId: string) => void;
}

const PropertyPanel: React.FC<PropertyPanelProps> = ({ node, onUpdate, onDelete }) => {
  const [form] = Form.useForm();

  React.useEffect(() => {
    if (node) {
      form.setFieldsValue(node);
    }
  }, [node, form]);

  if (!node) {
    return (
      <Card 
        title={
          <Space>
            <span>节点属性</span>
            <Tooltip title="点击节点查看详细属性">
              <QuestionCircleOutlined style={{ color: '#999' }} />
            </Tooltip>
          </Space>
        } 
        size="small"
      >
        <div style={{ color: '#999', textAlign: 'center', padding: '40px 20px' }}>
          <InfoCircleOutlined style={{ fontSize: '48px', color: '#d9d9d9', marginBottom: '16px' }} />
          <div style={{ fontSize: '14px', marginBottom: '8px' }}>请选择一个节点</div>
          <div style={{ fontSize: '12px' }}>点击画布中的节点查看和编辑属性</div>
        </div>
      </Card>
    );
  }

  const nodeInfo = getNodeTypeDescription(node.type);

  /**
   * 表单值改变处理
   */
  const handleValuesChange = (changedValues: Record<string, unknown>, allValues: Record<string, unknown>) => {
    onUpdate({ ...node, ...allValues });
  };

  /**
   * 插入条件示例
   */
  const insertConditionExample = (example: string) => {
    form.setFieldsValue({ condition: example });
    onUpdate({ ...node, condition: example });
    message.success('已插入示例条件');
  };

  return (
    <div style={{ height: '100%', overflow: 'auto' }}>
      {/* 节点说明卡片 */}
      <Card 
        size="small" 
        style={{ marginBottom: '12px' }}
        bodyStyle={{ padding: '12px' }}
      >
        <div style={{ marginBottom: '8px' }}>
          <Text strong style={{ fontSize: '14px' }}>{nodeInfo.title}</Text>
        </div>
        <Paragraph style={{ marginBottom: '8px', fontSize: '12px', color: '#666' }}>
          {nodeInfo.description}
        </Paragraph>
        <Alert
          message="使用说明"
          description={nodeInfo.usage}
          type="info"
          showIcon
          style={{ marginBottom: '8px', fontSize: '12px' }}
        />
        {nodeInfo.tips.length > 0 && (
          <div>
            <Text type="secondary" style={{ fontSize: '12px' }}>💡 提示：</Text>
            <ul style={{ margin: '4px 0', paddingLeft: '20px', fontSize: '12px' }}>
              {nodeInfo.tips.map((tip, index) => (
                <li key={index} style={{ marginBottom: '2px' }}>{tip}</li>
              ))}
            </ul>
          </div>
        )}
      </Card>

      {/* 节点属性表单 */}
      <Card title="节点配置" size="small">
        <Form
          form={form}
          layout="vertical"
          onValuesChange={handleValuesChange}
          initialValues={node}
        >
          <Form.Item label="节点ID">
            <Input value={node.id} disabled />
          </Form.Item>

          <Form.Item 
            label="节点名称" 
            name="name"
            rules={[{ required: true, message: '请输入节点名称' }]}
          >
            <Input placeholder="请输入节点名称" />
          </Form.Item>

          {node.type === NodeType.AGENT && (
            <>
              <Form.Item 
                label="Agent角色" 
                name="agentRole"
                rules={[{ required: true, message: '请选择Agent角色' }]}
              >
                <Select placeholder="请选择Agent角色">
                  <Option value="researcher">研究员 - 负责信息收集和分析</Option>
                  <Option value="writer">写作者 - 负责内容创作</Option>
                  <Option value="reviewer">审阅者 - 负责审核和修改</Option>
                  <Option value="coder">程序员 - 负责代码开发</Option>
                  <Option value="analyst">分析师 - 负责数据分析</Option>
                  <Option value="translator">翻译员 - 负责语言翻译</Option>
                  <Option value="architect">架构师 - 负责系统设计</Option>
                  <Option value="tester">测试工程师 - 负责质量测试</Option>
                </Select>
              </Form.Item>

              <Form.Item 
                label={
                  <Space>
                    <span>任务描述</span>
                    <Tooltip title="使用 {{参数名}} 表示参数，参数值在工作流参数中配置">
                      <QuestionCircleOutlined style={{ color: '#999' }} />
                    </Tooltip>
                  </Space>
                }
                name="inputTemplate"
                rules={[{ required: true, message: '请输入任务描述' }]}
              >
                <TextArea
                  rows={4}
                  placeholder="例如：研究{{model}}的能效数据，并生成分析报告"
                />
              </Form.Item>

              <Alert
                message="参数使用说明"
                description={
                  <div>
                    <div>• 使用 {'{{参数名}}'} 定义参数</div>
                    <div>• 例如：分析 {'{{product}}'} 的市场趋势</div>
                    <div>• 参数值在执行工作流时传入</div>
                  </div>
                }
                type="info"
                showIcon
                style={{ marginBottom: '16px', fontSize: '12px' }}
              />
            </>
          )}

          {(node.type === NodeType.CONDITION || node.type === NodeType.LOOP) && (
            <>
              <Form.Item 
                label={
                  <Space>
                    <span>条件表达式</span>
                    <Tooltip title="使用JavaScript表达式，返回true/false">
                      <QuestionCircleOutlined style={{ color: '#999' }} />
                    </Tooltip>
                  </Space>
                }
                name="condition"
                rules={[{ required: true, message: '请输入条件表达式' }]}
              >
                <TextArea
                  rows={3}
                  placeholder="例如：result.contains('APPROVED')"
                />
              </Form.Item>

              <Divider style={{ margin: '12px 0' }}>条件示例（点击插入）</Divider>
              
              <div style={{ maxHeight: '200px', overflow: 'auto' }}>
                {getConditionExamples().map((example, index) => (
                  <Card
                    key={index}
                    size="small"
                    hoverable
                    style={{ marginBottom: '8px', cursor: 'pointer' }}
                    bodyStyle={{ padding: '8px' }}
                    onClick={() => insertConditionExample(example.value)}
                  >
                    <div style={{ marginBottom: '4px' }}>
                      <Text strong style={{ fontSize: '12px' }}>{example.label}</Text>
                    </div>
                    <div style={{ marginBottom: '4px' }}>
                      <Text code style={{ fontSize: '11px' }}>{example.value}</Text>
                    </div>
                    <Text type="secondary" style={{ fontSize: '11px' }}>{example.description}</Text>
                  </Card>
                ))}
              </div>

              <Alert
                message="条件表达式说明"
                description={
                  <div>
                    <div>• 支持标准JavaScript语法</div>
                    <div>• 可用变量：result, score, status, iteration等</div>
                    <div>• 返回true继续执行，false停止或走其他分支</div>
                  </div>
                }
                type="warning"
                showIcon
                style={{ marginTop: '12px', fontSize: '12px' }}
              />
            </>
          )}

          <Divider style={{ margin: '16px 0' }} />

          <Form.Item style={{ marginBottom: 0 }}>
            <Button
              danger
              icon={<DeleteOutlined />}
              onClick={() => onDelete(node.id)}
              block
            >
              删除节点
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default PropertyPanel;
