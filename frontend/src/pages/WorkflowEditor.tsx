import { getErrorMessage } from '../utils/errorHandler';
import React, { useState, useCallback, useRef, useEffect } from 'react';
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
  ReactFlowInstance,
  MarkerType,
} from 'reactflow';
import 'reactflow/dist/style.css';
import '../styles/workflow-editor.css';
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
  Typography,
  Alert,
  Divider,
} from 'antd';
import {
  SaveOutlined,
  PlayCircleOutlined,
  SettingOutlined,
  FullscreenOutlined,
  ZoomInOutlined,
  ZoomOutOutlined,
  ClearOutlined,
} from '@ant-design/icons';
import { useNavigate, useLocation } from 'react-router-dom';

import { nodeTypes } from '../components/workflow/CustomNodes';
import { edgeTypes } from '../components/workflow/CustomEdges';
import NodePanel from '../components/workflow/NodePanel';
import PropertyPanel from '../components/workflow/PropertyPanel';
import { workflowTemplateApi } from '../services/workflow-template-api';
import {
  NodeType,
  EdgeType,
} from '../types/workflow-template';
import type {
  WorkflowNode,
  WorkflowEdge,
  WorkflowDefinition,
  WorkflowTemplate,
  CreateWorkflowTemplateRequest,
} from '../types/workflow-template';

const { Content, Sider } = Layout;
const { TextArea } = Input;
const { Option } = Select;
const { Title, Text } = Typography;

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
      return '未知节点';
  }
}

/**
 * 工作流编辑器页面
 */
