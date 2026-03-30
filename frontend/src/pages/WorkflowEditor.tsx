import React, { useState, useCallback, useRef } from 'react';
import ReactFlow, {
  Node,
  Edge,
  Connection,
  addEdge,
  Background,
  Controls,
  MiniMap,
  ReactFlowProvider,
  useNodesState,
  useEdgesState,
} from 'reactflow';
import 'reactflow/dist/style.css';
import {
  Layout,
  Card,
  Button,
  Space,
  Modal,
  Form,
  Input,
  Select,
  message,
  Drawer,
} from 'antd';
import {
  SaveOutlined,
  PlayCircleOutlined,
  SettingOutlined,
} from '@ant-design/icons';
import { useNavigate, useLocation } from 'react-router-dom';

import { nodeTypes } from '../components/workflow/CustomNodes';
import { edgeTypes } from '../components/workflow/CustomEdges';
import NodePanel from '../components/workflow/NodePanel';
import PropertyPanel from '../components/workflow/PropertyPanel';
import { workflowTemplateApi } from '../services/workflow-template-api';
import type {
  WorkflowNode,
  WorkflowEdge,
  WorkflowDefinition,
  NodeType,
  EdgeType,
  WorkflowTemplate,
  CreateWorkflowTemplateRequest,
} from '../types/workflow-template';

const { Content, Sider } = Layout;
const { TextArea } = Input;
const { Option } = Select;

/**
 * 工作流编辑器页面
 */
