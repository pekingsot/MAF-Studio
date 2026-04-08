import React, { useState, useEffect } from 'react';
import { Card, Table, Button, Modal, Form, Input, message, Space, Tag, Descriptions, Divider } from 'antd';
import { ReloadOutlined, PlayCircleOutlined, DeleteOutlined, FolderOpenOutlined } from '@ant-design/icons';
import { skillService, Skill } from '../../services/skillService';

const SkillManagementPage: React.FC = () => {
  const [skills, setSkills] = useState<Skill[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadModalVisible, setLoadModalVisible] = useState(false);
  const [executeModalVisible, setExecuteModalVisible] = useState(false);
  const [detailModalVisible, setDetailModalVisible] = useState(false);
  const [selectedSkill, setSelectedSkill] = useState<Skill | null>(null);
  const [executeForm] = Form.useForm();
  const [loadForm] = Form.useForm();

  useEffect(() => {
    loadSkills();
  }, []);

  const loadSkills = async () => {
    setLoading(true);
    try {
      const data = await skillService.getAllSkills();
      setSkills(data);
    } catch (error: any) {
      message.error(`加载Skill列表失败: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleLoadAll = async () => {
    setLoading(true);
    try {
      const data = await skillService.loadAllSkills();
      setSkills(data);
      message.success(`成功加载 ${data.length} 个Skills`);
    } catch (error: any) {
      message.error(`加载失败: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleLoad = async (values: { path: string }) => {
    try {
      const skill = await skillService.loadSkill(values.path);
      setSkills([...skills, skill]);
      message.success(`成功加载Skill: ${skill.name}`);
      setLoadModalVisible(false);
      loadForm.resetFields();
    } catch (error: any) {
      message.error(`加载失败: ${error.message}`);
    }
  };

  const handleUnload = async (skillId: string) => {
    Modal.confirm({
      title: '确认卸载',
      content: '确定要卸载这个Skill吗？',
      onOk: async () => {
        try {
          await skillService.unloadSkill(skillId);
          setSkills(skills.filter(s => s.id !== skillId));
          message.success('Skill已卸载');
        } catch (error: any) {
          message.error(`卸载失败: ${error.message}`);
        }
      },
    });
  };

  const handleExecute = async (values: any) => {
    if (!selectedSkill) return;

    try {
      const result = await skillService.executeSkill(selectedSkill.id, values);
      message.success('Skill执行成功');
      console.log('执行结果:', result);
      setExecuteModalVisible(false);
      executeForm.resetFields();
    } catch (error: any) {
      message.error(`执行失败: ${error.message}`);
    }
  };

  const showDetail = (skill: Skill) => {
    setSelectedSkill(skill);
    setDetailModalVisible(true);
  };

  const showExecute = (skill: Skill) => {
    setSelectedSkill(skill);
    setExecuteModalVisible(true);
  };

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: 120,
      ellipsis: true,
    },
    {
      title: '名称',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
    },
    {
      title: '版本',
      dataIndex: 'version',
      key: 'version',
      width: 100,
    },
    {
      title: '作者',
      dataIndex: 'author',
      key: 'author',
    },
    {
      title: '运行时',
      dataIndex: 'runtime',
      key: 'runtime',
      render: (runtime: string) => runtime ? <Tag color="blue">{runtime}</Tag> : '-',
    },
    {
      title: '标签',
      dataIndex: 'tags',
      key: 'tags',
      render: (tags: string[]) => (
        <Space>
          {tags?.map((tag, index) => (
            <Tag key={index}>{tag}</Tag>
          ))}
        </Space>
      ),
    },
    {
      title: '操作',
      key: 'action',
      render: (_: any, record: Skill) => (
        <Space>
          <Button type="link" onClick={() => showDetail(record)}>
            详情
          </Button>
          <Button
            type="link"
            icon={<PlayCircleOutlined />}
            onClick={() => showExecute(record)}
          >
            执行
          </Button>
          <Button
            type="link"
            danger
            icon={<DeleteOutlined />}
            onClick={() => handleUnload(record.id)}
          >
            卸载
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Card
        title="Skill管理"
        extra={
          <Space>
            <Button icon={<ReloadOutlined />} onClick={loadSkills}>
              刷新
            </Button>
            <Button icon={<FolderOpenOutlined />} onClick={handleLoadAll}>
              加载所有Skills
            </Button>
            <Button type="primary" onClick={() => setLoadModalVisible(true)}>
              加载Skill
            </Button>
          </Space>
        }
      >
        <Table
          columns={columns}
          dataSource={skills}
          rowKey="id"
          loading={loading}
          pagination={{
            pageSize: 10,
            showSizeChanger: true,
            showTotal: (total) => `共 ${total} 个Skills`,
          }}
        />
      </Card>

      <Modal
        title="加载Skill"
        open={loadModalVisible}
        onCancel={() => {
          setLoadModalVisible(false);
          loadForm.resetFields();
        }}
        onOk={() => loadForm.submit()}
      >
        <Form form={loadForm} onFinish={handleLoad} layout="vertical">
          <Form.Item
            name="path"
            label="Skill路径"
            rules={[{ required: true, message: '请输入Skill路径' }]}
          >
            <Input placeholder="例如: /app/skills/code-generator" />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="执行Skill"
        open={executeModalVisible}
        onCancel={() => {
          setExecuteModalVisible(false);
          executeForm.resetFields();
          setSelectedSkill(null);
        }}
        onOk={() => executeForm.submit()}
        width={600}
      >
        {selectedSkill && (
          <>
            <Descriptions column={1} bordered size="small">
              <Descriptions.Item label="Skill名称">{selectedSkill.name}</Descriptions.Item>
              <Descriptions.Item label="描述">{selectedSkill.description}</Descriptions.Item>
              <Descriptions.Item label="运行时">{selectedSkill.runtime}</Descriptions.Item>
            </Descriptions>

            <Divider />

            <Form form={executeForm} onFinish={handleExecute} layout="vertical">
              <Form.Item label="执行参数（JSON格式）">
                <Input.TextArea
                  rows={6}
                  placeholder='{"param1": "value1", "param2": "value2"}'
                />
              </Form.Item>
            </Form>
          </>
        )}
      </Modal>

      <Modal
        title="Skill详情"
        open={detailModalVisible}
        onCancel={() => {
          setDetailModalVisible(false);
          setSelectedSkill(null);
        }}
        footer={null}
        width={700}
      >
        {selectedSkill && (
          <Descriptions column={2} bordered>
            <Descriptions.Item label="ID">{selectedSkill.id}</Descriptions.Item>
            <Descriptions.Item label="名称">{selectedSkill.name}</Descriptions.Item>
            <Descriptions.Item label="版本">{selectedSkill.version}</Descriptions.Item>
            <Descriptions.Item label="作者">{selectedSkill.author}</Descriptions.Item>
            <Descriptions.Item label="运行时">{selectedSkill.runtime}</Descriptions.Item>
            <Descriptions.Item label="入口">{selectedSkill.entryPoint}</Descriptions.Item>
            <Descriptions.Item label="路径" span={2}>
              {selectedSkill.path}
            </Descriptions.Item>
            <Descriptions.Item label="描述" span={2}>
              {selectedSkill.description}
            </Descriptions.Item>
            <Descriptions.Item label="标签" span={2}>
              <Space>
                {selectedSkill.tags?.map((tag, index) => (
                  <Tag key={index}>{tag}</Tag>
                ))}
              </Space>
            </Descriptions.Item>
            <Descriptions.Item label="依赖" span={2}>
              <Space direction="vertical">
                {selectedSkill.dependencies?.map((dep, index) => (
                  <Tag key={index} color="blue">{dep}</Tag>
                ))}
              </Space>
            </Descriptions.Item>
            <Descriptions.Item label="加载时间" span={2}>
              {new Date(selectedSkill.loadedAt).toLocaleString()}
            </Descriptions.Item>
          </Descriptions>
        )}
      </Modal>
    </div>
  );
};

export default SkillManagementPage;
