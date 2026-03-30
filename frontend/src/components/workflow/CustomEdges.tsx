import React from 'react';
import {
  BaseEdge,
  EdgeLabelRenderer,
  getBezierPath,
  Position,
} from 'reactflow';
import { Tag } from 'antd';
import type { EdgeType } from '../../types/workflow-template';

/**
 * 获取边类型标签颜色
 */
const getEdgeTypeColor = (type: EdgeType) => {
  switch (type) {
    case 'sequential':
      return 'blue';
    case 'fan-out':
      return 'green';
    case 'fan-in':
      return 'purple';
    case 'conditional':
      return 'orange';
    case 'loop':
      return 'magenta';
    default:
      return 'default';
  }
};

/**
 * 获取边类型标签文本
 */
const getEdgeTypeText = (type: EdgeType) => {
  switch (type) {
    case 'sequential':
      return '顺序';
    case 'fan-out':
      return '并发';
    case 'fan-in':
      return '汇聚';
    case 'conditional':
      return '条件';
    case 'loop':
      return '循环';
    default:
      return type;
  }
};

/**
 * 自定义边组件
 */
export const CustomEdge: React.FC<{
  id: string;
  sourceX: number;
  sourceY: number;
  targetX: number;
  targetY: number;
  sourcePosition: Position;
  targetPosition: Position;
  data: { type: EdgeType; description?: string };
  style?: React.CSSProperties;
  markerEnd?: string;
}> = ({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  data,
  style = {},
  markerEnd,
}) => {
  const [edgePath, labelX, labelY] = getBezierPath({
    sourceX,
    sourceY,
    sourcePosition,
    targetX,
    targetY,
    targetPosition,
  });

  return (
    <>
      <BaseEdge
        path={edgePath}
        markerEnd={markerEnd}
        style={{
          ...style,
          strokeWidth: 2,
          stroke: data.type === 'fan-out' || data.type === 'fan-in' ? '#52c41a' : '#1890ff',
        }}
      />
      <EdgeLabelRenderer>
        <div
          style={{
            position: 'absolute',
            transform: `translate(-50%, -50%) translate(${labelX}px,${labelY}px)`,
            fontSize: 12,
            pointerEvents: 'all',
          }}
          className="nodrag nopan"
        >
          <Tag color={getEdgeTypeColor(data.type)}>{getEdgeTypeText(data.type)}</Tag>
        </div>
      </EdgeLabelRenderer>
    </>
  );
};

/**
 * 边类型映射
 */
export const edgeTypes = {
  custom: CustomEdge,
};
