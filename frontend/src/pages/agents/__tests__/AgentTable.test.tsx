import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import AgentTable from '../AgentTable';
import { Agent } from '../../../services/agentService';
import { AgentRuntimeStatus } from '../../../services/agentRuntimeService';

Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: jest.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: jest.fn(),
    removeListener: jest.fn(),
    addEventListener: jest.fn(),
    removeEventListener: jest.fn(),
    dispatchEvent: jest.fn(),
  })),
});

const renderWithRouter = (component: React.ReactElement) => {
  return render(
    <BrowserRouter>
      {component}
    </BrowserRouter>
  );
};

const mockAgents: Agent[] = [
  {
    id: 1,
    name: '测试智能体1',
    type: 'assistant',
    avatar: '🤖',
    description: '测试描述',
    systemPrompt: '测试提示词',
    status: 'Active',
    llmConfigs: [
      {
        llmConfigId: 1,
        llmConfigName: '测试配置',
        llmModelConfigId: 1,
        modelName: 'gpt-4',
        isPrimary: true,
        priority: 0,
        isValid: true,
        msg: '测试通过'
      }
    ],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 2,
    name: '测试智能体2',
    type: 'assistant',
    avatar: '🧠',
    description: '测试描述2',
    systemPrompt: '测试提示词2',
    status: 'Inactive',
    llmConfigs: [
      {
        llmConfigId: 1,
        llmConfigName: '测试配置',
        llmModelConfigId: 1,
        modelName: 'gpt-3.5',
        isPrimary: true,
        priority: 0,
        isValid: true,
        msg: '测试通过'
      },
      {
        llmConfigId: 2,
        llmConfigName: '备用配置',
        llmModelConfigId: 2,
        modelName: 'gpt-3.5-turbo',
        isPrimary: false,
        priority: 1,
        isValid: true,
        msg: '测试通过'
      }
    ],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
];

const mockAgentTypes = [
  { id: 1, code: 'assistant', name: '助手', description: 'AI助手', createdAt: new Date().toISOString(), updatedAt: new Date().toISOString(), isEnabled: true, sortOrder: 1 },
];

const mockLLMConfigs = [
  { id: 1, name: '测试配置', provider: 'openai', isEnabled: true, isDefault: true },
  { id: 2, name: '备用配置', provider: 'openai', isEnabled: true, isDefault: false },
];

const mockRuntimeStatuses: Record<string, AgentRuntimeStatus> = {
  '1': { agentId: 1, state: 'Ready', isAlive: true, taskCount: 0 },
  '2': { agentId: 2, state: 'Uninitialized', isAlive: false, taskCount: 0 },
};

const mockHandlers = {
  onEdit: jest.fn(),
  onDelete: jest.fn(),
  onActivate: jest.fn(),
  onDestroy: jest.fn(),
  onTest: jest.fn(),
  onPageSizeChange: jest.fn(),
};

describe('AgentTable 组件测试', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('状态显示测试', () => {
    it('应该正确显示"未初始化"状态', () => {
      const statuses: Record<string, AgentRuntimeStatus> = {
        '1': { agentId: 1, state: 'Uninitialized', isAlive: false, taskCount: 0 },
      };
      
      renderWithRouter(
        <AgentTable
          agents={[mockAgents[0]]}
          agentTypes={mockAgentTypes}
          llmConfigs={mockLLMConfigs}
          runtimeStatuses={statuses}
          loading={false}
          testingAgent={null}
          activatingAgent={null}
          pageSize={10}
          {...mockHandlers}
        />
      );
      
      expect(screen.getByText('未初始化')).toBeInTheDocument();
    });

    it('应该正确显示"可用"状态', () => {
      const statuses: Record<string, AgentRuntimeStatus> = {
        '1': { agentId: 1, state: 'Ready', isAlive: true, taskCount: 0 },
      };
      
      renderWithRouter(
        <AgentTable
          agents={[mockAgents[0]]}
          agentTypes={mockAgentTypes}
          llmConfigs={mockLLMConfigs}
          runtimeStatuses={statuses}
          loading={false}
          testingAgent={null}
          activatingAgent={null}
          pageSize={10}
          {...mockHandlers}
        />
      );
      
      expect(screen.getByText('可用')).toBeInTheDocument();
    });

    it('应该正确显示"忙碌"状态', () => {
      const statuses: Record<string, AgentRuntimeStatus> = {
        '1': { agentId: 1, state: 'Busy', isAlive: true, taskCount: 0 },
      };
      
      renderWithRouter(
        <AgentTable
          agents={[mockAgents[0]]}
          agentTypes={mockAgentTypes}
          llmConfigs={mockLLMConfigs}
          runtimeStatuses={statuses}
          loading={false}
          testingAgent={null}
          activatingAgent={null}
          pageSize={10}
          {...mockHandlers}
        />
      );
      
      expect(screen.getByText('忙碌')).toBeInTheDocument();
    });

    it('应该正确显示"错误"状态', () => {
      const statuses: Record<string, AgentRuntimeStatus> = {
        '1': { agentId: 1, state: 'Error', isAlive: false, taskCount: 0 },
      };
      
      renderWithRouter(
        <AgentTable
          agents={[mockAgents[0]]}
          agentTypes={mockAgentTypes}
          llmConfigs={mockLLMConfigs}
          runtimeStatuses={statuses}
          loading={false}
          testingAgent={null}
          activatingAgent={null}
          pageSize={10}
          {...mockHandlers}
        />
      );
      
      expect(screen.getByText('错误')).toBeInTheDocument();
    });
  });

  describe('操作按钮测试', () => {
    it('未初始化状态应该显示"激活"按钮', () => {
      const statuses: Record<string, AgentRuntimeStatus> = {
        '1': { agentId: 1, state: 'Uninitialized', isAlive: false, taskCount: 0 },
      };
      
      renderWithRouter(
        <AgentTable
          agents={[mockAgents[0]]}
          agentTypes={mockAgentTypes}
          llmConfigs={mockLLMConfigs}
          runtimeStatuses={statuses}
          loading={false}
          testingAgent={null}
          activatingAgent={null}
          pageSize={10}
          {...mockHandlers}
        />
      );
      
      expect(screen.getByText('激活')).toBeInTheDocument();
    });

    it('可用状态应该显示"测试"和"关闭"按钮', () => {
      const statuses: Record<string, AgentRuntimeStatus> = {
        '1': { agentId: 1, state: 'Ready', isAlive: true, taskCount: 0 },
      };
      
      renderWithRouter(
        <AgentTable
          agents={[mockAgents[0]]}
          agentTypes={mockAgentTypes}
          llmConfigs={mockLLMConfigs}
          runtimeStatuses={statuses}
          loading={false}
          testingAgent={null}
          activatingAgent={null}
          pageSize={10}
          {...mockHandlers}
        />
      );
      
      expect(screen.getByText('测试')).toBeInTheDocument();
      expect(screen.getByText('关闭')).toBeInTheDocument();
    });

    it('忙碌状态应该显示"执行中..."', () => {
      const statuses: Record<string, AgentRuntimeStatus> = {
        '1': { agentId: 1, state: 'Busy', isAlive: true, taskCount: 0 },
      };
      
      renderWithRouter(
        <AgentTable
          agents={[mockAgents[0]]}
          agentTypes={mockAgentTypes}
          llmConfigs={mockLLMConfigs}
          runtimeStatuses={statuses}
          loading={false}
          testingAgent={null}
          activatingAgent={null}
          pageSize={10}
          {...mockHandlers}
        />
      );
      
      expect(screen.getByText('执行中...')).toBeInTheDocument();
    });

    it('错误状态应该显示"关闭"按钮', () => {
      const statuses: Record<string, AgentRuntimeStatus> = {
        '1': { agentId: 1, state: 'Error', isAlive: false, taskCount: 0 },
      };
      
      renderWithRouter(
        <AgentTable
          agents={[mockAgents[0]]}
          agentTypes={mockAgentTypes}
          llmConfigs={mockLLMConfigs}
          runtimeStatuses={statuses}
          loading={false}
          testingAgent={null}
          activatingAgent={null}
          pageSize={10}
          {...mockHandlers}
        />
      );
      
      expect(screen.getByText('关闭')).toBeInTheDocument();
    });

    it('点击激活按钮应该调用onActivate', () => {
      const statuses: Record<string, AgentRuntimeStatus> = {
        '1': { agentId: 1, state: 'Uninitialized', isAlive: false, taskCount: 0 },
      };
      
      renderWithRouter(
        <AgentTable
          agents={[mockAgents[0]]}
          agentTypes={mockAgentTypes}
          llmConfigs={mockLLMConfigs}
          runtimeStatuses={statuses}
          loading={false}
          testingAgent={null}
          activatingAgent={null}
          pageSize={10}
          {...mockHandlers}
        />
      );
      
      fireEvent.click(screen.getByText('激活'));
      expect(mockHandlers.onActivate).toHaveBeenCalledWith(1);
    });

    it('点击测试按钮应该调用onTest', () => {
      const statuses: Record<string, AgentRuntimeStatus> = {
        '1': { agentId: 1, state: 'Ready', isAlive: true, taskCount: 0 },
      };
      
      renderWithRouter(
        <AgentTable
          agents={[mockAgents[0]]}
          agentTypes={mockAgentTypes}
          llmConfigs={mockLLMConfigs}
          runtimeStatuses={statuses}
          loading={false}
          testingAgent={null}
          activatingAgent={null}
          pageSize={10}
          {...mockHandlers}
        />
      );
      
      fireEvent.click(screen.getByText('测试'));
      expect(mockHandlers.onTest).toHaveBeenCalledWith(1);
    });

    it('点击编辑按钮应该调用onEdit', () => {
      const statuses: Record<string, AgentRuntimeStatus> = {
        '1': { agentId: 1, state: 'Ready', isAlive: true, taskCount: 0 },
      };
      
      renderWithRouter(
        <AgentTable
          agents={[mockAgents[0]]}
          agentTypes={mockAgentTypes}
          llmConfigs={mockLLMConfigs}
          runtimeStatuses={statuses}
          loading={false}
          testingAgent={null}
          activatingAgent={null}
          pageSize={10}
          {...mockHandlers}
        />
      );
      
      fireEvent.click(screen.getByText('编辑'));
      expect(mockHandlers.onEdit).toHaveBeenCalledWith(mockAgents[0]);
    });
  });

  describe('副模型显示测试', () => {
    it('应该正确显示副模型（左右结构）', () => {
      renderWithRouter(
        <AgentTable
          agents={[mockAgents[1]]}
          agentTypes={mockAgentTypes}
          llmConfigs={mockLLMConfigs}
          runtimeStatuses={mockRuntimeStatuses}
          loading={false}
          testingAgent={null}
          activatingAgent={null}
          pageSize={10}
          {...mockHandlers}
        />
      );
      
      expect(screen.getByText('备用配置')).toBeInTheDocument();
    });

    it('没有副模型时应该显示"无"', () => {
      renderWithRouter(
        <AgentTable
          agents={[mockAgents[0]]}
          agentTypes={mockAgentTypes}
          llmConfigs={mockLLMConfigs}
          runtimeStatuses={mockRuntimeStatuses}
          loading={false}
          testingAgent={null}
          activatingAgent={null}
          pageSize={10}
          {...mockHandlers}
        />
      );
      
      expect(screen.getByText('无')).toBeInTheDocument();
    });
  });

  describe('加载状态测试', () => {
    it('loading为true时应该显示加载状态', () => {
      renderWithRouter(
        <AgentTable
          agents={[]}
          agentTypes={mockAgentTypes}
          llmConfigs={mockLLMConfigs}
          runtimeStatuses={{}}
          loading={true}
          testingAgent={null}
          activatingAgent={null}
          pageSize={10}
          {...mockHandlers}
        />
      );
      
      expect(document.querySelector('.ant-spin')).toBeInTheDocument();
    });
  });
});
