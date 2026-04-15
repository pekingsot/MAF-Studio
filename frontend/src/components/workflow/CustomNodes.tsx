import React from 'react';
import { Handle, Position } from 'reactflow';
import { Tag, Tooltip } from 'antd';
import type { WorkflowNode } from '../../types/workflow-template';

/**
 * 节点通用样式
 */
const nodeStyles = {
  transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  cursor: 'pointer',
};

/**
 * 开始节点组件
 */
export const StartNode: React.FC<{ data: WorkflowNode }> = ({ data }) => {
  return (
    <div
      style={{
        ...nodeStyles,
        padding: '16px 32px',
        borderRadius: '12px',
        background: 'linear-gradient(135deg, #52c41a 0%, #73d13d 100%)',
        color: 'white',
        border: '3px solid #389e0d',
        minWidth: '120px',
        textAlign: 'center',
        boxShadow: '0 4px 12px rgba(82, 196, 26, 0.3)',
      }}
    >
      <div style={{ 
        fontWeight: 'bold', 
        fontSize: '16px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: '8px'
      }}>
        <span style={{ fontSize: '20px' }}>▶️</span>
        <span>{data.name}</span>
      </div>
      <Handle 
        type="source" 
        position={Position.Bottom}
        style={{
          background: '#389e0d',
          width: '12px',
          height: '12px',
          border: '2px solid white',
        }}
      />
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
        ...nodeStyles,
        padding: '16px',
        borderRadius: '12px',
        background: 'linear-gradient(135deg, #ffffff 0%, #f0f5ff 100%)',
        border: '2px solid #1890ff',
        minWidth: '220px',
        boxShadow: '0 4px 12px rgba(24, 144, 255, 0.15)',
      }}
    >
      <div style={{ 
        fontWeight: 'bold', 
        marginBottom: '12px', 
        color: '#1890ff',
        fontSize: '15px',
        display: 'flex',
        alignItems: 'center',
        gap: '8px'
      }}>
        <span style={{ fontSize: '20px' }}>🤖</span>
        <span>{data.name}</span>
      </div>
      <div style={{ fontSize: '13px', color: '#595959' }}>
        <div style={{ 
          marginBottom: '8px',
          padding: '6px 10px',
          background: '#e6f7ff',
          borderRadius: '6px',
          display: 'inline-block'
        }}>
          <span style={{ color: '#8c8c8c' }}>角色：</span>
          <Tag color="blue">{data.agentRole || '未指定'}</Tag>
        </div>
        {data.inputTemplate && (
          <Tooltip title={data.inputTemplate}>
            <div style={{ 
              marginTop: '8px',
              padding: '8px',
              background: '#fafafa',
              borderRadius: '6px',
              border: '1px solid #e8e8e8',
              fontSize: '12px',
              color: '#8c8c8c'
            }}>
              📝 {data.inputTemplate.substring(0, 40)}
              {data.inputTemplate.length > 40 && '...'}
            </div>
          </Tooltip>
        )}
      </div>
      <Handle 
        type="target" 
        position={Position.Top}
        style={{
          background: '#1890ff',
          width: '12px',
          height: '12px',
          border: '2px solid white',
        }}
      />
      <Handle 
        type="source" 
        position={Position.Bottom}
        style={{
          background: '#1890ff',
          width: '12px',
          height: '12px',
          border: '2px solid white',
        }}
      />
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
        ...nodeStyles,
        padding: '16px',
        borderRadius: '12px',
        background: 'linear-gradient(135deg, #ffffff 0%, #f9f0ff 100%)',
        border: '2px solid #722ed1',
        minWidth: '160px',
        textAlign: 'center',
        boxShadow: '0 4px 12px rgba(114, 46, 209, 0.15)',
      }}
    >
      <div style={{ 
        fontWeight: 'bold', 
        color: '#722ed1',
        fontSize: '15px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: '8px',
        marginBottom: '8px'
      }}>
        <span style={{ fontSize: '20px' }}>🔀</span>
        <span>{data.name}</span>
      </div>
      <div style={{ 
        fontSize: '12px', 
        color: '#8c8c8c',
        padding: '4px 8px',
        background: '#f9f0ff',
        borderRadius: '6px',
        display: 'inline-block'
      }}>
        汇聚结果
      </div>
      <Handle 
        type="target" 
        position={Position.Top} 
        id="input"
        style={{
          background: '#722ed1',
          width: '12px',
          height: '12px',
          border: '2px solid white',
        }}
      />
      <Handle 
        type="source" 
        position={Position.Bottom} 
        id="output"
        style={{
          background: '#722ed1',
          width: '12px',
          height: '12px',
          border: '2px solid white',
        }}
      />
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
        ...nodeStyles,
        padding: '16px',
        borderRadius: '12px',
        background: 'linear-gradient(135deg, #ffffff 0%, #fff7e6 100%)',
        border: '2px solid #fa8c16',
        minWidth: '200px',
        boxShadow: '0 4px 12px rgba(250, 140, 22, 0.15)',
      }}
    >
      <div style={{ 
        fontWeight: 'bold', 
        color: '#fa8c16',
        fontSize: '15px',
        display: 'flex',
        alignItems: 'center',
        gap: '8px',
        marginBottom: '12px'
      }}>
        <span style={{ fontSize: '20px' }}>❓</span>
        <span>{data.name}</span>
      </div>
      <div style={{ fontSize: '12px', color: '#595959' }}>
        <Tooltip title={data.condition || '未设置条件'}>
          <div style={{ 
            padding: '8px',
            background: '#fff7e6',
            borderRadius: '6px',
            border: '1px solid #ffd591',
            fontFamily: 'monospace',
            fontSize: '11px'
          }}>
            {data.condition || '未设置条件'}
          </div>
        </Tooltip>
        <div style={{ 
          marginTop: '8px',
          display: 'flex',
          justifyContent: 'space-around'
        }}>
          <Tag color="green">True</Tag>
          <Tag color="red">False</Tag>
        </div>
      </div>
      <Handle 
        type="target" 
        position={Position.Top}
        style={{
          background: '#fa8c16',
          width: '12px',
          height: '12px',
          border: '2px solid white',
        }}
      />
      <Handle
        type="source"
        position={Position.Bottom}
        id="true"
        style={{ 
          left: '30%',
          background: '#52c41a',
          width: '12px',
          height: '12px',
          border: '2px solid white',
        }}
      />
      <Handle
        type="source"
        position={Position.Bottom}
        id="false"
        style={{ 
          left: '70%',
          background: '#ff4d4f',
          width: '12px',
          height: '12px',
          border: '2px solid white',
        }}
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
        ...nodeStyles,
        padding: '16px',
        borderRadius: '12px',
        background: 'linear-gradient(135deg, #ffffff 0%, #fff0f6 100%)',
        border: '2px solid #eb2f96',
        minWidth: '200px',
        boxShadow: '0 4px 12px rgba(235, 47, 150, 0.15)',
      }}
    >
      <div style={{ 
        fontWeight: 'bold', 
        color: '#eb2f96',
        fontSize: '15px',
        display: 'flex',
        alignItems: 'center',
        gap: '8px',
        marginBottom: '12px'
      }}>
        <span style={{ fontSize: '20px' }}>🔄</span>
        <span>{data.name}</span>
      </div>
      <div style={{ fontSize: '12px', color: '#595959' }}>
        <Tooltip title={data.condition || '未设置条件'}>
          <div style={{ 
            padding: '8px',
            background: '#fff0f6',
            borderRadius: '6px',
            border: '1px solid #ffadd2',
            fontFamily: 'monospace',
            fontSize: '11px'
          }}>
            {data.condition || '未设置条件'}
          </div>
        </Tooltip>
      </div>
      <Handle 
        type="target" 
        position={Position.Top}
        style={{
          background: '#eb2f96',
          width: '12px',
          height: '12px',
          border: '2px solid white',
        }}
      />
      <Handle 
        type="source" 
        position={Position.Bottom} 
        id="next"
        style={{
          background: '#eb2f96',
          width: '12px',
          height: '12px',
          border: '2px solid white',
        }}
      />
      <Handle 
        type="source" 
        position={Position.Right} 
        id="loop" 
        style={{ 
          top: '50%',
          background: '#eb2f96',
          width: '12px',
          height: '12px',
          border: '2px solid white',
        }}
      />
    </div>
  );
};

