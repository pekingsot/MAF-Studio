import { getErrorMessage } from '../utils/errorHandler';
import React, { useState, useEffect } from 'react';
import {
  Card,
  Table,
  Button,
  Input,
  Space,
  Tag,
  Modal,
  message,
  Popconfirm,
  Select,
  Tooltip,
} from 'antd';
import {
  PlusOutlined,
  SearchOutlined,
  EditOutlined,
  DeleteOutlined,
  PlayCircleOutlined,
  CopyOutlined,
} from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import type { WorkflowTemplate } from '../types/workflow-template';
import { workflowTemplateApi } from '../services/workflow-template-api';

const { Search } = Input;
const { Option } = Select;

/**
 * 工作流模板管理页面
 */
const WorkflowTemplateManagement: React.FC = () => {
  const navigate = useNavigate();
  const [templates, setTemplates] = useState<WorkflowTemplate[]>([]);
  const [loading, setLoading] = useState(false);
  const [searchKeyword, setSearchKeyword] = useState('');
  const [categoryFilter, setCategoryFilter] = useState<string | undefined>();
  const [publicFilter, setPublicFilter] = useState<boolean | undefined>();

  useEffect(() => {
    loadTemplates();
  }, [categoryFilter, publicFilter]);

  /**
   * 加载模板列表
   */
  const loadTemplates = async () => {
    setLoading(true);
    try {
      const data = await workflowTemplateApi.getAll(publicFilter, categoryFilter);
      setTemplates(data);
    } catch (error: unknown) {
      message.error(`加载模板失败: ${getErrorMessage(error)}`);
    } finally {
      setLoading(false);
    }
  };

  /**
   * 搜索模板
   */
  const handleSearch = async () => {
    if (!searchKeyword.trim()) {
      loadTemplates();
      return;
    }

    setLoading(true);
    try {
      const data = await workflowTemplateApi.search(searchKeyword);
      setTemplates(data);
    } catch (error: unknown) {
      message.error(`搜索失败: ${getErrorMessage(error)}`);
    } finally {
      setLoading(false);
    }
  };

  /**
   * 删除模板
   */
  const handleDelete = async (id: number) => {
    try {
      await workflowTemplateApi.delete(id);
      message.success('删除成功');
      loadTemplates();
    } catch (error: unknown) {
      message.error(`删除失败: ${getErrorMessage(error)}`);
    }
  };

  /**
   * 复制模板
   */
  const handleCopy = (template: WorkflowTemplate) => {
    navigate('/workflow-editor', {
      state: {
        template: {
          ...template,
          name: `${template.name} (副本)`,
          id: undefined,
        },
      },
    });
  };

  /**
   * 编辑模板
   */
  const handleEdit = (template: WorkflowTemplate) => {
    navigate('/workflow-editor', {
      state: { template },
    });
  };

  /**
   * 执行模板
   */
  const handleExecute = (template: WorkflowTemplate) => {
    navigate('/workflow-execute', {
      state: { template },
    });
  };

  /**
   * 获取来源标签颜色
   */
  const getSourceColor = (source: string) => {
    switch (source) {
      case 'manual':
        return 'blue';
      case 'magentic':
        return 'green';
      case 'magentic_optimized':
        return 'orange';
      default:
        return 'default';
    }
  };

  /**
   * 获取来源标签文本
   */
  const getSourceText = (source: string) => {
    switch (source) {
      case 'manual':
        return '手动创建';
      case 'magentic':
        return 'Magentic生成';
      case 'magentic_optimized':
        return 'Magentic优化';
      default:
        return source;
    }
  };

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: 80,
    },
    {
      title: '模板名称',
      dataIndex: 'name',
      key: 'name',
      render: (text: string, record: WorkflowTemplate) => (
        <a onClick={() => handleEdit(record)}>{text}</a>
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
      render: (category: string) => category || '-',
    },
    {
      title: '标签',
      dataIndex: 'tags',
      key: 'tags',
      width: 200,
      render: (tags: string[]) => (
        <>
          {tags?.slice(0, 3).map((tag) => (
            <Tag key={tag}>{tag}</Tag>
          ))}
          {tags?.length > 3 && <Tag>+{tags.length - 3}</Tag>}
        </>
      ),
    },
    {
      title: '来源',
      dataIndex: 'source',
      key: 'source',
      width: 120,
      render: (source: string) => (
        <Tag color={getSourceColor(source)}>{getSourceText(source)}</Tag>
      ),
    },
    {
      title: '使用次数',
      dataIndex: 'usageCount',
      key: 'usageCount',
      width: 100,
      sorter: (a: WorkflowTemplate, b: WorkflowTemplate) => a.usageCount - b.usageCount,
    },
    {
      title: '公开',
      dataIndex: 'isPublic',
      key: 'isPublic',
      width: 80,
      render: (isPublic: boolean) => (
        <Tag color={isPublic ? 'green' : 'default'}>{isPublic ? '是' : '否'}</Tag>
      ),
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 180,
      render: (date: string) => new Date(date).toLocaleString(),
    },
    {
      title: '操作',
      key: 'action',
      width: 200,
      fixed: 'right' as const,
      render: (_: unknown, record: WorkflowTemplate) => (
        <Space size="small">
          <Tooltip title="执行">
            <Button
              type="link"
              icon={<PlayCircleOutlined />}
              onClick={() => handleExecute(record)}
            />
          </Tooltip>
          <Tooltip title="编辑">
            <Button
              type="link"
              icon={<EditOutlined />}
              onClick={() => handleEdit(record)}
            />
          </Tooltip>
          <Tooltip title="复制">
            <Button
              type="link"
              icon={<CopyOutlined />}
              onClick={() => handleCopy(record)}
            />
          </Tooltip>
          <Popconfirm
            title="确定删除此模板吗？"
            onConfirm={() => handleDelete(record.id)}
            okText="确定"
            cancelText="取消"
          >
            <Tooltip title="删除">
              <Button type="link" danger icon={<DeleteOutlined />} />
            </Tooltip>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      <Card
        title="工作流模板管理"
        extra={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => navigate('/workflow-editor')}
          >
            创建模板
          </Button>
        }
      >
        <Space style={{ marginBottom: 16 }} size="large">
          <Search
            placeholder="搜索模板名称或描述"
            value={searchKeyword}
            onChange={(e) => setSearchKeyword(e.target.value)}
            onSearch={handleSearch}
            style={{ width: 300 }}
            enterButton
          />
          <Select
            placeholder="分类筛选"
            value={categoryFilter}
            onChange={setCategoryFilter}
            style={{ width: 150 }}
            allowClear
          >
            <Option value="research">研究分析</Option>
            <Option value="writing">写作创作</Option>
            <Option value="translation">翻译</Option>
            <Option value="coding">编程开发</Option>
            <Option value="analysis">数据分析</Option>
          </Select>
          <Select
            placeholder="公开状态"
            value={publicFilter}
            onChange={setPublicFilter}
            style={{ width: 120 }}
            allowClear
          >
            <Option value={true}>公开</Option>
            <Option value={false}>私有</Option>
          </Select>
        </Space>

        <Table
          columns={columns}
          dataSource={templates}
          rowKey="id"
          loading={loading}
          pagination={{
            pageSize: 10,
            showSizeChanger: true,
            showTotal: (total) => `共 ${total} 条`,
          }}
          scroll={{ x: 1400 }}
        />
      </Card>
    </div>
  );
};

export default WorkflowTemplateManagement;
