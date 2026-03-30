import React, { useState, useEffect } from 'react';
import { Modal, Form, Select, InputNumber, Input, Button, Space, message, Card, Radio, List, Tag } from 'antd';
import { PlayCircleOutlined, SettingOutlined, SwapOutlined, TeamOutlined, MessageOutlined } from '@ant-design/icons';
import { collaborationService, Collaboration, CollaborationAgent } from '../services/collaborationService';

const { Option } = Select;
const { TextArea } = Input;

interface WorkflowConfigModalProps {
  visible: boolean;
  collaboration: Collaboration | null;
  taskId?: number;
  taskTitle?: string;
  onCancel: () => void;
  onSuccess: () => void;
}

interface WorkflowParameter {
  maxRounds?: number;
  stopKeywords?: string;
  endKeyword?: string;
  handoffKeyword?: string;
  aggregationStrategy?: string;
  maxConcurrency?: number;
  agentOrder?: number[];
}

const workflowTypes = [
  {
    value: 'Sequential',
    label: '顺序执行',
    icon: <PlayCircleOutlined />,
    description: 'Agent按顺序依次处理，前一个的输出作为后一个的输入',
    color: 'blue'
  },
  {
    value: 'Concurrent',
    label: '并发执行',
    icon: <TeamOutlined />,
    description: '多个Agent同时处理同一任务，最后汇总结果',
    color: 'green'
  },
  {
    value: 'Handoffs',
    label: '任务移交',
    icon: <SwapOutlined />,
    description: 'Agent之间可以相互移交任务，灵活协作',
    color: 'orange'
  },
  {
    value: 'GroupChat',
    label: '群聊协作',
    icon: <MessageOutlined />,
    description: '多Agent群聊讨论，适合复杂问题的多轮对话',
    color: 'purple'
  }
];

const aggregationStrategies = [
  { value: 'Merge', label: '合并所有结果' },
  { value: 'Vote', label: '投票选择最佳' },
  { value: 'First', label: '使用第一个结果' },
  { value: 'Last', label: '使用最后一个结果' }
];

