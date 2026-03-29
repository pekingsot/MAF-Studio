import React from 'react';
import { Descriptions } from 'antd';
import { CollaborationDetailData, STATUS_COLOR_MAP } from './types';

interface CollaborationInfoProps {
  collaboration: CollaborationDetailData;
}

const CollaborationInfo: React.FC<CollaborationInfoProps> = ({ collaboration }) => {
  return (
    <Descriptions bordered column={2}>
      <Descriptions.Item label="ID">{collaboration.id}</Descriptions.Item>
      <Descriptions.Item label="状态">
        <span style={{ color: STATUS_COLOR_MAP[collaboration.status] }}>
          {collaboration.status}
        </span>
      </Descriptions.Item>
      <Descriptions.Item label="智能体数量">
        {collaboration.agents.length}
      </Descriptions.Item>
      <Descriptions.Item label="任务数量">
        {collaboration.tasks.length}
      </Descriptions.Item>
      <Descriptions.Item label="创建时间">
        {new Date(collaboration.createdAt).toLocaleString('zh-CN')}
      </Descriptions.Item>
      <Descriptions.Item label="更新时间">
        {collaboration.updatedAt
          ? new Date(collaboration.updatedAt).toLocaleString('zh-CN')
          : '未更新'}
      </Descriptions.Item>
      <Descriptions.Item label="描述" span={2}>
        {collaboration.description || '无描述'}
      </Descriptions.Item>
    </Descriptions>
  );
};

export default CollaborationInfo;
