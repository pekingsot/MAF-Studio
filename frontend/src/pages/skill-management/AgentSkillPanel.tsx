import { getErrorMessage } from '../../utils/errorHandler';
import React, { useState, useEffect } from 'react';
import {
  Card, Table, Button, Modal, Form, Input, Select, Switch, InputNumber,
  message, Space, Tag, Tooltip, Badge, Empty, Spin
} from 'antd';
import {
  PlusOutlined, DeleteOutlined, EditOutlined, AppstoreOutlined,
  ThunderboltOutlined, FileTextOutlined, CheckCircleOutlined
} from '@ant-design/icons';
import { skillService, AgentSkill, SkillTemplate, SkillDefinition } from '../../services/skillService';

const { TextArea } = Input;
const { Option } = Select;

const SKILL_CONTENT_TEMPLATE = `---
name: skill-name
description: "Skill description - when to use this skill"
version: 1.0.0
author: 
allowed-tools:
  - ReadFile
tags:
  - tag1
permissions:
  network: false
  filesystem: true
  shell: false
---

# Skill Name

## When to use this skill
- Condition 1
- Condition 2

## How to use this skill
1. Step 1
2. Step 2

## Output Format
- Format description

## Constraints
- Constraint 1
`;

const AgentSkillPanel: React.FC<{ agentId: number; agentName: string }> = ({ agentId, agentName }) => {
  const [skills, setSkills] = useState<AgentSkill[]>([]);
  const [templates, setTemplates] = useState<SkillTemplate[]>([]);
  const [loading, setLoading] = useState(false);
  const [addModalVisible, setAddModalVisible] = useState(false);
  const [templateModalVisible, setTemplateModalVisible] = useState(false);
  const [editModalVisible, setEditModalVisible] = useState(false);
  const [previewModalVisible, setPreviewModalVisible] = useState(false);
  const [selectedSkill, setSelectedSkill] = useState<AgentSkill | null>(null);
  const [parsedDefinition, setParsedDefinition] = useState<SkillDefinition | null>(null);
  const [addForm] = Form.useForm();
  const [editForm] = Form.useForm();

  useEffect(() => {
    loadSkills();
    loadTemplates();
  }, [agentId]);

  const loadSkills = async () => {
    setLoading(true);
    try {
      const data = await skillService.getAgentSkills(agentId);
      setSkills(data);
    } catch (error: unknown) {
      message.error(`加载技能列表失败: ${getErrorMessage(error)}`);
    } finally {
      setLoading(false);
    }
  };

  const loadTemplates = async () => {
    try {
      const data = await skillService.getTemplates();
      setTemplates(data);
    } catch (error: unknown) {
      console.error('加载模板失败:', error);
    }
  };

  const handleAddSkill = async (values: Record<string, unknown>) => {
    try {
      await skillService.addSkillToAgent(agentId, {
        skill_name: values.skill_name,
        skill_content: values.skill_content,
        enabled: values.enabled ?? true,
        priority: values.priority ?? 0,
        runtime: values.runtime ?? 'python',
        entry_point: values.entry_point,
        allowed_tools: values.allowed_tools ? values.allowed_tools.split(',').map((s: string) => s.trim()).filter(Boolean) : undefined,
      });
      message.success(`技能 ${values.skill_name} 添加成功`);
      setAddModalVisible(false);
      addForm.resetFields();
      loadSkills();
    } catch (error: unknown) {
      message.error(`添加失败: ${getErrorMessage(error)}`);
    }
  };

  const handleAddFromTemplate = async (template: SkillTemplate) => {
    try {
      await skillService.addSkillFromTemplate(agentId, template.id, template.name);
      message.success(`从模板 ${template.name} 创建技能成功`);
      setTemplateModalVisible(false);
      loadSkills();
    } catch (error: unknown) {
      message.error(`创建失败: ${getErrorMessage(error)}`);
    }
  };

  const handleToggleEnabled = async (skill: AgentSkill) => {
    try {
      await skillService.updateSkill(agentId, skill.id, { enabled: !skill.enabled });
      message.success(`技能已${skill.enabled ? '禁用' : '启用'}`);
      loadSkills();
    } catch (error: unknown) {
      message.error(`操作失败: ${getErrorMessage(error)}`);
    }
  };

  const handleDeleteSkill = async (skill: AgentSkill) => {
    Modal.confirm({
      title: '确认删除',
      content: `确定要删除技能 "${skill.skill_name}" 吗？`,
      onOk: async () => {
        try {
          await skillService.deleteSkill(agentId, skill.id);
          message.success('技能已删除');
          loadSkills();
        } catch (error: unknown) {
          message.error(`删除失败: ${getErrorMessage(error)}`);
        }
      },
    });
  };

  const handleEditSkill = async (values: Record<string, unknown>) => {
    if (!selectedSkill) return;
    try {
      await skillService.updateSkill(agentId, selectedSkill.id, {
        skill_content: values.skill_content,
        enabled: values.enabled,
        priority: values.priority,
        runtime: values.runtime,
        entry_point: values.entry_point,
        allowed_tools: values.allowed_tools ? values.allowed_tools.split(',').map((s: string) => s.trim()).filter(Boolean) : undefined,
      });
      message.success('技能更新成功');
      setEditModalVisible(false);
      loadSkills();
    } catch (error: unknown) {
      message.error(`更新失败: ${getErrorMessage(error)}`);
    }
  };

  const handlePreview = async (skill: AgentSkill) => {
    setSelectedSkill(skill);
    try {
      const def = await skillService.parseSkillContent(skill.skill_content);
      setParsedDefinition(def);
    } catch {
      setParsedDefinition(null);
    }
    setPreviewModalVisible(true);
  };

  const handleEdit = (skill: AgentSkill) => {
    setSelectedSkill(skill);
    editForm.setFieldsValue({
      skill_content: skill.skill_content,
      enabled: skill.enabled,
      priority: skill.priority,
      runtime: skill.runtime,
      entry_point: skill.entry_point,
      allowed_tools: skill.allowed_tools?.join(', '),
    });
    setEditModalVisible(true);
  };

  const skillColumns = [
    {
      title: '技能名称',
      dataIndex: 'skill_name',
      key: 'skill_name',
      render: (name: string, record: AgentSkill) => (
        <Space>
          <span style={{ fontWeight: 500 }}>{name}</span>
          {!record.enabled && <Tag color="default">已禁用</Tag>}
        </Space>
      ),
    },
    {
      title: '运行时',
      dataIndex: 'runtime',
      key: 'runtime',
      width: 100,
      render: (runtime: string) => {
        const colorMap: Record<string, string> = { python: 'green', node: 'blue', bash: 'orange' };
        return <Tag color={colorMap[runtime] || 'default'}>{runtime}</Tag>;
      },
    },
    {
      title: '优先级',
      dataIndex: 'priority',
      key: 'priority',
      width: 80,
      sorter: (a: AgentSkill, b: AgentSkill) => a.priority - b.priority,
    },
    {
      title: '入口',
      dataIndex: 'entry_point',
      key: 'entry_point',
      width: 150,
      ellipsis: true,
      render: (v: string) => v || <span style={{ color: '#999' }}>仅指令</span>,
    },
    {
      title: '启用',
      dataIndex: 'enabled',
      key: 'enabled',
      width: 80,
      render: (enabled: boolean, record: AgentSkill) => (
        <Switch size="small" checked={enabled} onChange={() => handleToggleEnabled(record)} />
      ),
    },
    {
      title: '操作',
      key: 'action',
      width: 200,
      render: (_: unknown, record: AgentSkill) => (
        <Space size="small">
          <Tooltip title="预览解析结果">
            <Button type="link" size="small" icon={<FileTextOutlined />} onClick={() => handlePreview(record)} />
          </Tooltip>
          <Tooltip title="编辑">
            <Button type="link" size="small" icon={<EditOutlined />} onClick={() => handleEdit(record)} />
          </Tooltip>
          <Tooltip title="删除">
            <Button type="link" size="small" danger icon={<DeleteOutlined />} onClick={() => handleDeleteSkill(record)} />
          </Tooltip>
        </Space>
      ),
    },
  ];

  const templateColumns = [
    {
      title: '模板名称',
      dataIndex: 'name',
      key: 'name',
      render: (name: string, record: SkillTemplate) => (
        <Space>
          <span style={{ fontWeight: 500 }}>{name}</span>
          {record.is_official && <Tag color="gold">官方</Tag>}
        </Space>
      ),
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
    },
    {
      title: '分类',
      dataIndex: 'category',
      key: 'category',
      width: 120,
      render: (category: string) => category ? <Tag>{category}</Tag> : '-',
    },
    {
      title: '运行时',
      dataIndex: 'runtime',
      key: 'runtime',
      width: 100,
      render: (runtime: string) => <Tag color="green">{runtime}</Tag>,
    },
    {
      title: '使用次数',
      dataIndex: 'usage_count',
      key: 'usage_count',
      width: 90,
    },
    {
      title: '操作',
      key: 'action',
      width: 100,
      render: (_: unknown, record: SkillTemplate) => (
        <Button
          type="primary"
          size="small"
          icon={<PlusOutlined />}
          onClick={() => handleAddFromTemplate(record)}
        >
          使用
        </Button>
      ),
    },
  ];

  return (
    <div>
      <Card
        title={
          <Space>
            <ThunderboltOutlined />
            <span>{agentName} - 技能配置</span>
            <Badge count={skills.filter(s => s.enabled).length} style={{ backgroundColor: '#52c41a' }} />
          </Space>
        }
        extra={
          <Space>
            <Button icon={<AppstoreOutlined />} onClick={() => setTemplateModalVisible(true)}>
              从模板添加
            </Button>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => setAddModalVisible(true)}>
              自定义技能
            </Button>
          </Space>
        }
      >
        <Table
          columns={skillColumns}
          dataSource={skills}
          rowKey="id"
          loading={loading}
          pagination={false}
          locale={{ emptyText: <Empty description="暂无技能，点击上方按钮添加" /> }}
        />
      </Card>

      <Modal
        title="添加自定义技能"
        open={addModalVisible}
        onCancel={() => { setAddModalVisible(false); addForm.resetFields(); }}
        onOk={() => addForm.submit()}
        width={800}
        destroyOnClose
      >
        <Form form={addForm} onFinish={handleAddSkill} layout="vertical">
          <Form.Item name="skill_name" label="技能名称" rules={[{ required: true, message: '请输入技能名称' }]}>
            <Input placeholder="例如: code-review, api-doc-generator" />
          </Form.Item>
          <Space size="large">
            <Form.Item name="runtime" label="运行时" initialValue="python">
              <Select style={{ width: 120 }}>
                <Option value="python">Python</Option>
                <Option value="node">Node.js</Option>
                <Option value="bash">Bash</Option>
              </Select>
            </Form.Item>
            <Form.Item name="priority" label="优先级" initialValue={0}>
              <InputNumber min={0} max={100} />
            </Form.Item>
            <Form.Item name="enabled" label="启用" valuePropName="checked" initialValue={true}>
              <Switch />
            </Form.Item>
          </Space>
          <Form.Item name="entry_point" label="入口文件（可选）">
            <Input placeholder="例如: scripts/main.py" />
          </Form.Item>
          <Form.Item name="allowed_tools" label="允许工具（逗号分隔）">
            <Input placeholder="例如: ReadFile, WriteFile, SearchInCode" />
          </Form.Item>
          <Form.Item name="skill_content" label="SKILL.md 内容" rules={[{ required: true, message: '请输入技能内容' }]} initialValue={SKILL_CONTENT_TEMPLATE}>
            <TextArea rows={16} style={{ fontFamily: 'monospace', fontSize: 13 }} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="从模板添加技能"
        open={templateModalVisible}
        onCancel={() => setTemplateModalVisible(false)}
        footer={null}
        width={900}
      >
        <Table
          columns={templateColumns}
          dataSource={templates}
          rowKey="id"
          pagination={{ pageSize: 5 }}
          size="small"
        />
      </Modal>

      <Modal
        title={`编辑技能: ${selectedSkill?.skill_name}`}
        open={editModalVisible}
        onCancel={() => setEditModalVisible(false)}
        onOk={() => editForm.submit()}
        width={800}
        destroyOnClose
      >
        <Form form={editForm} onFinish={handleEditSkill} layout="vertical">
          <Space size="large">
            <Form.Item name="runtime" label="运行时">
              <Select style={{ width: 120 }}>
                <Option value="python">Python</Option>
                <Option value="node">Node.js</Option>
                <Option value="bash">Bash</Option>
              </Select>
            </Form.Item>
            <Form.Item name="priority" label="优先级">
              <InputNumber min={0} max={100} />
            </Form.Item>
            <Form.Item name="enabled" label="启用" valuePropName="checked">
              <Switch />
            </Form.Item>
          </Space>
          <Form.Item name="entry_point" label="入口文件">
            <Input placeholder="例如: scripts/main.py" />
          </Form.Item>
          <Form.Item name="allowed_tools" label="允许工具（逗号分隔）">
            <Input placeholder="例如: ReadFile, WriteFile, SearchInCode" />
          </Form.Item>
          <Form.Item name="skill_content" label="SKILL.md 内容" rules={[{ required: true }]}>
            <TextArea rows={16} style={{ fontFamily: 'monospace', fontSize: 13 }} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title={`技能预览: ${selectedSkill?.skill_name}`}
        open={previewModalVisible}
        onCancel={() => { setPreviewModalVisible(false); setParsedDefinition(null); }}
        footer={null}
        width={700}
      >
        {parsedDefinition ? (
          <div>
            <Card size="small" title="基本信息" style={{ marginBottom: 12 }}>
              <Space wrap>
                <Tag color="blue">{parsedDefinition.name}</Tag>
                {parsedDefinition.version && <Tag>版本: {parsedDefinition.version}</Tag>}
                {parsedDefinition.author && <Tag>作者: {parsedDefinition.author}</Tag>}
                {parsedDefinition.runtime && <Tag color="green">运行时: {parsedDefinition.runtime}</Tag>}
              </Space>
              {parsedDefinition.description && (
                <p style={{ marginTop: 8, color: '#666' }}>{parsedDefinition.description}</p>
              )}
            </Card>

            {parsedDefinition.allowed_tools.length > 0 && (
              <Card size="small" title="允许工具" style={{ marginBottom: 12 }}>
                <Space wrap>
                  {parsedDefinition.allowed_tools.map((tool, i) => (
                    <Tag key={i} color="cyan">{tool}</Tag>
                  ))}
                </Space>
              </Card>
            )}

            {parsedDefinition.tags.length > 0 && (
              <Card size="small" title="标签" style={{ marginBottom: 12 }}>
                <Space wrap>
                  {parsedDefinition.tags.map((tag, i) => (
                    <Tag key={i}>{tag}</Tag>
                  ))}
                </Space>
              </Card>
            )}

            {parsedDefinition.permissions && (
              <Card size="small" title="权限" style={{ marginBottom: 12 }}>
                <Space wrap>
                  <Tag color={parsedDefinition.permissions.network ? 'red' : 'default'}>
                    网络: {parsedDefinition.permissions.network ? '允许' : '禁止'}
                  </Tag>
                  <Tag color={parsedDefinition.permissions.filesystem ? 'red' : 'default'}>
                    文件系统: {parsedDefinition.permissions.filesystem ? '允许' : '禁止'}
                  </Tag>
                  <Tag color={parsedDefinition.permissions.shell ? 'red' : 'default'}>
                    Shell: {parsedDefinition.permissions.shell ? '允许' : '禁止'}
                  </Tag>
                </Space>
              </Card>
            )}

            {parsedDefinition.inputs.length > 0 && (
              <Card size="small" title="输入参数" style={{ marginBottom: 12 }}>
                <Table
                  size="small"
                  pagination={false}
                  dataSource={parsedDefinition.inputs}
                  rowKey="name"
                  columns={[
                    { title: '名称', dataIndex: 'name', key: 'name' },
                    { title: '类型', dataIndex: 'type', key: 'type' },
                    { title: '必填', dataIndex: 'required', key: 'required', render: (v: boolean) => v ? <CheckCircleOutlined style={{ color: '#52c41a' }} /> : '-' },
                    { title: '默认值', dataIndex: 'default', key: 'default' },
                    { title: '描述', dataIndex: 'description', key: 'description', ellipsis: true },
                  ]}
                />
              </Card>
            )}

            <Card size="small" title="指令内容">
              <pre style={{ whiteSpace: 'pre-wrap', fontSize: 13, maxHeight: 300, overflow: 'auto' }}>
                {parsedDefinition.instructions}
              </pre>
            </Card>
          </div>
        ) : (
          <Spin>解析中...</Spin>
        )}
      </Modal>
    </div>
  );
};

export default AgentSkillPanel;