export const ReviewNode: React.FC<{ data: WorkflowNode }> = ({ data }) => {
  return (
    <div
      style={{
        ...nodeStyles,
        padding: '16px',
        borderRadius: '12px',
        background: 'linear-gradient(135deg, #ffffff 0%, #fff7e6 100%)',
        border: '2px solid #fa8c16',
        minWidth: '220px',
        boxShadow: '0 4px 12px rgba(250, 140, 22, 0.15)',
      }}
    >
      <div style={{
        fontWeight: 'bold',
        marginBottom: '12px',
        color: '#fa8c16',
        fontSize: '15px',
        display: 'flex',
        alignItems: 'center',
        gap: '8px'
      }}>
        <span style={{ fontSize: '20px' }}>🔍</span>
        <span>{data.name}</span>
      </div>
      <div style={{ fontSize: '13px', color: '#595959' }}>
        <div style={{
          marginBottom: '8px',
          padding: '6px 10px',
          background: '#fff7e6',
          borderRadius: '6px',
          display: 'inline-block'
        }}>
          <span style={{ color: '#8c8c8c' }}>角色：</span>
          <Tag color="orange">{data.agentRole || '审核者'}</Tag>
        </div>
        {data.reviewCriteria && (
          <Tooltip title={data.reviewCriteria}>
            <div style={{
              marginTop: '8px',
              padding: '8px',
              background: '#fafafa',
              borderRadius: '6px',
              border: '1px solid #e8e8e8',
              fontSize: '12px',
              color: '#8c8c8c'
            }}>
              📋 {data.reviewCriteria.substring(0, 40)}
              {data.reviewCriteria.length > 40 && '...'}
            </div>
          </Tooltip>
        )}
        <div style={{
          marginTop: '8px',
          display: 'flex',
          gap: '8px',
          flexWrap: 'wrap'
        }}>
          {data.approvalKeyword && (
            <Tag color="green" style={{ fontSize: '11px' }}>
              ✅ 通过: {data.approvalKeyword}
            </Tag>
          )}
          {data.maxRetries && (
            <Tag color="red" style={{ fontSize: '11px' }}>
              🔄 最多{data.maxRetries}次
            </Tag>
          )}
          {data.rejectTargetNode && (
            <Tag color="volcano" style={{ fontSize: '11px' }}>
              ↩️ 打回: {data.rejectTargetNode}
            </Tag>
          )}
        </div>
      </div>
      <Handle
        type="target"
        position={Position.Top}
        style={{
          background: '#fa8c16',
          width: '12px',
          height: '12px',
          border: '2px solid white',
        }}
      />
      <Handle
        type="source"
        position={Position.Bottom}
        id="approved"
        style={{
          left: '30%',
          background: '#52c41a',
          width: '12px',
          height: '12px',
          border: '2px solid white',
        }}
      />
      <Handle
        type="source"
        position={Position.Bottom}
        id="rejected"
        style={{
          left: '70%',
          background: '#ff4d4f',
          width: '12px',
          height: '12px',
          border: '2px solid white',
        }}
      />
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
  review: ReviewNode,
};
