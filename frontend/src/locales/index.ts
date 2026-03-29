import zhCN from './zh-CN';
import enUS from './en-US';

export type Locale = 'zh-CN' | 'en-US';
export type Messages = typeof zhCN;

export const locales: Record<Locale, Messages> = {
  'zh-CN': zhCN,
  'en-US': enUS,
};

export const defaultLocale: Locale = 'zh-CN';

export const getMessages = (locale: Locale): Messages => {
  return locales[locale] || locales[defaultLocale];
};

export const getBrowserLocale = (): Locale => {
  const browserLang = navigator.language || (navigator as any).userLanguage;
  if (browserLang.startsWith('zh')) {
    return 'zh-CN';
  }
  return 'en-US';
};

export const getStoredLocale = (): Locale | null => {
  const stored = localStorage.getItem('locale');
  if (stored && (stored === 'zh-CN' || stored === 'en-US')) {
    return stored;
  }
  return null;
};

export const setStoredLocale = (locale: Locale): void => {
  localStorage.setItem('locale', locale);
};
