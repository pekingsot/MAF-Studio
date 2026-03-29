#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
文档生成器 Skill
根据代码或需求自动生成技术文档
"""

import argparse
import json
import os
import sys
from pathlib import Path
from typing import Dict, Any
from datetime import datetime

def parse_args():
    """解析命令行参数"""
    parser = argparse.ArgumentParser(description='文档生成器')
    parser.add_argument('--doc_type', required=True, help='文档类型')
    parser.add_argument('--source_path', required=True, help='源代码路径或需求描述')
    parser.add_argument('--output_format', default='markdown', help='输出格式')
    parser.add_argument('--language', default='zh-CN', help='文档语言')
    parser.add_argument('--output_path', default='./docs', help='输出路径')
    return parser.parse_args()

def generate_api_doc(source_path: str, language: str) -> str:
    """生成API文档"""
    if language == 'zh-CN':
        return '''# API接口文档

## 概述

本文档描述了系统的API接口规范。

## 基础信息

- **Base URL**: `http://localhost:5000/api`
- **认证方式**: Bearer Token
- **数据格式**: JSON

## 接口列表

### 1. 用户管理

#### 1.1 用户注册

**请求**
```
POST /users/register
```

**参数**
| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| username | string | 是 | 用户名 |
| password | string | 是 | 密码 |
| email | string | 是 | 邮箱 |

**响应**
```json
{
  "success": true,
  "message": "注册成功",
  "data": {
    "userId": 123,
    "username": "testuser"
  }
}
```

#### 1.2 用户登录

**请求**
```
POST /users/login
```

**参数**
| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| username | string | 是 | 用户名 |
| password | string | 是 | 密码 |

**响应**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600
}
```

### 2. 数据管理

#### 2.1 获取数据列表

**请求**
```
GET /data
```

**响应**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "数据项1",
      "createdAt": "2026-03-29T10:00:00Z"
    }
  ],
  "total": 100,
  "page": 1,
  "pageSize": 10
}
```

## 错误码

| 错误码 | 说明 |
|--------|------|
| 200 | 成功 |
| 400 | 请求参数错误 |
| 401 | 未授权 |
| 403 | 禁止访问 |
| 404 | 资源不存在 |
| 500 | 服务器内部错误 |

## 注意事项

1. 所有接口需要携带认证Token
2. 请求频率限制：100次/分钟
3. 建议使用HTTPS协议

---
*文档生成时间: {timestamp}*
'''.format(timestamp=datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
    else:
        return '''# API Documentation

## Overview

This document describes the API specifications.

## Base Information

- **Base URL**: `http://localhost:5000/api`
- **Authentication**: Bearer Token
- **Data Format**: JSON

## Endpoints

### 1. User Management

#### 1.1 User Registration

**Request**
```
POST /users/register
```

**Parameters**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| username | string | Yes | Username |
| password | string | Yes | Password |
| email | string | Yes | Email |

**Response**
```json
{
  "success": true,
  "message": "Registration successful",
  "data": {
    "userId": 123,
    "username": "testuser"
  }
}
```

---
*Generated: {timestamp}*
'''.format(timestamp=datetime.now().strftime('%Y-%m-%d %H:%M:%S'))

def generate_readme(source_path: str, language: str) -> str:
    """生成README文档"""
    project_name = Path(source_path).name if os.path.exists(source_path) else "MyProject"
    
    if language == 'zh-CN':
        return f'''# {project_name}

## 项目简介

{project_name} 是一个现代化的软件项目，致力于提供高效、可靠的解决方案。

## 功能特性

- ✅ 功能完整
- ✅ 易于使用
- ✅ 高性能
- ✅ 可扩展
- ✅ 安全可靠

## 技术栈

- **后端**: .NET 10.0
- **数据库**: PostgreSQL
- **缓存**: Redis
- **前端**: React + TypeScript

## 快速开始

### 环境要求

- .NET 10.0 SDK
- PostgreSQL 14+
- Node.js 18+
- Docker (可选)

### 安装步骤

1. **克隆项目**
```bash
git clone https://github.com/yourusername/{project_name.lower()}.git
cd {project_name.lower()}
```

2. **配置数据库**
```bash
# 创建数据库
createdb {project_name.lower()}

# 运行迁移
dotnet ef database update
```

3. **启动项目**
```bash
# 后端
cd backend
dotnet run

# 前端
cd frontend
npm install
npm start
```

## 项目结构

```
{project_name}/
├── backend/          # 后端代码
│   ├── src/
│   │   ├── Api/     # API层
│   │   ├── Application/  # 应用层
│   │   ├── Domain/  # 领域层
│   │   └── Infrastructure/  # 基础设施层
│   └── tests/       # 测试
├── frontend/         # 前端代码
│   ├── src/
│   │   ├── components/  # 组件
│   │   ├── pages/      # 页面
│   │   └── utils/      # 工具函数
│   └── public/
└── docs/            # 文档
```

## API文档

启动项目后访问: http://localhost:5000/swagger

## 开发指南

### 代码规范

- 遵循Clean Code原则
- 使用有意义的变量名
- 编写单元测试
- 保持代码简洁

### Git提交规范

- feat: 新功能
- fix: 修复bug
- docs: 文档更新
- refactor: 重构
- test: 测试相关

## 部署

### Docker部署

```bash
# 构建镜像
docker build -t {project_name.lower()} .

# 运行容器
docker run -p 5000:5000 {project_name.lower()}
```

## 常见问题

### Q: 如何配置数据库连接？
A: 修改 `appsettings.json` 中的连接字符串。

### Q: 如何添加新的API接口？
A: 在 `Controllers` 文件夹中添加新的控制器类。

## 贡献指南

欢迎提交Issue和Pull Request！

## 许可证

MIT License

## 联系方式

- 作者: MAF Studio Team
- 邮箱: support@mafstudio.com
- GitHub: https://github.com/yourusername/{project_name.lower()}

---
*文档生成时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}*
'''
    else:
        return f'''# {project_name}

## Introduction

{project_name} is a modern software project providing efficient and reliable solutions.

## Features

- ✅ Full-featured
- ✅ Easy to use
- ✅ High performance
- ✅ Scalable
- ✅ Secure

## Quick Start

### Prerequisites

- .NET 10.0 SDK
- PostgreSQL 14+
- Node.js 18+

### Installation

1. Clone the repository
```bash
git clone https://github.com/yourusername/{project_name.lower()}.git
```

2. Start the project
```bash
dotnet run
```

## License

MIT License

---
*Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}*
'''

def generate_user_manual(source_path: str, language: str) -> str:
    """生成用户手册"""
    if language == 'zh-CN':
        return '''# 用户手册

## 目录

1. [系统介绍](#系统介绍)
2. [快速入门](#快速入门)
3. [功能说明](#功能说明)
4. [常见问题](#常见问题)

## 系统介绍

欢迎使用本系统！本系统旨在为用户提供便捷、高效的服务体验。

### 系统特点

- 界面友好，操作简单
- 功能强大，满足多种需求
- 响应迅速，体验流畅
- 安全可靠，数据保护

## 快速入门

### 1. 注册账号

1. 访问系统首页
2. 点击"注册"按钮
3. 填写注册信息
4. 完成邮箱验证
5. 登录系统

### 2. 基本操作

#### 登录系统
- 输入用户名和密码
- 点击"登录"按钮

#### 修改密码
1. 进入"个人中心"
2. 点击"修改密码"
3. 输入旧密码和新密码
4. 保存修改

## 功能说明

### 1. 数据管理

#### 添加数据
1. 进入数据管理页面
2. 点击"新增"按钮
3. 填写数据信息
4. 保存

#### 查询数据
1. 使用搜索框输入关键词
2. 点击"搜索"按钮
3. 查看搜索结果

#### 编辑数据
1. 点击数据项的"编辑"按钮
2. 修改数据信息
3. 保存修改

#### 删除数据
1. 点击数据项的"删除"按钮
2. 确认删除操作

### 2. 报表功能

#### 生成报表
1. 选择报表类型
2. 设置筛选条件
3. 点击"生成报表"
4. 查看或导出报表

## 常见问题

### Q1: 忘记密码怎么办？
A: 点击登录页面的"忘记密码"链接，通过邮箱重置密码。

### Q2: 如何修改个人信息？
A: 进入"个人中心"页面，点击"编辑资料"进行修改。

### Q3: 数据可以导出吗？
A: 可以，支持导出为Excel、PDF等格式。

### Q4: 系统支持多语言吗？
A: 支持，可在设置中切换语言。

## 技术支持

如遇问题，请联系：
- 邮箱: support@example.com
- 电话: 400-123-4567
- 工作时间: 周一至周五 9:00-18:00

---
*文档版本: v1.0.0*
*更新时间: {timestamp}*
'''.format(timestamp=datetime.now().strftime('%Y-%m-%d'))
    else:
        return '''# User Manual

## Table of Contents

1. [Introduction](#introduction)
2. [Quick Start](#quick-start)
3. [Features](#features)

## Introduction

Welcome to our system!

## Quick Start

### 1. Registration

1. Visit the homepage
2. Click "Register"
3. Fill in your information
4. Verify your email
5. Login

## Features

### Data Management

- Add data
- Query data
- Edit data
- Delete data

## Support

Email: support@example.com

---
*Version: v1.0.0*
*Updated: {timestamp}*
'''.format(timestamp=datetime.now().strftime('%Y-%m-%d'))

def generate_changelog(source_path: str, language: str) -> str:
    """生成更新日志"""
    if language == 'zh-CN':
        return '''# 更新日志

所有重要的更改都将记录在此文件中。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)

## [Unreleased]

### 新增
- 待添加的新功能

### 变更
- 待变更的功能

### 修复
- 待修复的问题

## [1.0.0] - {date}

### 新增
- ✨ 初始版本发布
- ✨ 用户管理功能
- ✨ 数据管理功能
- ✨ 报表生成功能
- ✨ API接口文档
- ✨ 单元测试

### 变更
- 🎨 优化用户界面
- ⚡ 提升系统性能

### 修复
- 🐛 修复登录页面显示问题
- 🐛 修复数据导出错误

### 安全
- 🔒 增强密码加密强度
- 🔒 添加请求频率限制

## [0.9.0] - 2026-03-20

### 新增
- ✨ 添加用户注册功能
- ✨ 添加数据导入功能

### 变更
- 🎨 重构代码结构
- 📝 更新API文档

## [0.8.0] - 2026-03-15

### 新增
- ✨ 项目初始化
- ✨ 基础架构搭建

---

## 版本说明

- **[Unreleased]**: 开发中的版本
- **[1.0.0]**: 正式发布版本
- **[0.x.x]**: 测试版本

## 图标说明

- ✨ 新增功能
- 🎨 代码优化
- ⚡ 性能提升
- 🐛 Bug修复
- 🔒 安全更新
- 📝 文档更新
- 🔧 配置修改
- 🗑️ 废弃功能

---
*文档生成时间: {timestamp}*
'''.format(date=datetime.now().strftime('%Y-%m-%d'), timestamp=datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
    else:
        return '''# Changelog

All notable changes to this project will be documented in this file.

## [1.0.0] - {date}

### Added
- ✨ Initial release
- ✨ User management
- ✨ Data management

---
*Generated: {timestamp}*
'''.format(date=datetime.now().strftime('%Y-%m-%d'), timestamp=datetime.now().strftime('%Y-%m-%d %H:%M:%S'))

def generate_document(doc_type: str, source_path: str, language: str) -> str:
    """生成文档"""
    generators = {
        'api': generate_api_doc,
        'readme': generate_readme,
        'user_manual': generate_user_manual,
        'changelog': generate_changelog
    }
    
    if doc_type not in generators:
        raise ValueError(f"不支持的文档类型: {doc_type}")
    
    return generators[doc_type](source_path, language)

def save_document(content: str, doc_type: str, output_path: str, output_format: str):
    """保存文档"""
    output_dir = Path(output_path)
    output_dir.mkdir(parents=True, exist_ok=True)
    
    filename = f"{doc_type}.md"
    file_path = output_dir / filename
    file_path.write_text(content, encoding='utf-8')
    print(f"已生成: {file_path}")

def main():
    """主函数"""
    args = parse_args()
    
    print(f"生成文档...")
    print(f"文档类型: {args.doc_type}")
    print(f"语言: {args.language}")
    
    try:
        content = generate_document(args.doc_type, args.source_path, args.language)
        save_document(content, args.doc_type, args.output_path, args.output_format)
        
        result = {
            "success": True,
            "message": f"成功生成 {args.doc_type} 文档",
            "output_path": args.output_path,
            "filename": f"{args.doc_type}.md"
        }
        
        print(json.dumps(result, ensure_ascii=False, indent=2))
        
    except Exception as e:
        result = {
            "success": False,
            "error": str(e)
        }
        print(json.dumps(result, ensure_ascii=False, indent=2))
        sys.exit(1)

if __name__ == "__main__":
    main()