const WorkflowEditor: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const reactFlowWrapper = useRef<HTMLDivElement>(null);
  const [reactFlowInstance, setReactFlowInstance] = useState<ReactFlowInstance | null>(null);

  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);
  const [selectedNode, setSelectedNode] = useState<WorkflowNode | null>(null);
  const [saveModalVisible, setSaveModalVisible] = useState(false);
  const [form] = Form.useForm();

  const locationState = location.state as { template?: WorkflowTemplate } | undefined;

  useEffect(() => {
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
      markerEnd: {
        type: MarkerType.ArrowClosed,
        color: '#1890ff',
      },
    }));

    setNodes(reactFlowNodes);
    setEdges(reactFlowEdges);
    
    form.setFieldsValue(template);
  };

  /**
   * 拖放处理 - 优化版
   * 改进点：
   * 1. 更精确的位置计算
   * 2. 考虑缩放和平移
   * 3. 添加视觉反馈
   */
  const onDrop = useCallback(
    (event: React.DragEvent) => {
      event.preventDefault();

      const type = event.dataTransfer.getData('application/reactflow') as NodeType;

      if (!type || !reactFlowInstance) return;

      // 获取ReactFlow画布的边界
      const bounds = reactFlowWrapper.current?.getBoundingClientRect();
      if (!bounds) return;

      // 获取当前视口信息
      const { x: viewportX, y: viewportY, zoom } = reactFlowInstance.getViewport();

      // 计算相对于画布的位置（考虑缩放和平移）
      const position = reactFlowInstance.project({
        x: event.clientX - bounds.left,
        y: event.clientY - bounds.top,
      });

      // 确保位置在合理范围内，并考虑节点大小
      const nodeWidth = 200;
      const nodeHeight = 100;
      const safePosition = {
        x: Math.max(nodeWidth / 2, Math.min(position.x, 2000 - nodeWidth / 2)),
        y: Math.max(nodeHeight / 2, Math.min(position.y, 2000 - nodeHeight / 2)),
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
          position: safePosition,
          data: newNode,
        })
      );

      // 自动选中新节点并滚动到视图
      setSelectedNode(newNode);
      
      // 延迟滚动，确保节点已渲染
      setTimeout(() => {
        const nodeElement = document.querySelector(`[data-id="${newNode.id}"]`);
        if (nodeElement) {
          nodeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
      }, 100);
      
      message.success({
        content: `已添加 ${getDefaultNodeName(type)}`,
        duration: 2,
      });
    },
    [reactFlowInstance, setNodes]
  );

  const onDragOver = useCallback((event: React.DragEvent) => {
    event.preventDefault();
    event.dataTransfer.dropEffect = 'move';
  }, []);

  /**
   * 连线处理 - 改进版
   */
  const onConnect = useCallback(
    (params: Connection) => {
      Modal.confirm({
        title: '选择连接类型',
        icon: null,
        content: (
          <div>
            <Alert
              message="请选择节点之间的连接类型"
              description="不同类型代表不同的执行逻辑"
              type="info"
              showIcon
              style={{ marginBottom: '16px' }}
            />
            <Select
              defaultValue="sequential"
              style={{ width: '100%' }}
              id="edge-type-select"
            >
              <Option value="sequential">顺序执行 - 按顺序执行</Option>
              <Option value="fan-out">并发执行 - 同时执行多个节点</Option>
              <Option value="fan-in">汇聚结果 - 合并多个节点的结果</Option>
              <Option value="conditional">条件分支 - 根据条件选择路径</Option>
              <Option value="loop">循环执行 - 重复执行直到满足条件</Option>
            </Select>
          </div>
        ),
        onOk: () => {
          const selectElement = document.getElementById('edge-type-select') as HTMLSelectElement | null;
          const edgeType = selectElement?.value || 'sequential';

          const newEdge: Edge = {
            id: `edge-${Date.now()}`,
            source: params.source!,
            target: params.target!,
            type: 'custom',
            data: { type: edgeType as EdgeType },
            markerEnd: {
              type: MarkerType.ArrowClosed,
              color: edgeType === 'fan-out' || edgeType === 'fan-in' ? '#52c41a' : '#1890ff',
            },
          };

          setEdges((eds) => addEdge(params, eds).concat(newEdge));
          message.success('连接创建成功');
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
      Modal.confirm({
        title: '确认删除',
        content: '确定要删除这个节点吗？相关的连接也会被删除。',
        onOk: () => {
          setNodes((nds) => nds.filter((node) => node.id !== nodeId));
          setEdges((eds) =>
            eds.filter((edge) => edge.source !== nodeId && edge.target !== nodeId)
          );
          setSelectedNode(null);
          message.success('节点已删除');
        },
      });
    },
    [setNodes, setEdges]
  );

  /**
   * 清空画布
   */
  const handleClearCanvas = useCallback(() => {
    Modal.confirm({
      title: '确认清空',
      content: '确定要清空画布吗？所有节点和连接都会被删除。',
      onOk: () => {
        setNodes([]);
        setEdges([]);
        setSelectedNode(null);
        message.success('画布已清空');
      },
    });
  }, [setNodes, setEdges]);

  /**
   * 缩放控制
   */
  const handleZoomIn = useCallback(() => {
    if (reactFlowInstance) {
      reactFlowInstance.zoomIn();
    }
  }, [reactFlowInstance]);

  const handleZoomOut = useCallback(() => {
    if (reactFlowInstance) {
      reactFlowInstance.zoomOut();
    }
  }, [reactFlowInstance]);

  const handleFitView = useCallback(() => {
    if (reactFlowInstance) {
      reactFlowInstance.fitView({ padding: 0.2 });
    }
  }, [reactFlowInstance]);

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
  const handleSave = async (values: { name: string; description: string; category: string; tags?: string; isPublic?: boolean }) => {
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
    } catch (error: unknown) {
      message.error(`保存失败: ${getErrorMessage(error)}`);
    }
  };

  /**
   * 执行工作流
   */
  const handleExecute = () => {
    if (nodes.length === 0) {
      message.warning('请先添加节点');
      return;
    }

    const workflow = exportWorkflow();
    navigate('/workflow-execute', {
      state: {
        workflow,
      },
    });
  };

  return (
    <Layout style={{ height: '100%', background: '#f5f5f5' }}>
      {/* 左侧节点面板 */}
      <Sider 
        width={200} 
        theme="light" 
        style={{ 
          borderRight: '1px solid #e8e8e8',
          boxShadow: '2px 0 8px rgba(0,0,0,0.05)',
        }}
      >
        <div style={{ padding: '12px', height: '100%', display: 'flex', flexDirection: 'column' }}>
          <div style={{ marginBottom: '12px' }}>
            <Title level={4} style={{ margin: 0, marginBottom: '6px' }}>
              节点库
            </Title>
            <Text type="secondary" style={{ fontSize: '11px' }}>
              拖拽节点到右侧画布
            </Text>
          </div>
          <div style={{ flex: 1, overflow: 'auto' }}>
            <NodePanel />
          </div>
        </div>
      </Sider>

      <Layout style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
        {/* 顶部工具栏 */}
        <div
          style={{
            padding: '8px 20px',
            background: 'white',
            borderBottom: '1px solid #e8e8e8',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            boxShadow: '0 2px 8px rgba(0,0,0,0.05)',
            height: '56px',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center' }}>
            <Title level={4} style={{ margin: 0 }}>
              工作流编辑器
            </Title>
            <Divider type="vertical" style={{ height: '20px', margin: '0 12px' }} />
            <Space size="small">
              <Button 
                icon={<ZoomInOutlined />} 
                onClick={handleZoomIn}
                title="放大"
                size="small"
              />
              <Button 
                icon={<ZoomOutOutlined />} 
                onClick={handleZoomOut}
                title="缩小"
                size="small"
              />
              <Button 
                icon={<FullscreenOutlined />} 
                onClick={handleFitView}
                title="适应画布"
                size="small"
              />
              <Button 
                icon={<ClearOutlined />} 
                onClick={handleClearCanvas}
                danger
                title="清空画布"
                size="small"
              >
                清空
              </Button>
            </Space>
          </div>
          <Space>
            <Button 
              icon={<PlayCircleOutlined />} 
              onClick={handleExecute}
              type="default"
            >
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

        {/* 主要内容区域 */}
        <Content style={{ display: 'flex', flexDirection: 'row', flex: 1, height: 'calc(100% - 56px)', padding: 0, overflow: 'hidden' }}>
          {/* ReactFlow画布 */}
          <div 
            style={{ 
              flex: 1, 
              position: 'relative', 
              height: '100%',
              background: '#fafafa',
              border: '2px dashed #d9d9d9',
              borderRadius: '4px',
              minWidth: 0,
            }} 
            ref={reactFlowWrapper}
          >
            <ReactFlow
              nodes={nodes}
              edges={edges}
              onNodesChange={onNodesChange}
              onEdgesChange={onEdgesChange}
              onDrop={onDrop}
              onDragOver={onDragOver}
              onConnect={onConnect}
              onNodeClick={onNodeClick}
              onInit={setReactFlowInstance}
              nodeTypes={nodeTypes}
              edgeTypes={edgeTypes}
              fitView
              snapToGrid={true}
              snapGrid={[15, 15]}
              connectionLineStyle={{ strokeWidth: 2, stroke: '#1890ff' }}
              defaultEdgeOptions={{
                type: 'custom',
                markerEnd: {
                  type: MarkerType.ArrowClosed,
                  color: '#1890ff',
                },
              }}
              style={{ width: '100%', height: '100%' }}
            >
              <Background color="#bfbfbf" gap={20} size={1.5} />
              <Controls showInteractive={false} />
              <MiniMap 
                nodeColor={(node) => {
                  switch (node.type) {
                    case 'start':
                      return '#52c41a';
                    case 'agent':
                      return '#1890ff';
                    case 'aggregator':
                      return '#722ed1';
                    case 'condition':
                      return '#fa8c16';
                    case 'loop':
                      return '#eb2f96';
                    default:
                      return '#d9d9d9';
                  }
                }}
                style={{ 
                  background: 'white',
                  border: '1px solid #e8e8e8',
                }}
              />
              {nodes.length === 0 && (
                <div
                  style={{
                    position: 'absolute',
                    top: '50%',
                    left: '50%',
                    transform: 'translate(-50%, -50%)',
                    textAlign: 'center',
                    color: '#8c8c8c',
                    pointerEvents: 'none',
                    zIndex: 1,
                  }}
                >
                  <div style={{ fontSize: '48px', marginBottom: '16px' }}>📝</div>
                  <div style={{ fontSize: '16px', fontWeight: 'bold', marginBottom: '8px' }}>
                    从左侧拖拽节点到此处
                  </div>
                  <div style={{ fontSize: '12px' }}>
                    开始创建您的工作流
                  </div>
                </div>
              )}
            </ReactFlow>
          </div>

          {/* 右侧属性面板 */}
          <div
            style={{ 
              width: '250px',
              borderLeft: '1px solid #e8e8e8',
              background: 'white',
              boxShadow: '-2px 0 8px rgba(0,0,0,0.05)',
              height: '100%',
              overflow: 'auto',
            }}
          >
            <div style={{ padding: '12px', height: '100%', overflow: 'auto' }}>
              <PropertyPanel
                node={selectedNode}
                onUpdate={handleUpdateNode}
                onDelete={handleDeleteNode}
              />
            </div>
          </div>
        </Content>
      </Layout>

      {/* 保存模板对话框 */}
      <Modal
        title="保存工作流模板"
        open={saveModalVisible}
        onCancel={() => setSaveModalVisible(false)}
        onOk={() => form.submit()}
        width={600}
      >
        <Alert
          message="保存工作流模板后，可以在模板管理页面查看和复用"
          type="info"
          showIcon
          style={{ marginBottom: '16px' }}
        />
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

          <Form.Item label="是否公开" name="isPublic">
            <Select>
              <Option value={false}>私有 - 仅自己可见</Option>
              <Option value={true}>公开 - 所有人可见</Option>
            </Select>
          </Form.Item>
        </Form>
      </Modal>
    </Layout>
  );
};

export default WorkflowEditor;
