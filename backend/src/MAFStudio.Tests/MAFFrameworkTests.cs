using System.Reflection;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace MAFStudio.Tests;

public class MafFrameworkTests
{
    [Fact]
    public void InspectGroupChatManager()
    {
        var type = typeof(GroupChatManager);
        
        Console.WriteLine($"========== GroupChatManager 类型信息 ==========");
        Console.WriteLine($"基类: {type.BaseType?.Name}");
        
        Console.WriteLine($"\n========== 属性 ==========");
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            Console.WriteLine($"  {prop.PropertyType.Name} {prop.Name}");
        }
        
        Console.WriteLine($"\n========== 方法 ==========");
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (!method.IsSpecialName)
            {
                Console.WriteLine($"  {method.ReturnType.Name} {method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
            }
        }
        
        Console.WriteLine($"\n========== 虚方法 ==========");
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (method.IsVirtual && !method.IsFinal && !method.IsSpecialName)
            {
                Console.WriteLine($"  virtual {method.ReturnType.Name} {method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
            }
        }
    }
    
    [Fact]
    public void InspectChatProtocolExecutor()
    {
        var assembly = typeof(AgentWorkflowBuilder).Assembly;
        var executorType = assembly.GetType("Microsoft.Agents.AI.Workflows.ChatProtocolExecutor");
        
        if (executorType != null)
        {
            Console.WriteLine($"========== ChatProtocolExecutor 类型信息 ==========");
            Console.WriteLine($"基类: {executorType.BaseType?.Name}");
            
            Console.WriteLine($"\n========== 字段 ==========");
            foreach (var field in executorType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                Console.WriteLine($"  {field.FieldType.Name} {field.Name}");
            }
            
            Console.WriteLine($"\n========== 方法 ==========");
            foreach (var method in executorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!method.IsSpecialName)
                {
                    Console.WriteLine($"  {method.ReturnType.Name} {method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
                }
            }
        }
        else
        {
            Console.WriteLine("未找到 ChatProtocolExecutor 类型");
        }
    }
    
    [Fact]
    public void InspectGroupChatHost()
    {
        var assembly = typeof(AgentWorkflowBuilder).Assembly;
        var hostType = assembly.GetType("Microsoft.Agents.AI.Workflows.Specialized.GroupChatHost");
        
        if (hostType != null)
        {
            Console.WriteLine($"========== GroupChatHost 类型信息 ==========");
            Console.WriteLine($"基类: {hostType.BaseType?.Name}");
            
            Console.WriteLine($"\n========== 字段 ==========");
            foreach (var field in hostType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                Console.WriteLine($"  {field.FieldType.Name} {field.Name}");
            }
            
            Console.WriteLine($"\n========== 方法 ==========");
            foreach (var method in hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!method.IsSpecialName)
                {
                    Console.WriteLine($"  {method.ReturnType.Name} {method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
                }
            }
        }
        else
        {
            Console.WriteLine("未找到 GroupChatHost 类型");
        }
    }
    
    [Fact]
    public void InspectAllGroupChatRelatedTypes()
    {
        var assembly = typeof(AgentWorkflowBuilder).Assembly;
        var types = assembly.GetTypes()
            .Where(t => t.FullName != null && (
                t.FullName.Contains("GroupChat") || 
                t.FullName.Contains("ChatProtocol") ||
                t.FullName.Contains("Executor")))
            .ToList();
        
        Console.WriteLine($"========== 所有相关类型 ==========");
        foreach (var t in types)
        {
            Console.WriteLine($"  {t.FullName}");
        }
    }
    
    [Fact]
    public void TestGroupChatManagerSelection()
    {
        Console.WriteLine("========== 测试 GroupChatManager 选择逻辑 ==========");
        
        var assembly = typeof(AgentWorkflowBuilder).Assembly;
        var hostType = assembly.GetType("Microsoft.Agents.AI.Workflows.Specialized.GroupChatHost");
        
        if (hostType != null)
        {
            var takeTurnMethods = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == "TakeTurnAsync")
                .ToList();
            
            Console.WriteLine($"\nTakeTurnAsync 方法数量: {takeTurnMethods.Count}");
            foreach (var method in takeTurnMethods)
            {
                Console.WriteLine($"  {method.ReturnType.Name} {method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
            }
            
            var managerField = hostType.GetField("_manager", BindingFlags.NonPublic | BindingFlags.Instance);
            if (managerField != null)
            {
                Console.WriteLine($"\n_manager 字段类型: {managerField.FieldType.Name}");
            }
        }
    }
    
    [Fact]
    public void InspectStatefulExecutor()
    {
        var assembly = typeof(AgentWorkflowBuilder).Assembly;
        var executorType = assembly.GetType("Microsoft.Agents.AI.Workflows.StatefulExecutor`1");
        
        if (executorType != null)
        {
            Console.WriteLine($"========== StatefulExecutor<T> 类型信息 ==========");
            Console.WriteLine($"基类: {executorType.BaseType?.Name}");
            
            Console.WriteLine($"\n========== 方法 ==========");
            foreach (var method in executorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!method.IsSpecialName)
                {
                    Console.WriteLine($"  {method.ReturnType.Name} {method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
                }
            }
        }
        else
        {
            Console.WriteLine("未找到 StatefulExecutor<T> 类型");
        }
    }
}
