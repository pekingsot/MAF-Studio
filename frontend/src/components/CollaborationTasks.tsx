import React, { useState, useCallback, useMemo } from 'react';
import {
  Table, Button, Space, Popconfirm, Tag, message, Tooltip, Drawer,
  Card, Alert, Form, Input, Select, Row, Col, Divider
} from 'antd';
import {
  PlayCircleOutlined, EditOutlined, MessageOutlined, DeleteOutlined,
  GithubOutlined, ApartmentOutlined, BulbOutlined, RobotOutlined,
  TeamOutlined, SaveOutlined, ThunderboltOutlined, SafetyCertificateOutlined
} from '@ant-design/icons';
import type { Key } from 'react';
import type { ColumnsType } from 'antd/es/table';
import ReactFlow, { Background, Controls, MiniMap, useNodesState, useEdgesState } from 'reactflow';
import 'reactflow/dist/style.css';
import { collaborationService } from '../services/collaborationService';
import { collaborationWorkflowService } from '../services/collaborationWorkflowService';
import { nodeTypes } from '../components/workflow/CustomNodes';
import { edgeTypes } from '../components/workflow/CustomEdges';
import type { WorkflowDefinition, WorkflowNode } from '../types/workflow-template';

const { TextArea } = Input;
const { Option } = Select;

interface Task {
  id: number | string;
  title: string;
  description?: string;
  prompt?: string;
  status: string;
  createdAt: string;
  collaborationId: string;
  gitUrl?: string;
  gitBranch?: string;
  gitToken?: string;
  config?: string;
  taskFlow?: string;
}

interface Agent {
  agentId: number;
  agentName: string;
  agentType?: string;
  role?: string;
}

interface CollaborationTasksProps {
  collaborationId: string;
  tasks: Task[];
  agents?: Agent[];
  onCreate: () => void;
  onExecute: (task: Task) => void;
  onEdit: (task: Task) => void;
  onViewHistory: (task: Task) => void;
  onDelete: (task: Task) => void;
  onRefresh: () => void;
}

const isMagenticTask = (task: Task) => {
  try {
    const config = task.config ? JSON.parse(task.config) : {};
    return config.workflowType === 'Magentic' || config.workflowType === 'ReviewIterative';
  } catch {
    return false;
  }
};

const hasTaskFlow = (task: Task) => {
  if (!task.taskFlow) return false;
  try {
    const flow = JSON.parse(task.taskFlow);
    return flow.nodes && flow.nodes.length > 0;
  } catch {
    return false;
  }
};

const getWorkflowTypeLabel = (task: Task) => {
  try {
    const config = task.config ? JSON.parse(task.config) : {};
    const wt = config.workflowType || '';
    if (wt === 'Magentic' || wt === 'ReviewIterative') return <Tag color="purple"><ThunderboltOutlined /> Magentic</Tag>;
    if (wt === 'Sequential') return <Tag color="blue">顺序执行</Tag>;
    if (wt === 'Concurrent') return <Tag color="green">并发执行</Tag>;
    if (wt === 'GroupChat') return <Tag color="orange">群聊协作</Tag>;
    return <Tag>默认</Tag>;
  } catch {
    return <Tag>默认</Tag>;
  }
};

