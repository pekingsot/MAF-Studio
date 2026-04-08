# 文档生成器

**描述**: 根据代码或需求自动生成技术文档，支持API文档、README、用户手册等
**版本**: 1.0.0
**作者**: MAF Studio Team

## 标签
- 文档生成
- API文档
- README

## 依赖
- python >= 3.8

## 参数
- **doc_type**: 文档类型（api, readme, user_manual, changelog）
- **source_path**: 源代码路径或需求描述
- **output_format**: 输出格式（markdown, html, pdf）
- **language**: 文档语言（zh-CN, en-US）

## 示例用法

### 生成API文档
```json
{
  "doc_type": "api",
  "source_path": "./src/api",
  "output_format": "markdown",
  "language": "zh-CN"
}
```

### 生成README
```json
{
  "doc_type": "readme",
  "source_path": "./",
  "output_format": "markdown",
  "language": "zh-CN"
}
```

## 输出格式

生成的文档将包含：
1. 项目介绍
2. 安装说明
3. 使用方法
4. API参考
5. 示例代码
6. 常见问题

## 支持的文档类型

- **api**: API接口文档
- **readme**: 项目README
- **user_manual**: 用户手册
- **changelog**: 更新日志
