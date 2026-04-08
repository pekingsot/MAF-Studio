# Skill开发指南

## 概述

MAF Studio的Skill系统允许用户创建可重用的功能模块，通过标准化的SKILL.md文件定义Skill的元数据和参数。

## Skill目录结构

```
skills/
├── code-generator/
│   ├── SKILL.md          # Skill清单文件（必需）
│   ├── main.py           # 主入口脚本
│   ├── requirements.txt  # Python依赖（可选）
│   └── README.md         # 使用说明（可选）
├── doc-generator/
│   ├── SKILL.md
│   └── main.py
└── data-analyzer/
    ├── SKILL.md
    └── main.py
```

## SKILL.md文件格式

SKILL.md是Skill的核心清单文件，必须包含以下信息：

```markdown
# Skill名称

**描述**: Skill的详细描述
**版本**: 1.0.0
**作者**: 作者名称

## 标签
- 标签1
- 标签2

## 依赖
- python >= 3.8
- node >= 18.0

## 参数
- **param1**: 参数说明
- **param2**: 参数说明

## 示例用法

### 示例1
```json
{
  "param1": "value1",
  "param2": "value2"
}
```
```

### 必需字段

| 字段 | 说明 | 示例 |
|------|------|------|
| 名称 | Skill的标题 | # 代码生成器 |
| 描述 | Skill的功能描述 | **描述**: 根据需求生成代码 |
| 版本 | 版本号 | **版本**: 1.0.0 |
| 作者 | 作者信息 | **作者**: MAF Studio Team |

### 可选字段

| 字段 | 说明 | 示例 |
|------|------|------|
| 标签 | 用于分类和搜索 | ## 标签 |
| 依赖 | 运行时依赖 | ## 依赖 |
| 参数 | 执行参数说明 | ## 参数 |
| 示例用法 | 使用示例 | ## 示例用法 |

## Skill开发规范

### 1. 入口脚本

Skill必须提供一个可执行的入口脚本，默认为`main.py`（Python）或`index.js`（Node.js）。

#### Python示例

```python
#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import argparse
import json
import sys

def parse_args():
    parser = argparse.ArgumentParser(description='Skill描述')
    parser.add_argument('--param1', required=True, help='参数1')
    parser.add_argument('--param2', default='default', help='参数2')
    return parser.parse_args()

def main():
    args = parse_args()
    
    # 执行Skill逻辑
    result = {
        "success": True,
        "message": "执行成功",
        "data": {...}
    }
    
    # 输出JSON格式结果
    print(json.dumps(result, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
```

#### Node.js示例

```javascript
#!/usr/bin/env node

const args = process.argv.slice(2);
const params = {};

// 解析参数
args.forEach(arg => {
    const [key, value] = arg.split('=');
    params[key.replace('--', '')] = value;
});

// 执行Skill逻辑
const result = {
    success: true,
    message: '执行成功',
    data: {...}
};

console.log(JSON.stringify(result, null, 2));
```

### 2. 参数传递

MAF Studio通过命令行参数传递执行参数：

```bash
python main.py --param1 value1 --param2 value2
```

### 3. 输出格式

Skill必须输出JSON格式的结果：

```json
{
  "success": true,
  "message": "执行成功",
  "data": {
    "result": "结果数据"
  }
}
```

错误输出：

```json
{
  "success": false,
  "error": "错误信息"
}
```

### 4. 返回码

- `0`: 执行成功
- `1`: 执行失败

## Skill生命周期

### 1. 加载阶段

```csharp
// 加载Skill
var skill = await skillLoader.LoadSkillAsync("/path/to/skill");
```

SkillLoader会：
1. 检查目录是否存在
2. 读取SKILL.md文件
3. 解析元数据
4. 验证必需字段
5. 返回Skill对象

### 2. 执行阶段

```csharp
// 执行Skill
var result = await skillExecutor.ExecuteSkillAsync(
    skillId: "skill-001",
    parameters: new Dictionary<string, object>
    {
        ["param1"] = "value1",
        ["param2"] = "value2"
    }
);
```

SkillExecutor会：
1. 获取Skill对象
2. 验证入口文件存在
3. 根据runtime选择执行器
4. 构建命令行参数
5. 执行脚本
6. 捕获输出和错误
7. 返回执行结果

### 3. 卸载阶段

```csharp
// 卸载Skill
var success = skillLoader.UnloadSkill(skillId);
```

## 最佳实践

### 1. 错误处理

```python
try:
    # Skill逻辑
    result = do_something()
except Exception as e:
    result = {
        "success": False,
        "error": str(e)
    }
    print(json.dumps(result, ensure_ascii=False, indent=2))
    sys.exit(1)
```

### 2. 日志记录

```python
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

logger.info("开始执行Skill...")
logger.debug(f"参数: {args}")
```

### 3. 参数验证

```python
def validate_args(args):
    if not args.param1:
        raise ValueError("param1不能为空")
    
    if args.param2 not in ['option1', 'option2']:
        raise ValueError("param2必须是option1或option2")
```

### 4. 资源清理

```python
import tempfile
import shutil

temp_dir = tempfile.mkdtemp()
try:
    # 使用临时目录
    pass
finally:
    # 清理资源
    shutil.rmtree(temp_dir)
```

## 示例Skill

### 代码生成器

**目录**: `skills/code-generator/`

**功能**: 根据需求描述生成代码

**使用**:
```bash
python main.py \
  --language python \
  --framework fastapi \
  --requirement "创建用户管理API"
```

**输出**:
```
output/
├── main.py
├── requirements.txt
├── README.md
└── test_main.py
```

### 文档生成器

**目录**: `skills/doc-generator/`

**功能**: 生成技术文档

**使用**:
```bash
python main.py \
  --doc_type api \
  --source_path ./src \
  --language zh-CN
```

**输出**:
```
docs/
└── api.md
```

## 调试技巧

### 1. 本地测试

```bash
# 直接执行脚本
cd skills/code-generator
python main.py --language python --framework fastapi --requirement "test"
```

### 2. 检查输出

```bash
# 检查JSON格式
python main.py ... | python -m json.tool
```

### 3. 环境验证

```bash
# 检查Python版本
python --version

# 检查依赖
pip list
```

## 发布Skill

### 1. 目录结构检查

确保包含：
- ✅ SKILL.md
- ✅ 入口脚本（main.py或index.js）
- ✅ README.md（推荐）
- ✅ requirements.txt或package.json（如需要）

### 2. 测试

```bash
# 运行单元测试
pytest tests/

# 手动测试
python main.py --help
```

### 3. 打包

```bash
# 创建压缩包
tar -czf code-generator.tar.gz code-generator/
```

### 4. 分发

- 上传到GitHub仓库
- 发布到Skill市场
- 分享压缩包

## 常见问题

### Q1: Skill加载失败？

检查：
- SKILL.md文件是否存在
- 格式是否正确
- 必需字段是否完整

### Q2: Skill执行失败？

检查：
- 运行时环境是否安装
- 依赖是否满足
- 参数是否正确

### Q3: 如何调试Skill？

建议：
- 使用print输出调试信息
- 检查错误输出
- 验证JSON格式

### Q4: 如何处理大文件？

建议：
- 使用流式处理
- 分块读取文件
- 避免一次性加载到内存

## 参考资源

- [MAF Studio技术文档](../MAF_STUDIO_TECHNICAL_DOCUMENTATION.md)
- [Python官方文档](https://docs.python.org/)
- [Node.js官方文档](https://nodejs.org/docs/)

---
*文档版本: v1.0.0*
*更新时间: 2026-03-29*
