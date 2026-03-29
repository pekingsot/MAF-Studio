import React from 'react';
import { Button, Space, Typography, Card } from 'antd';
import { PlusOutlined, ReloadOutlined } from '@ant-design/icons';

const { Title } = Typography;

interface PageHeaderProps {
  title: string;
  subTitle?: string;
  onCreate?: () => void;
  onRefresh?: () => void;
  createText?: string;
  loading?: boolean;
  extra?: React.ReactNode;
}

export const PageHeader: React.FC<PageHeaderProps> = ({
  title,
  subTitle,
  onCreate,
  onRefresh,
  createText = '新建',
  loading = false,
  extra,
}) => {
  return (
    <Card 
      style={{ marginBottom: 16 }} 
      styles={{ body: { padding: '16px 24px' } }}
    >
      <div className="flex-between">
        <div>
          <Title level={4} style={{ margin: 0 }}>
            {title}
          </Title>
          {subTitle && (
            <span style={{ color: 'var(--text-color-secondary)', marginLeft: 8 }}>
              {subTitle}
            </span>
          )}
        </div>
        <Space>
          {extra}
          {onRefresh && (
            <Button 
              icon={<ReloadOutlined />} 
              onClick={onRefresh}
              loading={loading}
            >
              刷新
            </Button>
          )}
          {onCreate && (
            <Button 
              type="primary" 
              icon={<PlusOutlined />} 
              onClick={onCreate}
            >
              {createText}
            </Button>
          )}
        </Space>
      </div>
    </Card>
  );
};

export default PageHeader;