const WorkflowConfigModal: React.FC<WorkflowConfigModalProps> = ({
  visible,
  collaboration,
  taskId,
  taskTitle,
  onCancel,
  onSuccess
}) => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [workflowType, setWorkflowType] = useState('Sequential');
  const [agents, setAgents] = useState<CollaborationAgent[]>([]);
  const [input, setInput] = useState('');

  useEffect(() => {
    if (visible && collaboration) {
      setAgents(collaboration.agents);
      if (taskTitle) {
        setInput(`执行任务: ${taskTitle}`);
      }
    }
  }, [visible, collaboration, taskTitle]);

  const handleWorkflowTypeChange = (value: string) => {
    setWorkflowType(value);
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      setLoading(true);

      const request = {
        taskId: taskId,
        workflowType: values.workflowType,
        input: values.input || input,
        parameters: {
          maxRounds: values.maxRounds,
          stopKeywords: values.stopKeywords,
          endKeyword: values.endKeyword,
          handoffKeyword: values.handoffKeyword,
          aggregationStrategy: values.aggregationStrategy,
          maxConcurrency: values.maxConcurrency,
          agentOrder: values.agentOrder
        }
      };

      if (values.workflowType === 'GroupChat') {
        message.loading('工作流执行中，请查看聊天窗口...', 0);
        const eventSource = new EventSource(
          `/api/collaborations/${collaboration?.id}/workflow/groupchat?input=${encodeURIComponent(request.input || '')}`
        );
        
        eventSource.onmessage = (event) => {
          const data = JSON.parse(event.data);
          console.log('收到消息:', data);
        };

        eventSource.onerror = () => {
          eventSource.close();
          message.destroy();
          message.success('工作流执行完成');
          setLoading(false);
          onSuccess();
        };
      } else {
        const result = await fetch(`/api/collaborations/${collaboration?.id}/workflow/execute`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(request)
        }).then(res => res.json());

        if (result.success) {
          message.success('工作流执行成功');
          onSuccess();
        } else {
          message.error(`执行失败: ${result.error}`);
        }
      }
    } catch (error: any) {
      message.error(`执行失败: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const renderWorkflowTypeSelector = () => (
    <List
      grid={{ gutter: 16, column: 2 }}
      dataSource={workflowTypes}
      renderItem={item => (
        <List.Item>
          <Card
            hoverable
            onClick={() => {
              form.setFieldsValue({ workflowType: item.value });
              handleWorkflowTypeChange(item.value);
            }}
            style={{
              borderColor: workflowType === item.value ? item.color : undefined,
              borderWidth: workflowType === item.value ? 2 : 1
            }}
          >
            <Space direction="vertical" style={{ width: '100%' }}>
              <Space>
                <Tag color={item.color}>{item.icon}</Tag>
                <strong>{item.label}</strong>
              </Space>
              <div style={{ fontSize: 12, color: '#666' }}>{item.description}</div>
            </Space>
          </Card>
        </List.Item>
      )}
    />
  );

  const renderParameters = () => {
    switch (workflowType) {
      case 'Sequential':
        return (
          <>
            <Form.Item label="Agent执行顺序" name="agentOrder">
              <Select
                mode="multiple"
                placeholder="选择Agent并调整顺序"
              >
                {agents.map(agent => (
                  <Option key={agent.agentId} value={agent.agentId}>
                    {agent.agentName} {agent.role && `(${agent.role})`}
                  </Option>
                ))}
              </Select>
            </Form.Item>
            <Form.Item label="停止关键词" name="stopKeywords">
              <Input placeholder="多个关键词用逗号分隔，如：完成,DONE,END" />
            </Form.Item>
          </>
        );

      case 'Concurrent':
        return (
          <>
            <Form.Item label="参与的Agent" name="agentOrder">
              <Select
                mode="multiple"
                placeholder="选择参与的Agent"
              >
                {agents.map(agent => (
                  <Option key={agent.agentId} value={agent.agentId}>
                    {agent.agentName} {agent.role && `(${agent.role})`}
                  </Option>
                ))}
              </Select>
            </Form.Item>
            <Form.Item label="结果聚合策略" name="aggregationStrategy">
              <Select defaultValue="Merge">
                {aggregationStrategies.map(strategy => (
                  <Option key={strategy.value} value={strategy.value}>
                    {strategy.label}
                  </Option>
                ))}
              </Select>
            </Form.Item>
            <Form.Item label="最大并发数" name="maxConcurrency">
              <InputNumber min={1} max={10} defaultValue={3} style={{ width: '100%' }} />
            </Form.Item>
          </>
        );

      case 'Handoffs':
        return (
          <>
            <Form.Item label="参与的Agent" name="agentOrder">
              <Select
                mode="multiple"
                placeholder="选择参与的Agent"
              >
                {agents.map(agent => (
                  <Option key={agent.agentId} value={agent.agentId}>
                    {agent.agentName} {agent.role && `(${agent.role})`}
                  </Option>
                ))}
              </Select>
            </Form.Item>
            <Form.Item label="移交关键词" name="handoffKeyword">
              <Input placeholder="默认: [HANDOFF:" defaultValue="[HANDOFF:" />
            </Form.Item>
            <Form.Item label="最大移交次数" name="maxRounds">
              <InputNumber min={1} max={100} defaultValue={10} style={{ width: '100%' }} />
            </Form.Item>
          </>
        );

      case 'GroupChat':
        return (
          <>
            <Form.Item label="参与的Agent" name="agentOrder">
              <Select
                mode="multiple"
                placeholder="选择参与的Agent"
              >
                {agents.map(agent => (
                  <Option key={agent.agentId} value={agent.agentId}>
                    {agent.agentName} {agent.role && `(${agent.role})`}
                  </Option>
                ))}
              </Select>
            </Form.Item>
            <Form.Item label="最大轮次" name="maxRounds">
              <InputNumber min={1} max={50} defaultValue={10} style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item label="结束关键词" name="endKeyword">
              <Input placeholder="默认: [END]" defaultValue="[END]" />
            </Form.Item>
          </>
        );

      default:
        return null;
    }
  };

  return (
    <Modal
      title={
        <Space>
          <SettingOutlined />
          执行工作流
          {taskTitle && <Tag color="blue">任务: {taskTitle}</Tag>}
        </Space>
      }
      open={visible}
      onCancel={onCancel}
      width={800}
      footer={[
        <Button key="cancel" onClick={onCancel}>
          取消
        </Button>,
        <Button
          key="submit"
          type="primary"
          loading={loading}
          onClick={handleSubmit}
          icon={<PlayCircleOutlined />}
        >
          执行工作流
        </Button>
      ]}
    >
      <Form
        form={form}
        layout="vertical"
        initialValues={{
          workflowType: 'Sequential',
          maxRounds: 10,
          maxConcurrency: 3,
          aggregationStrategy: 'Merge'
        }}
      >
        <Form.Item name="workflowType" label="选择工作流类型" hidden>
          <Input />
        </Form.Item>

        {renderWorkflowTypeSelector()}

        <Form.Item label="输入内容" name="input">
          <TextArea
            rows={4}
            placeholder="输入任务描述或指令..."
            value={input}
            onChange={(e) => setInput(e.target.value)}
          />
        </Form.Item>

        <Card title="工作流参数" size="small" style={{ marginTop: 16 }}>
          {renderParameters()}
        </Card>
      </Form>
    </Modal>
  );
};

export default WorkflowConfigModal;