const WorkflowEditor: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const reactFlowWrapper = useRef<HTMLDivElement>(null);

  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);
  const [selectedNode, setSelectedNode] = useState<WorkflowNode | null>(null);
  const [saveModalVisible, setSaveModalVisible] = useState(false);
  const [form] = Form.useForm();

  const locationState = location.state as { template?: WorkflowTemplate } | undefined;

  React.useEffect(() => {
    if (locationState?.template) {
      loadTemplate(locationState.template);
    }
  }, [locationState]);

  /**
   * 加载模板
   */
  const loadTemplate = (template: WorkflowTemplate) => {
    const workflow = template.workflow;
    
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
    
    form.setFieldsValue(template);
  };

  /**
   * 拖放处理
   */
  const onDrop = useCallback(
    (event: React.DragEvent) => {
      event.preventDefault();

      const reactFlowBounds = reactFlowWrapper.current?.getBoundingClientRect();
      const type = event.dataTransfer.getData('application/reactflow') as NodeType;

      if (!type || !reactFlowBounds) return;

      const position = {
        x: event.clientX - reactFlowBounds.left,
        y: event.clientY - reactFlowBounds.top,
      };

      const newNode: WorkflowNode = {
        id: `node-${Date.now()}`,
        type,
        name: getDefaultNodeName(type),
      };

      setNodes((nds) =>
        nds.concat({
          id: newNode.id,
          type,
          position,
          data: newNode,
        })
      );
    },
    [setNodes]
  );

  const onDragOver = useCallback((event: React.DragEvent) => {
    event.preventDefault();
    event.dataTransfer.dropEffect = 'move';
  }, []);

  /**
   * 连线处理
   */
  const onConnect = useCallback(
    (params: Connection) => {
      Modal.confirm({
        title: '选择边类型',
        content: (
          <Select
            defaultValue="sequential"
            style={{ width: '100%' }}
            id="edge-type-select"
          >
            <Option value="sequential">顺序执行</Option>
            <Option value="fan-out">并发执行</Option>
            <Option value="fan-in">汇聚结果</Option>
            <Option value="conditional">条件分支</Option>
            <Option value="loop">循环执行</Option>
          </Select>
        ),
        onOk: () => {
          const selectElement = document.getElementById('edge-type-select') as any;
          const edgeType = selectElement?.value || 'sequential';

          const newEdge: Edge = {
            id: `edge-${Date.now()}`,
            source: params.source!,
            target: params.target!,
            type: 'custom',
            data: { type: edgeType as EdgeType },
          };

          setEdges((eds) => addEdge(params, eds).concat(newEdge));
        },
      });
    },
    [setEdges]
  );

  /**
   * 节点点击处理
   */
  const onNodeClick = useCallback((event: React.MouseEvent, node: Node) => {
    setSelectedNode(node.data as WorkflowNode);
  }, []);

  /**
   * 更新节点
   */
  const handleUpdateNode = useCallback(
    (updatedNode: WorkflowNode) => {
      setNodes((nds) =>
        nds.map((node) =>
          node.id === updatedNode.id ? { ...node, data: updatedNode } : node
        )
      );
      setSelectedNode(updatedNode);
    },
    [setNodes]
  );

  /**
   * 删除节点
   */
  const handleDeleteNode = useCallback(
    (nodeId: string) => {
      setNodes((nds) => nds.filter((node) => node.id !== nodeId));
      setEdges((eds) =>
        eds.filter((edge) => edge.source !== nodeId && edge.target !== nodeId)
      );
      setSelectedNode(null);
    },
    [setNodes, setEdges]
  );

  /**
   * 导出工作流定义
   */
  const exportWorkflow = (): WorkflowDefinition => {
    const workflowNodes: WorkflowNode[] = nodes.map((node) => node.data as WorkflowNode);

    const workflowEdges: WorkflowEdge[] = edges.map((edge) => ({
      type: edge.data?.type || 'sequential',
      from: edge.source,
      to: edge.target,
      description: edge.data?.description,
    }));

    return {
      nodes: workflowNodes,
      edges: workflowEdges,
    };
  };

  /**
   * 保存模板
   */
  const handleSave = async (values: any) => {
    try {
      const workflow = exportWorkflow();

      const request: CreateWorkflowTemplateRequest = {
        name: values.name,
        description: values.description,
        category: values.category,
        tags: values.tags?.split(',').map((t: string) => t.trim()),
        workflow,
        isPublic: values.isPublic || false,
        source: 'manual',
      };

      await workflowTemplateApi.create(request);
      message.success('保存成功');
      setSaveModalVisible(false);
      navigate('/workflow-templates');
    } catch (error: any) {
      message.error(`保存失败: ${error.message}`);
    }
  };

  /**
   * 执行工作流
   */
  const handleExecute = () => {
    const workflow = exportWorkflow();
    navigate('/workflow-execute', {
      state: {
        workflow,
      },
    });
  };

  return (
    <Layout style={{ height: '100vh' }}>
      <Sider width={240} theme="light" style={{ borderRight: '1px solid #f0f0f0' }}>
        <div style={{ padding: '16px' }}>
          <NodePanel />
        </div>
      </Sider>

      <Layout>
        <div
          style={{
            padding: '16px',
            background: 'white',
            borderBottom: '1px solid #f0f0f0',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <h2 style={{ margin: 0 }}>工作流编辑器</h2>
          <Space>
            <Button icon={<PlayCircleOutlined />} onClick={handleExecute}>
              执行工作流
            </Button>
            <Button
              type="primary"
              icon={<SaveOutlined />}
              onClick={() => setSaveModalVisible(true)}
            >
              保存模板
            </Button>
          </Space>
        </div>

        <Content>
          <div style={{ display: 'flex', height: '100%' }}>
            <div style={{ flex: 1 }} ref={reactFlowWrapper}>
              <ReactFlow
                nodes={nodes}
                edges={edges}
                onNodesChange={onNodesChange}
                onEdgesChange={onEdgesChange}
                onDrop={onDrop}
                onDragOver={onDragOver}
                onConnect={onConnect}
                onNodeClick={onNodeClick}
                nodeTypes={nodeTypes}
                edgeTypes={edgeTypes}
                fitView
              >
                <Background />
                <Controls />
                <MiniMap />
              </ReactFlow>
            </div>

            <Sider width={300} theme="light" style={{ borderLeft: '1px solid #f0f0f0' }}>
              <div style={{ padding: '16px' }}>
                <PropertyPanel
                  node={selectedNode}
                  onUpdate={handleUpdateNode}
                  onDelete={handleDeleteNode}
                />
              </div>
            </Sider>
          </div>
        </Content>
      </Layout>

      <Modal
        title="保存工作流模板"
        open={saveModalVisible}
        onCancel={() => setSaveModalVisible(false)}
        onOk={() => form.submit()}
      >
        <Form form={form} layout="vertical" onFinish={handleSave}>
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

          <Form.Item label="是否公开" name="isPublic" valuePropName="checked">
            <Select>
              <Option value={false}>私有</Option>
              <Option value={true}>公开</Option>
            </Select>
          </Form.Item>
        </Form>
      </Modal>
    </Layout>
  );
};

/**
 * 获取默认节点名称
 */
function getDefaultNodeName(type: NodeType): string {
  switch (type) {
    case NodeType.START:
      return '开始';
    case NodeType.AGENT:
      return 'Agent节点';
    case NodeType.AGGREGATOR:
      return '聚合节点';
    case NodeType.CONDITION:
      return '条件节点';
    case NodeType.LOOP:
      return '循环节点';
    default:
      return '节点';
  }
}

export default () => (
  <ReactFlowProvider>
    <WorkflowEditor />
  </ReactFlowProvider>
);
