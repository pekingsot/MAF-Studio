import React from 'react';
import { Handle, Position } from 'reactflow';
import { Tag } from 'antd';
import type { WorkflowNode } from '../types/workflow-template';

/**
 * 开始节点组件
 */
export const StartNode: React.FC<{ data: WorkflowNode }> = ({ data }) => {
  return (
    <div
      style={{
        padding: '10px 20px',
        borderRadius: '8px',
        background: '#52c41a',
        color: 'white',
        border: '2px solid #389e0d',
        minWidth: '100px',
        textAlign: 'center',
      }}
    >
      <div style={{ fontWeight: 'bold' }}>▶️ {data.name}</div>
      <Handle type="source" position={Position.Bottom} />
    </div>
  );
};

/**
 * Agent节点组件
 */
export const AgentNode: React.FC<{ data: WorkflowNode }> = ({ data }) => {
  return (
    <div
      style={{
        padding: '12px',
        borderRadius: '8px',
        background: 'white',
        border: '2px solid #1890ff',
        minWidth: '200px',
        boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
      }}
    >
      <div style={{ fontWeight: 'bold', marginBottom: '8px', color: '#1890ff' }}>
        🤖 {data.name}
      </div>
      <div style={{ fontSize: '12px', color: '#666' }}>
        <div>角色: {data.agentRole || '未指定'}</div>
        {data.inputTemplate && (
          <div style={{ marginTop: '4px' }}>
            任务: {data.inputTemplate.substring(0, 50)}
            {data.inputTemplate.length > 50 && '...'}
          </div>
        )}
      </div>
      <Handle type="target" position={Position.Top} />
      <Handle type="source" position={Position.Bottom} />
    </div>
  );
};

/**
 * 聚合节点组件
 */
export const AggregatorNode: React.FC<{ data: WorkflowNode }> = ({ data }) => {
  return (
    <div
      style={{
        padding: '12px',
        borderRadius: '8px',
        background: 'white',
        border: '2px solid #722ed1',
        minWidth: '150px',
        textAlign: 'center',
        boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
      }}
    >
      <div style={{ fontWeight: 'bold', color: '#722ed1' }}>🔀 {data.name}</div>
      <div style={{ fontSize: '12px', color: '#999', marginTop: '4px' }}>汇聚结果</div>
      <Handle type="target" position={Position.Top} id="input" />
      <Handle type="source" position={Position.Bottom} id="output" />
    </div>
  );
};

/**
 * 条件节点组件
 */
export const ConditionNode: React.FC<{ data: WorkflowNode }> = ({ data }) => {
  return (
    <div
      style={{
        padding: '12px',
        borderRadius: '8px',
        background: 'white',
        border: '2px solid #fa8c16',
        minWidth: '180px',
        boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
      }}
    >
      <div style={{ fontWeight: 'bold', color: '#fa8c16' }}>❓ {data.name}</div>
      <div style={{ fontSize: '12px', color: '#666', marginTop: '4px' }}>
        条件: {data.condition || '未设置'}
      </div>
      <Handle type="target" position={Position.Top} />
      <Handle
        type="source"
        position={Position.Bottom}
        id="true"
        style={{ left: '30%' }}
      />
      <Handle
        type="source"
        position={Position.Bottom}
        id="false"
        style={{ left: '70%' }}
      />
    </div>
  );
};

/**
 * 循环节点组件
 */
export const LoopNode: React.FC<{ data: WorkflowNode }> = ({ data }) => {
  return (
    <div
      style={{
        padding: '12px',
        borderRadius: '8px',
        background: 'white',
        border: '2px solid #eb2f96',
        minWidth: '180px',
        boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
      }}
    >
      <div style={{ fontWeight: 'bold', color: '#eb2f96' }}>🔄 {data.name}</div>
      <div style={{ fontSize: '12px', color: '#666', marginTop: '4px' }}>
        条件: {data.condition || '未设置'}
      </div>
      <Handle type="target" position={Position.Top} />
      <Handle type="source" position={Position.Bottom} id="next" />
      <Handle type="source" position={Position.Right} id="loop" style={{ top: '50%' }} />
    </div>
  );
};

/**
 * 节点类型映射
 */
export const nodeTypes = {
  start: StartNode,
  agent: AgentNode,
  aggregator: AggregatorNode,
  condition: ConditionNode,
  loop: LoopNode,
};
