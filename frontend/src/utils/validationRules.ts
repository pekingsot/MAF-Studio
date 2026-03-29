import { Rule } from 'antd/es/form';

interface RuleObject {
  required?: boolean;
  message?: string;
  type?: string;
  min?: number;
  max?: number;
  pattern?: RegExp;
  validator?: (rule: unknown, value: unknown) => Promise<void>;
}

export const validationRules = {
  required: (message = '此项为必填项'): RuleObject => ({
    required: true,
    message,
  }),

  email: (message = '请输入有效的邮箱地址'): RuleObject => ({
    type: 'email',
    message,
  }),

  phone: (message = '请输入有效的手机号码'): RuleObject => ({
    pattern: /^1[3-9]\d{9}$/,
    message,
  }),

  password: (message = '密码至少8位，包含字母和数字'): RuleObject => ({
    pattern: /^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d@$!%*#?&]{8,}$/,
    message,
  }),

  minLength: (min: number, message?: string): RuleObject => ({
    min,
    message: message || `最少需要 ${min} 个字符`,
  }),

  maxLength: (max: number, message?: string): RuleObject => ({
    max,
    message: message || `最多允许 ${max} 个字符`,
  }),

  range: (min: number, max: number, message?: string): RuleObject => ({
    min,
    max,
    message: message || `长度需要在 ${min} 到 ${max} 个字符之间`,
  }),

  number: (message = '请输入有效的数字'): RuleObject => ({
    type: 'number',
    message,
  }),

  integer: (message = '请输入整数'): RuleObject => ({
    pattern: /^-?\d+$/,
    message,
  }),

  positiveNumber: (message = '请输入正数'): RuleObject => ({
    pattern: /^\d+(\.\d+)?$/,
    message,
  }),

  url: (message = '请输入有效的URL'): RuleObject => ({
    type: 'url',
    message,
  }),

  noSpaces: (message = '不能包含空格'): RuleObject => ({
    pattern: /^\S+$/,
    message,
  }),

  noSpecialChars: (message = '不能包含特殊字符'): RuleObject => ({
    pattern: /^[\u4e00-\u9fa5a-zA-Z0-9_]+$/,
    message,
  }),

  chineseOnly: (message = '只能输入中文'): RuleObject => ({
    pattern: /^[\u4e00-\u9fa5]+$/,
    message,
  }),

  englishOnly: (message = '只能输入英文'): RuleObject => ({
    pattern: /^[a-zA-Z]+$/,
    message,
  }),

  alphanumeric: (message = '只能输入字母和数字'): RuleObject => ({
    pattern: /^[a-zA-Z0-9]+$/,
    message,
  }),

  username: (message = '用户名只能包含字母、数字和下划线，长度4-20'): RuleObject => ({
    pattern: /^[a-zA-Z0-9_]{4,20}$/,
    message,
  }),

  agentName: (message = '名称只能包含中文、字母、数字和下划线'): RuleObject => ({
    pattern: /^[\u4e00-\u9fa5a-zA-Z0-9_\s]+$/,
    message,
  }),

  confirmPassword: (passwordField = 'password', message = '两次输入的密码不一致'): Rule => ({
    validator: (_rule: unknown, value: string, callback?: { form?: { getFieldValue: (name: string) => string } }) => {
      if (callback?.form) {
        const password = callback.form.getFieldValue(passwordField);
        if (value && value !== password) {
          return Promise.reject(new Error(message));
        }
      }
      return Promise.resolve();
    },
  } as Rule),

  uniqueName: (
    checkFn: (value: string) => Promise<boolean>,
    message = '该名称已存在'
  ): Rule => ({
    validator: async (_rule: unknown, value: string) => {
      if (!value) return Promise.resolve();
      const isUnique = await checkFn(value);
      if (!isUnique) {
        return Promise.reject(new Error(message));
      }
      return Promise.resolve();
    },
  } as Rule),

  custom: (validator: (value: unknown) => boolean | Promise<boolean>, message: string): Rule => ({
    validator: async (_rule: unknown, value: unknown) => {
      if (!value) return Promise.resolve();
      const isValid = await validator(value);
      if (!isValid) {
        return Promise.reject(new Error(message));
      }
      return Promise.resolve();
    },
  } as Rule),
};

export const formRules = {
  username: [
    validationRules.required('请输入用户名'),
    validationRules.username(),
  ] as Rule[],

  email: [
    validationRules.required('请输入邮箱'),
    validationRules.email(),
  ] as Rule[],

  password: [
    validationRules.required('请输入密码'),
    validationRules.password(),
  ] as Rule[],

  loginPassword: [
    validationRules.required('请输入密码'),
  ] as Rule[],

  confirmPassword: [
    validationRules.required('请确认密码'),
    validationRules.confirmPassword('password'),
  ] as Rule[],

  agentName: [
    validationRules.required('请输入名称'),
    validationRules.maxLength(50),
    validationRules.agentName(),
  ] as Rule[],

  agentDescription: [
    validationRules.maxLength(500),
  ] as Rule[],

  required: (message?: string): Rule[] => [
    { required: true, message: message || '此项为必填项' },
  ],

  optional: (): Rule[] => [],
};
