import React, { useMemo } from 'react';
import { List, Button, Space, Tag, Popconfirm, Tooltip, Divider } from 'antd';
import { PlusOutlined, ApiOutlined, CheckCircleOutlined, CloseCircleOutlined, LoadingOutlined, StarFilled } from '@ant-design/icons';
import { LLMConfig, LLMModelConfig, ConnectionStatus } from './types';

interface ModelListProps {
  config: LLMConfig;
  connectionStatus: Record<string, ConnectionStatus>;
  testingIds: Set<number>;
  onTest: (configId: number, modelId: number) => void;
  onEdit: (configId: number, model: LLMModelConfig) => void;
  onSetDefault: (configId: number, modelId: number) => void;
  onDelete: (configId: number, modelId: number) => void;
  onAddModel: (configId: number) => void;
}

const ModelList: React.FC<ModelListProps> = React.memo(({
  config,
  connectionStatus,
  testingIds,
  onTest,
  onEdit,
  onSetDefault,
  onDelete,
  onAddModel,
}) => {
  const models = useMemo(() => {
    const modelList = config.models || [];
    return [...modelList].sort((a, b) => a.id - b.id);
  }, [config.models]);

  if (models.length === 0) {
    return (
      <div style={{ padding: '12px 0', color: '#999' }}>
        暂无模型配置，请添加至少一个模型
        <Button type="link" onClick={() => onAddModel(config.id)}>
          立即添加
        </Button>
      </div>
    );
  }

  return (
    <>
      <Divider orientation="left" style={{ margin: '12px 0' }}>
        模型配置 ({models.length})
      </Divider>
      <List
        dataSource={models}
        renderItem={(model) => {
          const testId = config.id * 1000 + model.id;
          const isTesting = testingIds.has(testId);

          return (
            <List.Item
              style={{ paddingRight: 0 }}
              actions={[
                <Space key="actions" size={0} style={{ minWidth: 320, justifyContent: 'flex-start', display: 'flex' }}>
                  <Button
                    type="link"
                    icon={<ApiOutlined />}
                    onClick={() => onTest(config.id, model.id)}
                    loading={isTesting}
                  >
                    测试
                  </Button>
                  <Button type="link" onClick={() => onEdit(config.id, model)}>
                    编辑
                  </Button>
                  {!model.isDefault && (
                    <Button type="link" onClick={() => onSetDefault(config.id, model.id)}>
                      设为默认
                    </Button>
                  )}
                  {models.length > 1 && (
                    <Popconfirm
                      title="确定要删除这个模型吗？"
                      onConfirm={() => onDelete(config.id, model.id)}
                      okText="确定"
                      cancelText="取消"
                    >
                      <Button type="link" danger>
                        删除
                      </Button>
                    </Popconfirm>
                  )}
                </Space>,
              ]}
            >
              <List.Item.Meta
                avatar={
                  model.isDefault ? (
                    <StarFilled style={{ color: '#faad14', fontSize: 18 }} />
                  ) : null
                }
                title={
                  <Space>
                    <span style={{ fontWeight: 500 }}>{model.displayName || model.modelName}</span>
                    <span style={{ color: '#999', fontSize: 12 }}>{model.modelName}</span>
                    {!model.isEnabled && <Tag color="red">禁用</Tag>}
                    {model.isDefault && <Tag color="gold">默认</Tag>}
                  </Space>
                }
                description={
                  <Space split={<Divider type="vertical" />}>
                    <span>温度: {model.temperature}</span>
                    <span>最大Token: {model.maxTokens}</span>
                    <span>窗口: {model.contextWindow.toLocaleString()}</span>
                    {isTesting ? (
                      <Tag icon={<LoadingOutlined spin />} color="processing">测试中</Tag>
                    ) : model.availabilityStatus === 1 ? (
                      <Tooltip
                        title={
                          <div>
                            <div>连接成功</div>
                            {model.lastTestTime && <div>测试时间: {new Date(model.lastTestTime).toLocaleString()}</div>}
                          </div>
                        }
                      >
                        <Tag
                          icon={<CheckCircleOutlined />}
                          color="success"
                        >
                          {model.testResult || '成功'}
                        </Tag>
                      </Tooltip>
                    ) : model.availabilityStatus === 0 && model.lastTestTime ? (
                      <Tooltip
                        title={
                          <div>
                            <div>{model.testResult || '连接失败'}</div>
                            {model.lastTestTime && <div>测试时间: {new Date(model.lastTestTime).toLocaleString()}</div>}
                          </div>
                        }
                      >
                        <Tag
                          icon={<CloseCircleOutlined />}
                          color="error"
                        >
                          失败
                        </Tag>
                      </Tooltip>
                    ) : (
                      <Tag color="default">未测试</Tag>
                    )}
                  </Space>
                }
              />
            </List.Item>
          );
        }}
      />
      {models.length > 0 && (
        <Button
          type="dashed"
          icon={<PlusOutlined />}
          onClick={() => onAddModel(config.id)}
          style={{ marginTop: 12 }}
        >
          添加更多模型
        </Button>
      )}
    </>
  );
});

ModelList.displayName = 'ModelList';

export default ModelList;
