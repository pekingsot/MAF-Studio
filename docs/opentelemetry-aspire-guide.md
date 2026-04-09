# OpenTelemetry和Aspire Dashboard使用指南

## 📋 概述

本文档说明如何使用OpenTelemetry和Aspire Dashboard来观察和调试MAF工作流。

---

## ✅ 已完成的配置

### 1. 安装的NuGet包

```xml
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.15.2" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.15.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.15.0" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.15.2" />
```

### 2. Program.cs配置

```csharp
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

// ...

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "MAFStudio", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddSource("Microsoft.Agents.*")  // MAF的追踪源
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }));
```

---

## 🚀 启动Aspire Dashboard

### 方式1：使用Docker（推荐）

```bash
# 启动Aspire Dashboard容器
docker run -d --name aspire-dashboard \
  -p 18888:18888 \
  -p 4317:18889 \
  mcr.microsoft.com/dotnet/aspire-dashboard:latest

# 访问Dashboard
# http://localhost:18888
```

### 方式2：使用Jaeger（替代方案）

```bash
# 启动Jaeger容器
docker run -d --name jaeger \
  -p 16686:16686 \
  -p 4317:4317 \
  -p 4318:4318 \
  jaegertracing/all-in-one:latest

# 访问Jaeger UI
# http://localhost:16686
```

### 方式3：使用Zipkin（替代方案）

```bash
# 启动Zipkin容器
docker run -d -p 9411:9411 openzipkin/zipkin

# 访问Zipkin UI
# http://localhost:9411
```

---

## 📊 查看追踪数据

### 1. 启动后端服务

```bash
cd /home/pekingost/projects/maf-studio/backend
dotnet run --project src/MAFStudio.Api
```

### 2. 执行工作流

执行任何Agent协作任务，OpenTelemetry会自动收集追踪数据。

### 3. 查看Dashboard

访问Aspire Dashboard：http://localhost:18888

**Trace（追踪）视图**：

```
┌─────────────────────────────────────────────────────┐
│  Distributed Tracing                                │
└─────────────────────────────────────────────────────┘

Service: MAFStudio
Trace ID: abc123-def456-ghi789
Duration: 12.5s

┌─ HTTP POST /api/collaborations/1/execute (2.3s)
│  └─ Agent: Manager (1.2s)
│     └─ LLM Call (0.8s)
│        └─ Prompt: "你是一个协调者..."
│        └─ Response: "请架构师发言"
│
├─ Agent: Architect (4.5s)
│  └─ Tool: DrawDiagram (1.5s)
│     └─ Input: {"type": "flowchart"}
│     └─ Output: "diagram.png"
│
├─ Agent: Coder (5.7s)
│  └─ Tool: WriteCode (2.8s)
│     └─ Input: {"language": "csharp"}
│     └─ Output: "Program.cs"
│
└─ Agent: Reviewer (3.1s)
   └─ Tool: CodeReview (1.8s)
      └─ Result: "Approved"
```

**Metrics（指标）视图**：

```
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ HTTP请求     │  │ Agent调用    │  │ 工具调用     │
│              │  │              │  │              │
│ 总数: 156    │  │ 总数: 45     │  │ 总数: 23     │
│ 成功: 152    │  │ 成功: 43     │  │ 成功: 22     │
│ 失败: 4      │  │ 失败: 2      │  │ 失败: 1      │
│ 平均耗时:    │  │ 平均耗时:    │  │ 平均耗时:    │
│ 245ms        │  │ 3.2s         │  │ 1.5s         │
└──────────────┘  └──────────────┘  └──────────────┘
```

---

## 🔍 追踪内容详解

### 1. HTTP请求追踪

自动追踪所有HTTP请求：
- 请求路径
- 请求方法
- 响应状态码
- 请求耗时

### 2. Agent调用追踪

追踪Agent的执行过程：
- Agent名称
- 提示词内容
- LLM调用
- 响应内容
- 执行耗时

### 3. 工具调用追踪

追踪工具的调用过程：
- 工具名称
- 输入参数
- 输出结果
- 执行耗时

---

## 🎯 使用场景

### 1. 性能分析

查看每个步骤的耗时，找出性能瓶颈：

```
Manager (2.3s)
  └─ Planning (1.2s)  ← 耗时较长，可能需要优化
     └─ LLM Call (0.8s)
```

### 2. 错误追踪

快速定位错误发生的位置：

```
Coder (5.7s)
  └─ Tool: WriteCode (2.8s)
     └─ Error: "文件权限不足"  ← 错误点
```

### 3. 调试分析

查看完整的执行流程和中间状态：

```
Prompt: "你是一个协调者，负责引导讨论..."
Response: "请架构师发言"
Reasoning: "当前讨论到架构设计，需要架构师介入"
```

---

## 📝 日志集成

OpenTelemetry会自动收集日志，可以在Dashboard中查看：

```
[14:23:45.123] INFO  Manager - Planning started
  └─ Prompt: "你是一个协调者..."
  
[14:23:45.856] INFO  Manager - Selected next agent
  └─ Agent: Architect
  └─ Reason: "需要设计架构方案"
  
[14:23:46.234] INFO  Architect - Processing request
  └─ Prompt: "你是一个架构师..."
  └─ Model: gpt-4o
  └─ Tokens: 1,234
```

---

## 🛠️ 高级配置

### 1. 自定义追踪源

```csharp
.AddSource("MyCustomSource")
```

### 2. 添加自定义属性

```csharp
using var activity = ActivitySource.StartActivity("CustomOperation");
activity?.SetTag("custom.attribute", "value");
```

### 3. 配置采样率

```csharp
.WithTracing(tracing => tracing
    .SetSampler(new TraceIdRatioBasedSampler(0.5))  // 50%采样率
    // ...
)
```

---

## 🚨 故障排查

### 问题1：Dashboard无法访问

**解决方案**：
```bash
# 检查容器是否运行
docker ps

# 查看容器日志
docker logs aspire-dashboard

# 重启容器
docker restart aspire-dashboard
```

### 问题2：追踪数据未显示

**解决方案**：
1. 检查OpenTelemetry配置是否正确
2. 确认OTLP Endpoint地址正确
3. 检查防火墙是否阻止4317端口

### 问题3：性能影响

**解决方案**：
```csharp
// 调整采样率
.SetSampler(new TraceIdRatioBasedSampler(0.1))  // 10%采样率
```

---

## 📚 参考资料

1. [OpenTelemetry官方文档](https://opentelemetry.io/)
2. [.NET Aspire官方文档](https://docs.microsoft.com/dotnet/aspire/)
3. [MAF官方文档](https://learn.microsoft.com/semantic-kernel/)

---

## 🎯 总结

通过OpenTelemetry和Aspire Dashboard，你可以：

1. ✅ 实时查看工作流执行过程
2. ✅ 分析性能瓶颈
3. ✅ 快速定位错误
4. ✅ 查看完整的日志和追踪数据
5. ✅ 可视化Agent调用链路

**这是调试和优化MAF工作流的强大工具！** 🎯
