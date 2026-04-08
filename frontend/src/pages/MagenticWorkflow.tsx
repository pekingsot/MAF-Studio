import React, { useState } from 'react';
import {
  Card,
  Form,
  Input,
  Button,
  Select,
  message,
  Space,
  Modal,
  Checkbox,
  Row,
  Col,
  Divider,
} from 'antd';
import {
  RobotOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  EditOutlined,
  SaveOutlined,
} from '@ant-design/icons';
import ReactFlow, {
  Node,
  Edge,
  Background,
  Controls,
  MiniMap,
} from 'reactflow';
import 'reactflow/dist/style.css';

import { workflowTemplateApi } from '../services/workflow-template-api';
import { nodeTypes } from '../components/workflow/CustomNodes';
import { edgeTypes } from '../components/workflow/CustomEdges';
import type { WorkflowDefinition, WorkflowNode, WorkflowEdge } from '../types/workflow-template';

const { TextArea } = Input;
const { Option } = Select;

/**
 * Magentic工作流页面
 */
const MagenticWorkflow: React.FC = () => {
  const [form] = Form.useForm();
  const [saveForm] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [workflow, setWorkflow] = useState<WorkflowDefinition | null>(null);
  const [nodes, setNodes] = useState<Node[]>([]);
  const [edges, setEdges] = useState<Edge[]>([]);
  const [reviewModalVisible, setReviewModalVisible] = useState(false);
  const [saveModalVisible, setSaveModalVisible] = useState(false);

  /**
   * 生成Magentic计划
   */
  const handleGenerate = async (values: any) => {
    setLoading(true);
    try {
      const result = await workflowTemplateApi.generateMagenticPlan({
        collaborationId: values.collaborationId,
        task: values.task,
      });

      if (result.success && result.workflow) {
        setWorkflow(result.workflow);
        renderWorkflow(result.workflow);
        setReviewModalVisible(true);
        message.success('计划生成成功');
      } else {
        message.error(`生成失败: ${result.error}`);
      }
    } catch (error: any) {
      message.error(`生成失败: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  /**
   * 渲染工作流
   */
  const renderWorkflow = (workflow: WorkflowDefinition) => {
    const reactFlowNodes: Node[] = workflow.nodes.map((node, index) => ({
      id: node.id,
      type: node.type,
      position: { x: 250, y: index * 150 },
      data: node,
    }));

    const reactFlowEdges: Edge[] = workflow.edges.map((edge, index) => ({
      id: `edge-${index}`,
      source: edge.from,
      target: Array.isArray(edge.to) ? edge.to[0] : edge.to,
      type: 'custom',
      data: { type: edge.type, description: edge.description },
    }));

    setNodes(reactFlowNodes);
    setEdges(reactFlowEdges);
  };

  /**
   * 执行工作流
   */
  const handleExecute = async () => {
    if (!workflow) return;

    try {
      const values = await form.getFieldsValue();
      const result = await workflowTemplateApi.execute(0, {
        collaborationId: values.collaborationId,
        input: values.task,
      });

      if (result.success) {
        message.success('执行成功');
        setReviewModalVisible(false);
      } else {
        message.error(`执行失败: ${result.error}`);
      }
    } catch (error: any) {
      message.error(`执行失败: ${error.message}`);
    }
  };

  /**
   * 拒绝计划
   */
  const handleReject = () => {
    setReviewModalVisible(false);
    setWorkflow(null);
    setNodes([]);
    setEdges([]);
    message.info('已拒绝计划，请重新生成');
  };

  /**
   * 编辑计划
   */
  const handleEdit = () => {
    message.info('请在工作流编辑器中修改计划');
  };

  /**
   * 保存为模板
   */
  const handleSave = async (values: any) => {
    if (!workflow) return;

    try {
      await workflowTemplateApi.saveMagenticPlan({
        name: values.name,
        description: values.description,
        category: values.category,
        tags: values.tags?.split(',').map((t: string) => t.trim()),
        workflow,
        isPublic: values.isPublic || false,
        enableLearning: values.enableLearning || false,
        originalTask: form.getFieldValue('task'),
      });

      message.success('保存成功');
      setSaveModalVisible(false);
    } catch (error: any) {
      message.error(`保存失败: ${error.message}`);
    }
  };

  return (
    <div style={{ padding: '24px' }}>
      <Card title="🤖 Magentic工作流">
        <Form form={form} layout="vertical" onFinish={handleGenerate}>
          <Row gutter={16}>
            <Col span={8}>
              <Form.Item
                label="协作ID"
                name="collaborationId"
                rules={[{ required: true, message: '请输入协作ID' }]}
              >
                <Input type="number" placeholder="请输入协作ID" />
              </Form.Item>
            </Col>

            <Col span={16}>
              <Form.Item
                label="任务描述"
                name="task"
                rules={[{ required: true, message: '请输入任务描述' }]}
              >
                <TextArea
                  rows={4}
                  placeholder="请详细描述任务，例如：研究ResNet-50、BERT、GPT-2三个AI模型的能效，并生成分析报告"
                />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              icon={<RobotOutlined />}
              loading={loading}
            >
              生成工作流计划
            </Button>
          </Form.Item>
        </Form>

        {workflow && (
          <Card title="工作流预览" style={{ marginTop: '16px' }}>
            <div style={{ height: '400px' }}>
              <ReactFlow
                nodes={nodes}
                edges={edges}
                nodeTypes={nodeTypes}
                edgeTypes={edgeTypes}
                fitView
              >
                <Background />
                <Controls />
                <MiniMap />
              </ReactFlow>
            </div>

            <Divider />

            <Space>
              <Button
                type="primary"
                icon={<CheckCircleOutlined />}
                onClick={handleExecute}
              >
                执行工作流
              </Button>
              <Button icon={<EditOutlined />} onClick={handleEdit}>
                编辑计划
              </Button>
              <Button icon={<SaveOutlined />} onClick={() => setSaveModalVisible(true)}>
                保存为模板
              </Button>
              <Button danger icon={<CloseCircleOutlined />} onClick={handleReject}>
                拒绝重新规划
              </Button>
            </Space>
          </Card>
        )}
      </Card>

      <Modal
        title="人工审核 - Magentic工作流计划"
        open={reviewModalVisible}
        onCancel={() => setReviewModalVisible(false)}
        width={900}
        footer={[
          <Button key="reject" danger onClick={handleReject}>
            拒绝重新规划
          </Button>,
          <Button key="edit" icon={<EditOutlined />} onClick={handleEdit}>
            编辑计划
          </Button>,
          <Button key="save" icon={<SaveOutlined />} onClick={() => setSaveModalVisible(true)}>
            保存为模板
          </Button>,
          <Button key="execute" type="primary" icon={<CheckCircleOutlined />} onClick={handleExecute}>
            审核通过执行
          </Button>,
        ]}
      >
        <div style={{ height: '500px' }}>
          <ReactFlow
            nodes={nodes}
            edges={edges}
            nodeTypes={nodeTypes}
            edgeTypes={edgeTypes}
            fitView
          >
            <Background />
            <Controls />
            <MiniMap />
          </ReactFlow>
        </div>
      </Modal>

      <Modal
        title="保存为模板"
        open={saveModalVisible}
        onCancel={() => setSaveModalVisible(false)}
        onOk={() => saveForm.submit()}
      >
        <Form form={saveForm} layout="vertical" onFinish={handleSave}>
          <Form.Item
            label="模板名称"
            name="name"
            rules={[{ required: true, message: '请输入模板名称' }]}
          >
            <Input placeholder="请输入模板名称" />
          </Form.Item>

          <Form.Item label="模板描述" name="description">
            <TextArea rows={3} placeholder="请输入模板描述" />
          </Form.Item>

          <Form.Item label="分类" name="category">
            <Select placeholder="请选择分类">
              <Option value="research">研究分析</Option>
              <Option value="writing">写作创作</Option>
              <Option value="translation">翻译</Option>
              <Option value="coding">编程开发</Option>
              <Option value="analysis">数据分析</Option>
            </Select>
          </Form.Item>

          <Form.Item label="标签" name="tags">
            <Input placeholder="多个标签用逗号分隔，例如：AI,研究,报告" />
          </Form.Item>

          <Form.Item name="isPublic" valuePropName="checked">
            <Checkbox>公开模板（其他用户可以使用）</Checkbox>
          </Form.Item>

          <Form.Item name="enableLearning" valuePropName="checked">
            <Checkbox>让Magentic学习（类似任务自动使用此模板）</Checkbox>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default MagenticWorkflow;
