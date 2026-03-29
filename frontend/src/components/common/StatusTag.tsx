import React from 'react';
import { Tag, Tooltip } from 'antd';
import type { TagProps } from 'antd';

export type StatusType = 'success' | 'warning' | 'error' | 'info' | 'default';

interface StatusTagProps extends Omit<TagProps, 'color'> {
  status: StatusType;
  label: string;
  tooltip?: string;
}

const STATUS_COLORS: Record<StatusType, string> = {
  success: 'green',
  warning: 'orange',
  error: 'red',
  info: 'blue',
  default: 'default',
};

export const StatusTag: React.FC<StatusTagProps> = ({
  status,
  label,
  tooltip,
  ...rest
}) => {
  const tag = (
    <Tag color={STATUS_COLORS[status]} {...rest}>
      {label}
    </Tag>
  );

  if (tooltip) {
    return <Tooltip title={tooltip}>{tag}</Tooltip>;
  }

  return tag;
};

export const AgentStatusTag: React.FC<{ status: string }> = ({ status }) => {
  const STATUS_MAP: Record<string, { color: string; label: string }> = {
    Active: { color: 'green', label: '活跃' },
    Inactive: { color: 'default', label: '未激活' },
    Busy: { color: 'orange', label: '忙碌' },
    Error: { color: 'red', label: '错误' },
  };

  const config = STATUS_MAP[status] || { color: 'default', label: status };
  return <Tag color={config.color}>{config.label}</Tag>;
};

export const RuntimeStatusTag: React.FC<{ state: string }> = ({ state }) => {
  const STATE_MAP: Record<string, { color: string; label: string }> = {
    Uninitialized: { color: 'default', label: '未初始化' },
    Ready: { color: 'green', label: '就绪' },
    Busy: { color: 'orange', label: '忙碌' },
    Sleeping: { color: 'purple', label: '休眠' },
    Error: { color: 'red', label: '错误' },
  };

  const config = STATE_MAP[state] || { color: 'default', label: state };
  return <Tag color={config.color}>{config.label}</Tag>;
};

export default StatusTag;
