import React, { createContext, useContext, useState, useCallback, useEffect, ReactNode } from 'react';
import { Locale, Messages, getMessages, getBrowserLocale, getStoredLocale, setStoredLocale, defaultLocale } from '../locales';

interface I18nContextType {
  locale: Locale;
  messages: Messages;
  setLocale: (locale: Locale) => void;
  t: (key: string, params?: Record<string, string | number>) => string;
}

const I18nContext = createContext<I18nContextType | undefined>(undefined);

interface I18nProviderProps {
  children: ReactNode;
  defaultLocaleOverride?: Locale;
}

export const I18nProvider: React.FC<I18nProviderProps> = ({ children, defaultLocaleOverride }) => {
  const [locale, setLocaleState] = useState<Locale>(() => {
    return getStoredLocale() || defaultLocaleOverride || getBrowserLocale() || defaultLocale;
  });

  const [messages, setMessages] = useState<Messages>(() => getMessages(locale));

  useEffect(() => {
    setMessages(getMessages(locale));
    setStoredLocale(locale);
    document.documentElement.lang = locale;
  }, [locale]);

  const setLocale = useCallback((newLocale: Locale) => {
    setLocaleState(newLocale);
  }, []);

  const t = useCallback((key: string, params?: Record<string, string | number>): string => {
    const keys = key.split('.');
    let value: Record<string, unknown> | string | undefined = messages as Record<string, unknown>;

    for (const k of keys) {
      if (value && typeof value === 'object' && k in value) {
        value = value[k] as Record<string, unknown> | string | undefined;
      } else {
        console.warn(`Translation key not found: ${key}`);
        return key;
      }
    }

    if (typeof value !== 'string') {
      console.warn(`Translation value is not a string: ${key}`);
      return key;
    }

    if (params) {
      return value.replace(/\{\{(\w+)\}\}/g, (match, paramKey) => {
        return params[paramKey] !== undefined ? String(params[paramKey]) : match;
      });
    }

    return value;
  }, [messages]);

  return (
    <I18nContext.Provider value={{ locale, messages, setLocale, t }}>
      {children}
    </I18nContext.Provider>
  );
};

export const useI18n = (): I18nContextType => {
  const context = useContext(I18nContext);
  if (context === undefined) {
    throw new Error('useI18n must be used within an I18nProvider');
  }
  return context;
};

export const useTranslation = () => {
  const { t, locale } = useI18n();
  return { t, locale };
};

export default I18nContext;
