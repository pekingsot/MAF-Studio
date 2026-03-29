#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
代码生成器 Skill
根据需求描述自动生成高质量代码
"""

import argparse
import json
import os
import sys
from pathlib import Path
from typing import Dict, Any

def parse_args():
    """解析命令行参数"""
    parser = argparse.ArgumentParser(description='代码生成器')
    parser.add_argument('--language', required=True, help='目标编程语言')
    parser.add_argument('--framework', required=True, help='目标框架')
    parser.add_argument('--requirement', required=True, help='需求描述')
    parser.add_argument('--output_path', default='./output', help='输出路径')
    return parser.parse_args()

def generate_python_fastapi_code(requirement: str) -> Dict[str, str]:
    """生成Python FastAPI代码"""
    return {
        'main.py': '''"""
FastAPI应用主文件
自动生成 - 需求: {requirement}
"""

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import List, Optional
import uvicorn

app = FastAPI(title="自动生成的API", version="1.0.0")

class Item(BaseModel):
    id: Optional[int] = None
    name: str
    description: Optional[str] = None

# 内存存储（实际项目中应使用数据库）
items_db = {{}}

@app.get("/")
async def root():
    return {{"message": "API运行正常"}}

@app.get("/items", response_model=List[Item])
async def get_items():
    return list(items_db.values())

@app.get("/items/{{item_id}}", response_model=Item)
async def get_item(item_id: int):
    if item_id not in items_db:
        raise HTTPException(status_code=404, detail="Item not found")
    return items_db[item_id]

@app.post("/items", response_model=Item)
async def create_item(item: Item):
    item.id = len(items_db) + 1
    items_db[item.id] = item
    return item

@app.put("/items/{{item_id}}", response_model=Item)
async def update_item(item_id: int, item: Item):
    if item_id not in items_db:
        raise HTTPException(status_code=404, detail="Item not found")
    item.id = item_id
    items_db[item_id] = item
    return item

@app.delete("/items/{{item_id}}")
async def delete_item(item_id: int):
    if item_id not in items_db:
        raise HTTPException(status_code=404, detail="Item not found")
    del items_db[item_id]
    return {{"message": "Item deleted"}}

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)
'''.format(requirement=requirement),
        
        'requirements.txt': '''fastapi==0.104.1
uvicorn[standard]==0.24.0
pydantic==2.5.0
''',
        
        'README.md': '''# 自动生成的FastAPI项目

## 需求
{requirement}

## 安装依赖
```bash
pip install -r requirements.txt
```

## 运行项目
```bash
python main.py
```

## API文档
启动后访问: http://localhost:8000/docs

## 功能
- GET / - 根路径
- GET /items - 获取所有项目
- GET /items/{{item_id}} - 获取单个项目
- POST /items - 创建项目
- PUT /items/{{item_id}} - 更新项目
- DELETE /items/{{item_id}} - 删除项目
'''.format(requirement=requirement),
        
        'test_main.py': '''"""
