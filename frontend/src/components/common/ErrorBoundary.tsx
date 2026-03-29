import React, { Component, ErrorInfo, ReactNode } from 'react';
import { Result, Button } from 'antd';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
    };
  }

  static getDerivedStateFromError(error: Error): Partial<State> {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    this.setState({ errorInfo });
    console.error('ErrorBoundary caught an error:', error, errorInfo);
  }

  handleReset = (): void => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
    });
  };

  handleReload = (): void => {
    window.location.reload();
  };

  render(): ReactNode {
    const { hasError, error } = this.state;
    const { children, fallback } = this.props;

    if (hasError) {
      if (fallback) {
        return fallback;
      }

      return (
        <div style={{ padding: 48, minHeight: '100vh', background: '#f5f5f5' }}>
          <Result
            status="500"
            title="页面出错了"
            subTitle={error?.message || '抱歉，页面遇到了一些问题'}
            extra={[
              <Button key="reset" type="primary" onClick={this.handleReset}>
                重试
              </Button>,
              <Button key="reload" onClick={this.handleReload}>
                刷新页面
              </Button>,
              <Button key="home" onClick={() => window.location.href = '/'}>
                返回首页
              </Button>,
            ]}
          />
          {process.env.NODE_ENV === 'development' && error && (
            <div style={{
              marginTop: 24,
              padding: 16,
              background: '#fff',
              borderRadius: 8,
              maxWidth: 800,
              margin: '24px auto',
            }}>
              <h4 style={{ color: '#ff4d4f', marginBottom: 8 }}>错误详情：</h4>
              <pre style={{
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word',
                fontSize: 12,
                color: '#666',
              }}>
                {error.stack || error.message}
              </pre>
            </div>
          )}
        </div>
      );
    }

    return children;
  }
}

export default ErrorBoundary;
