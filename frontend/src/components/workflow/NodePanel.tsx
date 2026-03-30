import React from 'react';
import { Card } from 'antd';
import { NodeType } from '../../types/workflow-template';

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
    <Card title="节点类型" size="small" style={{ width: '100%' }}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
        <div
          draggable
          onDragStart={(e) => onDragStart(e, NodeType.START)}
          style={{
            padding: '12px',
            borderRadius: '6px',
            background: '#f6ffed',
            border: '1px solid #b7eb8f',
            cursor: 'grab',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <span style={{ fontSize: '20px' }}>▶️</span>
          <span>开始节点</span>
        </div>

        <div
          draggable
          onDragStart={(e) => onDragStart(e, NodeType.AGENT)}
          style={{
            padding: '12px',
            borderRadius: '6px',
            background: '#e6f7ff',
            border: '1px solid #91d5ff',
            cursor: 'grab',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <span style={{ fontSize: '20px' }}>🤖</span>
          <span>Agent节点</span>
        </div>

        <div
          draggable
          onDragStart={(e) => onDragStart(e, NodeType.AGGREGATOR)}
          style={{
            padding: '12px',
            borderRadius: '6px',
            background: '#f9f0ff',
            border: '1px solid #d3adf7',
            cursor: 'grab',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <span style={{ fontSize: '20px' }}>🔀</span>
          <span>聚合节点</span>
        </div>

        <div
          draggable
          onDragStart={(e) => onDragStart(e, NodeType.CONDITION)}
          style={{
            padding: '12px',
            borderRadius: '6px',
            background: '#fff7e6',
            border: '1px solid #ffd591',
            cursor: 'grab',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <span style={{ fontSize: '20px' }}>❓</span>
          <span>条件节点</span>
        </div>

        <div
          draggable
          onDragStart={(e) => onDragStart(e, NodeType.LOOP)}
          style={{
            padding: '12px',
            borderRadius: '6px',
            background: '#fff0f6',
            border: '1px solid #ffadd2',
            cursor: 'grab',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <span style={{ fontSize: '20px' }}>🔄</span>
          <span>循环节点</span>
        </div>
      </div>
    </Card>
  );
};

export default NodePanel;