单元测试
"""
from fastapi.testclient import TestClient
from main import app

client = TestClient(app)

def test_root():
    response = client.get("/")
    assert response.status_code == 200

def test_create_item():
    response = client.post("/items", json={{"name": "Test Item"}})
    assert response.status_code == 200
    assert response.json()["name"] == "Test Item"

def test_get_items():
    response = client.get("/items")
    assert response.status_code == 200
'''
    }

def generate_javascript_express_code(requirement: str) -> Dict[str, str]:
    """生成JavaScript Express代码"""
    return {
        'index.js': '''/**
 * Express应用主文件
 * 自动生成 - 需求: {requirement}
 */

const express = require('express');
const app = express();
const PORT = process.env.PORT || 3000;

// 中间件
app.use(express.json());

// 内存存储
let items = [];
let nextId = 1;

// 路由
app.get('/', (req, res) => {{
    res.json({{ message: 'API运行正常' }});
}});

app.get('/items', (req, res) => {{
    res.json(items);
}});

app.get('/items/:id', (req, res) => {{
    const item = items.find(i => i.id === parseInt(req.params.id));
    if (!item) return res.status(404).json({{ error: 'Item not found' }});
    res.json(item);
}});

app.post('/items', (req, res) => {{
    const item = {{
        id: nextId++,
        name: req.body.name,
        description: req.body.description
    }};
    items.push(item);
    res.status(201).json(item);
}});

app.put('/items/:id', (req, res) => {{
    const index = items.findIndex(i => i.id === parseInt(req.params.id));
    if (index === -1) return res.status(404).json({{ error: 'Item not found' }});
    
    items[index] = {{
        ...items[index],
        name: req.body.name,
        description: req.body.description
    }};
    res.json(items[index]);
}});

app.delete('/items/:id', (req, res) => {{
    const index = items.findIndex(i => i.id === parseInt(req.params.id));
    if (index === -1) return res.status(404).json({{ error: 'Item not found' }});
    
    items.splice(index, 1);
    res.json({{ message: 'Item deleted' }});
}});

app.listen(PORT, () => {{
    console.log(`Server running on port ${{PORT}}`);
}});
'''.format(requirement=requirement),
        
        'package.json': '''{{
  "name": "generated-express-api",
  "version": "1.0.0",
  "description": "自动生成的Express API",
  "main": "index.js",
  "scripts": {{
    "start": "node index.js",
    "dev": "nodemon index.js",
    "test": "jest"
  }},
  "dependencies": {{
    "express": "^4.18.2"
  }},
  "devDependencies": {{
    "nodemon": "^3.0.1",
    "jest": "^29.7.0"
  }}
}}''',
        
        'README.md': '''# 自动生成的Express项目

## 需求
{requirement}

## 安装依赖
```bash
npm install
```

## 运行项目
```bash
npm start
```

## API端点
- GET / - 根路径
- GET /items - 获取所有项目
- GET /items/:id - 获取单个项目
- POST /items - 创建项目
- PUT /items/:id - 更新项目
- DELETE /items/:id - 删除项目
'''.format(requirement=requirement)
    }

def generate_code(language: str, framework: str, requirement: str) -> Dict[str, str]:
    """根据语言和框架生成代码"""
    generators = {{
        'python': {{
            'fastapi': generate_python_fastapi_code,
            'flask': generate_python_fastapi_code,  # 简化处理
        }},
        'javascript': {{
            'express': generate_javascript_express_code,
        }}
    }}
    
    if language not in generators:
        raise ValueError(f"不支持的语言: {language}")
    
    if framework not in generators[language]:
        raise ValueError(f"不支持的框架: {framework}")
    
    return generators[language][framework](requirement)

def save_files(files: Dict[str, str], output_path: str):
    """保存生成的文件"""
    output_dir = Path(output_path)
    output_dir.mkdir(parents=True, exist_ok=True)
    
    for filename, content in files.items():
        file_path = output_dir / filename
        file_path.parent.mkdir(parents=True, exist_ok=True)
        file_path.write_text(content, encoding='utf-8')
        print(f"已生成: {file_path}")

def main():
    """主函数"""
    args = parse_args()
    
    print(f"生成代码...")
    print(f"语言: {args.language}")
    print(f"框架: {args.framework}")
    print(f"需求: {args.requirement}")
    
    try:
        files = generate_code(args.language, args.framework, args.requirement)
        save_files(files, args.output_path)
        
        result = {{
            "success": True,
            "message": f"成功生成 {len(files)} 个文件",
            "output_path": args.output_path,
            "files": list(files.keys())
        }}
        
        print(json.dumps(result, ensure_ascii=False, indent=2))
        
    except Exception as e:
        result = {{
            "success": False,
            "error": str(e)
        }}
        print(json.dumps(result, ensure_ascii=False, indent=2))
        sys.exit(1)

if __name__ == "__main__":
    main()
