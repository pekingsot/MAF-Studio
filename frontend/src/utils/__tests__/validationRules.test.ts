import { validationRules, formRules } from '../validationRules';

describe('表单验证规则测试', () => {
  describe('基础验证规则', () => {
    describe('required', () => {
      it('应该创建必填规则', () => {
        const rule = validationRules.required() as { required: boolean; message: string };
        expect(rule.required).toBe(true);
        expect(rule.message).toBe('此项为必填项');
      });

      it('应该支持自定义消息', () => {
        const rule = validationRules.required('请输入名称') as { message: string };
        expect(rule.message).toBe('请输入名称');
      });
    });

    describe('email', () => {
      it('应该创建邮箱验证规则', () => {
        const rule = validationRules.email() as { type: string };
        expect(rule.type).toBe('email');
      });
    });

    describe('phone', () => {
      const rule = validationRules.phone() as { pattern: RegExp };

      it('应该验证有效的手机号', () => {
        expect(rule.pattern.test('13800138000')).toBe(true);
        expect(rule.pattern.test('15912345678')).toBe(true);
      });

      it('应该拒绝无效的手机号', () => {
        expect(rule.pattern.test('12800138000')).toBe(false);
        expect(rule.pattern.test('1380013800')).toBe(false);
        expect(rule.pattern.test('abc')).toBe(false);
      });
    });

    describe('password', () => {
      const rule = validationRules.password() as { pattern: RegExp };

      it('应该验证有效的密码', () => {
        expect(rule.pattern.test('Password123')).toBe(true);
        expect(rule.pattern.test('abc12345')).toBe(true);
      });

      it('应该拒绝无效的密码', () => {
        expect(rule.pattern.test('12345678')).toBe(false);
        expect(rule.pattern.test('abcdefgh')).toBe(false);
        expect(rule.pattern.test('short')).toBe(false);
      });
    });

    describe('minLength / maxLength', () => {
      it('应该创建最小长度规则', () => {
        const rule = validationRules.minLength(5) as { min: number };
        expect(rule.min).toBe(5);
      });

      it('应该创建最大长度规则', () => {
        const rule = validationRules.maxLength(20) as { max: number };
        expect(rule.max).toBe(20);
      });

      it('应该创建范围规则', () => {
        const rule = validationRules.range(5, 20) as { min: number; max: number };
        expect(rule.min).toBe(5);
        expect(rule.max).toBe(20);
      });
    });

    describe('username', () => {
      const rule = validationRules.username() as { pattern: RegExp };

      it('应该验证有效的用户名', () => {
        expect(rule.pattern.test('testuser')).toBe(true);
        expect(rule.pattern.test('user_123')).toBe(true);
        expect(rule.pattern.test('TestUser')).toBe(true);
      });

      it('应该拒绝无效的用户名', () => {
        expect(rule.pattern.test('abc')).toBe(false);
        expect(rule.pattern.test('a'.repeat(21))).toBe(false);
        expect(rule.pattern.test('user@name')).toBe(false);
      });
    });

    describe('alphanumeric', () => {
      const rule = validationRules.alphanumeric() as { pattern: RegExp };

      it('应该验证字母数字组合', () => {
        expect(rule.pattern.test('abc123')).toBe(true);
        expect(rule.pattern.test('ABC')).toBe(true);
        expect(rule.pattern.test('123')).toBe(true);
      });

      it('应该拒绝特殊字符', () => {
        expect(rule.pattern.test('abc_123')).toBe(false);
        expect(rule.pattern.test('abc-123')).toBe(false);
      });
    });

    describe('noSpaces', () => {
      const rule = validationRules.noSpaces() as { pattern: RegExp };

      it('应该验证无空格字符串', () => {
        expect(rule.pattern.test('abc')).toBe(true);
        expect(rule.pattern.test('abc123')).toBe(true);
      });

      it('应该拒绝包含空格的字符串', () => {
        expect(rule.pattern.test('abc 123')).toBe(false);
        expect(rule.pattern.test(' abc')).toBe(false);
        expect(rule.pattern.test('abc ')).toBe(false);
      });
    });

    describe('url', () => {
      it('应该创建 URL 验证规则', () => {
        const rule = validationRules.url() as { type: string };
        expect(rule.type).toBe('url');
      });
    });
  });

  describe('预定义表单规则', () => {
    describe('formRules.username', () => {
      it('应该包含必填和用户名格式验证', () => {
        const rules = formRules.username as Array<{ required?: boolean }>;
        expect(rules).toHaveLength(2);
        expect(rules[0].required).toBe(true);
      });
    });

    describe('formRules.email', () => {
      it('应该包含必填和邮箱格式验证', () => {
        const rules = formRules.email as Array<{ required?: boolean }>;
        expect(rules).toHaveLength(2);
        expect(rules[0].required).toBe(true);
      });
    });

    describe('formRules.password', () => {
      it('应该包含必填和密码格式验证', () => {
        const rules = formRules.password as Array<{ required?: boolean }>;
        expect(rules).toHaveLength(2);
        expect(rules[0].required).toBe(true);
      });
    });

    describe('formRules.agentName', () => {
      it('应该包含必填和名称格式验证', () => {
        const rules = formRules.agentName as Array<{ required?: boolean }>;
        expect(rules).toHaveLength(3);
        expect(rules[0].required).toBe(true);
      });
    });

    describe('formRules.required', () => {
      it('应该创建必填规则数组', () => {
        const rules = formRules.required('请输入') as Array<{ required?: boolean }>;
        expect(rules).toHaveLength(1);
        expect(rules[0].required).toBe(true);
      });
    });

    describe('formRules.optional', () => {
      it('应该返回空规则数组', () => {
        const rules = formRules.optional();
        expect(rules).toHaveLength(0);
      });
    });
  });
});
