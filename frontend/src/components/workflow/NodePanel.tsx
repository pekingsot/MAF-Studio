import React from 'react';
import { Card, Typography, Divider } from 'antd';
import { NodeType } from '../../types/workflow-template';

const { Text } = Typography;

/**
 * 节点配置
 */
const nodeConfig = [
  {
    type: NodeType.START,
    emoji: '▶️',
    name: '开始节点',
    description: '工作流的起始点',
    color: '#52c41a',
    bgColor: '#f6ffed',
    borderColor: '#b7eb8f',
  },
  {
    type: NodeType.AGENT,
    emoji: '🤖',
    name: 'Agent节点',
    description: '执行智能体任务',
    color: '#1890ff',
    bgColor: '#e6f7ff',
    borderColor: '#91d5ff',
  },
  {
    type: NodeType.AGGREGATOR,
    emoji: '🔀',
    name: '聚合节点',
    description: '汇总多个结果',
    color: '#722ed1',
    bgColor: '#f9f0ff',
    borderColor: '#d3adf7',
  },
  {
    type: NodeType.CONDITION,
    emoji: '❓',
    name: '条件节点',
    description: '分支逻辑判断',
    color: '#fa8c16',
    bgColor: '#fff7e6',
    borderColor: '#ffd591',
  },
  {
    type: NodeType.LOOP,
    emoji: '🔄',
    name: '循环节点',
    description: '循环执行任务',
    color: '#eb2f96',
    bgColor: '#fff0f6',
    borderColor: '#ffadd2',
  },
];

/**
 * 节点面板组件
 */
const NodePanel: React.FC = () => {
  /**
   * 拖拽开始处理
   */
  const onDragStart = (event: React.DragEvent, nodeType: NodeType) => {
    event.dataTransfer.setData('application/reactflow', nodeType);
    event.dataTransfer.effectAllowed = 'move';
  };

  return (
    <div style={{ width: '100%' }}>
      <div style={{ 
        marginBottom: '16px',
        padding: '12px',
        background: '#f0f5ff',
        borderRadius: '8px',
        border: '1px solid #d6e4ff'
      }}>
        <Text type="secondary" style={{ fontSize: '12px' }}>
          💡 提示：拖拽节点到右侧画布开始创建工作流
        </Text>
      </div>
      
      <Divider style={{ margin: '12px 0' }}>节点类型</Divider>
      
      <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
        {nodeConfig.map((config) => (
          <div
            key={config.type}
            draggable
            onDragStart={(e) => onDragStart(e, config.type)}
            style={{
              padding: '14px',
              borderRadius: '10px',
              background: config.bgColor,
              border: `2px solid ${config.borderColor}`,
              cursor: 'grab',
              display: 'flex',
              alignItems: 'center',
              gap: '12px',
              transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
              boxShadow: '0 2px 4px rgba(0,0,0,0.05)',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.transform = 'translateX(4px)';
              e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.15)';
              e.currentTarget.style.borderColor = config.color;
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.transform = 'translateX(0)';
              e.currentTarget.style.boxShadow = '0 2px 4px rgba(0,0,0,0.05)';
              e.currentTarget.style.borderColor = config.borderColor;
            }}
          >
            <span style={{ fontSize: '24px' }}>{config.emoji}</span>
            <div style={{ flex: 1 }}>
              <div style={{ 
                fontWeight: 'bold', 
                fontSize: '14px',
                color: config.color,
                marginBottom: '2px'
              }}>
                {config.name}
              </div>
              <div style={{ fontSize: '11px', color: '#8c8c8c' }}>
                {config.description}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default NodePanel;
