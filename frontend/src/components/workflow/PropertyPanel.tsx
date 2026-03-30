import React from 'react';
import { Card, Form, Input, Select, Button, Space, message } from 'antd';
import { DeleteOutlined } from '@ant-design/icons';
import type { WorkflowNode, ParameterDefinition } from '../../types/workflow-template';
import { NodeType } from '../../types/workflow-template';

const { TextArea } = Input;
const { Option } = Select;

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
      <Card title="节点属性" size="small">
        <div style={{ color: '#999', textAlign: 'center', padding: '20px' }}>
          请选择一个节点
        </div>
      </Card>
    );
  }

  /**
   * 表单值改变处理
   */
  const handleValuesChange = (changedValues: any, allValues: any) => {
    onUpdate({ ...node, ...allValues });
  };

  return (
    <Card title="节点属性" size="small">
      <Form
        form={form}
        layout="vertical"
        onValuesChange={handleValuesChange}
        initialValues={node}
      >
        <Form.Item label="节点ID">
          <Input value={node.id} disabled />
        </Form.Item>

        <Form.Item label="节点名称" name="name">
          <Input placeholder="请输入节点名称" />
        </Form.Item>

        {node.type === NodeType.AGENT && (
          <>
            <Form.Item label="Agent角色" name="agentRole">
              <Select placeholder="请选择Agent角色">
                <Option value="researcher">研究员</Option>
                <Option value="writer">写作者</Option>
                <Option value="reviewer">审阅者</Option>
                <Option value="coder">程序员</Option>
                <Option value="analyst">分析师</Option>
                <Option value="translator">翻译员</Option>
              </Select>
            </Form.Item>

            <Form.Item label="任务描述" name="inputTemplate">
              <TextArea
                rows={4}
                placeholder="使用 {{参数名}} 表示参数，例如：研究{{model}}的能效数据"
              />
            </Form.Item>

            <Form.Item label="参数配置">
              <div style={{ color: '#999', fontSize: '12px' }}>
                在工作流参数中配置具体值
              </div>
            </Form.Item>
          </>
        )}

        {(node.type === NodeType.CONDITION || node.type === NodeType.LOOP) && (
          <Form.Item label="条件表达式" name="condition">
            <TextArea
              rows={2}
              placeholder="例如：result.contains('APPROVED')"
            />
          </Form.Item>
        )}

        <Form.Item>
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
  );
};

export default PropertyPanel;
