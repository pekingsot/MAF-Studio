import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import StatusTag, { AgentStatusTag, RuntimeStatusTag } from '../StatusTag';

const renderWithRouter = (component: React.ReactElement) => {
  return render(
    <BrowserRouter>
      {component}
    </BrowserRouter>
  );
};

describe('StatusTag 组件测试', () => {
  describe('基础 StatusTag', () => {
    it('应该正确渲染成功状态', () => {
      renderWithRouter(<StatusTag status="success" label="成功" />);
      expect(screen.getByText('成功')).toBeInTheDocument();
    });

    it('应该正确渲染错误状态', () => {
      renderWithRouter(<StatusTag status="error" label="错误" />);
      expect(screen.getByText('错误')).toBeInTheDocument();
    });

    it('应该正确渲染警告状态', () => {
      renderWithRouter(<StatusTag status="warning" label="警告" />);
      expect(screen.getByText('警告')).toBeInTheDocument();
    });

    it('应该正确渲染默认状态', () => {
      renderWithRouter(<StatusTag status="default" label="默认" />);
      expect(screen.getByText('默认')).toBeInTheDocument();
    });

    it('应该正确渲染带 tooltip 的标签', () => {
      renderWithRouter(
        <StatusTag status="success" label="成功" tooltip="操作成功完成" />
      );
      expect(screen.getByText('成功')).toBeInTheDocument();
    });
  });

  describe('AgentStatusTag', () => {
    it('应该正确渲染活跃状态', () => {
      renderWithRouter(<AgentStatusTag status="Active" />);
      expect(screen.getByText('活跃')).toBeInTheDocument();
    });

    it('应该正确渲染未激活状态', () => {
      renderWithRouter(<AgentStatusTag status="Inactive" />);
      expect(screen.getByText('未激活')).toBeInTheDocument();
    });

    it('应该正确渲染忙碌状态', () => {
      renderWithRouter(<AgentStatusTag status="Busy" />);
      expect(screen.getByText('忙碌')).toBeInTheDocument();
    });

    it('应该正确渲染错误状态', () => {
      renderWithRouter(<AgentStatusTag status="Error" />);
      expect(screen.getByText('错误')).toBeInTheDocument();
    });
  });

  describe('RuntimeStatusTag', () => {
    it('应该正确渲染未初始化状态', () => {
      renderWithRouter(<RuntimeStatusTag state="Uninitialized" />);
      expect(screen.getByText('未初始化')).toBeInTheDocument();
    });

    it('应该正确渲染就绪状态', () => {
      renderWithRouter(<RuntimeStatusTag state="Ready" />);
      expect(screen.getByText('就绪')).toBeInTheDocument();
    });

    it('应该正确渲染休眠状态', () => {
      renderWithRouter(<RuntimeStatusTag state="Sleeping" />);
      expect(screen.getByText('休眠')).toBeInTheDocument();
    });
  });
});