const CollaborationTasks: React.FC<CollaborationTasksProps> = ({
  collaborationId,
  tasks,
  agents = [],
  onCreate,
  onExecute,
  onEdit,
  onViewHistory,
  onDelete,
  onRefresh,
}) => {
  const [selectedRowKeys, setSelectedRowKeys] = useState<Key[]>([]);

  const [orchestrationDrawerVisible, setOrchestrationDrawerVisible] = useState(false);
  const [orchestrationTask, setOrchestrationTask] = useState<Task | null>(null);
  const [orchestrationNodes, setOrchestrationNodes, onOrchestrationNodesChange] = useNodesState([]);
  const [orchestrationEdges, setOrchestrationEdges, onOrchestrationEdgesChange] = useEdgesState([]);
  const [orchestrationSaving, setOrchestrationSaving] = useState(false);
  const [autoGenerating, setAutoGenerating] = useState(false);
  const [selectedNode, setSelectedNode] = useState<WorkflowNode | null>(null);

  const handleSelectionChange = useCallback((newSelectedRowKeys: Key[]) => {
    setSelectedRowKeys(newSelectedRowKeys);
  }, []);

  const handleBatchDelete = useCallback(async () => {
    if (selectedRowKeys.length === 0) {
      message.warning('请先选择要删除的任务');
      return;
    }
    try {
      const taskIds = selectedRowKeys.map(id => typeof id === 'string' ? parseInt(id) : id) as number[];
      const result = await collaborationService.batchDeleteTasks(taskIds);
      message.success(result.message);
      setSelectedRowKeys([]);
      onRefresh();
    } catch (error) {
      message.error('批量删除失败');
    }
  }, [selectedRowKeys, onRefresh]);

  const handleOpenOrchestration = (task: Task) => {
    setOrchestrationTask(task);
    setSelectedNode(null);
    if (task.taskFlow) {
      try {
        const flow = JSON.parse(task.taskFlow) as WorkflowDefinition;
        const reactFlowNodes = flow.nodes.map((node, index) => ({
          id: node.id,
          type: node.type,
          position: { x: 250, y: index * 120 },
          data: node,
        }));
        const reactFlowEdges: any[] = [];
        flow.edges.forEach((edge: any, index: number) => {
          const targets = Array.isArray(edge.to) ? edge.to : [edge.to];
          targets.forEach((target: string, targetIndex: number) => {
            reactFlowEdges.push({
              id: `edge-${index}-${targetIndex}`,
              source: edge.from,
              target: target,
              type: 'custom',
              data: { type: edge.type, description: edge.description },
              animated: edge.type === 'fan-out',
            });
          });
        });
        setOrchestrationNodes(reactFlowNodes);
        setOrchestrationEdges(reactFlowEdges);
      } catch {
        setOrchestrationNodes([]);
        setOrchestrationEdges([]);
      }
    } else {
      setOrchestrationNodes([]);
      setOrchestrationEdges([]);
    }
    setOrchestrationDrawerVisible(true);
  };

  const handleAddOrchestrationNode = (type: string) => {
    const id = `node-${Date.now()}`;
    const newNode: WorkflowNode = {
      id,
      type: type as any,
      name: type === 'start' ? '开始' : type === 'agent' ? '新Agent节点' : type === 'aggregator' ? '汇总结果' : type === 'condition' ? '条件判断' : type === 'review' ? '审核节点' : '循环节点',
    };
    const yPos = orchestrationNodes.length > 0
      ? Math.max(...orchestrationNodes.map(n => n.position.y)) + 120
      : 0;
    setOrchestrationNodes([
      ...orchestrationNodes,
      { id, type, position: { x: 300, y: yPos }, data: newNode },
    ]);
  };

  const handleOrchestrationNodeClick = (_: React.MouseEvent, node: any) => {
    setSelectedNode(node.data as WorkflowNode);
  };

  const handleUpdateNode = (updatedNode: WorkflowNode) => {
    setOrchestrationNodes(nodes =>
      nodes.map(n => n.id === updatedNode.id ? { ...n, data: updatedNode } : n)
    );
    setSelectedNode(updatedNode);
  };

  const handleDeleteNode = (nodeId: string) => {
    setOrchestrationNodes(nodes => nodes.filter(n => n.id !== nodeId));
    setOrchestrationEdges(edges => edges.filter(e => e.source !== nodeId && e.target !== nodeId));
    if (selectedNode?.id === nodeId) {
      setSelectedNode(null);
    }
  };

  const handleAutoGenerateFlow = async () => {
    if (!orchestrationTask) return;
    setAutoGenerating(true);
    try {
      const taskDesc = orchestrationTask.description || orchestrationTask.title || '请生成工作流';
      const result = await collaborationWorkflowService.generateMagenticPlan(
        Number(collaborationId),
        taskDesc
      );
      if (result.success && result.workflow) {
        const flow = result.workflow;
        const reactFlowNodes = flow.nodes.map((node, index) => ({
          id: node.id,
          type: node.type,
          position: { x: 250, y: index * 120 },
          data: node,
        }));
        const reactFlowEdges = flow.edges.map((edge, index) => ({
          id: `edge-${index}`,
          source: edge.from,
          target: Array.isArray(edge.to) ? edge.to[0] : edge.to,
          type: 'custom',
          data: { type: edge.type, description: edge.description },
          animated: edge.type === 'fan-out',
        }));
        setOrchestrationNodes(reactFlowNodes);
        setOrchestrationEdges(reactFlowEdges);
        setSelectedNode(null);
        message.success('流程编排已自动生成，您可以继续人工修改');
      } else {
        message.error(result.error || '自动生成流程编排失败');
      }
    } catch (error: any) {
      message.error('自动生成流程编排失败: ' + (error.message || '未知错误'));
    } finally {
      setAutoGenerating(false);
    }
  };

  const handleSaveOrchestration = async () => {
    if (!orchestrationTask) return;
    setOrchestrationSaving(true);
    try {
      const nodes = orchestrationNodes.map(n => n.data as WorkflowNode);
      const edges = orchestrationEdges.map((e, index) => ({
        type: (e.data as any)?.type || 'sequential',
        from: e.source,
        to: e.target,
        description: (e.data as any)?.description,
      }));
      const taskFlow = JSON.stringify({ nodes, edges });
      await collaborationService.updateTaskFlow(String(orchestrationTask.id), taskFlow);
      message.success('流程编排保存成功');
      setOrchestrationDrawerVisible(false);
      setSelectedNode(null);
      onRefresh();
    } catch (error: any) {
      message.error('保存失败: ' + (error.message || '未知错误'));
    } finally {
      setOrchestrationSaving(false);
    }
  };

  const columns: ColumnsType<Task> = useMemo(() => [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: 60,
    },
    {
      title: '标题',
      dataIndex: 'title',
      key: 'title',
      width: '15%',
      ellipsis: true,
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
      width: '22%',
      ellipsis: true,
    },
    {
      title: '工作流类型',
      key: 'workflowType',
      width: 100,
      render: (_: unknown, record: Task) => getWorkflowTypeLabel(record),
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 80,
      render: (status: string) => {
        const colorMap: Record<string, string> = {
          Pending: 'default',
          InProgress: 'processing',
          Completed: 'success',
          Failed: 'error',
          Cancelled: 'warning',
        };
        const textMap: Record<string, string> = {
          Pending: '待处理',
          InProgress: '进行中',
          Completed: '已完成',
          Failed: '失败',
          Cancelled: '已关闭',
        };
        return <Tag color={colorMap[status]}>{textMap[status] || status}</Tag>;
      },
    },
    {
      title: 'Git配置',
      key: 'git',
      width: 80,
      render: (_: any, record: Task) => {
        if (!record.gitUrl) {
          return <Tag color="default">无</Tag>;
        }
        return (
          <Tooltip title={
            <div>
              <div>仓库: {record.gitUrl}</div>
              {record.gitBranch && <div>分支: {record.gitBranch}</div>}
            </div>
          }>
            <Tag color="blue" icon={<GithubOutlined />}>
              {record.gitBranch || 'main'}
            </Tag>
          </Tooltip>
        );
      },
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 130,
      render: (date: string) => new Date(date).toLocaleString('zh-CN'),
    },
    {
      title: '操作',
      key: 'action',
      width: 280,
      render: (_: any, record: Task) => {
        const magentic = isMagenticTask(record);
        const hasFlow = hasTaskFlow(record);
        const canExecute = record.status !== 'Completed' && record.status !== 'Cancelled'
          && record.status !== 'Failed'
          && (!magentic || hasFlow);

        return (
          <Space size="small" wrap>
            {magentic && (
              <Button
                size="small"
                icon={<ApartmentOutlined />}
                onClick={() => handleOpenOrchestration(record)}
              >
                流程编排
              </Button>
            )}
            <Tooltip title={!canExecute && magentic && !hasFlow ? '请先配置流程编排' : ''}>
              <Button
                type="primary"
                size="small"
                icon={<PlayCircleOutlined />}
                onClick={() => onExecute(record)}
                disabled={!canExecute}
              >
                执行
              </Button>
            </Tooltip>
            <Button
              size="small"
              icon={<EditOutlined />}
              onClick={() => onEdit(record)}
            >
              编辑
            </Button>
            <Button
              size="small"
              icon={<MessageOutlined />}
              onClick={() => onViewHistory(record)}
            >
              协作过程
            </Button>
            <Popconfirm
              title="确定删除此任务吗？"
              onConfirm={() => onDelete(record)}
              okText="确定"
              cancelText="取消"
            >
              <Button
                size="small"
                danger
                icon={<DeleteOutlined />}
              >
                删除
              </Button>
            </Popconfirm>
          </Space>
        );
      },
    },
  ], [onExecute, onEdit, onViewHistory, onDelete, agents]);

  const rowSelection = {
    selectedRowKeys,
    onChange: handleSelectionChange,
  };

  return (
    <div>
      <Space style={{ marginBottom: 16 }}>
        <Button type="primary" onClick={onCreate}>
          创建任务
        </Button>
        {selectedRowKeys.length > 0 && (
          <Popconfirm
            title={`确定要删除选中的 ${selectedRowKeys.length} 个任务吗？`}
            onConfirm={handleBatchDelete}
            okText="确定"
            cancelText="取消"
          >
            <Button danger icon={<DeleteOutlined />}>
              批量删除 ({selectedRowKeys.length})
            </Button>
          </Popconfirm>
        )}
      </Space>
      <Table
        dataSource={tasks}
        columns={columns}
        rowKey="id"
        pagination={false}
        rowSelection={rowSelection}
      />

      <Drawer
        title={
          <Space>
            <ApartmentOutlined />
            <span>流程编排 - {orchestrationTask?.title}</span>
          </Space>
        }
        open={orchestrationDrawerVisible}
        onClose={() => { setOrchestrationDrawerVisible(false); setSelectedNode(null); }}
        width="90vw"
        destroyOnClose
        extra={
          <Space>
            <Button onClick={() => { setOrchestrationDrawerVisible(false); setSelectedNode(null); }}>取消</Button>
            <Button type="primary" icon={<SaveOutlined />} onClick={handleSaveOrchestration} loading={orchestrationSaving}>
              保存编排
            </Button>
          </Space>
        }
      >
        {orchestrationTask && (
          <div>
            <Alert
              message="在此编排Magentic工作流的执行流程。点击「自动生成流程编排」让Manager生成，或手动添加节点。点击节点可编辑属性。"
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
            />
            <Row gutter={16}>
              <Col span={selectedNode ? 14 : 24}>
                <Card size="small" title="添加节点" style={{ marginBottom: 12 }}>
                  <Space wrap>
                    <Button
                      type="primary"
                      size="small"
                      onClick={handleAutoGenerateFlow}
                      icon={<BulbOutlined />}
                      loading={autoGenerating}
                    >
                      自动生成流程编排
                    </Button>
                    <Button size="small" onClick={() => handleAddOrchestrationNode('start')} icon={<PlayCircleOutlined />}>
                      开始节点
                    </Button>
                    <Button size="small" onClick={() => handleAddOrchestrationNode('agent')} icon={<RobotOutlined />}>
                      Agent节点
                    </Button>
                    <Button size="small" onClick={() => handleAddOrchestrationNode('aggregator')} icon={<TeamOutlined />}>
                      汇总节点
                    </Button>
                    <Button size="small" onClick={() => handleAddOrchestrationNode('condition')} icon={<BulbOutlined />}>
                      条件节点
                    </Button>
                    <Button size="small" onClick={() => handleAddOrchestrationNode('review')} icon={<SafetyCertificateOutlined />}>
                      审核节点
                    </Button>
                  </Space>
                </Card>
                <Card size="small" title="工作流图" style={{ marginBottom: 12 }}>
                  <div style={{ height: '450px' }}>
                    <ReactFlow
                      nodes={orchestrationNodes}
                      edges={orchestrationEdges}
                      nodeTypes={nodeTypes}
                      edgeTypes={edgeTypes}
                      onNodesChange={onOrchestrationNodesChange}
                      onEdgesChange={onOrchestrationEdgesChange}
                      onNodeClick={handleOrchestrationNodeClick}
                      fitView
                    >
                      <Background />
                      <Controls />
                      <MiniMap />
                    </ReactFlow>
                  </div>
                </Card>
                <Card size="small" title="任务信息" style={{ marginBottom: 12 }}>
                  <div style={{ marginBottom: 8 }}>
                    <strong>任务描述：</strong>
                    {orchestrationTask.description || orchestrationTask.title}
                  </div>
                  {agents.length > 0 && (
                    <div>
                      <strong>可用Agent：</strong>
                      <div style={{ marginTop: 4 }}>
                        {agents.map(agent => (
                          <Tag key={agent.agentId} color={agent.role === 'Manager' ? 'gold' : 'blue'} style={{ marginBottom: 4 }}>
                            {agent.agentName} ({agent.role || 'Worker'})
                          </Tag>
                        ))}
                      </div>
                    </div>
                  )}
                </Card>
              </Col>
              {selectedNode && (
                <Col span={10}>
                  <Card
                    size="small"
                    title={
                      <Space>
                        <EditOutlined />
                        <span>节点属性 - {selectedNode.name}</span>
                        <Button type="text" size="small" onClick={() => setSelectedNode(null)}>
                          关闭
                        </Button>
                      </Space>
                    }
                    style={{ position: 'sticky', top: 0 }}
                  >
                    <Form layout="vertical" size="small">
                      <Form.Item label="节点ID">
                        <Input value={selectedNode.id} disabled />
                      </Form.Item>
                      <Form.Item label="节点名称" required>
                        <Input
                          value={selectedNode.name}
                          onChange={(e) => handleUpdateNode({ ...selectedNode, name: e.target.value })}
                          placeholder="请输入节点名称"
                        />
                      </Form.Item>

                      {selectedNode.type === 'agent' && (
                        <>
                          <Form.Item label="选择Agent">
                            <Select
                              value={selectedNode.agentId || undefined}
                              onChange={(val) => {
                                const agent = agents.find(a => String(a.agentId) === val);
                                handleUpdateNode({
                                  ...selectedNode,
                                  agentId: val,
                                  agentRole: agent?.agentType || agent?.role || agent?.agentName || '',
                                });
                              }}
                              placeholder="请选择Agent"
                              allowClear
                              showSearch
                              optionFilterProp="children"
                            >
                              {agents.map(agent => (
                                <Option key={agent.agentId} value={String(agent.agentId)}>
                                  <Space>
                                    <Tag color={agent.role === 'Manager' ? 'gold' : 'blue'}>
                                      {agent.agentType || agent.role || 'Worker'}
                                    </Tag>
                                    {agent.agentName}
                                  </Space>
                                </Option>
                              ))}
                            </Select>
                          </Form.Item>
                          <Form.Item label="Agent职业">
                            <Input
                              value={selectedNode.agentRole || ''}
                              disabled
                              placeholder="选择Agent后自动填充"
                            />
                          </Form.Item>
                          <Form.Item label="任务描述/输入模板">
                            <TextArea
                              value={selectedNode.inputTemplate || ''}
                              onChange={(e) => handleUpdateNode({ ...selectedNode, inputTemplate: e.target.value })}
                              rows={4}
                              placeholder="使用 {{参数名}} 表示参数，例如：请分析{{topic}}的{{aspect}}"
                            />
                          </Form.Item>
                          <Alert
                            message="参数使用说明"
                            description={
                              <div style={{ fontSize: 12 }}>
                                <div>• {'{{input}}'} = 上游节点的输出结果</div>
                                <div>• {'{{originalInput}}'} / {'{{task}}'} = 原始任务描述</div>
                                <div>• {'{{lastResult}}'} = 最近一个节点的输出</div>
                                <div>• {'{{nodeId}}'} = 引用指定节点的输出（如 {'{{node-1}}'}）</div>
                                <div>• 不写模板时，默认使用上游节点的输出作为输入</div>
                              </div>
                            }
                            type="info"
                            showIcon
                            style={{ marginBottom: 12 }}
                          />
                        </>
                      )}

                      {(selectedNode.type === 'condition' || selectedNode.type === 'loop') && (
                        <>
                          <Form.Item label="条件表达式">
                            <TextArea
                              value={selectedNode.condition || ''}
                              onChange={(e) => handleUpdateNode({ ...selectedNode, condition: e.target.value })}
                              rows={3}
                              placeholder={'例如：result.Contains("APPROVED")'}
                            />
                          </Form.Item>
                          <Alert
                            message="C# 条件表达式语法"
                            description={
                              <div style={{ fontSize: 12, fontFamily: 'monospace' }}>
                                <div><b>字符串包含：</b>result.Contains("关键词")</div>
                                <div><b>字符串相等：</b>result.Equals("完成")</div>
                                <div><b>开头匹配：</b>result.StartsWith("错误")</div>
                                <div><b>结尾匹配：</b>result.EndsWith("通过")</div>
                                <div><b>长度判断：</b>result.Length &gt; 100</div>
                                <div><b>空值判断：</b>result.IsNullOrEmpty</div>
                                <div><b>取反：</b>!result.Contains("失败")</div>
                                <div><b>组合：</b>result.Contains("A") &amp;&amp; result.Length &gt; 50</div>
                                <div><b>或：</b>result.Contains("A") || result.Contains("B")</div>
                              </div>
                            }
                            type="info"
                            showIcon
                            style={{ marginBottom: 12 }}
                          />
                        </>
                      )}

                      {selectedNode.type === 'review' && (
                        <>
                          <Form.Item label="选择审核Agent">
                            <Select
                              value={selectedNode.agentId || undefined}
                              onChange={(val) => {
                                const agent = agents.find(a => String(a.agentId) === val);
                                handleUpdateNode({
                                  ...selectedNode,
                                  agentId: val,
                                  agentRole: agent?.agentType || agent?.role || '审核者',
                                });
                              }}
                              placeholder="请选择审核Agent"
                              allowClear
                              showSearch
                              optionFilterProp="children"
                            >
                              {agents.map(agent => (
                                <Option key={agent.agentId} value={String(agent.agentId)}>
                                  <Space>
                                    <Tag color={agent.role === 'Manager' ? 'gold' : 'blue'}>
                                      {agent.agentType || agent.role || 'Worker'}
                                    </Tag>
                                    {agent.agentName}
                                  </Space>
                                </Option>
                              ))}
                            </Select>
                          </Form.Item>
                          <Form.Item label="审核角色">
                            <Input
                              value={selectedNode.agentRole || ''}
                              disabled
                              placeholder="例如：审核者、QA"
                            />
                          </Form.Item>
                          <Form.Item label="审核标准">
                            <TextArea
                              value={selectedNode.reviewCriteria || ''}
                              onChange={(e) => handleUpdateNode({ ...selectedNode, reviewCriteria: e.target.value })}
                              rows={3}
                              placeholder="例如：请从代码质量、安全性、可维护性方面审核"
                            />
                          </Form.Item>
                          <Form.Item label="审核通过关键词">
                            <Input
                              value={selectedNode.approvalKeyword || ''}
                              onChange={(e) => handleUpdateNode({ ...selectedNode, approvalKeyword: e.target.value })}
                              placeholder="[APPROVED]"
                            />
                          </Form.Item>
                          <Form.Item label="打回目标节点ID">
                            <Select
                              value={selectedNode.rejectTargetNode || undefined}
                              onChange={(val) => handleUpdateNode({ ...selectedNode, rejectTargetNode: val })}
                              placeholder="审核不通过时打回到哪个节点"
                              allowClear
                            >
                              {orchestrationNodes
                                .filter(n => n.id !== selectedNode.id && n.data?.type !== 'start')
                                .map(n => (
                                  <Option key={n.id} value={n.id}>
                                    {n.data?.name || n.id}
                                  </Option>
                                ))}
                            </Select>
                          </Form.Item>
                          <Form.Item label="最大重试次数">
                            <Select
                              value={selectedNode.maxRetries || 3}
                              onChange={(val) => handleUpdateNode({ ...selectedNode, maxRetries: val })}
                            >
                              <Option value={1}>1次</Option>
                              <Option value={2}>2次</Option>
                              <Option value={3}>3次</Option>
                              <Option value={5}>5次</Option>
                              <Option value={10}>10次</Option>
                            </Select>
                          </Form.Item>
                          <Alert
                            message="审核节点说明"
                            description={
                              <div style={{ fontSize: 12 }}>
                                <div>• 审核Agent会审核前序节点的工作成果</div>
                                <div>• 审核通过（回复包含通过关键词）→ 走"通过"路径</div>
                                <div>• 审核不通过 → 打回到目标节点重新执行</div>
                                <div>• 超过最大重试次数后强制通过</div>
                              </div>
                            }
                            type="info"
                            showIcon
                            style={{ marginBottom: 12 }}
                          />
                        </>
                      )}

                      {selectedNode.type === 'start' && (
                        <Alert message="开始节点无需配置参数，工作流将从此节点开始执行。" type="info" showIcon />
                      )}

                      {selectedNode.type === 'aggregator' && (
                        <>
                          <Form.Item label="选择汇总Agent">
                            <Select
                              value={selectedNode.agentId || undefined}
                              onChange={(val) => {
                                const agent = agents.find(a => String(a.agentId) === val);
                                handleUpdateNode({
                                  ...selectedNode,
                                  agentId: val,
                                  agentRole: agent?.agentType || agent?.role || agent?.agentName || '',
                                });
                              }}
                              placeholder="请选择负责汇总的Agent"
                              allowClear
                              showSearch
                              optionFilterProp="children"
                            >
                              {agents.map(agent => (
                                <Option key={agent.agentId} value={String(agent.agentId)}>
                                  <Space>
                                    <Tag color={agent.role === 'Manager' ? 'gold' : 'blue'}>
                                      {agent.agentType || agent.role || 'Worker'}
                                    </Tag>
                                    {agent.agentName}
                                  </Space>
                                </Option>
                              ))}
                            </Select>
                          </Form.Item>
                          <Form.Item label="Agent职业">
                            <Input
                              value={selectedNode.agentRole || ''}
                              disabled
                              placeholder="选择Agent后自动填充"
                            />
                          </Form.Item>
                          <Form.Item label="汇总指令">
                            <TextArea
                              value={selectedNode.inputTemplate || ''}
                              onChange={(e) => handleUpdateNode({ ...selectedNode, inputTemplate: e.target.value })}
                              rows={3}
                              placeholder="请输入汇总指令，例如：请汇总以上所有结果，生成最终报告"
                            />
                          </Form.Item>
                          <Alert
                            message="汇总节点由指定Agent汇总所有上游节点的结果，请选择合适的Agent并填写汇总指令。"
                            type="info"
                            showIcon
                          />
                        </>
                      )}

                      <Divider />
                      <Button
                        danger
                        icon={<DeleteOutlined />}
                        onClick={() => handleDeleteNode(selectedNode.id)}
                        block
                        disabled={selectedNode.type === 'start'}
                      >
                        删除节点
                      </Button>
                    </Form>
                  </Card>
                </Col>
              )}
            </Row>
          </div>
        )}
      </Drawer>
    </div>
  );
};

export default CollaborationTasks;
