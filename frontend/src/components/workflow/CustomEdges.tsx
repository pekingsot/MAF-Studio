import React from 'react';
import {
  BaseEdge,
  EdgeLabelRenderer,
  getBezierPath,
  Position,
  EdgeProps,
} from 'reactflow';
import { Tag, Tooltip } from 'antd';
import { EdgeType } from '../../types/workflow-template';

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
    case 'approved':
      return 'green';
    case 'rejected':
      return 'red';
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
    case 'approved':
      return '通过';
    case 'rejected':
      return '打回';
    default:
      return type;
  }
};

/**
 * 获取边类型颜色值
 */
const getEdgeColor = (type: EdgeType) => {
  switch (type) {
    case 'sequential':
      return '#1890ff';
    case 'fan-out':
      return '#52c41a';
    case 'fan-in':
      return '#722ed1';
    case 'conditional':
      return '#fa8c16';
    case 'loop':
      return '#eb2f96';
    case 'approved':
      return '#52c41a';
    case 'rejected':
      return '#ff4d4f';
    default:
      return '#1890ff';
  }
};

/**
 * 自定义边组件
 */
export const CustomEdge: React.FC<EdgeProps<{ type: EdgeType; description?: string }>> = ({
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

  const edgeType = data?.type || EdgeType.SEQUENTIAL;
  const edgeColor = getEdgeColor(edgeType);

  return (
    <>
      <BaseEdge
        path={edgePath}
        markerEnd={markerEnd}
        style={{
          ...style,
          strokeWidth: 3,
          stroke: edgeColor,
          transition: 'all 0.3s ease',
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
          <Tooltip title={data?.description || getEdgeTypeText(edgeType)}>
            <Tag 
              color={getEdgeTypeColor(edgeType)}
              style={{
                margin: 0,
                padding: '2px 8px',
                borderRadius: '12px',
                fontSize: '11px',
                boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
                transition: 'all 0.3s ease',
              }}
            >
              {getEdgeTypeText(edgeType)}
            </Tag>
          </Tooltip>
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
