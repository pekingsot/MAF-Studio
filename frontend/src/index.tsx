import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { ConfigProvider } from 'antd';
import zhCN from 'antd/locale/zh_CN';
import enUS from 'antd/locale/en_US';
import App from './App';
import { AuthProvider } from './contexts/AuthContext';
import { I18nProvider, useI18n } from './contexts/I18nContext';
import { ErrorBoundary } from './components/common';
import './styles/variables.css';
import './index.css';

const AntdConfigProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { locale } = useI18n();
  return (
    <ConfigProvider locale={locale === 'zh-CN' ? zhCN : enUS}>
      {children}
    </ConfigProvider>
  );
};

const root = ReactDOM.createRoot(
  document.getElementById('root') as HTMLElement
);

root.render(
  <React.StrictMode>
    <ErrorBoundary>
      <I18nProvider>
        <AntdConfigProvider>
          <BrowserRouter>
            <AuthProvider>
              <App />
            </AuthProvider>
          </BrowserRouter>
        </AntdConfigProvider>
      </I18nProvider>
    </ErrorBoundary>
  </React.StrictMode>
);
